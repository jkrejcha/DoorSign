using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorSign
{
	public class Settings
	{
		public MessageOfTheDay MessageOfTheDay { get; set; }
		public Chat KickMessage { get; set; }
		public UInt16 Port { get; set; }

		public String GetMotdJson()
		{
			return JsonConvert.SerializeObject(MessageOfTheDay, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
			});
		}

		public String GetKickMessageJson()
		{
			return JsonConvert.SerializeObject(KickMessage, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore,
			});
		}
	}
}
