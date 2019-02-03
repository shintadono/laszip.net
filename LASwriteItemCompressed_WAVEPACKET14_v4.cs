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

using System;
using System.Diagnostics;
using System.IO;

namespace LASzip.Net
{
	class LASwriteItemCompressed_WAVEPACKET14_v4 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_WAVEPACKET14_v4(ArithmeticEncoder enc)
		{
			// not used as a encoder. just gives access to outstream
			Debug.Assert(enc != null);
			this.enc = enc;

			// zero outstreams and encoders
			outstream_wavepacket = null;
			enc_wavepacket = null;

			// zero num_bytes and init booleans
			num_bytes_wavepacket = 0;
			changed_wavepacket = false;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_packet_index = null;
			}
			current_context = 0;
		}

		public override bool init(laszip_point item, ref uint context)
		{
			// on the first init create outstreams and encoders
			if (outstream_wavepacket == null)
			{
				// create outstreams
				outstream_wavepacket = new MemoryStream();

				// create layer encoders
				enc_wavepacket = new ArithmeticEncoder();
			}
			else
			{
				// otherwise just seek back
				outstream_wavepacket.Seek(0, SeekOrigin.Begin);
			}

			// init layer encoders
			enc_wavepacket.init(outstream_wavepacket);

			// set changed booleans to FALSE
			changed_wavepacket = false;

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = context; // all other items use context set by POINT14 writer

			// create and init entropy models and integer compressors (and init contect from item)
			createAndInitModelsAndCompressors(current_context, item.wave_packet);

			return true;
		}

		public override bool write(laszip_point item, ref uint context)
		{
			// get last
			byte[] last_item = contexts[current_context].last_item;

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

			if (!changed_wavepacket)
			{
				for (int i = 0; i < 29; i++)
				{
					if (item.wave_packet[i] != last_item[i])
					{
						changed_wavepacket = true;
						break;
					}
				}
			}

			// compress
			enc_wavepacket.encodeSymbol(contexts[current_context].m_packet_index, item.wave_packet[0]);

			LASwavepacket13 this_item_m = LASwavepacket13.unpack(item.wave_packet, 1);
			LASwavepacket13 last_item_m = LASwavepacket13.unpack(last_item, 1);

			// calculate the difference between the two offsets
			long curr_diff_64 = (long)this_item_m.offset - (long)last_item_m.offset;
			int curr_diff_32 = (int)curr_diff_64;

			// if the current difference can be represented with 32 bits
			if (curr_diff_64 == (long)(curr_diff_32))
			{
				if (curr_diff_32 == 0) // current difference is zero
				{
					enc_wavepacket.encodeSymbol(contexts[current_context].m_offset_diff[contexts[current_context].sym_last_offset_diff], 0);
					contexts[current_context].sym_last_offset_diff = 0;
				}
				else if (curr_diff_32 == (int)last_item_m.packet_size)
				{
					enc_wavepacket.encodeSymbol(contexts[current_context].m_offset_diff[contexts[current_context].sym_last_offset_diff], 1);
					contexts[current_context].sym_last_offset_diff = 1;
				}
				else //
				{
					enc_wavepacket.encodeSymbol(contexts[current_context].m_offset_diff[contexts[current_context].sym_last_offset_diff], 2);
					contexts[current_context].sym_last_offset_diff = 2;
					contexts[current_context].ic_offset_diff.compress(contexts[current_context].last_diff_32, curr_diff_32);
					contexts[current_context].last_diff_32 = curr_diff_32;
				}
			}
			else
			{
				enc_wavepacket.encodeSymbol(contexts[current_context].m_offset_diff[contexts[current_context].sym_last_offset_diff], 3);
				contexts[current_context].sym_last_offset_diff = 3;

				enc_wavepacket.writeInt64(this_item_m.offset);
			}

			contexts[current_context].ic_packet_size.compress((int)last_item_m.packet_size, (int)this_item_m.packet_size);
			contexts[current_context].ic_return_point.compress(last_item_m.return_point.i32, this_item_m.return_point.i32);
			contexts[current_context].ic_xyz.compress(last_item_m.x.i32, this_item_m.x.i32, 0);
			contexts[current_context].ic_xyz.compress(last_item_m.y.i32, this_item_m.y.i32, 1);
			contexts[current_context].ic_xyz.compress(last_item_m.z.i32, this_item_m.z.i32, 2);

			Array.Copy(item.wave_packet, last_item, 29);

			return true;
		}

		public override bool chunk_sizes()
		{
			Stream outstream = enc.getByteStreamOut();

			// finish the encoders
			enc_wavepacket.done();

			// output the sizes of all layer (i.e.. number of bytes per layer)
			uint num_bytes = 0;
			if (changed_wavepacket)
			{
				num_bytes = (uint)outstream_wavepacket.Position;
				num_bytes_wavepacket += num_bytes;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			return true;
		}

		public override bool chunk_bytes()
		{
			Stream outstream = enc.getByteStreamOut();

			// output the bytes of all layers
			if (changed_wavepacket)
			{
				outstream.Write(outstream_wavepacket.GetBuffer(), 0, (int)outstream_wavepacket.Position);
			}

			return true;
		}

		// not used as a encoder. just gives access to outstream
		ArithmeticEncoder enc;

		MemoryStream outstream_wavepacket;

		ArithmeticEncoder enc_wavepacket;

		bool changed_wavepacket;

		uint num_bytes_wavepacket;

		uint current_context;
		readonly LAScontextWAVEPACKET14[] contexts =
		{
			new LAScontextWAVEPACKET14(),
			new LAScontextWAVEPACKET14(),
			new LAScontextWAVEPACKET14(),
			new LAScontextWAVEPACKET14()
		};

		bool createAndInitModelsAndCompressors(uint context, byte[] item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models (if needed)

			if (contexts[context].m_packet_index == null)
			{
				contexts[context].m_packet_index = enc_wavepacket.createSymbolModel(256);
				contexts[context].m_offset_diff[0] = enc_wavepacket.createSymbolModel(4);
				contexts[context].m_offset_diff[1] = enc_wavepacket.createSymbolModel(4);
				contexts[context].m_offset_diff[2] = enc_wavepacket.createSymbolModel(4);
				contexts[context].m_offset_diff[3] = enc_wavepacket.createSymbolModel(4);
				contexts[context].ic_offset_diff = new IntegerCompressor(enc_wavepacket, 32);
				contexts[context].ic_packet_size = new IntegerCompressor(enc_wavepacket, 32);
				contexts[context].ic_return_point = new IntegerCompressor(enc_wavepacket, 32);
				contexts[context].ic_xyz = new IntegerCompressor(enc_wavepacket, 32, 3);
			}

			// then init entropy models
			enc_wavepacket.initSymbolModel(contexts[context].m_packet_index);
			enc_wavepacket.initSymbolModel(contexts[context].m_offset_diff[0]);
			enc_wavepacket.initSymbolModel(contexts[context].m_offset_diff[1]);
			enc_wavepacket.initSymbolModel(contexts[context].m_offset_diff[2]);
			enc_wavepacket.initSymbolModel(contexts[context].m_offset_diff[3]);
			contexts[context].ic_offset_diff.initCompressor();
			contexts[context].ic_packet_size.initCompressor();
			contexts[context].ic_return_point.initCompressor();
			contexts[context].ic_xyz.initCompressor();

			// init current context from item
			contexts[context].last_diff_32 = 0;
			contexts[context].sym_last_offset_diff = 0;
			Array.Copy(item, contexts[context].last_item, 29);

			contexts[context].unused = false;

			return true;
		}
	}
}
