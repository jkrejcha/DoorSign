using Newtonsoft.Json;
using System;
using System.IO;

namespace DoorSign
{
	class Program
	{
		private static MinecraftServer Server { get; set; }
		private static Logger Log { get; set; }
		private static Settings Config { get; set; }

		static void Main(string[] args)
		{
			Log = new Logger(Console.Out);
#if DEBUG
			Log.MinLevel = LogLevel.Debug;
#endif
			try
			{
				String configStr = File.ReadAllText("config.cfg");
				Config = JsonConvert.DeserializeObject<Settings>(configStr);
			}
			catch (IOException iex) when (iex is FileNotFoundException)
			{
				Log.Warning("Could not find configuration file; creating a new one");
				Config = new Settings();
				WriteDefaultConfig();
			}
			catch (UnauthorizedAccessException)
			{
				Log.Warning("Application does not have permission to access the configuration file; using default one");
				Config = new Settings();
			}
			catch (Exception ex)
			{
				Log.Warning("Error occurred while loading configuration file");
				Log.Warning(ex.ToString());
			}
			Server = new MinecraftServer(Config, Log);
			Console.CancelKeyPress += Console_CancelKeyPress;
			Server.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		private static void WriteDefaultConfig()
		{
			try
			{
				File.WriteAllText("config.cfg", JsonConvert.SerializeObject(Config, Formatting.Indented));
			}
			catch (IOException iex)
			{
				Log.Error("Could not save default configuration.");
				Log.Error(iex.ToString());
			}
			catch (UnauthorizedAccessException)
			{
				Log.Warning("Application does not have permission to access the configuration file; using default one");
				Config = new Settings();
			}
		}

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			Server.StopAsync().GetAwaiter().GetResult();
		}
	}
}
