//===============================================================================
//
//  FILE:  laswriteitemraw.cs
//
//  CONTENTS:
//
//    Common interface for all classes that write the items that compose a point.
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

using System.IO;

namespace LASzip.Net
{
	abstract class LASwriteItemRaw : LASwriteItem
	{
		public LASwriteItemRaw()
		{
			outstream=null;
		}

		public bool init(Stream outstream)
		{
			if(outstream==null) return false;
			this.outstream=outstream;
			return true;
		}

		protected Stream outstream=null;
	}
}
