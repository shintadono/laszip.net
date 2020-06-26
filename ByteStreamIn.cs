//===============================================================================
//
//  FILE:  ByteStreamIn.cs
//
//  CONTENTS:
//
//    Extension class for input streams with endian handling.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2012, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2018-2019 by Shinta <shintadono@googlemail.com>
//
//    This is free software; you can redistribute and/or modify it under the
//    terms of the GNU Lesser General Licence as published by the Free Software
//    Foundation. See the COPYING file for more information.
//
//    This software is distributed WITHOUT ANY WARRANTY and without even the
//    implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//
//  CHANGE HISTORY: omitted for easier Copy&Paste (pls see the original)
//
//===============================================================================

using System;
using System.IO;

namespace LASzip.Net
{
	static class ByteStreamIn
	{
		[ThreadStatic]
		static byte[] _buffer;
		static byte[] buffer
		{
			get
			{
				if (_buffer == null)
				{
					_buffer = new byte[8];
				}
				return _buffer;
			}
		}

		//// read a single byte
		//public static uint getByte(this Stream stream)
		//{
		//	int ret = stream.ReadByte();
		//	if (ret == -1) throw new EndOfStreamException();
		//	return (uint)ret;
		//}

		// read an array of bytes
		public static bool getBytes(this Stream stream, byte[] bytes, int num_bytes)
		{
			return stream.Read(bytes, 0, num_bytes) == num_bytes;
		}

		// read an array of bytes
		public static bool getBytes(this Stream stream, byte[] bytes, int offset, int num_bytes)
		{
			return stream.Read(bytes, offset, num_bytes) == num_bytes;
		}

		// read 8 bit field
		public static bool get8bits(this Stream stream, out byte val)
		{
			val = 0;
			int ret = stream.ReadByte();
			if (ret < 0) return false;
			val = (byte)ret;
			return true;
		}

		// read 16 bit field
		public static bool get16bits(this Stream stream, out ushort val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 2) != 2) return false;
			val = BitConverter.ToUInt16(buffer, 0);
			return true;
		}

		// read 16 bit field
		public static bool get16bits(this Stream stream, ushort[] val, int num_ushorts)
		{
			if (stream.Read(buffer, 0, 2 * num_ushorts) != 2 * num_ushorts) return false;
			for (int i = 0; i < num_ushorts; i++) val[i] = BitConverter.ToUInt16(buffer, 2 * i);
			return true;
		}

		// read 32 bit field
		public static bool get32bits(this Stream stream, out int val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 4) != 4) return false;
			val = BitConverter.ToInt32(buffer, 0);
			return true;
		}

		// read 32 bit field
		public static bool get32bits(this Stream stream, out uint val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 4) != 4) return false;
			val = BitConverter.ToUInt32(buffer, 0);
			return true;
		}

		// read 32 bit field
		public static bool get32bits(this Stream stream, out float val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 4) != 4) return false;
			val = BitConverter.ToSingle(buffer, 0);
			return true;
		}

		// read 64 bit field
		public static bool get64bits(this Stream stream, out ulong val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 8) != 8) return false;
			val = BitConverter.ToUInt64(buffer, 0);
			return true;
		}

		// read 64 bit field
		public static bool get64bits(this Stream stream, out long val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 8) != 8) return false;
			val = BitConverter.ToInt64(buffer, 0);
			return true;
		}

		// read 64 bit field
		public static bool get64bits(this Stream stream, out double val)
		{
			val = 0;
			if (stream.Read(buffer, 0, 8) != 8) return false;
			val = BitConverter.ToDouble(buffer, 0);
			return true;
		}
	};
}
