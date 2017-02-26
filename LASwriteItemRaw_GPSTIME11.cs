//===============================================================================
//
//  FILE:  laswriteitemraw_gpstime11.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemRaw for GPSTIME11 items.
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

using System;

namespace LASzip.Net
{
	class LASwriteItemRaw_GPSTIME11 : LASwriteItemRaw
	{
		public LASwriteItemRaw_GPSTIME11() { }

		public override bool write(laszip.point item)
		{
			try
			{
				outstream.Write(BitConverter.GetBytes(item.gps_time), 0, 8);
			}
			catch
			{
				return false;
			}

			return true;
		}
	}
}
