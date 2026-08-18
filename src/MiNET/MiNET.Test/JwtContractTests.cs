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
using System.Security.Cryptography;
using Jose;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace MiNET.Test
{
	/// <summary>
	///     Every login goes through jose-jwt, and the server leans on five distinct shapes of its API.
	///     Four of them are unauthenticated reads used to inspect a token before anything has verified
	///     it, which is safe only because a fifth call verifies the signature afterwards. A library
	///     change that quietly relaxed that fifth call would leave every test green and every login
	///     forgeable, so the negative case here matters more than the positive ones.
	/// </summary>
	[TestClass]
	public class JwtContractTests
	{
		private static ECDsa NewKey() => ECDsa.Create(ECCurve.NamedCurves.nistP384);

		/// <summary>
		///     MiNET does not use jose-jwt's own JSON mapper. <see cref="NewtonsoftMapper" /> implements
		///     the library's <c>IJsonMapper</c> and installs itself globally, because the login models
		///     are Newtonsoft-attributed and camel-cased. If the interface or the settings hook moves,
		///     serialization silently reverts to the library default and every property name changes
		///     case on the wire.
		/// </summary>
		[TestMethod]
		public void OurNewtonsoftMapperIsTheOneJoseUses()
		{
			// Touching the type runs its static constructor, which is what installs the mapper.
			var mapper = new NewtonsoftMapper();
			Assert.IsInstanceOfType(JWT.DefaultSettings.JsonMapper, typeof(NewtonsoftMapper));

			string json = mapper.Serialize(new {DisplayName = "Notch"});
			Assert.IsTrue(json.Contains("displayName"), $"expected camelCase from our mapper, got {json}");
		}

		/// <summary>
		///     The handshake token the server sends is ES384 with the public key carried in a custom
		///     x5u header. Both the algorithm and that header are read by a real client, so this pins
		///     the exact call shape used in CryptoUtils and LoginMessageHandler.
		/// </summary>
		[TestMethod]
		public void Es384TokensCarryTheX5uHeaderWeSet()
		{
			ECDsa key = NewKey();
			string token = JWT.Encode(new {salt = "abc"}, key, JwsAlgorithm.ES384, new Dictionary<string, object> {{"x5u", "public-key-blob"}});

			IDictionary<string, dynamic> headers = JWT.Headers(token);
			Assert.AreEqual("ES384", (string) headers["alg"]);
			Assert.AreEqual("public-key-blob", (string) headers["x5u"]);
		}

		/// <summary>
		///     The login chain has to be read before it can be verified: the key that verifies a link
		///     is carried in the link before it, so the server parses the payload of an untrusted
		///     token to find it. That is what JWT.Payload is for here, and it must keep working
		///     without a key.
		/// </summary>
		[TestMethod]
		public void PayloadCanBeReadBeforeAnythingHasVerifiedTheToken()
		{
			string token = JWT.Encode(new {identityPublicKey = "the-next-key"}, NewKey(), JwsAlgorithm.ES384);

			var payload = JObject.Parse(JWT.Payload(token));

			Assert.AreEqual("the-next-key", (string) payload["identityPublicKey"]);
		}

		/// <summary>
		///     Typed decode with the matching key is how a chain link is finally trusted, and it has to
		///     both verify and populate the model through our mapper.
		/// </summary>
		[TestMethod]
		public void TypedDecodeWithTheRightKeyVerifiesAndFillsTheModel()
		{
			ECDsa key = NewKey();
			string token = JWT.Encode(new {identityPublicKey = "key-material", certificateAuthority = true}, key, JwsAlgorithm.ES384);

			var data = JWT.Decode<CertificateData>(token, key);

			Assert.AreEqual("key-material", data.IdentityPublicKey);
			Assert.IsTrue(data.CertificateAuthority);
		}

		/// <summary>
		///     The one that actually guards the door. A token signed by anyone else must be rejected,
		///     not merely reported. If an upgrade ever made verification lenient, every other test in
		///     this file would still pass while the server accepted forged identities.
		/// </summary>
		[TestMethod]
		public void TypedDecodeWithTheWrongKeyIsRejected()
		{
			string token = JWT.Encode(new {identityPublicKey = "key-material"}, NewKey(), JwsAlgorithm.ES384);

			Assert.ThrowsExactly<IntegrityException>(() => JWT.Decode<CertificateData>(token, NewKey()));
		}
	}
}
