using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorSign
{
	[JsonObject]
	public class Chat
	{
		public String text;
		public bool? bold;
		public bool? italic;
		public bool? underlined;
		public bool? strikethrough;
		public bool? obfuscated;
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

		public bool ShouldSerializebold() => bold != null;
		public bool ShouldSerializeitalic() => italic != null;
		public bool ShouldSerializeunderlined() => underlined != null;
		public bool ShouldSerializestrikethrough() => strikethrough != null;
		public bool ShouldSerializeobfuscated() => obfuscated != null;
		public bool ShouldSerializeclickEvent() => clickEvent != null;
		public bool ShouldSerializehoverEvent() => hoverEvent != null;
		public bool ShouldSerializecolor() => !String.IsNullOrWhiteSpace(color);
		public bool ShouldSerializeextra() => extra != null && extra.Length > 0;

		public static implicit operator Chat(String str)
		{
			return new Chat()
			{
				text = str,
			};
		}
	}
}
