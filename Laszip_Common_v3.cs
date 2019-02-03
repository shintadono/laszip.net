//===============================================================================
//
//  FILE:  laszip_common_v3.cs
//
//  CONTENTS:
//
//    Common defines and functionalities for version 3 of LASitemReadCompressed
//    and LASitemwriteCompressed.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - tools to catch reality
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

namespace LASzip.Net
{
	class LAScontextPOINT14
	{
		public bool unused = false;

		public laszip_point last_item = new laszip_point();
		public bool last_item_gps_time_change = false;
		public readonly ushort[] last_intensity = new ushort[8];
		public readonly StreamingMedian5[] last_X_diff_median5 = new StreamingMedian5[12];
		public readonly StreamingMedian5[] last_Y_diff_median5 = new StreamingMedian5[12];
		public readonly int[] last_Z = new int[8];

		public readonly ArithmeticModel[] m_changed_values = new ArithmeticModel[8];
		public ArithmeticModel m_scanner_channel = null;
		public readonly ArithmeticModel[] m_number_of_returns = new ArithmeticModel[16];
		public ArithmeticModel m_return_number_gps_same = null;
		public readonly ArithmeticModel[] m_return_number = new ArithmeticModel[16];
		public IntegerCompressor ic_dX = null;
		public IntegerCompressor ic_dY = null;
		public IntegerCompressor ic_Z = null;

		public readonly ArithmeticModel[] m_classification = new ArithmeticModel[64];
		public readonly ArithmeticModel[] m_flags = new ArithmeticModel[64];
		public readonly ArithmeticModel[] m_user_data = new ArithmeticModel[64];

		public IntegerCompressor ic_intensity = null;
		public IntegerCompressor ic_scan_angle = null;
		public IntegerCompressor ic_point_source_ID = null;

		// GPS time stuff
		public uint last = 0, next = 0;
		public readonly U64I64F64[] last_gpstime = new U64I64F64[4];
		public readonly int[] last_gpstime_diff = new int[4];
		public readonly int[] multi_extreme_counter = new int[4];

		public ArithmeticModel m_gpstime_multi = null;
		public ArithmeticModel m_gpstime_0diff = null;
		public IntegerCompressor ic_gpstime = null;
	}

	class LAScontextRGB14
	{
		public bool unused = false;

		public readonly ushort[] last_item = new ushort[3];

		public ArithmeticModel m_byte_used = null;
		public ArithmeticModel m_rgb_diff_0 = null;
		public ArithmeticModel m_rgb_diff_1 = null;
		public ArithmeticModel m_rgb_diff_2 = null;
		public ArithmeticModel m_rgb_diff_3 = null;
		public ArithmeticModel m_rgb_diff_4 = null;
		public ArithmeticModel m_rgb_diff_5 = null;
	}

	class LAScontextRGBNIR14
	{
		public bool unused = false;

		public readonly ushort[] last_item = new ushort[4];

		public ArithmeticModel m_rgb_bytes_used = null;
		public ArithmeticModel m_rgb_diff_0 = null;
		public ArithmeticModel m_rgb_diff_1 = null;
		public ArithmeticModel m_rgb_diff_2 = null;
		public ArithmeticModel m_rgb_diff_3 = null;
		public ArithmeticModel m_rgb_diff_4 = null;
		public ArithmeticModel m_rgb_diff_5 = null;

		public ArithmeticModel m_nir_bytes_used = null;
		public ArithmeticModel m_nir_diff_0 = null;
		public ArithmeticModel m_nir_diff_1 = null;
	}

	class LAScontextWAVEPACKET14
	{
		public bool unused = false;

		public readonly byte[] last_item = new byte[29];
		public int last_diff_32 = 0;
		public uint sym_last_offset_diff = 0;

		public ArithmeticModel m_packet_index = null;
		public readonly ArithmeticModel[] m_offset_diff = new ArithmeticModel[4];
		public IntegerCompressor ic_offset_diff = null;
		public IntegerCompressor ic_packet_size = null;
		public IntegerCompressor ic_return_point = null;
		public IntegerCompressor ic_xyz = null;
	}

	class LAScontextBYTE14
	{
		public bool unused = false;

		public byte[] last_item;

		public ArithmeticModel[] m_bytes;
	}

	class Laszip_Common_v3
	{
		// for LAS points with correctly populated return numbers (1 <= r <= n) and
		// number of returns of given pulse (1 <= n <= 15) the return mapping that
		// serializes the possible combinations into one number should be the following
		//
		//  { ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,   0, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,   1,   2, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,   3,   4,   5, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,   6,   7,   8,   9, ---, ---, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,  10,  11,  12,  13,  14, ---, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,  15,  16,  17,  18,  19,  20, ---, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,  21,  22,  23,  24,  25,  26,  27, ---, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,  28,  29,  30,  31,  32,  33,  34,  35, ---, ---, ---, ---, ---, ---, --- },
		//  { ---,  36,  37,  38,  39,  40,  41,  42,  43,  44, ---, ---, ---, ---, ---, --- },
		//  { ---,  45,  46,  47,  48,  49,  50,  51,  52,  53,  54, ---, ---, ---, ---, --- },
		//  { ---,  55,  56,  57,  58,  59,  60,  61,  62,  63,  64,  65, ---, ---, ---, --- },
		//  { ---,  66,  67,  68,  69,  70,  71,  72,  73,  74,  75,  76,  77, ---, ---, --- },
		//  { ---,  78,  89,  80,  81,  82,  83,  84,  85,  86,  87,  88,  89,  90, ---, --- },
		//  { ---,  91,  92,  93,  94,  95,  96,  97,  98,  99, 100, 101, 102, 103, 104, --- },
		//  { ---, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119 }
		//
		// we drastically simplify the number of return combinations that we want to distinguish
		// down to 16 as higher returns will not have significant entropy differences
		//
		//  { --, --, --, --, --, --, --, --, --, --, --, --, --, --, --, -- },
		//  { --,  0, --, --, --, --, --, --, --, --, --, --, --, --, --, -- },
		//  { --,  1,  2, --, --, --, --, --, --, --, --, --, --, --, --, -- },
		//  { --,  3,  4,  5, --, --, --, --, --, --, --, --, --, --, --, -- },
		//  { --,  6,  7,  8,  9, --, --, --, --, --, --, --, --, --, --, -- },
		//  { --, 10, 11, 12, 13, 14, --, --, --, --, --, --, --, --, --, -- },
		//  { --, 10, 11, 12, 13, 14, 15, --, --, --, --, --, --, --, --, -- },
		//  { --, 10, 11, 12, 12, 13, 14, 15, --, --, --, --, --, --, --, -- },
		//  { --, 10, 11, 12, 12, 13, 13, 14, 15, --, --, --, --, --, --, -- },
		//  { --, 10, 11, 11, 12, 12, 13, 13, 14, 15, --, --, --, --, --, -- },
		//  { --, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, --, --, --, --, -- },
		//  { --, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, --, --, --, -- },
		//  { --, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, --, --, -- },
		//  { --, 10, 10, 11, 11, 12, 12, 12, 13, 13, 14, 14, 15, 15, --, -- },
		//  { --, 10, 10, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 15, 15, -- },
		//  { --, 10, 10, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15 }
		//
		// however, as some files start the numbering of r and n with 0, only have return counts
		// r, only have number of return per pulse n, or mix up position of r and n, we complete
		// the table to also map those "undesired" r and n combinations to different contexts
		//internal static readonly byte[,] number_return_map_4bit =
		//{
		//	{ 15, 14, 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0 },
		//	{ 14,  0,  1,  3,  6, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 },
		//	{ 13,  1,  2,  4,  7, 11, 11, 11, 11, 11, 11, 10, 10, 10, 10, 10 },
		//	{ 12,  3,  4,  5,  8, 12, 12, 12, 12, 11, 11, 11, 11, 11, 11, 11 },
		//	{ 11,  6,  7,  8,  9, 13, 13, 12, 12, 12, 12, 11, 11, 11, 11, 11 },
		//	{ 10, 10, 11, 12, 13, 14, 14, 13, 13, 12, 12, 12, 12, 12, 12, 12 },
		//	{  9, 10, 11, 12, 13, 14, 15, 14, 13, 13, 13, 12, 12, 12, 12, 12 },
		//	{  8, 10, 11, 12, 12, 13, 14, 15, 14, 13, 13, 13, 13, 12, 12, 12 },
		//	{  7, 10, 11, 12, 12, 13, 13, 14, 15, 14, 14, 13, 13, 13, 13, 13 },
		//	{  6, 10, 11, 11, 12, 12, 13, 13, 14, 15, 14, 14, 14, 13, 13, 13 },
		//	{  5, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 14, 14, 14, 13, 13 },
		//	{  4, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 14, 14, 14 },
		//	{  3, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 15, 14, 14 },
		//	{  2, 10, 10, 11, 11, 12, 12, 12, 13, 13, 14, 14, 15, 15, 15, 14 },
		//	{  1, 10, 10, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 15, 15, 15 },
		//	{  0, 10, 10, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15 }
		//};
		// simplify down to 10 contexts
		//internal static readonly byte[,] number_return_map_10ctx = 
		//{
		//	{  0,  1,  2,  3,  4,  5,  6,  7,  8,  9,  9,  9,  9,  9,  9,  9 },
		//	{  1,  0,  1,  3,  6,  6,  6,  6,  6,  6,  6,  6,  6,  6,  6,  6 },
		//	{  2,  1,  2,  4,  7,  7,  7,  7,  7,  7,  7,  6,  6,  6,  6,  6 },
		//	{  3,  3,  4,  5,  8,  7,  7,  7,  7,  7,  7,  7,  7,  7,  7,  7 },
		//	{  4,  6,  7,  8,  9,  8,  8,  7,  7,  7,  7,  7,  7,  7,  7,  7 },
		//	{  5,  6,  7,  7,  8,  9,  8,  8,  8,  7,  7,  7,  7,  7,  7,  7 },
		//	{  6,  6,  7,  7,  8,  8,  9,  8,  8,  8,  8,  7,  7,  7,  7,  7 },
		//	{  7,  6,  7,  7,  7,  8,  8,  9,  8,  8,  8,  8,  8,  7,  7,  7 },
		//	{  8,  6,  7,  7,  7,  8,  8,  8,  9,  8,  8,  8,  8,  8,  8,  8 },
		//	{  9,  6,  7,  7,  7,  7,  8,  8,  8,  9,  8,  8,  8,  8,  8,  8 },
		//	{  9,  6,  7,  7,  7,  7,  8,  8,  8,  8,  9,  8,  8,  8,  8,  8 },
		//	{  9,  6,  6,  7,  7,  7,  7,  8,  8,  8,  8,  9,  9,  8,  8,  8 },
		//	{  9,  6,  6,  7,  7,  7,  7,  8,  8,  8,  8,  9,  9,  9,  8,  8 },
		//	{  9,  6,  6,  7,  7,  7,  7,  7,  8,  8,  8,  8,  9,  9,  9,  8 },
		//	{  9,  6,  6,  7,  7,  7,  7,  7,  8,  8,  8,  8,  8,  9,  9,  9 },
		//	{  9,  6,  6,  7,  7,  7,  7,  7,  8,  8,  8,  8,  8,  8,  9,  9 }
		//};
		// simplify even further down to 6 contexts
		internal static readonly byte[,] number_return_map_6ctx =
		{
			{  0,  1,  2,  3,  4,  5,  3,  4,  4,  5,  5,  5,  5,  5,  5,  5 },
			{  1,  0,  1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3 },
			{  2,  1,  2,  4,  4,  4,  4,  4,  4,  4,  4,  3,  3,  3,  3,  3 },
			{  3,  3,  4,  5,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4 },
			{  4,  3,  4,  4,  5,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4 },
			{  5,  3,  4,  4,  4,  5,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4 },
			{  3,  3,  4,  4,  4,  4,  5,  4,  4,  4,  4,  4,  4,  4,  4,  4 },
			{  4,  3,  4,  4,  4,  4,  4,  5,  4,  4,  4,  4,  4,  4,  4,  4 },
			{  4,  3,  4,  4,  4,  4,  4,  4,  5,  4,  4,  4,  4,  4,  4,  4 },
			{  5,  3,  4,  4,  4,  4,  4,  4,  4,  5,  4,  4,  4,  4,  4,  4 },
			{  5,  3,  4,  4,  4,  4,  4,  4,  4,  4,  5,  4,  4,  4,  4,  4 },
			{  5,  3,  3,  4,  4,  4,  4,  4,  4,  4,  4,  5,  5,  4,  4,  4 },
			{  5,  3,  3,  4,  4,  4,  4,  4,  4,  4,  4,  5,  5,  5,  4,  4 },
			{  5,  3,  3,  4,  4,  4,  4,  4,  4,  4,  4,  4,  5,  5,  5,  4 },
			{  5,  3,  3,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  5,  5,  5 },
			{  5,  3,  3,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  5,  5 }
		};

		// for LAS points with return number (1 <= r <= n) and a number of returns
		// of given pulse (1 <= n <= 15) the level of penetration counted in number
		// of returns should really simply be n - r with all invalid combinations
		// being mapped to 15 like shown below
		//
		//  {  0, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  0, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  1,  0, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  2,  1,  0, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  3,  2,  1,  0, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  4,  3,  2,  1,  0, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  5,  4,  3,  2,  1,  0, 15, 15, 15, 15, 15, 15, 15, 15, 15 },
		//  { 15,  6,  5,  4,  3,  2,  1,  0, 15, 15, 15, 15, 15, 15, 15, 15 }
		//  { 15,  7,  6,  5,  4,  3,  2,  1,  0, 15, 15, 15, 15, 15, 15, 15 }
		//  { 15,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15, 15, 15, 15, 15, 15 }
		//  { 15,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15, 15, 15, 15, 15 }
		//  { 15, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15, 15, 15, 15 }
		//  { 15, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15, 15, 15 }
		//  { 15, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15, 15 }
		//  { 15, 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0, 15 }
		//  { 15, 14, 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0 }
		//
		// however, some files start the numbering of r and n with 0, only have
		// return counts r, or only have number of returns of given pulse n, or
		// mix up the position of r and n. we therefore "complete" the table to
		// also map those "undesired" r & n combinations to different contexts.
		//
		// We also stop the enumeration of the levels of penetration at 7 and
		// map all higher penetration levels also to 7 in order to keep the total
		// number of contexts reasonably small.
		//
		//internal static readonly byte[,] number_return_level_4bit = 
		//{
		//	{  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 },
		//	{  1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14 },
		//	{  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13 },
		//	{  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12 },
		//	{  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11 },
		//	{  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10 },
		//	{  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9 },
		//	{  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  8 },
		//	{  8,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7 },
		//	{  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6 },
		//	{ 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5 },
		//	{ 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4 },
		//	{ 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3 },
		//	{ 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2 },
		//	{ 14, 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0,  1 },
		//	{ 15, 14, 13, 12, 11, 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  0 }
		//};

		// simplify down to 8 contexts
		// FunFact: number_return_level_8ctx[r, n] == Math.Min(Math.Abs(r-n), 7);
		internal static readonly byte[,] number_return_level_8ctx =
		{
			{  0,  1,  2,  3,  4,  5,  6,  7,  7,  7,  7,  7,  7,  7,  7,  7 },
			{  1,  0,  1,  2,  3,  4,  5,  6,  7,  7,  7,  7,  7,  7,  7,  7 },
			{  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  7,  7,  7,  7,  7,  7 },
			{  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  7,  7,  7,  7,  7 },
			{  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  7,  7,  7,  7 },
			{  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  7,  7,  7 },
			{  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  7,  7 },
			{  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7,  7 },
			{  7,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6,  7 },
			{  7,  7,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5,  6 },
			{  7,  7,  7,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4,  5 },
			{  7,  7,  7,  7,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3,  4 },
			{  7,  7,  7,  7,  7,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2,  3 },
			{  7,  7,  7,  7,  7,  7,  7,  6,  5,  4,  3,  2,  1,  0,  1,  2 },
			{  7,  7,  7,  7,  7,  7,  7,  7,  6,  5,  4,  3,  2,  1,  0,  1 },
			{  7,  7,  7,  7,  7,  7,  7,  7,  7,  6,  5,  4,  3,  2,  1,  0 }
		};
	}
}
