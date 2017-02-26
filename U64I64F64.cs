//===============================================================================
//
//  FILE:  u64i64f64.cs
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

using System.Runtime.InteropServices;

namespace LASzip.Net
{
	[StructLayout(LayoutKind.Explicit, Pack=1)]
	struct U64I64F64
	{
		[FieldOffset(0)]
		public ulong u64;
		[FieldOffset(0)]
		public long i64;
		[FieldOffset(0)]
		public double f64;
	}
}
