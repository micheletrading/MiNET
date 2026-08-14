#region LICENSE

// The contents of this file are subject to the Common Public Attribution
// License Version 1.0. (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// https://github.com/NiclasOlofsson/MiNET/blob/master/LICENSE.
// The License is based on the Mozilla Public License Version 1.1, but Sections 14
// and 15 have been added to cover use of software over a computer network and
// provide for limited attribution for the Original Developer. In addition, Exhibit A has
// been modified to be consistent with Exhibit B.
//
// Software distributed under the License is distributed on an "AS IS" basis,
// WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
// the specific language governing rights and limitations under the License.
//
// The Original Code is MiNET.
//
// The Original Developer is the Initial Developer.  The Initial Developer of
// the Original Code is Niclas Olofsson.
//
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2026 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using log4net;

namespace MiNET.Net
{
	/// <summary>
	///     Startup labeling of handler methods: walks the IL of every <c>HandleMcpe*</c> method in
	///     the loaded assembly set and labels each VERIFIED (no path reaches a blocking primitive:
	///     locks, waits, sleeps, sync I/O) or UNVERIFIED (a path does, or contains a call the walk
	///     cannot see through). The label is a static, global fact about the method; the dispatch
	///     code uses it to call a verified handler directly instead of paying a queue hop, and
	///     routes everything unverified through the per-session dispatch queue exactly as before.
	///     <para>
	///     Closed world: virtual and interface calls fan out to every override found in the scanned
	///     assemblies, so the verdict is sound for the code that is actually loaded and says nothing
	///     about assemblies loaded later. Path-insensitive: a run-once initializer behind a guard
	///     still reads as blocking; such findings are reviewed and, if ruled clean, the ruling
	///     belongs in the code (restructure) rather than in an override list here, so a label is
	///     always backed by what the IL provably says.
	///     </para>
	///     <para>
	///     Deliberately dependency-free: built on System.Reflection.Metadata (in-box) rather than a
	///     third-party IL library, since this ships inside core MiNET. The trade is hand-rolled IL
	///     operand decoding and name-based cross-assembly resolution (method identity matched by
	///     declaring type full name, method name and parameter count, which is exact enough for this
	///     codebase's handler surface).
	///     </para>
	/// </summary>
	public static class HandlerVerification
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HandlerVerification));

		/// <summary>
		///     The labels the startup scan produced, keyed "Namespace.Type::Method"; empty until
		///     <see cref="ScanAndReport" /> runs. Read by the dispatch path; written once at startup.
		/// </summary>
		public static IReadOnlyDictionary<string, MethodLabel> Labels { get; private set; } = new Dictionary<string, MethodLabel>();

		/// <summary>Whether the method implementing <paramref name="methodName" /> on (or inherited by) <paramref name="declaringType" /> carries the verified label. Anything unknown is unverified.</summary>
		public static bool IsVerified(Type declaringType, string methodName)
		{
			MethodInfo method = declaringType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (method?.DeclaringType == null) return false;

			return Labels.TryGetValue($"{method.DeclaringType.FullName}::{methodName}", out MethodLabel label) && label.Verified;
		}

		/// <summary>Method calls that ARE the finding: reaching one of these marks the caller unverified.</summary>
		private static readonly string[] BlockingPrefixes =
		{
			"System.Threading.Monitor::Enter",
			"System.Threading.Monitor::Wait",
			"System.Threading.WaitHandle::WaitOne",
			"System.Threading.WaitHandle::WaitAny",
			"System.Threading.WaitHandle::WaitAll",
			"System.Threading.SemaphoreSlim::Wait",
			"System.Threading.Tasks.Task::Wait",
			"System.Threading.Tasks.Task`1::get_Result",
			"System.Runtime.CompilerServices.TaskAwaiter::GetResult",
			"System.Runtime.CompilerServices.TaskAwaiter`1::GetResult",
			"System.Threading.Thread::Sleep",
			"System.Threading.Thread::Join",
			"System.Threading.ReaderWriterLockSlim::Enter",
			"System.Threading.SpinWait::SpinUntil",
			"System.Threading.ManualResetEventSlim::Wait",
			"System.Threading.CountdownEvent::Wait",
			"System.IO.File::",
			"System.IO.StreamWriter::",
			"System.IO.StreamReader::",
			"System.Console::Write",
			"System.Console::Read",
		};

		// Deliberately NOT on the list above: INetworkHandler::SendPacket. Sending from a handler
		// looks like the violation and is not the cost - it is a channel write, an interlocked and a
		// counter, and every expensive thing (wrapper build, compression, fragmentation, syscalls)
		// happens on the far side of that queue on the send lane. Disqualifying on it once cost
		// PlayerAuthInput its inline path and caught McpeSubChunkRequestPacket only by coincidence.
		// What actually disqualifies a slow handler is measured duration; see EngineMetrics's
		// demotion, which this scan feeds rather than replaces.

		// Namespaces treated as leaves: never traversed into, assumed non-blocking unless a method
		// there is itself on the blocking list. Keeps the walk inside our own code plus the edges
		// that matter; a BCL method that blocks and is not listed is a gap in the list, not a
		// license for the walk to wander the whole framework.
		private static readonly string[] LeafNamespacePrefixes =
		{
			"System.", "Microsoft.", "log4net", "Newtonsoft", "fNbt", "SixLabors",
		};

		public sealed class MethodLabel
		{
			public string Method { get; init; }
			public bool Verified { get; init; }
			public string Reason { get; init; }
		}

		/// <summary>
		///     Scans <paramref name="assemblies" /> (their on-disk images) and returns a label per
		///     <c>HandleMcpe*</c> method found, keyed "Namespace.Type::Method". Assemblies without a
		///     file location (dynamic) are skipped; their handlers simply stay unverified.
		/// </summary>
		public static Dictionary<string, MethodLabel> ScanHandlers(IEnumerable<Assembly> assemblies)
		{
			var scanner = new Scanner();
			foreach (Assembly assembly in assemblies.Distinct())
			{
				try
				{
					if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location)) scanner.Load(assembly.Location);
				}
				catch (Exception e)
				{
					Log.Warn($"Handler verification could not read {assembly.FullName}: {e.Message}");
				}
			}

			return scanner.ScanHandlerMethods();
		}

		/// <summary>
		///     The startup entry point: scans the given assemblies, logs the scoreboard, and warns,
		///     one line each with its blocking chain, for every unverified handler declared on
		///     <paramref name="activeHandlerTypes" /> or their base types. Those warnings are the
		///     cleanup worklist: every one is a handler that keeps paying the dispatch-queue hop.
		/// </summary>
		public static Dictionary<string, MethodLabel> ScanAndReport(IEnumerable<Assembly> assemblies, params Type[] activeHandlerTypes)
		{
			Dictionary<string, MethodLabel> labels = ScanHandlers(assemblies);
			Labels = labels;

			int verified = labels.Values.Count(l => l.Verified);
			Log.Info($"Handler verification: {verified} of {labels.Count} handler methods verified lock-free; unverified handlers dispatch through the queue.");

			var activeTypeNames = new HashSet<string>();
			foreach (Type type in activeHandlerTypes)
			{
				for (Type t = type; t != null && t != typeof(object); t = t.BaseType) activeTypeNames.Add(t.FullName);
			}

			foreach (MethodLabel label in labels.Values.OrderBy(l => l.Method))
			{
				if (label.Verified) continue;

				int separator = label.Method.IndexOf("::", StringComparison.Ordinal);
				string declaringType = separator > 0 ? label.Method.Substring(0, separator) : label.Method;
				if (!activeTypeNames.Contains(declaringType)) continue;

				Log.Warn($"Unverified handler {label.Method}: {label.Reason}");
			}

			return labels;
		}

		/// <summary>
		///     The dependency-free closed-world walker. One instance per scan; not thread safe and
		///     not reused.
		/// </summary>
		private sealed class Scanner : IDisposable
		{
			private readonly List<PEReader> _peReaders = new List<PEReader>();

			// Per loaded module: its reader plus name-keyed indexes built once up front.
			private sealed class Module
			{
				public MetadataReader Reader;
				public PEReader Pe;
				public Dictionary<string, TypeDefinitionHandle> TypesByFullName;
			}

			private readonly List<Module> _modules = new List<Module>();
			private readonly Dictionary<string, Module> _modulesByAssemblyName = new Dictionary<string, Module>(StringComparer.OrdinalIgnoreCase);

			// Virtual slot -> overrides, keyed "DeclaringTypeFullName::Name/ParamCount".
			private readonly Dictionary<string, List<(Module Module, MethodDefinitionHandle Method)>> _overridesBySlot = new Dictionary<string, List<(Module, MethodDefinitionHandle)>>();

			private readonly Dictionary<string, (bool Verified, string Reason)> _memo = new Dictionary<string, (bool, string)>();
			private readonly HashSet<string> _inProgress = new HashSet<string>();

			public void Load(string path)
			{
				var pe = new PEReader(File.OpenRead(path));
				_peReaders.Add(pe);
				if (!pe.HasMetadata) return;

				MetadataReader reader = pe.GetMetadataReader();
				var module = new Module {Reader = reader, Pe = pe, TypesByFullName = new Dictionary<string, TypeDefinitionHandle>()};

				foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
				{
					TypeDefinition type = reader.GetTypeDefinition(handle);
					module.TypesByFullName[FullName(reader, type)] = handle;
				}

				_modules.Add(module);
				_modulesByAssemblyName[reader.GetString(reader.GetAssemblyDefinition().Name)] = module;
			}

			public Dictionary<string, MethodLabel> ScanHandlerMethods()
			{
				BuildOverrideMap();

				var labels = new Dictionary<string, MethodLabel>();
				foreach (Module module in _modules)
				{
					foreach (TypeDefinitionHandle typeHandle in module.Reader.TypeDefinitions)
					{
						TypeDefinition type = module.Reader.GetTypeDefinition(typeHandle);
						foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
						{
							MethodDefinition method = module.Reader.GetMethodDefinition(methodHandle);
							string name = module.Reader.GetString(method.Name);
							if (!name.StartsWith("HandleMcpe", StringComparison.Ordinal)) continue;
							if (method.RelativeVirtualAddress == 0) continue;

							(bool verified, string reason) = Walk(module, methodHandle, 0);
							string key = MethodKey(module, methodHandle);
							labels[key] = new MethodLabel {Method = key, Verified = verified, Reason = reason};
						}
					}
				}

				return labels;
			}

			private void BuildOverrideMap()
			{
				foreach (Module module in _modules)
				{
					foreach (TypeDefinitionHandle typeHandle in module.Reader.TypeDefinitions)
					{
						TypeDefinition type = module.Reader.GetTypeDefinition(typeHandle);
						foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
						{
							MethodDefinition method = module.Reader.GetMethodDefinition(methodHandle);
							if ((method.Attributes & MethodAttributes.Virtual) == 0) continue;
							if (method.RelativeVirtualAddress == 0) continue;

							string name = module.Reader.GetString(method.Name);
							int paramCount = ParameterCount(module.Reader, method);

							// Register under its own slot and every base type declaring the same
							// name/arity up the chain, across assemblies.
							(Module m, TypeDefinitionHandle t) current = (module, typeHandle);
							while (true)
							{
								string slot = $"{FullName(current.m.Reader, current.m.Reader.GetTypeDefinition(current.t))}::{name}/{paramCount}";
								if (!_overridesBySlot.TryGetValue(slot, out List<(Module, MethodDefinitionHandle)> list)) _overridesBySlot[slot] = list = new List<(Module, MethodDefinitionHandle)>();
								if (!list.Contains((module, methodHandle))) list.Add((module, methodHandle));

								(Module m, TypeDefinitionHandle t)? baseType = ResolveBaseType(current.m, current.t);
								if (baseType == null) break;
								current = baseType.Value;
							}
						}
					}
				}
			}

			private (bool Verified, string Reason) Walk(Module module, MethodDefinitionHandle methodHandle, int depth)
			{
				string key = MethodKey(module, methodHandle);
				if (_memo.TryGetValue(key, out (bool Verified, string Reason) cached)) return cached;
				if (_inProgress.Contains(key)) return (true, null); // cycle: the other path decides
				if (depth > 40) return (false, "call depth limit");

				MethodDefinition method = module.Reader.GetMethodDefinition(methodHandle);
				if (method.RelativeVirtualAddress == 0) return (true, null);

				_inProgress.Add(key);
				(bool Verified, string Reason) result = (true, null);

				MethodBodyBlock body = module.Pe.GetMethodBody(method.RelativeVirtualAddress);
				byte[] il = body.GetILBytes() ?? Array.Empty<byte>();

				foreach ((int token, bool isVirtualCall) in EnumerateCallTokens(il))
				{
					var handle = MetadataTokens.EntityHandle(token);
					(string typeName, string methodName, int paramCount, Module targetModule, MethodDefinitionHandle targetHandle) = ResolveCallTarget(module, handle);
					if (typeName == null)
					{
						result = (false, $"unresolvable call in {ShortKey(key)}");
						continue;
					}

					string probe = $"{typeName}::{methodName}";
					if (BlockingPrefixes.Any(b => probe.StartsWith(b, StringComparison.Ordinal)))
					{
						result = (false, $"{probe} @ {ShortKey(key)}");
						break;
					}

					if (LeafNamespacePrefixes.Any(ns => typeName.StartsWith(ns, StringComparison.Ordinal))) continue;

					// Resolve to definitions to traverse: the direct target plus, for callvirt,
					// every override the closed world holds. A plain call to a virtual slot is a
					// base call (base.Method()) and runs exactly its direct target; fanning it out
					// would chain every override into one walk and blow the depth limit.
					var targets = new List<(Module, MethodDefinitionHandle)>();
					if (targetModule != null) targets.Add((targetModule, targetHandle));
					if (isVirtualCall && _overridesBySlot.TryGetValue($"{typeName}::{methodName}/{paramCount}", out List<(Module, MethodDefinitionHandle)> overrides))
					{
						foreach ((Module, MethodDefinitionHandle) o in overrides)
						{
							if (!targets.Contains(o)) targets.Add(o);
						}
					}

					if (targets.Count == 0)
					{
						// A call into a loaded-but-unindexed target (or a delegate/reflection edge).
						result = (false, $"opaque call to {probe} @ {ShortKey(key)}");
						continue;
					}

					foreach ((Module targetMod, MethodDefinitionHandle target) in targets)
					{
						(bool subVerified, string subReason) = Walk(targetMod, target, depth + 1);
						if (!subVerified)
						{
							result = (false, $"{subReason} via {ShortKey(key)}");
							goto done;
						}
					}
				}

				done:
				_inProgress.Remove(key);
				_memo[key] = result;
				return result;
			}

			/// <summary>Walks the IL byte stream, yielding the metadata token of every call, callvirt and newobj operand, flagged with whether the dispatch is virtual. Operand sizes for every other opcode are skipped by table so token offsets stay exact.</summary>
			private static IEnumerable<(int Token, bool IsVirtualCall)> EnumerateCallTokens(byte[] il)
			{
				int i = 0;
				while (i < il.Length)
				{
					byte op = il[i++];

					if (op == 0xFE)
					{
						if (i >= il.Length) yield break;
						byte op2 = il[i++];
						// Two-byte opcodes: operand sizes per ECMA-335. Most are none; the ones
						// with operands: ldftn/ldvirtftn/initobj/constrained/sizeof (token, 4),
						// unaligned (1), no/tail/volatile/readonly (0), arglist/... (0).
						switch (op2)
						{
							case 0x06: // ldftn
							case 0x07: // ldvirtftn
							case 0x15: // initobj
							case 0x16: // constrained.
							case 0x1C: // sizeof
								i += 4;
								break;
							case 0x12: // unaligned.
								i += 1;
								break;
							case 0x09: // ldarg
							case 0x0A: // ldarga
							case 0x0B: // starg
							case 0x0C: // ldloc
							case 0x0D: // ldloca
							case 0x0E: // stloc
								i += 2;
								break;
						}
						continue;
					}

					// call = 0x28, callvirt = 0x6F, newobj = 0x73: yield the 4-byte token.
					if (op == 0x28 || op == 0x6F || op == 0x73)
					{
						if (i + 4 > il.Length) yield break;
						yield return (BitConverter.ToInt32(il, i), op == 0x6F);
						i += 4;
						continue;
					}

					i += OperandSize(op, il, ref i);
				}
			}

			/// <summary>Operand byte count for single-byte opcodes; switch (0x45) consumes its own jump table via <paramref name="i" />.</summary>
			private static int OperandSize(byte op, byte[] il, ref int i)
			{
				switch (op)
				{
					// 4-byte operands: branches (long), ldc.i4, ldc.r4, tokens, calli, jmp, etc.
					case 0x22: // ldc.r4
					case 0x20: // ldc.i4
					case 0x27: // jmp
					case 0x29: // calli
					case 0x38: case 0x39: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3E: case 0x3F: case 0x40: case 0x41: case 0x42: case 0x43: case 0x44: // long branches
					case 0x70: // cpobj
					case 0x71: // ldobj
					case 0x72: // ldstr
					case 0x74: // castclass
					case 0x75: // isinst
					case 0x79: // unbox
					case 0x7B: case 0x7C: case 0x7D: case 0x7E: case 0x7F: case 0x80: // field ops
					case 0x81: // stobj
					case 0x8C: // box
					case 0x8D: // newarr
					case 0x8F: // ldelema
					case 0xA3: case 0xA4: case 0xA5: // ldelem/stelem/unbox.any
					case 0xC2: // refanyval
					case 0xC6: // mkrefany
					case 0xD0: // ldtoken
						return 4;

					case 0x21: // ldc.i8
					case 0x23: // ldc.r8
						return 8;

					// 1-byte operands: short branches, ldc.i4.s, short vars.
					case 0x0E: case 0x0F: case 0x10: case 0x11: case 0x12: case 0x13: // short arg/loc
					case 0x1F: // ldc.i4.s
					case 0x2B: case 0x2C: case 0x2D: case 0x2E: case 0x2F: case 0x30: case 0x31: case 0x32: case 0x33: case 0x34: case 0x35: case 0x36: case 0x37: // short branches
					case 0xDE: // leave.s
						return 1;

					case 0xDD: // leave
						return 4;

					case 0x45: // switch: uint32 count then count 4-byte targets
					{
						if (i + 4 > il.Length) return il.Length - i;
						int count = BitConverter.ToInt32(il, i);
						return 4 + count * 4;
					}

					default:
						return 0;
				}
			}

			private (string TypeName, string MethodName, int ParamCount, Module Module, MethodDefinitionHandle Handle) ResolveCallTarget(Module module, EntityHandle handle)
			{
				switch (handle.Kind)
				{
					case HandleKind.MethodDefinition:
					{
						var methodHandle = (MethodDefinitionHandle) handle;
						MethodDefinition method = module.Reader.GetMethodDefinition(methodHandle);
						TypeDefinition type = module.Reader.GetTypeDefinition(method.GetDeclaringType());
						return (FullName(module.Reader, type), module.Reader.GetString(method.Name), ParameterCount(module.Reader, method), module, methodHandle);
					}

					case HandleKind.MemberReference:
					{
						MemberReference member = module.Reader.GetMemberReference((MemberReferenceHandle) handle);
						string methodName = module.Reader.GetString(member.Name);
						string typeName = ResolveTypeName(module, member.Parent);
						if (typeName == null) return (null, null, 0, null, default);

						int paramCount = SignatureParameterCount(module.Reader, member.Signature);

						// A definition in a loaded module lets the walk continue into the body.
						if (TryFindMethod(typeName, methodName, paramCount, out Module targetModule, out MethodDefinitionHandle targetHandle))
						{
							return (typeName, methodName, paramCount, targetModule, targetHandle);
						}

						return (typeName, methodName, paramCount, null, default);
					}

					case HandleKind.MethodSpecification:
					{
						MethodSpecification spec = module.Reader.GetMethodSpecification((MethodSpecificationHandle) handle);
						return ResolveCallTarget(module, spec.Method);
					}

					default:
						return (null, null, 0, null, default);
				}
			}

			private bool TryFindMethod(string typeName, string methodName, int paramCount, out Module module, out MethodDefinitionHandle handle)
			{
				foreach (Module m in _modules)
				{
					if (!m.TypesByFullName.TryGetValue(typeName, out TypeDefinitionHandle typeHandle)) continue;

					TypeDefinition type = m.Reader.GetTypeDefinition(typeHandle);
					foreach (MethodDefinitionHandle candidate in type.GetMethods())
					{
						MethodDefinition method = m.Reader.GetMethodDefinition(candidate);
						if (m.Reader.GetString(method.Name) != methodName) continue;
						if (ParameterCount(m.Reader, method) != paramCount) continue;

						module = m;
						handle = candidate;
						return true;
					}
				}

				module = null;
				handle = default;
				return false;
			}

			private (Module, TypeDefinitionHandle)? ResolveBaseType(Module module, TypeDefinitionHandle typeHandle)
			{
				TypeDefinition type = module.Reader.GetTypeDefinition(typeHandle);
				EntityHandle baseHandle = type.BaseType;
				if (baseHandle.IsNil) return null;

				string baseName = ResolveTypeName(module, baseHandle);
				if (baseName == null || baseName == "System.Object") return null;

				foreach (Module m in _modules)
				{
					if (m.TypesByFullName.TryGetValue(baseName, out TypeDefinitionHandle baseType)) return (m, baseType);
				}

				return null;
			}

			private string ResolveTypeName(Module module, EntityHandle handle)
			{
				switch (handle.Kind)
				{
					case HandleKind.TypeDefinition:
						return FullName(module.Reader, module.Reader.GetTypeDefinition((TypeDefinitionHandle) handle));

					case HandleKind.TypeReference:
					{
						TypeReference reference = module.Reader.GetTypeReference((TypeReferenceHandle) handle);
						string ns = module.Reader.GetString(reference.Namespace);
						string name = module.Reader.GetString(reference.Name);

						// A nested type's reference carries its declaring type as the resolution
						// scope and no namespace of its own; without the prefix, List`1+Enumerator
						// would read as bare "Enumerator" and evade the leaf-namespace check.
						if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
						{
							string declaring = ResolveTypeName(module, reference.ResolutionScope);
							return declaring == null ? null : $"{declaring}+{name}";
						}

						return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
					}

					case HandleKind.TypeSpecification:
					{
						// A constructed generic (Dictionary<string, X>, our own generic types): the
						// open type's full name is what the leaf check and the definition lookup key
						// on, so decode the signature down to it. Receivers that are themselves
						// generic parameters stay unresolvable, which keeps the verdict conservative.
						TypeSpecification spec = module.Reader.GetTypeSpecification((TypeSpecificationHandle) handle);
						BlobReader blob = module.Reader.GetBlobReader(spec.Signature);
						return ResolveTypeSpecName(module, ref blob);
					}

					default:
						return null;
				}
			}

			/// <summary>The open-type name behind a TypeSpec signature: GENERICINST resolves to its generic type definition, arrays to System.Array (their accessors are pure), everything else (generic parameters, pointers, byrefs) to null.</summary>
			private string ResolveTypeSpecName(Module module, ref BlobReader blob)
			{
				SignatureTypeCode code = blob.ReadSignatureTypeCode();
				switch (code)
				{
					case SignatureTypeCode.GenericTypeInstance:
					{
						// GENERICINST (CLASS | VALUETYPE) TypeDefOrRef GenArgCount Type*; the reader
						// folds CLASS/VALUETYPE into TypeHandle and the args are irrelevant here.
						SignatureTypeCode kind = blob.ReadSignatureTypeCode();
						return kind == SignatureTypeCode.TypeHandle ? ResolveTypeName(module, blob.ReadTypeHandle()) : null;
					}

					case SignatureTypeCode.TypeHandle:
						return ResolveTypeName(module, blob.ReadTypeHandle());

					case SignatureTypeCode.SZArray:
					case SignatureTypeCode.Array:
						return "System.Array";

					default:
						return null;
				}
			}

			private static string FullName(MetadataReader reader, TypeDefinition type)
			{
				string ns = reader.GetString(type.Namespace);
				string name = reader.GetString(type.Name);

				if (type.IsNested)
				{
					TypeDefinition declaring = reader.GetTypeDefinition(type.GetDeclaringType());
					return $"{FullName(reader, declaring)}+{name}";
				}

				return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
			}

			private static int ParameterCount(MetadataReader reader, MethodDefinition method)
			{
				return SignatureParameterCount(reader, method.Signature);
			}

			private static int SignatureParameterCount(MetadataReader reader, BlobHandle signature)
			{
				BlobReader blob = reader.GetBlobReader(signature);
				var header = blob.ReadSignatureHeader();
				if (header.Kind != SignatureKind.Method && header.Kind != SignatureKind.Property) return 0;
				if (header.IsGeneric) blob.ReadCompressedInteger(); // generic parameter count
				return blob.ReadCompressedInteger();
			}

			private string MethodKey(Module module, MethodDefinitionHandle handle)
			{
				MethodDefinition method = module.Reader.GetMethodDefinition(handle);
				TypeDefinition type = module.Reader.GetTypeDefinition(method.GetDeclaringType());
				return $"{FullName(module.Reader, type)}::{module.Reader.GetString(method.Name)}";
			}

			private static string ShortKey(string key)
			{
				int lastDot = key.LastIndexOf('.', Math.Max(0, key.IndexOf("::", StringComparison.Ordinal)));
				return lastDot > 0 ? key.Substring(lastDot + 1) : key;
			}

			public void Dispose()
			{
				foreach (PEReader pe in _peReaders) pe.Dispose();
			}
		}
	}
}
