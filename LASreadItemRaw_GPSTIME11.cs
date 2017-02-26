//===============================================================================
//
//  FILE:  lasreaditemraw_gpstime11.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemRaw for GPSTIME11 items.
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
using System.IO;

namespace LASzip.Net
{
	class LASreadItemRaw_GPSTIME11 : LASreadItemRaw
	{
		public LASreadItemRaw_GPSTIME11() { }

		public override void read(laszip.point item)
		{
			if(instream.Read(buffer, 0, 8)!=8) throw new EndOfStreamException();

			item.gps_time=BitConverter.ToDouble(buffer, 0);
		}

		byte[] buffer=new byte[8];
	}
}
