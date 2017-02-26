//===============================================================================
//
//  FILE:  lasreaditemraw_point10.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemRaw for POINT10 items.
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

using System.IO;
using System.Runtime.InteropServices;

namespace LASzip.Net
{
	class LASreadItemRaw_POINT10 : LASreadItemRaw
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		struct LAStempReadPoint10
		{
			public int x;
			public int y;
			public int z;
			public ushort intensity;
			public byte flags;
			public byte classification_and_classification_flags;
			public sbyte scan_angle_rank;
			public byte user_data;
			public ushort point_source_ID;
		}

		public LASreadItemRaw_POINT10() { }
		public unsafe override void read(laszip.point item)
		{
			if(instream.Read(buffer, 0, 20)!=20) throw new EndOfStreamException();

			fixed(byte* pBuffer=buffer)
			{
				LAStempReadPoint10* p10=(LAStempReadPoint10*)pBuffer;
				item.X=p10->x;
				item.Y=p10->y;
				item.Z=p10->z;
				item.intensity=p10->intensity;
				item.flags=p10->flags;
				item.classification_and_classification_flags = p10->classification_and_classification_flags;
				item.scan_angle_rank=p10->scan_angle_rank;
				item.user_data=p10->user_data;
				item.point_source_ID=p10->point_source_ID;
			}
		}

		byte[] buffer=new byte[20];
	}
}
