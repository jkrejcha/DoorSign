using System;
using System.IO;

namespace DoorSign
{
	public class Logger
	{
		private TextWriter Output;
		public LogLevel MinLevel;

		public Logger(TextWriter Output, LogLevel MinLevel = LogLevel.Info)
		{
			this.Output = Output;
			this.MinLevel = MinLevel;
		}

		public void Debug(String Message)
		{
			Log(Message, LogLevel.Debug);
		}

		public void Info(String Message)
		{
			Log(Message, LogLevel.Info);
		}

		public void Warning(String Message)
		{
			Log(Message, LogLevel.Warning);
		}

		public void Error(String Message)
		{
			Log(Message, LogLevel.Error);
		}

		public void Log(String Message, LogLevel Level)
		{
			if (Level < MinLevel) return;
			DateTime now = System.DateTime.Now;
			String text = "[" + now.ToShortDateString() + " " + now.ToLongTimeString() + "] " + "[" + Level.ToString() + "] " + Message + Environment.NewLine;
			Output.Write(text, 0, text.Length);
		}
	}
	public enum LogLevel
	{
		NotUsable = 0,
		Finest = 1,
		Finer = 2,
		Fine = 3,
		Debug = 4,
		Info = 5,
		Warning = 6,
		Error = 7,
	}
}
