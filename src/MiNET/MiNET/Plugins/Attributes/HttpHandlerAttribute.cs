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
// All portions of the code written by Niclas Olofsson are Copyright (c) 2014-2018 Niclas Olofsson.
// All Rights Reserved.

#endregion

using System;

namespace MiNET.Plugins.Attributes
{
	/// <summary>
	///     Routes an HTTP request on the server port to a plugin method. The method takes an
	///     <see cref="HttpRequest" /> and returns an <see cref="HttpResponse" />.
	///     <para>
	///         The path is a template, where <c>{name}</c> captures one segment into
	///         <see cref="HttpRequest.RouteValues" />: <c>/plot/{id}/owner</c> matches
	///         <c>/plot/17/owner</c> and not <c>/plot/17</c>.
	///     </para>
	///     <para>
	///         Two things a handler owns that the server does not. It runs on the connection's own
	///         task, NOT the level tick, so touching world state from it races the tick like any
	///         other off-tick caller. And the route is served on the same port a client dials, which
	///         is public the moment that port is: nothing here authenticates, so a handler that does
	///         anything privileged checks its own credential.
	///     </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
	public class HttpHandlerAttribute : Attribute
	{
		public HttpHandlerAttribute(string method, string path)
		{
			Method = method;
			Path = path;
		}

		/// <summary>The HTTP method, matched case insensitively.</summary>
		public string Method { get; set; }

		/// <summary>Path template, leading slash included.</summary>
		public string Path { get; set; }
	}
}
