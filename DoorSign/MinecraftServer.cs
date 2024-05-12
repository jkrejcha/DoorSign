using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DoorSign
{
	/// <summary>
	/// A simple Minecraft server that implements the server list, login, and
	/// disconnect packets.
	/// </summary>
	public sealed class MinecraftServer
	{
		private Settings Settings { get; set; }
		private Logger Logger { get; set; }

		private TcpListener TcpListener { get; set; }

		private Task ServerLoopTask { get; set; }

		private CancellationTokenSource ServerLoopCancellationTokenSource { get; set; }
		public bool Running { get; private set; }

		public MinecraftServer(Settings Settings, Logger Logger)
		{
			this.Settings = Settings;
			this.Logger = Logger;
		}

		public async Task StartAsync()
		{
			Logger.Info("Starting DoorSign Minecraft Server on port " + Settings.Port);
			TcpListener = TcpListener.Create(Settings.Port);
			TcpListener.Start();
			ServerLoopCancellationTokenSource = new CancellationTokenSource();
			Running = true;
			Logger.Info("Server started.");
			CancellationToken token = ServerLoopCancellationTokenSource.Token;
			await DoServerLoopAsync(token);
		}

		public async Task DoServerLoopAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				if (TcpListener.Pending())
				{
					try
					{
						TcpClient client = await TcpListener.AcceptTcpClientAsync();
						client.LingerState = new LingerOption(false, 0);
						Logger.Debug("Handling new client");
						_ = HandleClientAsync(client);
					}
					catch (Exception ex)
					{
						Logger.Warning(ex.ToString());
					}
				}
				else
				{
					try
					{

						await Task.Delay(100, token);
					}
					catch (OperationCanceledException)
					{
						return;
					}
				}
			}
		}

		public async Task StopAsync()
		{
			Logger.Info("Stopping server...");
			ServerLoopCancellationTokenSource.Cancel();
			await ServerLoopTask;
			Running = false;
			Logger.Info("Server has been shut down.");
		}

		private void Disconnect(TcpClient client)
		{
			if (client.Connected)
			{
				client.GetStream().Close();
			}
			client.Close();
		}

		private async Task HandleClientAsync(TcpClient client)
		{
			NetworkStream networkStream = client.GetStream();
			while (client.Connected)
			{
				MinecraftStream minecraftStream = new MinecraftStream(networkStream);
				await HandleConnection(client, minecraftStream, (IPEndPoint)client.Client.RemoteEndPoint);
			}
		}

		private async Task HandleConnection(TcpClient connection, MinecraftStream dataStream, IPEndPoint endpoint)
		{
			long length = dataStream.ReadVarInt();
			long packetId = dataStream.ReadVarInt();

			Byte[] response;
			bool shouldDisconnect = false;

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
					bool isPing = IsServerListPing(dataStream);
					response = isPing ?
							   BuildServerListPacket(Settings.GetMotdJson()) :
							   BuildLoginKickPacket(Settings.GetKickMessageJson());
					if (!isPing)
					{
						Logger.Info("A user (IP: " + endpoint.Address.ToString() + ") tried to login to the server");
						shouldDisconnect = true;
					}
					break;
				case 0x01:
					Logger.Debug("Lag Test Packet (response time shows up in server list)");
					MinecraftStream minecraftStream = new MinecraftStream(new MemoryStream());
					minecraftStream.WriteVarInt((UInt32)length);
					minecraftStream.WriteVarInt((UInt32)packetId);
					Byte[] echoData = new byte[length];
					_ = await dataStream.ReadAsync(echoData, 0, (Int32)length);
					minecraftStream.Write(echoData);
					response = ((MemoryStream)minecraftStream.BackingStream).ToArray(); // send back the same data as the client requests
					break;
				default:
					Logger.Warning("Unknown packet " + packetId + "; will try kick disconnect");
					response = BuildLoginKickPacket(Settings.GetKickMessageJson(), true);
					shouldDisconnect = true;
					break;
			}
			if (response != null)
			{
				SendDebugMessage(response);
				await connection.GetStream().WriteAsync(response, 0, response.Length);
				await dataStream.FlushAsync();
			}
			if (shouldDisconnect)
			{
				Disconnect(connection);
			}
		}

		/// <summary>
		/// Gets the type of handshake packet this is
		/// </summary>
		/// <param name="data">The data</param>
		/// <returns></returns>
		private bool IsServerListPing(MinecraftStream data)
		{
			Logger.Debug("Is Server List?");
			MinecraftStream s = data;
			//s.ReadVarInt(); // length
			//s.ReadVarInt(); // packet ID
			s.ReadVarInt(); // protocol version
			s.ReadString(); // server IP
			s.ReadUInt16(); // server port
			int response = s.ReadVarInt();
			Logger.Debug($"Handshake Packet = {response}, Server List Packet = 1");
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
