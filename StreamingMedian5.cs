//===============================================================================
//
//  FILE:  streamingmedian5.cs
//
//  CONTENTS:
//
//    Common defines and functionalities for version 2 of LASitemReadCompressed
//    and LASitemwriteCompressed.
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
	struct StreamingMedian5
	{
		public int values0;
		public int values1;
		public int values2;
		public int values3;
		public int values4;
		public bool low;

		public void init()
		{
			values0=values1=values2=values3=values4=0;
			low=false;
		}

		public void add(int v)
		{
			if(!low)
			{
				if(v<values2)
				{
					values4=values3;
					values3=values2;
					if(v<values0)
					{
						values2=values1;
						values1=values0;
						values0=v;
					}
					else if(v<values1)
					{
						values2=values1;
						values1=v;
					}
					else
					{
						values2=v;
					}
				}
				else
				{
					if(v<values3)
					{
						values4=values3;
						values3=v;
					}
					else
					{
						values4=v;
					}
					low=true;
				}
			}
			else
			{
				if(values2<v)
				{
					values0=values1;
					values1=values2;
					if(values4<v)
					{
						values2=values3;
						values3=values4;
						values4=v;
					}
					else if(values3<v)
					{
						values2=values3;
						values3=v;
					}
					else
					{
						values2=v;
					}
				}
				else
				{
					if(values1<v)
					{
						values0=values1;
						values1=v;
					}
					else
					{
						values0=v;
					}
					low=false;
				}
			}
		}

		public int get()
		{
			return values2;
		}
	}
}
