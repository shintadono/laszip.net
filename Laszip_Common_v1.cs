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
//    (c) of the C# port 2014 by Shinta <shintadono@googlemail.com>
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
	[StructLayout(LayoutKind.Sequential, Pack=1)]
	struct LASwavepacket13
	{
		public ulong offset;
		public uint packet_size;
		public U32I32F32 return_point;
		public U32I32F32 x;
		public U32I32F32 y;
		public U32I32F32 z;

		//public LASwavepacket13 unpack(byte[] item)
		//{
		//	// unpack a LAS wavepacket out of raw memory
		//	LASwavepacket13 r;

		//	r.offset=makeU64(item);
		//	r.packet_size=makeU32(item+8);
		//	r.return_point.u32=makeU32(item+12);

		//	r.x.u32=makeU32(item+16);
		//	r.y.u32=makeU32(item+20);
		//	r.z.u32=makeU32(item+24);

		//	return r;
		//}

		//public void pack(byte[] item)
		//{
		//	// pack a LAS wavepacket into raw memory
		//	packU32((U32)(offset&0xFFFFFFFF), item);
		//	packU32((U32)(offset>>32), item+4);

		//	packU32(packet_size, item+8);
		//	packU32(return_point.u32, item+12);
		//	packU32(x.u32, item+16);
		//	packU32(y.u32, item+20);
		//	packU32(z.u32, item+24);
		//}

		//static ulong makeU64(byte[] item)
		//{
		//	U64 dw0=(U64)makeU32(item);
		//	U64 dw1=(U64)makeU32(item+4);

		//	return dw0|(dw1<<32);
		//}

		//static uint makeU32(byte[] item)
		//{
		//	U32 b0=(U32)item[0];
		//	U32 b1=(U32)item[1];
		//	U32 b2=(U32)item[2];
		//	U32 b3=(U32)item[3];

		//	return b0|(b1<<8)|(b2<<16)|(b3<<24);
		//}

		//static void packU32(uint v, byte[] item)
		//{
		//	item[0]=v&0xFF;
		//	item[1]=(v>>8)&0xFF;
		//	item[2]=(v>>16)&0xFF;
		//	item[3]=(v>>24)&0xFF;
		//}
	}
}
