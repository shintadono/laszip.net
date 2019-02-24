//===============================================================================
//
//  FILE:  lasreaditemcompressed_rgbnir14_v3.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for RGBNIR14 items (version 3).
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
	class LASreadItemCompressed_RGBNIR14_v3 : LASreadItemCompressed
	{
		public LASreadItemCompressed_RGBNIR14_v3(ArithmeticDecoder dec, LASZIP_DECOMPRESS_SELECTIVE decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL)
		{
			// not used as a decoder. just gives access to instream
			Debug.Assert(dec != null);
			this.dec = dec;

			// zero instreams and decoders
			instream_RGB = null;
			instream_NIR = null;
			dec_RGB = null;
			dec_NIR = null;

			// zero num_bytes and init booleans
			num_bytes_RGB = 0;
			num_bytes_NIR = 0;
			changed_RGB = false;
			changed_NIR = false;

			requested_RGB = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.RGB);
			requested_NIR = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.NIR);

			// init the bytes buffer to zero
			bytes = null;
			num_bytes_allocated = 0;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_rgb_bytes_used = null;
				contexts[c].m_nir_bytes_used = null;
			}
			current_context = 0;
		}

		public override bool chunk_sizes()
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			// read bytes per layer
			if (!instream.get32bits(out num_bytes_RGB)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_NIR)) throw new EndOfStreamException();

			return true;
		}

		public override bool init(laszip_point item, ref uint context) // context is only read
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			// on the first init create instreams and decoders
			if (instream_RGB == null)
			{
				// create decoders
				dec_RGB = new ArithmeticDecoder();
				dec_NIR = new ArithmeticDecoder();
			}

			// how many bytes do we need to read
			int num_bytes = 0;
			if (requested_RGB) num_bytes += num_bytes_RGB;
			if (requested_NIR) num_bytes += num_bytes_NIR;

			// make sure the buffer is sufficiently large
			if (num_bytes > num_bytes_allocated)
			{
				try
				{
					bytes = new byte[num_bytes];
				}
				catch
				{
					return false;
				}
				num_bytes_allocated = num_bytes;
			}

			// load the requested bytes and init the corresponding instreams an decoders
			num_bytes = 0;
			if (requested_RGB)
			{
				if (num_bytes_RGB != 0)
				{
					if (!instream.getBytes(bytes, 0, num_bytes_RGB)) throw new EndOfStreamException();
					instream_RGB = new MemoryStream(bytes, num_bytes, num_bytes_RGB);
					dec_RGB.init(instream_RGB);
					changed_RGB = true;
					num_bytes += num_bytes_RGB;
				}
				else
				{
					instream_RGB = new MemoryStream(0);
					changed_RGB = false;
				}
			}
			else
			{
				if (num_bytes_RGB != 0)
				{
					instream.Seek(num_bytes_RGB, SeekOrigin.Current);
				}
				changed_RGB = false;
			}

			if (requested_NIR)
			{
				if (num_bytes_NIR != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_NIR)) throw new EndOfStreamException();
					instream_NIR = new MemoryStream(bytes, num_bytes, num_bytes_RGB);
					dec_NIR.init(instream_NIR);
					changed_NIR = true;
				}
				else
				{
					instream_NIR = new MemoryStream(0);
					changed_NIR = false;
				}
			}
			else
			{
				if (num_bytes_NIR != 0)
				{
					instream.Seek(num_bytes_NIR, SeekOrigin.Current);
				}
				changed_NIR = false;
			}

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = context; // all other items use context set by POINT14 reader

			// create and init models and decompressors
			createAndInitModelsAndDecompressors(current_context, item.rgb);

			return true;
		}

		public override void read(laszip_point item, ref uint context) // context is only read
		{
			// get last
			ushort[] last_item = contexts[current_context].last_item;

			// check for context switch
			if (current_context != context)
			{
				current_context = context; // all other items use context set by POINT14 reader
				if (contexts[current_context].unused)
				{
					createAndInitModelsAndDecompressors(current_context, last_item);
					last_item = contexts[current_context].last_item;
				}
			}

			////////////////////////////////////////
			// decompress RGB layer
			////////////////////////////////////////
			if (changed_RGB)
			{
				byte corr;
				uint sym = dec_RGB.decodeSymbol(contexts[current_context].m_rgb_bytes_used);
				if ((sym & (1 << 0)) != 0)
				{
					corr = (byte)dec_RGB.decodeSymbol(contexts[current_context].m_rgb_diff_0);
					item.rgb[0] = (ushort)MyDefs.U8_FOLD(corr + (last_item[0] & 255));
				}
				else
				{
					item.rgb[0] = (ushort)(last_item[0] & 0xFF);
				}

				if ((sym & (1 << 1)) != 0)
				{
					corr = (byte)dec_RGB.decodeSymbol(contexts[current_context].m_rgb_diff_1);
					item.rgb[0] |= (ushort)(MyDefs.U8_FOLD(corr + (last_item[0] >> 8)) << 8);
				}
				else
				{
					item.rgb[0] |= (ushort)(last_item[0] & 0xFF00);
				}

				if ((sym & (1 << 6)) != 0)
				{
					int diff = (item.rgb[0] & 0x00FF) - (last_item[0] & 0x00FF);
					if ((sym & (1 << 2)) != 0)
					{
						corr = (byte)dec_RGB.decodeSymbol(contexts[current_context].m_rgb_diff_2);
						item.rgb[1] = (ushort)MyDefs.U8_FOLD(corr + MyDefs.U8_CLAMP(diff + (last_item[1] & 255)));
					}
					else
					{
						item.rgb[1] = (ushort)(last_item[1] & 0xFF);
					}

					if ((sym & (1 << 4)) != 0)
					{
						corr = (byte)dec_RGB.decodeSymbol(contexts[current_context].m_rgb_diff_4);
						diff = (diff + ((item.rgb[1] & 0x00FF) - (last_item[1] & 0x00FF))) / 2;
						item.rgb[2] = (ushort)MyDefs.U8_FOLD(corr + MyDefs.U8_CLAMP(diff + (last_item[2] & 255)));
					}
					else
					{
						item.rgb[2] = (ushort)(last_item[2] & 0xFF);
					}

					diff = (item.rgb[0] >> 8) - (last_item[0] >> 8);
					if ((sym & (1 << 3)) != 0)
					{
						corr = (byte)dec_RGB.decodeSymbol(contexts[current_context].m_rgb_diff_3);
						item.rgb[1] |= (ushort)(MyDefs.U8_FOLD(corr + MyDefs.U8_CLAMP(diff + (last_item[1] >> 8))) << 8);
					}
					else
					{
						item.rgb[1] |= (ushort)(last_item[1] & 0xFF00);
					}

					if ((sym & (1 << 5)) != 0)
					{
						corr = (byte)dec_RGB.decodeSymbol(contexts[current_context].m_rgb_diff_5);
						diff = (diff + ((item.rgb[1] >> 8) - (last_item[1] >> 8))) / 2;
						item.rgb[2] |= (ushort)(MyDefs.U8_FOLD(corr + MyDefs.U8_CLAMP(diff + (last_item[2] >> 8))) << 8);
					}
					else
					{
						item.rgb[2] |= (ushort)(last_item[2] & 0xFF00);
					}
				}
				else
				{
					item.rgb[1] = item.rgb[0];
					item.rgb[2] = item.rgb[0];
				}

				last_item[0] = item.rgb[0];
				last_item[1] = item.rgb[1];
				last_item[2] = item.rgb[2];
			}
			else
			{
				item.rgb[0] = last_item[0];
				item.rgb[1] = last_item[1];
				item.rgb[2] = last_item[2];
			}

			////////////////////////////////////////
			// decompress NIR layer
			////////////////////////////////////////
			if (changed_NIR)
			{
				byte corr;
				uint sym = dec_NIR.decodeSymbol(contexts[current_context].m_nir_bytes_used);
				if ((sym & (1 << 0)) != 0)
				{
					corr = (byte)dec_NIR.decodeSymbol(contexts[current_context].m_nir_diff_0);
					item.rgb[3] = (ushort)MyDefs.U8_FOLD(corr + (last_item[3] & 255));
				}
				else
				{
					item.rgb[3] = (ushort)(last_item[3] & 0xFF);
				}

				if ((sym & (1 << 1)) != 0)
				{
					corr = (byte)dec_NIR.decodeSymbol(contexts[current_context].m_nir_diff_1);
					item.rgb[3] |= (ushort)(MyDefs.U8_FOLD(corr + (last_item[3] >> 8)) << 8);
				}
				else
				{
					item.rgb[3] |= (ushort)(last_item[3] & 0xFF00);
				}

				last_item[3] = item.rgb[3];
			}
			else
			{
				item.rgb[3] = last_item[3];
			}
		}

		// not used as a decoder. just gives access to instream
		ArithmeticDecoder dec;

		MemoryStream instream_RGB;
		MemoryStream instream_NIR;

		ArithmeticDecoder dec_RGB;
		ArithmeticDecoder dec_NIR;

		bool changed_RGB;
		bool changed_NIR;

		int num_bytes_RGB;
		int num_bytes_NIR;

		bool requested_RGB;
		bool requested_NIR;

		byte[] bytes;
		int num_bytes_allocated;

		uint current_context;
		readonly LAScontextRGBNIR14[] contexts =
		{
			new LAScontextRGBNIR14(),
			new LAScontextRGBNIR14(),
			new LAScontextRGBNIR14(),
			new LAScontextRGBNIR14()
		};

		bool createAndInitModelsAndDecompressors(uint context, ushort[] item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models (if needed)
			if (requested_RGB)
			{
				if (contexts[context].m_rgb_bytes_used == null)
				{
					contexts[context].m_rgb_bytes_used = dec_RGB.createSymbolModel(128);
					contexts[context].m_rgb_diff_0 = dec_RGB.createSymbolModel(256);
					contexts[context].m_rgb_diff_1 = dec_RGB.createSymbolModel(256);
					contexts[context].m_rgb_diff_2 = dec_RGB.createSymbolModel(256);
					contexts[context].m_rgb_diff_3 = dec_RGB.createSymbolModel(256);
					contexts[context].m_rgb_diff_4 = dec_RGB.createSymbolModel(256);
					contexts[context].m_rgb_diff_5 = dec_RGB.createSymbolModel(256);
				}

				// then init entropy models
				dec_RGB.initSymbolModel(contexts[context].m_rgb_bytes_used);
				dec_RGB.initSymbolModel(contexts[context].m_rgb_diff_0);
				dec_RGB.initSymbolModel(contexts[context].m_rgb_diff_1);
				dec_RGB.initSymbolModel(contexts[context].m_rgb_diff_2);
				dec_RGB.initSymbolModel(contexts[context].m_rgb_diff_3);
				dec_RGB.initSymbolModel(contexts[context].m_rgb_diff_4);
				dec_RGB.initSymbolModel(contexts[context].m_rgb_diff_5);
			}

			if (requested_NIR)
			{
				if (contexts[context].m_nir_bytes_used == null)
				{
					contexts[context].m_nir_bytes_used = dec_NIR.createSymbolModel(4);
					contexts[context].m_nir_diff_0 = dec_NIR.createSymbolModel(256);
					contexts[context].m_nir_diff_1 = dec_NIR.createSymbolModel(256);
				}

				// then init entropy models
				dec_NIR.initSymbolModel(contexts[context].m_nir_bytes_used);
				dec_NIR.initSymbolModel(contexts[context].m_nir_diff_0);
				dec_NIR.initSymbolModel(contexts[context].m_nir_diff_1);
			}

			// init current context from item
			contexts[context].last_item[0] = item[0];
			contexts[context].last_item[1] = item[1];
			contexts[context].last_item[2] = item[2];
			contexts[context].last_item[3] = item[3];

			contexts[context].unused = false;

			return true;
		}
	}
}
