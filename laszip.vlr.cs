//===============================================================================
//
//  FILE:  laszip.vlr.cs
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

namespace LASzip.Net
{
	public partial class laszip
	{
		public class vlr
		{
			public ushort reserved;
			public readonly byte[] user_id = new byte[16];
			public ushort record_id;
			public ushort record_length_after_header;
			public readonly byte[] description = new byte[32];
			public byte[] data;
		}
	}
}
