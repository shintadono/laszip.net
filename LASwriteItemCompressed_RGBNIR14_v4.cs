//===============================================================================
//
//  FILE:  laswriteitemcompressed_rgbnir14_v4.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for RGBNIR14 items (version 4).
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2018-2019 by Shinta <shintadono@googlemail.com>
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

using System.Diagnostics;
using System.IO;

namespace LASzip.Net
{
	class LASwriteItemCompressed_RGBNIR14_v4 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_RGBNIR14_v4(ArithmeticEncoder enc)
		{
		}

		public override bool init(laszip.point item, ref uint context)
		{
		}

		public override bool write(laszip.point item, ref uint context)
		{
		}

		public override bool chunk_sizes();
		public override bool chunk_bytes();

		// not used as a encoder. just gives access to outstream
		ArithmeticEncoder enc;

		Stream outstream_RGB;
		Stream outstream_NIR;

		ArithmeticEncoder enc_RGB;
		ArithmeticEncoder enc_NIR;

		bool changed_RGB;
		bool changed_NIR;

		uint num_bytes_RGB;
		uint num_bytes_NIR;

		uint current_context;
		readonly LAScontextPOINT14[] contexts =
		{
			new LAScontextPOINT14(),
			new LAScontextPOINT14(),
			new LAScontextPOINT14(),
			new LAScontextPOINT14()
		};

		bool createAndInitModelsAndCompressors(uint context, laszip.point item);
	}
}
