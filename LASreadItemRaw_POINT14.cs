//===============================================================================
//
//  FILE:  lasreaditemraw_point14.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemRaw for POINT14 items.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - tools to catch reality
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

using System.IO;

namespace LASzip.Net
{
	class LASreadItemRaw_POINT14 : LASreadItemRaw
	{
		public LASreadItemRaw_POINT14() { }

		public unsafe override void read(laszip_point item, ref uint context) // context is unused
		{
			if (!instream.getBytes(buffer, 30)) throw new EndOfStreamException();

			fixed (byte* pBuffer = buffer)
			{
				LASpoint14* p14 = (LASpoint14*)pBuffer;

				item.X = p14->X;
				item.Y = p14->Y;
				item.Z = p14->Z;
				item.intensity = p14->intensity;

				var return_number = (byte)(p14->returns & 0xF);
				var number_of_returns = (byte)((p14->returns >> 4) & 0xF);

				if (number_of_returns > 7)
				{
					if (return_number > 6)
					{
						if (return_number >= number_of_returns)
						{
							item.number_of_returns = 7;
						}
						else
						{
							item.number_of_returns = 6;
						}
					}
					else
					{
						item.return_number = return_number;
					}

					item.number_of_returns = 7;
				}
				else
				{
					item.return_number = return_number;
					item.number_of_returns = number_of_returns;
				}

				item.scan_direction_flag = (byte)((p14->flags >> 6) & 1);
				item.edge_of_flight_line = (byte)((p14->flags >> 7) & 1);
				item.classification_and_classification_flags = (byte)((p14->flags << 5) & 0xE0); // Copy Withheld, Key-point & Synthetic flag.
				if (p14->classification < 32) item.classification_and_classification_flags |= p14->classification;
				item.scan_angle_rank = MyDefs.I8_CLAMP(MyDefs.I16_QUANTIZE(0.006 * p14->scan_angle));
				item.user_data = p14->user_data;
				item.point_source_ID = p14->point_source_ID;
				item.extended_scanner_channel = (byte)((p14->flags >> 4) & 3);
				item.extended_classification_flags = (byte)(p14->flags & 0xF);
				item.extended_classification = p14->classification;
				item.extended_return_number = return_number;
				item.extended_number_of_returns = number_of_returns;
				item.extended_scan_angle = p14->scan_angle;
				item.gps_time = p14->gps_time;
			}
		}

		readonly byte[] buffer = new byte[30];
	}
}
