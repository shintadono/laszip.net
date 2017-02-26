//===============================================================================
//
//  FILE:  lasitem.cs
//
//  CONTENTS:
//
//    Contains the LASitem struct as well as the IDs of the currently supported
//    entropy coding scheme
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

namespace LASzip.Net
{
	class LASitem
	{
		public enum Type { BYTE=0, SHORT, INT, LONG, FLOAT, DOUBLE, POINT10, GPSTIME11, RGB12, WAVEPACKET13, POINT14, RGBNIR14 }
		public Type type;

		public ushort size;
		public ushort version;

		public bool is_type(LASitem.Type t)
		{
			if(t!=type) return false;
			switch(t)
			{
				case Type.POINT10: if(size!=20) return false;
					break;
				case Type.POINT14: if(size!=30) return false;
					break;
				case Type.GPSTIME11: if(size!=8) return false;
					break;
				case Type.RGB12: if(size!=6) return false;
					break;
				case Type.WAVEPACKET13: if(size!=29) return false;
					break;
				case Type.BYTE: if(size<1) return false;
					break;
				default: return false;
			}
			return true;
		}

		public string get_name()
		{
			switch(type)
			{
				case Type.POINT10: return "POINT10";
				case Type.POINT14: return "POINT14";
				case Type.GPSTIME11: return "GPSTIME11";
				case Type.RGB12: return "RGB12";
				case Type.WAVEPACKET13: return "WAVEPACKET13";
				case Type.BYTE: return "BYTE";
				default: break;
			}
			return null;
		}
	}
}
