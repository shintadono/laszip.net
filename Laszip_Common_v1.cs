//===============================================================================
//
//  FILE:  laszip_common_v1.hpp
//
//  CONTENTS:
//
//    Common defines and functionalities for version 1 of LASitemReadCompressed
//    and LASitemwriteCompressed.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//    uday.karan@gmail.com - http://github.com/verma
//
//  COPYRIGHT:
//
//    (c) 2007-2014, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2014-2018 by Shinta <shintadono@googlemail.com>
//
//    This is free software; you can redistribute and/or modify it under the
//    terms of the GNU Lesser General Licence as published by the Free Software
//    Foundation. See the COPYING file for more information.
//
//    This software is distributed WITHOUT ANY WARRANTY and without even the
//    implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//
//  CHANGE HISTORY:
//
//    10 April 2014 - refactor LASwavepacket13 and add other functions to it
//
//===============================================================================
using System.Runtime.InteropServices;

namespace LASzip.Net
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct LASpoint10
	{
		public int X;
		public int Y;
		public int Z;
		public ushort intensity;

		// all the following bits combine to flags
		//public byte return_number : 3;
		//public byte number_of_returns : 3;
		//public byte scan_direction_flag : 1;
		//public byte edge_of_flight_line : 1;
		public byte flags;

		// all the following bits combine to classification_and_classification_flags
		//public byte classification : 5;
		//public byte synthetic_flag : 1;
		//public byte keypoint_flag : 1;
		//public byte withheld_flag : 1;
		public byte classification_and_classification_flags;
		public sbyte scan_angle_rank;
		public byte user_data;
		public ushort point_source_ID;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct LASwavepacket13
	{
		public ulong offset;
		public uint packet_size;
		public U32I32F32 return_point;
		public U32I32F32 x;
		public U32I32F32 y;
		public U32I32F32 z;

		public static LASwavepacket13 unpack(byte[] item, int offset = 0)
		{
			// unpack a LAS wavepacket out of raw memory
			LASwavepacket13 r = new LASwavepacket13();

			r.offset = makeU64(item, offset);
			r.packet_size = makeU32(item, offset + 8);
			r.return_point.u32 = makeU32(item, offset + 12);

			r.x.u32 = makeU32(item, offset + 16);
			r.y.u32 = makeU32(item, offset + 20);
			r.z.u32 = makeU32(item, offset + 24);

			return r;
		}

		public void pack(byte[] item, int item_offset = 0)
		{
			// pack a LAS wavepacket into raw memory
			packU32((uint)(offset & 0xFFFFFFFF), item, item_offset);
			packU32((uint)(offset >> 32), item, item_offset + 4);

			packU32(packet_size, item, item_offset + 8);
			packU32(return_point.u32, item, item_offset + 12);
			packU32(x.u32, item, item_offset + 16);
			packU32(y.u32, item, item_offset + 20);
			packU32(z.u32, item, item_offset + 24);
		}

		static ulong makeU64(byte[] item, int offset = 0)
		{
			ulong dw0 = (ulong)makeU32(item, offset);
			ulong dw1 = (ulong)makeU32(item, offset + 4);

			return dw0 | (dw1 << 32);
		}

		static uint makeU32(byte[] item, int offset = 0)
		{
			uint b0 = (uint)item[offset + 0];
			uint b1 = (uint)item[offset + 1];
			uint b2 = (uint)item[offset + 2];
			uint b3 = (uint)item[offset + 3];

			return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
		}

		static void packU32(uint v, byte[] item, int offset = 0)
		{
			item[offset + 0] = (byte)(v & 0xFF);
			item[offset + 1] = (byte)((v >> 8) & 0xFF);
			item[offset + 2] = (byte)((v >> 16) & 0xFF);
			item[offset + 3] = (byte)((v >> 24) & 0xFF);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct LASpoint14
	{
		public int X;
		public int Y;
		public int Z;
		public ushort intensity;

		//public byte return_number : 4;
		public byte return_number { get { return (byte)(returns & 0xF); } set { returns = (byte)((returns & 0xF0) | (value & 0xF)); } }
		//public byte number_of_returns : 4;
		public byte number_of_returns { get { return (byte)((returns >> 4) & 0xF); } set { returns = (byte)((returns & 0xF) | ((value & 0xF) << 4)); } }
		public byte returns;

		//public byte classification_flags : 4;
		public byte classification_flags { get { return (byte)(flags & 0xF); } set { flags = (byte)((flags & 0xF0) | (value & 0xF)); } }
		//public byte scanner_channel : 2;
		public byte scanner_channel { get { return (byte)((flags >> 4) & 3); } set { flags = (byte)((flags & 0xCF) | ((value & 3) << 4)); } }
		//public byte scan_direction_flag : 1;
		public byte scan_direction_flag { get { return (byte)((flags >> 6) & 1); } set { flags = (byte)((flags & 0xBF) | ((value & 1) << 6)); } }
		//public byte edge_of_flight_line : 1;
		public byte edge_of_flight_line { get { return (byte)((flags >> 7) & 1); } set { flags = (byte)((flags & 0x7F) | ((value & 1) << 7)); } }
		public byte flags;

		public byte classification;
		public byte user_data;
		public short scan_angle;
		public ushort point_source_ID;
		public double gps_time;
	}
}
