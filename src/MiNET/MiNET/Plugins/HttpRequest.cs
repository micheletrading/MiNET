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
using System.Collections.Generic;
using System.Net;

namespace MiNET.Plugins
{
	/// <summary>
	///     One HTTP request as a plugin handler sees it. Parsed already; there is no stream and no
	///     chunked body, because the server port answers one request per connection.
	/// </summary>
	public class HttpRequest
	{
		public string Method { get; init; }

		/// <summary>Request path, query string excluded.</summary>
		public string Path { get; init; }

		/// <summary>The raw query string without its leading '?', or empty.</summary>
		public string Query { get; init; } = "";

		/// <summary>Header names are case insensitive, as HTTP requires.</summary>
		public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		///     Segments captured by the route template's <c>{name}</c> placeholders. Filled in by the
		///     router once a template has matched, so it is empty on the way in.
		/// </summary>
		public IReadOnlyDictionary<string, string> RouteValues { get; internal set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public string Body { get; init; } = "";

		/// <summary>Who connected. The peer's address, so a proxy in front of us reads as the proxy.</summary>
		public IPEndPoint RemoteEndPoint { get; init; }

		public string Header(string name) => Headers.TryGetValue(name, out string value) ? value : null;
	}
}
