//===============================================================================
//
//  FILE:  mydefs.cs
//
//  CONTENTS:
//
//    Basic data type definitions and operations to be robust across platforms.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2012, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2014 by Shinta <shintadono@googlemail.com>
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
	class MyDefs
	{
		const int U8_MAX_PLUS_ONE=0x0100; // 256
		const int U16_MAX_PLUS_ONE=0x00010000; // 65536
		const long U32_MAX_PLUS_ONE=0x0000000100000000; // 4294967296

		public static int U8_FOLD(int n)
		{
			return n<byte.MinValue?(n+U8_MAX_PLUS_ONE):(n>byte.MaxValue?(n-U8_MAX_PLUS_ONE):n);
		}

		public static sbyte I8_CLAMP(int n)
		{
			return n<=sbyte.MinValue?sbyte.MinValue:(n>=sbyte.MaxValue?sbyte.MaxValue:(sbyte)n);
		}

		public static byte U8_CLAMP(int n)
		{
			return n<=byte.MinValue?byte.MinValue:(n>=byte.MaxValue?byte.MaxValue:(byte)n);
		}

		public static short I16_CLAMP(int n)
		{
			return n<=short.MinValue?short.MinValue:(n>=short.MaxValue?short.MaxValue:(short)n);
		}

		public static ushort U16_CLAMP(int n)
		{
			return n<=ushort.MinValue?ushort.MinValue:(n>=ushort.MaxValue?ushort.MaxValue:(ushort)n);
		}

		//#define I32_CLAMP(n)    (((n) <= I32_MIN) ? I32_MIN : (((n) >= I32_MAX) ? I32_MAX : ((I32)(n))))
		//#define U32_CLAMP(n)    (((n) <= U32_MIN) ? U32_MIN : (((n) >= U32_MAX) ? U32_MAX : ((U32)(n))))

		//#define I8_QUANTIZE(n) (((n) >= 0) ? (I8)((n)+0.5f) : (I8)((n)-0.5f))
		//#define U8_QUANTIZE(n) (((n) >= 0) ? (U8)((n)+0.5f) : (U8)(0))

		public static short I16_QUANTIZE(double n)
		{
			return (short)(n>=0?n+0.5:n-0.5);
		}

		//#define U16_QUANTIZE(n) (((n) >= 0) ? (U16)((n)+0.5f) : (U16)(0))

		public static int I32_QUANTIZE(double n)
		{
			return (int)(n>=0?n+0.5:n-0.5);
		}

		//#define U32_QUANTIZE(n) (((n) >= 0) ? (U32)((n)+0.5f) : (U32)(0))

		//#define I64_QUANTIZE(n) (((n) >= 0) ? (I64)((n)+0.5f) : (I64)((n)-0.5f))
		//#define U64_QUANTIZE(n) (((n) >= 0) ? (U64)((n)+0.5f) : (U64)(0))

		public static short I16_FLOOR(double n)
		{
			return (short)n>n?(short)((short)n-1):(short)n;
		}

		public static int I32_FLOOR(double n)
		{
			return (int)n>n?(int)n-1:(int)n;
		}

		public static long I64_FLOOR(double n)
		{
			return (long)n>n?(long)n-1:(long)n;
		}

		//#define I16_CEIL(n) ((((I16)(n)) < (n)) ? (((I16)(n))+1) : ((I16)(n)))
		//#define I32_CEIL(n) ((((I32)(n)) < (n)) ? (((I32)(n))+1) : ((I32)(n)))
		//#define I64_CEIL(n) ((((I64)(n)) < (n)) ? (((I64)(n))+1) : ((I64)(n)))

		//#define I8_FITS_IN_RANGE(n) (((n) >= I8_MIN) && ((n) <= I8_MAX) ? TRUE : FALSE)
		//#define U8_FITS_IN_RANGE(n) (((n) >= U8_MIN) && ((n) <= U8_MAX) ? TRUE : FALSE)
		//#define I16_FITS_IN_RANGE(n) (((n) >= I16_MIN) && ((n) <= I16_MAX) ? TRUE : FALSE)
		//#define U16_FITS_IN_RANGE(n) (((n) >= U16_MIN) && ((n) <= U16_MAX) ? TRUE : FALSE)
	}
}
