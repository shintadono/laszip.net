//===============================================================================
//
//  FILE:  lasquantizer.cs
//
//  CONTENTS:
//
//    This class assists with converting between a fixed-point notation based on
//    scaled integers plus large offsets and standard double-precision floating-
//    point numbers.For efficieny in storage and uniform precision (far from the
//    origin) the LAS format stores all point coordinates as scaled and offset
//    32-bit integers.
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
	struct LASquantizer
	{
		public double x_scale_factor;
		public double y_scale_factor;
		public double z_scale_factor;
		public double x_offset;
		public double y_offset;
		public double z_offset;

		public double get_x(int X) { return x_scale_factor * X + x_offset; }
		public double get_y(int Y) { return y_scale_factor * Y + y_offset; }
		public double get_z(int Z) { return z_scale_factor * Z + z_offset; }

		public int get_X(double x) { if (x >= x_offset) return (int)((x - x_offset) / x_scale_factor + 0.5); else return (int)((x - x_offset) / x_scale_factor - 0.5); }
		public int get_Y(double y) { if (y >= y_offset) return (int)((y - y_offset) / y_scale_factor + 0.5); else return (int)((y - y_offset) / y_scale_factor - 0.5); }
		public int get_Z(double z) { if (z >= z_offset) return (int)((z - z_offset) / z_scale_factor + 0.5); else return (int)((z - z_offset) / z_scale_factor - 0.5); }

		LASquantizer(double factor = 0.01)
		{
			x_scale_factor = factor;
			y_scale_factor = factor;
			z_scale_factor = factor;
			x_offset = 0.0;
			y_offset = 0.0;
			z_offset = 0.0;
		}
	}
}
