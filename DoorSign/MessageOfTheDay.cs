using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoorSign
{
	/// <summary>
	/// A wrapper for the message of the day. This includes the server list text,
	/// player count, and version reported to the client.
	/// </summary>
	public struct MessageOfTheDay
	{
		public Chat description;
		public PlayerCount players;
		public ServerVersion version;
	}
}
