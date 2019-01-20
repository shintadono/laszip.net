//===============================================================================
//
//  FILE:  laswriteitemcompressed_wavepacket14_v4.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for WAVEPACKET14 items (version 4).
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
	class LASwriteItemCompressed_WAVEPACKET14_v4 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_WAVEPACKET14_v4(ArithmeticEncoder enc)
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

		Stream outstream_wavepacket;

		ArithmeticEncoder enc_wavepacket;

		bool changed_wavepacket;

		uint num_bytes_wavepacket;

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
