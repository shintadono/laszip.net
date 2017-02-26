//===============================================================================
//
//  FILE:  laszip.geokey.cs
//
//  CONTENTS:
//
//    C# port of a simple DLL interface to LASzip.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2017, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2014-2017 by Shinta <shintadono@googlemail.com>
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
	public partial class laszip
	{
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct geokey
		{
			public ushort key_id;
			public ushort tiff_tag_location;
			public ushort count;
			public ushort value_offset;
		}
	}
}
