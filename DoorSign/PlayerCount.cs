using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorSign
{
	/// <summary>
	/// The player count.
	/// </summary>
	[JsonObject]
	public struct PlayerCount
	{
		/// <summary>
		/// Maximum amount of users this server supports.
		/// </summary>
		public Int32 max;
		/// <summary>
		/// The amount of users currently online.
		/// </summary>
		public Int32 online;
	}
}
