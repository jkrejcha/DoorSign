using System;

namespace DoorSign
{
	class Program
	{
		private static MinecraftServer Server { get; set; }

		static void Main(string[] args)
		{
			Settings config = new Settings();
			Logger logger = new Logger(Console.Out);
#if DEBUG
			logger.MinLevel = LogLevel.Debug;
#endif
			config.KickMessage = "The server is not online right now...";
			config.MessageOfTheDay = new MessageOfTheDay()
			{
				description = "§3The server is not online at the moment\n(Although DoorSign is...)",
				players = new PlayerCount()
				{
					max = 0,
					online = 0,
				},
				version = new ServerVersion()
				{
					name = "DoorSign 1.13.2",
					protocol = 404,
				},
			};
			config.Port = 25565;
			Server = new MinecraftServer(config, new Logger(Console.Out, LogLevel.Info));
			Console.CancelKeyPress += Console_CancelKeyPress;
			Server.Start();
			while (true) { System.Threading.Thread.Sleep(1000); }
		}

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			Server.Stop();
		}
	}
}
