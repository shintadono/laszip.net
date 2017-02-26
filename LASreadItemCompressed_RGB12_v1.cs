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

using System.Diagnostics;

namespace LASzip.Net
{
	class LASreadItemCompressed_RGB12_v1 : LASreadItemCompressed
	{
		public LASreadItemCompressed_RGB12_v1(ArithmeticDecoder dec)
		{
			// set decoder
			Debug.Assert(dec!=null);
			this.dec=dec;

			// create models and integer compressors
			m_byte_used=dec.createSymbolModel(64);
			ic_rgb=new IntegerCompressor(dec, 8, 6);
		}

		public override bool init(laszip.point item)
		{
			// init state

			// init models and integer compressors
			dec.initSymbolModel(m_byte_used);
			ic_rgb.initDecompressor();

			// init last item
			r=item.rgb[0];
			g=item.rgb[1];
			b=item.rgb[2];

			return true;
		}

		public override void read(laszip.point item)
		{
			uint sym=dec.decodeSymbol(m_byte_used);

			ushort[] item16=item.rgb;

			if((sym&(1<<0))!=0) item16[0]=(ushort)ic_rgb.decompress(r&255, 0);
			else item16[0]=(ushort)(r&0xFF);

			if((sym&(1<<1))!=0) item16[0]|=(ushort)(((ushort)ic_rgb.decompress(r>>8, 1))<<8);
			else item16[0]|=(ushort)(r&0xFF00);

			if((sym&(1<<2))!=0) item16[1]=(ushort)ic_rgb.decompress(g&255, 2);
			else item16[1]=(ushort)(g&0xFF);

			if((sym&(1<<3))!=0) item16[1]|=(ushort)(((ushort)ic_rgb.decompress(g>>8, 3))<<8);
			else item16[1]|=(ushort)(g&0xFF00);

			if((sym&(1<<4))!=0) item16[2]=(ushort)ic_rgb.decompress(b&255, 4);
			else item16[2]=(ushort)(b&0xFF);

			if((sym&(1<<5))!=0) item16[2]|=(ushort)(((ushort)ic_rgb.decompress(b>>8, 5))<<8);
			else item16[2]|=(ushort)(b&0xFF00);

			r=item16[0];
			g=item16[1];
			b=item16[2];
		}

		ArithmeticDecoder dec;
		ushort r, g, b;

		ArithmeticModel m_byte_used;
		IntegerCompressor ic_rgb;
	}
}
