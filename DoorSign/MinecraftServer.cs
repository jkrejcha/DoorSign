using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DoorSign
{
	/// <summary>
	/// A simple Minecraft server that implements the server list, login, and
	/// disconnect packets.
	/// </summary>
	public sealed class MinecraftServer
	{
		private const int BufferSize = 4096;

		private Settings Settings { get; set; }
		private Logger Logger { get; set; }
		private Socket ServerSocket { get; set; }
		private List<Socket> ClientSockets { get; } = new List<Socket>();
		private byte[] Buffer { get; } = new byte[BufferSize];
		public bool Running { get; private set; }

		public MinecraftServer(Settings Settings, Logger Logger)
		{
			this.Settings = Settings;
			this.Logger = Logger;
		}

		public void Start()
		{
			Logger.Info("Starting DoorSign Minecraft Server on port " + Settings.Port);
			ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			ServerSocket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
			ServerSocket.Listen(10);
			ServerSocket.BeginAccept(AcceptCallback, null);
			Running = true;
			Logger.Info("Server started.");
		}

		public void Stop()
		{
			Logger.Info("Stopping server...");
			CloseAllSockets();
			Running = false;
			Logger.Info("Server has been shut down.");
		}

		/// <summary>
		/// Close all connected clients (we do not need to shutdown the server socket as its connections
		/// are already closed with the clients)
		/// </summary>
		internal void CloseAllSockets()
		{
			ClientSockets.ForEach(socket => socket.Shutdown(SocketShutdown.Both));
			ServerSocket.Close();
		}

		internal void Disconnect(Socket s)
		{
			s.Disconnect(false);
			s.Close();
			ClientSockets.Remove(s);
		}

		internal Socket GetConnection(IPEndPoint ip)
		{
			return ClientSockets.FirstOrDefault(s => (IPEndPoint)s.RemoteEndPoint == ip);
		}

		private void AcceptCallback(IAsyncResult AR)
		{
			Socket socket;
			try
			{
				socket = ServerSocket.EndAccept(AR);
			}
			catch (ObjectDisposedException) // I cannot seem to avoid this (on exit when properly closing sockets)
			{
				return;
			}

			ClientSockets.Add(socket);
			socket.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, socket);
			ServerSocket.BeginAccept(AcceptCallback, null);
		}

		private void ReceiveCallback(IAsyncResult AR)
		{
			Socket current = (Socket)AR.AsyncState;
			int received;

			try
			{
				received = current.EndReceive(AR);
			}
			catch (ObjectDisposedException)
			{
				return;
			}
			catch (SocketException)
			{
				Logger.Info("Client forcefully disconnected (or lost connection).");
				current.Close();
				ClientSockets.Remove(current);
				return;
			}

			byte[] recBuf = new byte[received];
			Array.Copy(Buffer, recBuf, received);
			HandleConnection(current, recBuf, (IPEndPoint)current.RemoteEndPoint);
			try
			{
				current.BeginReceive(Buffer, 0, BufferSize, SocketFlags.None, ReceiveCallback, current);
			}
			catch (Exception)
			{
				//TODO: log?
			}
		}

		private void HandleConnection(Socket connection, byte[] data, IPEndPoint endpoint)
		{
			if (data.Length == 0) // connection no longer valid; lets disconnect
			{
				Disconnect(connection);
				return;
			}
			Logger.Debug("Got message from client.");
			MinecraftStream dataStream = new MinecraftStream(data);
			long length = dataStream.ReadVarInt();
			long packetId = dataStream.ReadVarInt();

			switch (packetId)
			{
				case 0x00:
					Logger.Debug("Packet Type: Handshake (0x00) for server list ping and login.");
					if (length == 1)
					{
						// send the client ping back
						Logger.Debug("0-length packet (for request)");
						return;
					}
					bool isPing = IsServerListPing(data);
					byte[] response = isPing ?
									  BuildServerListPacket(Settings.GetMotdJson()) :
									  BuildLoginKickPacket(Settings.GetKickMessageJson());
					SendDebugMessage(response);
					connection.Send(response);
					if (!isPing)
					{
						Logger.Info("A user (IP: " + endpoint.Address.ToString() + ") tried to login to the server");
						Disconnect(connection);
					}
					break;
				case 0x01:
					Logger.Debug("Lag Test Packet (response time shows up in server list)");
					connection.Send(data); // send back the same data as the client requests
					break;
				default:
					Logger.Warning("Unknown packet " + packetId + "; will try kick disconnect");
					connection.Send(BuildLoginKickPacket(Settings.GetKickMessageJson(), true));
					Disconnect(connection);
					return;
			}
		}

		/// <summary>
		/// Gets the type of handshake packet this is
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns></returns>
		private bool IsServerListPing(byte[] data)
		{
			Logger.Debug("Is Server List?");
			MinecraftStream s = new MinecraftStream(data);
			s.ReadVarInt(); // length
			s.ReadVarInt(); // packet ID
			s.ReadVarInt(); // protocol version
			s.ReadString(); // server IP
			s.ReadUInt16(); // server port
			int response = s.ReadVarInt();
			Logger.Debug(response.ToString());
			return response == 0x01;
		}

		private void SendDebugMessage(byte[] data)
		{
			String s = String.Empty;
			foreach (byte b in data)
			{
				s += b.ToString("X2") + ", ";
			}
			Logger.Debug(s);
			MinecraftStream dataStream = new MinecraftStream(data);
			long length = dataStream.ReadVarInt();
			long packetId = dataStream.ReadVarInt();

			Logger.Debug("Packet length: " + length);
			Logger.Debug("Packet Type: " + packetId);
		}

		private byte[] BuildServerListPacket(String pingData)
		{
			MinecraftStream s = new MinecraftStream();
			uint stringLength = GetStringLength(pingData);
			s.WriteVarInt(stringLength + 1); // length (packet ID + string length length + string length)
			s.WriteVarInt(0x00); // server list ping
			s.WriteString(pingData); // the string
			s.Position = 0;
			using (BinaryReader br = new BinaryReader(s))
			{
				return br.ReadBytes((int)s.Length);
			}
		}

		/// <summary>
		/// Gets the meta-length of the string (which is the length of the length, and the length of the string itself).
		/// </summary>
		/// <param name="str">String to get the length of</param>
		/// <returns></returns>
		private uint GetStringLength(String str)
		{
			uint length = (uint)Encoding.UTF8.GetByteCount(str);
			return length + (uint)MinecraftStream.GetVarIntLength((int)length);
		}

		private byte[] BuildLoginKickPacket(String kickPacketString, bool inGameKick = false)
		{
			MinecraftStream s = new MinecraftStream();
			s.WriteVarInt(GetStringLength(kickPacketString) + 1); // length (packet ID + string length length + string length)
			s.WriteVarInt(inGameKick ? (uint)0x0A : 0x00);
			s.WriteString(kickPacketString);
			s.Position = 0;
			using (BinaryReader br = new BinaryReader(s))
			{
				return br.ReadBytes((int)s.Length);
			}
		}
	}
}
