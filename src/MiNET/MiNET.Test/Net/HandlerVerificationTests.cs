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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET;
using MiNET.Net;

namespace MiNET.Test.Net
{
	/// <summary>
	///     The startup handler-labeling: methods that provably reach a blocking primitive (or a
	///     call the walk cannot see through) must come back UNVERIFIED, and only provably clean
	///     ones VERIFIED - the label is what licenses skipping the dispatch queue, so a false
	///     VERIFIED is the dangerous direction and every synthetic case below leans that way.
	/// </summary>
	[TestClass]
	public class HandlerVerificationTests
	{
		// Synthetic handler surface, compiled into this test assembly and scanned for real.
		private class SyntheticHandlers
		{
			private readonly object _sync = new object();
			private int _state;

			public void HandleMcpeSyntheticClean(int value)
			{
				_state = value + _state;
			}

			public void HandleMcpeSyntheticLocking(int value)
			{
				lock (_sync)
				{
					_state = value;
				}
			}

			public void HandleMcpeSyntheticSleeping(int value)
			{
				Thread.Sleep(value);
			}

			public void HandleMcpeSyntheticFileIo(string path)
			{
				_state = File.Exists(path) ? 1 : 0;
			}

			public void HandleMcpeSyntheticIndirect(int value)
			{
				Helper(value);
			}

			private void Helper(int value)
			{
				lock (_sync)
				{
					_state = value;
				}
			}
		}

		private static Dictionary<string, HandlerVerification.MethodLabel> ScanSelf()
		{
			return HandlerVerification.ScanHandlers(new[] {typeof(HandlerVerificationTests).Assembly, typeof(Player).Assembly});
		}

		private static HandlerVerification.MethodLabel Label(Dictionary<string, HandlerVerification.MethodLabel> labels, string method)
		{
			HandlerVerification.MethodLabel label = labels.Values.FirstOrDefault(l => l.Method.EndsWith(method));
			Assert.IsNotNull(label, $"the scan never labeled {method}");
			return label;
		}

		[TestMethod]
		public void SyntheticHandlers_LabelExactlyAsTheirIlSays()
		{
			Dictionary<string, HandlerVerification.MethodLabel> labels = ScanSelf();

			Assert.IsTrue(Label(labels, "::HandleMcpeSyntheticClean").Verified, "a field assignment must verify");
			Assert.IsFalse(Label(labels, "::HandleMcpeSyntheticLocking").Verified, "a lock statement must not verify");
			Assert.IsFalse(Label(labels, "::HandleMcpeSyntheticSleeping").Verified, "a sleep must not verify");
			Assert.IsFalse(Label(labels, "::HandleMcpeSyntheticFileIo").Verified, "file I/O must not verify");
			Assert.IsFalse(Label(labels, "::HandleMcpeSyntheticIndirect").Verified, "a lock one call deep must not verify");
		}

		[TestMethod]
		public void CoreSurface_ScansWhole_AndTheHottestHandlerDispatchesDirectly()
		{
			Dictionary<string, HandlerVerification.MethodLabel> labels = ScanSelf();

			// The whole handler surface labels, nothing throws, and the count is the full core set.
			int core = labels.Keys.Count(k => k.StartsWith("MiNET."));
			Assert.IsTrue(core > 100, $"expected the full core handler surface, labeled only {core}");

			// PlayerAuthInput is the hottest handler on the server (one per client tick, per player)
			// and the only one whose label is worth a test of its own: it carries the direct-dispatch
			// path, so anything that reintroduces a lock on its movement tail - or on the pool-threaded
			// item-use and crafting branches folded into it - silently costs every player a queue hop
			// per tick. This fails the moment that happens; the reason string names the new blocker.
			HandlerVerification.MethodLabel authInput = Label(labels, "Player::HandleMcpePlayerAuthInput");
			Assert.IsTrue(authInput.Verified, $"PlayerAuthInput must stay lock-free: {authInput.Reason}");

			// MovePlayer still reaches Level's entity locks (a move can kill a painting). Unverified is
			// the correct label today, and it is legacy inbound on 2168 besides.
			Assert.IsFalse(Label(labels, "Player::HandleMcpeMovePlayer").Verified);
		}
	}
}
