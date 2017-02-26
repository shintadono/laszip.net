//===============================================================================
//
//  FILE:  lasreaditemraw_rgbnir12.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemRaw for RGBNIR14 items.
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
	class LASreadItemRaw_RGBNIR14 : LASreadItemRaw
	{
		public LASreadItemRaw_RGBNIR14() { }

		public override void read(laszip.point item)
		{
			byte[] buf=new byte[8];
			if(instream.Read(buf, 0, 8)!=8) throw new EndOfStreamException();
			item.rgb[0]=BitConverter.ToUInt16(buf, 0);
			item.rgb[1]=BitConverter.ToUInt16(buf, 2);
			item.rgb[2]=BitConverter.ToUInt16(buf, 4);
			item.rgb[3]=BitConverter.ToUInt16(buf, 6);
		}
	}
}
