using System;
using System.IO;
using System.Text;

namespace DoorSign
{
    /// <summary>
    /// A big-endian stream for reading/writing Minecraft data types.
    /// </summary>
    public class MinecraftStream : Stream
    {
        public static readonly Encoding StringEncoding = Encoding.UTF8;

        public Stream BackingStream { get; private set; }

        public override Boolean CanRead => BackingStream.CanRead;

        public override Boolean CanSeek => BackingStream.CanSeek;

        public override Boolean CanWrite => BackingStream.CanWrite;

        public override Int64 Length => BackingStream.Length;

        public override Int64 Position { get => BackingStream.Position; set { BackingStream.Position = value; } }

        public MinecraftStream()
        {
            this.BackingStream = new MemoryStream();
        }

        public MinecraftStream(Stream backingStream)
        {
            this.BackingStream = backingStream;
        }

        public MinecraftStream(Byte[] data)
        {
            this.BackingStream = new MemoryStream(data);
        }

        /// <summary>
        /// Reads a variable-length integer from the stream.
        /// </summary>
        public int ReadVarInt()
        {
            uint result = 0;
            int length = 0;
            while (true)
            {
                byte current = ReadUInt8();
                result |= (current & 0x7Fu) << length++ * 7;
                if (length > 5)
                    throw new InvalidDataException("VarInt may not be longer than 60 bits.");
                if ((current & 0x80) != 128)
                    break;
            }
            return (int)result;
        }

        /// <summary>
        /// Writes a variable-length integer to the stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteVarInt(uint value)
        {
            int length = 0;
            while (true)
            {
                length++;
                if ((value & 0xFFFFFF80u) == 0)
                {
                    WriteUInt8((byte)value);
                    break;
                }
                WriteUInt8((byte)(value & 0x7F | 0x80));
                value >>= 7;
            }
        }

        public static int GetVarIntLength(int _value)
        {
            uint value = (uint)_value;
            int length = 0;
            while (true)
            {
                length++;
                if ((value & 0xFFFFFF80u) == 0)
                    break;
                value >>= 7;
            }
            return length;
        }

        public byte ReadUInt8()
        {
            int value = ReadByte();
            if (value == -1)
                throw new EndOfStreamException();
            return (byte)value;
        }

        public void WriteUInt8(byte value)
        {
            WriteByte(value);
        }

        public ushort ReadUInt16()
        {
            return (ushort)(
                (ReadUInt8() << 8) |
                ReadUInt8());
        }

        public void WriteUInt16(ushort value)
        {
            Write(new[]
            {
                (byte)((value & 0xFF00) >> 8),
                (byte)(value & 0xFF)
            }, 0, 2);
        }

        public short ReadInt16()
        {
            return (short)ReadUInt16();
        }

        public void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        public uint ReadUInt32()
        {
            return (uint)(
                (ReadUInt8() << 24) |
                (ReadUInt8() << 16) |
                (ReadUInt8() << 8 ) |
                 ReadUInt8());
        }

        public void WriteUInt32(uint value)
        {
            Write(new[]
            {
                (byte)((value & 0xFF000000) >> 24),
                (byte)((value & 0xFF0000) >> 16),
                (byte)((value & 0xFF00) >> 8),
                (byte)(value & 0xFF)
            }, 0, 4);
        }

        public byte[] ReadUInt8Array(int length)
        {
            var result = new byte[length];
            if (length == 0) return result;
            int n = length;
            while (true) {
                n -= Read(result, length - n, n);
                if (n == 0)
                    break;
                System.Threading.Thread.Sleep(1);
            }
            return result;
        }

        public void WriteUInt8Array(byte[] value)
        {
            Write(value, 0, value.Length);
        }

        public void WriteUInt8Array(byte[] value, int offset, int count)
        {
            Write(value, offset, count);
        }

        public string ReadString()
        {
            long length = ReadVarInt();
            if (length == 0) return string.Empty;
            var data = ReadUInt8Array((int)length);
            return StringEncoding.GetString(data);
        }

        public void WriteString(string value)
        {
            WriteVarInt((uint)StringEncoding.GetByteCount(value));
            if (value.Length > 0)
                WriteUInt8Array(StringEncoding.GetBytes(value));
        }

        public override void Flush() => BackingStream.Flush();

        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => BackingStream.Read(buffer, offset, count);

        public override Int64 Seek(Int64 offset, SeekOrigin origin) => BackingStream.Seek(offset, origin);

        public override void SetLength(Int64 value) => BackingStream.SetLength(value);

        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => BackingStream.Write(buffer, offset, count);
    }
}
