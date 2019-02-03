//===============================================================================
//
//  FILE:  laswriteitemcompressed_rgb14_v4.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for RGB14 items (version 4).
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

using System;
using System.Diagnostics;
using System.IO;

namespace LASzip.Net
{
	class LASwriteItemCompressed_RGB14_v4 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_RGB14_v4(ArithmeticEncoder enc)
		{
			// not used as a encoder. just gives access to outstream
			Debug.Assert(enc != null);
			this.enc = enc;

			// zero outstreams and encoders
			outstream_RGB = null;
			enc_RGB = null;

			// zero num_bytes and init booleans
			num_bytes_RGB = 0;
			changed_RGB = false;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_byte_used = null;
			}
			current_context = 0;
		}

		public override bool init(laszip_point item, ref uint context)
		{
			// on the first init create outstreams and encoders
			if (outstream_RGB == null)
			{
				// create outstreams
				outstream_RGB = new MemoryStream();

				// create layer encoders
				enc_RGB = new ArithmeticEncoder();
			}
			else
			{
				// otherwise just seek back
				outstream_RGB.Seek(0, SeekOrigin.Begin);
			}

			// init layer encoders
			enc_RGB.init(outstream_RGB);

			// set changed booleans to FALSE
			changed_RGB = false;

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = context; // all other items use context set by POINT14 writer

			// create and init entropy models and integer compressors (and init contect from item)
			createAndInitModelsAndCompressors(current_context, item.rgb);

			return true;
		}

		public override bool write(laszip_point item, ref uint context)
		{
			// get last
			ushort[] last_item = contexts[current_context].last_item;

			// check for context switch
			if (current_context != context)
			{
				current_context = context; // all other items use context set by POINT14 writer
				if (contexts[current_context].unused)
				{
					createAndInitModelsAndCompressors(current_context, last_item);
				}
				last_item = contexts[current_context].last_item;
			}

			// compress
			int diff_l = 0;
			int diff_h = 0;

			bool sym0 = (last_item[0] & 0x00FF) != (item.rgb[0] & 0x00FF);
			bool sym1 = (last_item[0] & 0xFF00) != (item.rgb[0] & 0xFF00);
			bool sym2 = (last_item[1] & 0x00FF) != (item.rgb[1] & 0x00FF);
			bool sym3 = (last_item[1] & 0xFF00) != (item.rgb[1] & 0xFF00);
			bool sym4 = (last_item[2] & 0x00FF) != (item.rgb[2] & 0x00FF);
			bool sym5 = (last_item[2] & 0xFF00) != (item.rgb[2] & 0xFF00);
			bool sym6 =
				((item.rgb[0] & 0x00FF) != (item.rgb[1] & 0x00FF)) ||
				((item.rgb[0] & 0x00FF) != (item.rgb[2] & 0x00FF)) ||
				((item.rgb[0] & 0xFF00) != (item.rgb[1] & 0xFF00)) ||
				((item.rgb[0] & 0xFF00) != (item.rgb[2] & 0xFF00));

			uint sym = sym0 ? 1u : 0u;
			sym |= sym1 ? 2u : 0u;
			sym |= sym2 ? 4u : 0u;
			sym |= sym3 ? 8u : 0u;
			sym |= sym4 ? 16u : 0u;
			sym |= sym5 ? 32u : 0u;
			sym |= sym6 ? 64u : 0u;

			enc_RGB.encodeSymbol(contexts[current_context].m_byte_used, sym);
			if (sym0)
			{
				diff_l = ((int)(item.rgb[0] & 255)) - (last_item[0] & 255);
				enc_RGB.encodeSymbol(contexts[current_context].m_rgb_diff_0, (uint)MyDefs.U8_FOLD(diff_l));
			}
			if (sym1)
			{
				diff_h = ((int)(item.rgb[0] >> 8)) - (last_item[0] >> 8);
				enc_RGB.encodeSymbol(contexts[current_context].m_rgb_diff_1, (uint)MyDefs.U8_FOLD(diff_h));
			}
			if (sym6)
			{
				if (sym2)
				{
					int corr = ((int)(item.rgb[1] & 255)) - MyDefs.U8_CLAMP(diff_l + (last_item[1] & 255));
					enc_RGB.encodeSymbol(contexts[current_context].m_rgb_diff_2, (uint)MyDefs.U8_FOLD(corr));
				}
				if (sym4)
				{
					diff_l = (diff_l + (item.rgb[1] & 255) - (last_item[1] & 255)) / 2;
					int corr = ((int)(item.rgb[2] & 255)) - MyDefs.U8_CLAMP(diff_l + (last_item[2] & 255));
					enc_RGB.encodeSymbol(contexts[current_context].m_rgb_diff_4, (uint)MyDefs.U8_FOLD(corr));
				}
				if (sym3)
				{
					int corr = ((int)(item.rgb[1] >> 8)) - MyDefs.U8_CLAMP(diff_h + (last_item[1] >> 8));
					enc_RGB.encodeSymbol(contexts[current_context].m_rgb_diff_3, (uint)MyDefs.U8_FOLD(corr));
				}
				if (sym5)
				{
					diff_h = (diff_h + (item.rgb[1] >> 8) - (last_item[1] >> 8)) / 2;
					int corr = ((int)(item.rgb[2] >> 8)) - MyDefs.U8_CLAMP(diff_h + (last_item[2] >> 8));
					enc_RGB.encodeSymbol(contexts[current_context].m_rgb_diff_5, (uint)MyDefs.U8_FOLD(corr));
				}
			}
			if (sym != 0)
			{
				changed_RGB = true;
			}

			last_item[0] = item.rgb[0];
			last_item[1] = item.rgb[1];
			last_item[2] = item.rgb[2];

			return true;
		}

		public override bool chunk_sizes()
		{
			Stream outstream = enc.getByteStreamOut();

			// finish the encoders
			enc_RGB.done();

			// output the sizes of all layer (i.e.. number of bytes per layer)
			uint num_bytes = 0;
			if (changed_RGB)
			{
				num_bytes = (uint)outstream_RGB.Position;
				num_bytes_RGB += num_bytes;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			return true;
		}

		public override bool chunk_bytes()
		{
			Stream outstream = enc.getByteStreamOut();

			// output the bytes of all layers
			if (changed_RGB)
			{
				outstream.Write(outstream_RGB.GetBuffer(), 0, (int)outstream_RGB.Position);
			}

			return true;
		}

		// not used as a encoder. just gives access to outstream
		ArithmeticEncoder enc;

		MemoryStream outstream_RGB;

		ArithmeticEncoder enc_RGB;

		bool changed_RGB;

		uint num_bytes_RGB;

		uint current_context;
		readonly LAScontextRGB14[] contexts =
		{
			new LAScontextRGB14(),
			new LAScontextRGB14(),
			new LAScontextRGB14(),
			new LAScontextRGB14()
		};

		bool createAndInitModelsAndCompressors(uint context, ushort[] item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models (if needed)
			if (contexts[context].m_byte_used == null)
			{
				contexts[context].m_byte_used = enc_RGB.createSymbolModel(128);
				contexts[context].m_rgb_diff_0 = enc_RGB.createSymbolModel(256);
				contexts[context].m_rgb_diff_1 = enc_RGB.createSymbolModel(256);
				contexts[context].m_rgb_diff_2 = enc_RGB.createSymbolModel(256);
				contexts[context].m_rgb_diff_3 = enc_RGB.createSymbolModel(256);
				contexts[context].m_rgb_diff_4 = enc_RGB.createSymbolModel(256);
				contexts[context].m_rgb_diff_5 = enc_RGB.createSymbolModel(256);
			}

			// then init entropy models
			enc_RGB.initSymbolModel(contexts[context].m_byte_used);
			enc_RGB.initSymbolModel(contexts[context].m_rgb_diff_0);
			enc_RGB.initSymbolModel(contexts[context].m_rgb_diff_1);
			enc_RGB.initSymbolModel(contexts[context].m_rgb_diff_2);
			enc_RGB.initSymbolModel(contexts[context].m_rgb_diff_3);
			enc_RGB.initSymbolModel(contexts[context].m_rgb_diff_4);
			enc_RGB.initSymbolModel(contexts[context].m_rgb_diff_5);

			// init current context from item
			contexts[context].last_item[0] = item[0];
			contexts[context].last_item[1] = item[1];
			contexts[context].last_item[2] = item[2];

			contexts[context].unused = false;

			return true;
		}
	}
}
