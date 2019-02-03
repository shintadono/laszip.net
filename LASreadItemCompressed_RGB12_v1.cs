//===============================================================================
//
//  FILE:  lasreaditemcompressed_rgb12_v1.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for RGB12 items (version 1).
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2014-2019 by Shinta <shintadono@googlemail.com>
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

namespace LASzip.Net
{
	class LASreadItemCompressed_RGB12_v1 : LASreadItemCompressed
	{
		public LASreadItemCompressed_RGB12_v1(ArithmeticDecoder dec)
		{
			// set decoder
			Debug.Assert(dec != null);
			this.dec = dec;

			// create models and integer compressors
			m_byte_used = dec.createSymbolModel(64);
			ic_rgb = new IntegerCompressor(dec, 8, 6);
		}

		public override bool init(laszip_point item, ref uint context) // context is unused
		{
			// init state

			// init models and integer compressors
			dec.initSymbolModel(m_byte_used);
			ic_rgb.initDecompressor();

			// init last item
			last_r = item.rgb[0];
			last_g = item.rgb[1];
			last_b = item.rgb[2];

			return true;
		}

		public override void read(laszip_point item, ref uint context) // context is unused
		{
			uint sym = dec.decodeSymbol(m_byte_used);

			ushort[] item_rgb = item.rgb;

			if ((sym & (1 << 0)) != 0) item_rgb[0] = (ushort)ic_rgb.decompress(last_r & 255, 0);
			else item_rgb[0] = (ushort)(last_r & 0xFF);

			if ((sym & (1 << 1)) != 0) item_rgb[0] |= (ushort)(((ushort)ic_rgb.decompress(last_r >> 8, 1)) << 8);
			else item_rgb[0] |= (ushort)(last_r & 0xFF00);

			if ((sym & (1 << 2)) != 0) item_rgb[1] = (ushort)ic_rgb.decompress(last_g & 255, 2);
			else item_rgb[1] = (ushort)(last_g & 0xFF);

			if ((sym & (1 << 3)) != 0) item_rgb[1] |= (ushort)(((ushort)ic_rgb.decompress(last_g >> 8, 3)) << 8);
			else item_rgb[1] |= (ushort)(last_g & 0xFF00);

			if ((sym & (1 << 4)) != 0) item_rgb[2] = (ushort)ic_rgb.decompress(last_b & 255, 4);
			else item_rgb[2] = (ushort)(last_b & 0xFF);

			if ((sym & (1 << 5)) != 0) item_rgb[2] |= (ushort)(((ushort)ic_rgb.decompress(last_b >> 8, 5)) << 8);
			else item_rgb[2] |= (ushort)(last_b & 0xFF00);

			last_r = item_rgb[0];
			last_g = item_rgb[1];
			last_b = item_rgb[2];
		}

		ArithmeticDecoder dec;
		ushort last_r, last_g, last_b;

		ArithmeticModel m_byte_used;
		IntegerCompressor ic_rgb;
	}
}
