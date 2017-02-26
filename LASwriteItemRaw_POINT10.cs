//===============================================================================
//
//  FILE:  laswriteitemraw_point10.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemRaw for POINT10 items.
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
	class LASwriteItemRaw_POINT10 : LASwriteItemRaw
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		struct LAStempWritePoint10
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

		public LASwriteItemRaw_POINT10() { }

		public unsafe override bool write(laszip.point item)
		{
			fixed(byte* pBuffer=buffer)
			{
				LAStempWritePoint10* p10=(LAStempWritePoint10*)pBuffer;
				p10->x=item.X;
				p10->y=item.Y;
				p10->z=item.Z;
				p10->intensity=item.intensity;
				p10->flags=item.flags;
				p10->classification_and_classification_flags = item.classification_and_classification_flags;
				p10->scan_angle_rank=item.scan_angle_rank;
				p10->user_data=item.user_data;
				p10->point_source_ID=item.point_source_ID;
			}

			try
			{
				outstream.Write(buffer, 0, 20);
			}
			catch
			{
				return false;
			}

			return true;
		}

		byte[] buffer=new byte[20];
	}
}
