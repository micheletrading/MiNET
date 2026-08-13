using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiNET.Client;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Utils.IO;
using Newtonsoft.Json;

namespace MiNET.Test
{
	[TestClass]
	public class ClientTests
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ClientTests));

		public class DataScraperTraceHandler : BedrockTraceHandler
		{
			public bool Finished { get; private set; }

			public DataScraperTraceHandler(MiNetClient client) : base(client)
			{
			}

			public override void HandleMcpeStartGame(McpeStartGame message)
			{
				var fileNameBlockstates = Path.GetTempPath() + "blockstates_" + Guid.NewGuid() + ".json";

				File.WriteAllText(fileNameBlockstates, JsonConvert.SerializeObject(message.blockProperties));
				
				Finished = true;
			}
		}

		[TestMethod, DataRow("127.0.0.1", 19132, "TheGrey"), Timeout(10 * 1000), Ignore("Used to get blockstates data from the vanilla server")]
		public void GenerateBlockstates(string ip, int port, string username)
		{
			if (!IPAddress.TryParse(ip, out IPAddress address))
			{
				address = Dns.GetHostAddresses(ip).FirstOrDefault();
			}

			var endpoint = new IPEndPoint(address, port);

			var client = new MiNetClient(endpoint, username);
			var scraper = new DataScraperTraceHandler(client);
			client.MessageHandler = new DataScraperTraceHandler(client);

			Assert.IsTrue(client.ConnectNetherNetAsync().GetAwaiter().GetResult(), "could not connect over NetherNet");

			while(!scraper.Finished)
			{
				// waiting for data...
			}

		}
	}
}
