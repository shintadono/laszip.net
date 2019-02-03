//===============================================================================
//
//  FILE:  laszip_decompress_selective_v3.cs
//
//  CONTENTS:
//
//    Contains bit mask definitions for selective decompression.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - fast tools to catch reality
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

namespace LASzip.Net
{
	[Flags]
	public enum LASZIP_DECOMPRESS_SELECTIVE : uint
	{
		CHANNEL_RETURNS_XY = 0x00000000,
		Z = 0x00000001,
		CLASSIFICATION = 0x00000002,
		FLAGS = 0x00000004,
		INTENSITY = 0x00000008,
		SCAN_ANGLE = 0x00000010,
		USER_DATA = 0x00000020,
		POINT_SOURCE = 0x00000040,
		GPS_TIME = 0x00000080,
		RGB = 0x00000100,
		NIR = 0x00000200,
		WAVEPACKET = 0x00000400,
		BYTE0 = 0x00010000,
		BYTE1 = 0x00020000,
		BYTE2 = 0x00040000,
		BYTE3 = 0x00080000,
		BYTE4 = 0x00100000,
		BYTE5 = 0x00200000,
		BYTE6 = 0x00400000,
		BYTE7 = 0x00800000,
		EXTRA_BYTES = 0xFFFF0000,

		ALL = Z | CLASSIFICATION | FLAGS | INTENSITY | SCAN_ANGLE | USER_DATA | POINT_SOURCE | GPS_TIME |
			RGB | NIR | WAVEPACKET | BYTE0 | BYTE1 | BYTE2 | BYTE3 | BYTE4 | BYTE5 | BYTE6 | BYTE7 | EXTRA_BYTES
	}
}
