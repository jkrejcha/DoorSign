using Newtonsoft.Json;
using System;

namespace DoorSign
{
	public class Settings
	{
		public MessageOfTheDay MessageOfTheDay { get; set; }
		public Chat KickMessage { get; set; }
		public UInt16 Port { get; set; }

		public Settings()
		{
			MessageOfTheDay = new MessageOfTheDay()
			{
				description = "A Minecraft Server",
				players = new PlayerCount()
				{
					online = 0,
					max = 20,
				},
				version = new ServerVersion()
				{
					name = "DoorSign 1.20.4",
					protocol = 765,
				},
			};
			KickMessage = "The server is not online right now...";
			Port = 25565;
		}

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
