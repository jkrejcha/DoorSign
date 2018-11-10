using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorSign
{
	[JsonObject]
	public class ServerVersion
	{
		public String name;
		public int protocol;
	}
}
