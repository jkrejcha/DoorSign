using Newtonsoft.Json;
using System;

namespace DoorSign
{
	[JsonObject]
	public struct Chat
	{
		public String text;
		public Boolean? bold;
		public Boolean? italic;
		public Boolean? underlined;
		public Boolean? strikethrough;
		public Boolean? obfuscated;
		public String color;
		public ClickEvent clickEvent;
		public HoverEvent hoverEvent;
		public Chat[] extra;

		[JsonObject]
		public class ClickEvent
		{
			public String open_url;
		}

		[JsonObject]
		public class HoverEvent
		{
			public Chat show_text;
		}

		public readonly Boolean ShouldSerializebold() => bold != null;
		public readonly Boolean ShouldSerializeitalic() => italic != null;
		public readonly Boolean ShouldSerializeunderlined() => underlined != null;
		public readonly Boolean ShouldSerializestrikethrough() => strikethrough != null;
		public readonly Boolean ShouldSerializeobfuscated() => obfuscated != null;
		public readonly Boolean ShouldSerializeclickEvent() => clickEvent != null;
		public readonly Boolean ShouldSerializehoverEvent() => hoverEvent != null;
		public readonly Boolean ShouldSerializecolor() => !String.IsNullOrWhiteSpace(color);
		public readonly Boolean ShouldSerializeextra() => extra != null && extra.Length > 0;

		public static implicit operator Chat(String str)
		{
			return new Chat()
			{
				text = str,
			};
		}
	}
}
