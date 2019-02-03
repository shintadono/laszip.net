//===============================================================================
//
//  FILE:  laszip.Inventory.cs
//
//  CONTENTS:
//
//    C# port of a simple DLL interface to LASzip.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2017, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2014-2018 by Shinta <shintadono@googlemail.com>
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
	public partial class laszip
	{
		class Inventory
		{
			public bool active { get; private set; } = false;
			public uint number_of_point_records;
			public readonly uint[] number_of_points_by_return = new uint[16];
			public int max_X, min_X, max_Y, min_Y, max_Z, min_Z;

			public void add(laszip_point point)
			{
				number_of_point_records++;
				if (point.extended_point_type != 0)
				{
					number_of_points_by_return[point.extended_return_number]++;
				}
				else
				{
					number_of_points_by_return[point.return_number]++;
				}

				if (active)
				{
					if (point.X < min_X) min_X = point.X;
					else if (point.X > max_X) max_X = point.X;
					if (point.Y < min_Y) min_Y = point.Y;
					else if (point.Y > max_Y) max_Y = point.Y;
					if (point.Z < min_Z) min_Z = point.Z;
					else if (point.Z > max_Z) max_Z = point.Z;
				}
				else
				{
					min_X = max_X = point.X;
					min_Y = max_Y = point.Y;
					min_Z = max_Z = point.Z;
					active = true;
				}
			}
		}
	}
}
