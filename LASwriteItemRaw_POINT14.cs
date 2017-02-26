//===============================================================================
//
//  FILE:  laswriteitemraw_point14.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemRaw for POINT14 items.
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
	class LASwriteItemRaw_POINT14 : LASwriteItemRaw
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		struct LAStempWritePoint14
		{
			public int x;
			public int y;
			public int z;
			public ushort intensity;

			//public byte return_number : 4;
			public byte return_number { get { return (byte)(returns&0xF); } set { returns=(byte)((returns&0xF0)|(value&0xF)); } }
			//public byte number_of_returns : 4;
			public byte number_of_returns { get { return (byte)((returns>>4)&0xF); } set { returns=(byte)((returns&0xF)|((value&0xF)<<4)); } }
			public byte returns;

			//public byte classification_flags : 4;
			public byte classification_flags { get { return (byte)(flags&0xF); } set { flags=(byte)((flags&0xF0)|(value&0xF)); } }
			//public byte scanner_channel : 2;
			public byte scanner_channel { get { return (byte)((flags>>4)&3); } set { flags=(byte)((flags&0xCF)|((value&3)<<4)); } }
			//public byte scan_direction_flag : 1;
			public byte scan_direction_flag { get { return (byte)((flags>>6)&1); } set { flags=(byte)((flags&0xBF)|((value&1)<<6)); } }
			//public byte edge_of_flight_line : 1;
			public byte edge_of_flight_line { get { return (byte)((flags>>7)&1); } set { flags=(byte)((flags&0x7F)|((value&1)<<7)); } }
			public byte flags;

			public byte classification;
			public byte user_data;
			public short scan_angle;
			public ushort point_source_ID;
			public double gps_time;
		}

		public LASwriteItemRaw_POINT14() { }

		public unsafe override bool write(laszip.point item)
		{
			fixed(byte* pBuffer=buffer)
			{
				LAStempWritePoint14* p14=(LAStempWritePoint14*)pBuffer;

				p14->x=item.X;
				p14->y=item.Y;
				p14->z=item.Z;
				p14->intensity=item.intensity;
				p14->scan_direction_flag=item.scan_direction_flag;
				p14->edge_of_flight_line=item.edge_of_flight_line;
				p14->classification=(byte)(item.classification_and_classification_flags & 31);
				p14->user_data=item.user_data;
				p14->point_source_ID=item.point_source_ID;

				if(item.extended_point_type!=0)
				{
					p14->classification_flags=(byte)(item.extended_classification_flags|(item.classification_and_classification_flags >> 5));
					if(item.extended_classification>31) p14->classification=item.extended_classification;
					p14->scanner_channel=item.extended_scanner_channel;
					p14->return_number=item.extended_return_number;
					p14->number_of_returns=item.extended_number_of_returns;
					p14->scan_angle=item.extended_scan_angle;
				}
				else
				{
					p14->classification_flags=(byte)(item.classification_and_classification_flags >> 5);
					p14->scanner_channel=0;
					p14->return_number=item.return_number;
					p14->number_of_returns=item.number_of_returns;
					p14->scan_angle=MyDefs.I16_QUANTIZE(item.scan_angle_rank/0.006f);
				}

				p14->gps_time=item.gps_time;
			}

			try
			{
				outstream.Write(buffer, 0, 30);
			}
			catch
			{
				return false;
			}

			return true;
		}

		byte[] buffer=new byte[30];
	}
}
