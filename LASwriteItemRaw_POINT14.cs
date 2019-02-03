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
//    (c) 2007-2018, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2014-2019 by Shinta <shintadono@googlemail.com>
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
	class LASwriteItemRaw_POINT14 : LASwriteItemRaw
	{
		public LASwriteItemRaw_POINT14() { }

		public unsafe override bool write(laszip_point item, ref uint context)
		{
			fixed (byte* pBuffer = buffer)
			{
				LASpoint14* p14 = (LASpoint14*)pBuffer;

				p14->X = item.X;
				p14->Y = item.Y;
				p14->Z = item.Z;
				p14->intensity = item.intensity;
				p14->scan_direction_flag = item.scan_direction_flag;
				p14->edge_of_flight_line = item.edge_of_flight_line;
				p14->classification = (byte)(item.classification_and_classification_flags & 31);
				p14->user_data = item.user_data;
				p14->point_source_ID = item.point_source_ID;

				if (item.extended_point_type != 0)
				{
					p14->classification_flags = (byte)((item.extended_classification_flags & 8) | (item.classification_and_classification_flags >> 5));
					if (item.classification == 0) p14->classification = item.extended_classification;
					p14->scanner_channel = item.extended_scanner_channel;
					p14->return_number = item.extended_return_number;
					p14->number_of_returns = item.extended_number_of_returns;
					p14->scan_angle = item.extended_scan_angle;
				}
				else
				{
					p14->classification_flags = (byte)(item.classification_and_classification_flags >> 5);
					p14->scanner_channel = 0;
					p14->return_number = item.return_number;
					p14->number_of_returns = item.number_of_returns;
					p14->scan_angle = MyDefs.I16_QUANTIZE(item.scan_angle_rank / 0.006f);
				}

				p14->gps_time = item.gps_time;
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

		readonly byte[] buffer = new byte[30];
	}
}
