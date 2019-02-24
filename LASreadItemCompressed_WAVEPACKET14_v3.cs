//===============================================================================
//
//  FILE:  lasreaditemcompressed_wavepacket14_v3.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for WAVEPACKET14 items (version 3).
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
	class LASreadItemCompressed_WAVEPACKET14_v3 : LASreadItemCompressed
	{
		public LASreadItemCompressed_WAVEPACKET14_v3(ArithmeticDecoder dec, LASZIP_DECOMPRESS_SELECTIVE decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL)
		{
			// not used as a decoder. just gives access to instream
			Debug.Assert(dec != null);
			this.dec = dec;

			// zero instreams and decoders
			instream_wavepacket = null;
			dec_wavepacket = null;

			// zero num_bytes and init booleans
			num_bytes_wavepacket = 0;
			changed_wavepacket = false;

			requested_wavepacket = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.WAVEPACKET);

			// init the bytes buffer to zero
			bytes = null;
			num_bytes_allocated = 0;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_packet_index = null;
			}
			current_context = 0;
		}

		public override bool chunk_sizes()
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			// read bytes per layer
			if (!instream.get32bits(out num_bytes_wavepacket)) throw new EndOfStreamException();

			return true;
		}

		public override bool init(laszip_point item, ref uint context) // context is only read
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			// on the first init create instreams and decoders
			if (instream_wavepacket == null)
			{
				// create decoders
				dec_wavepacket = new ArithmeticDecoder();
			}

			// make sure the buffer is sufficiently large
			if (num_bytes_wavepacket > num_bytes_allocated)
			{
				try
				{
					bytes = new byte[num_bytes_wavepacket];
				}
				catch
				{
					return false;
				}
				num_bytes_allocated = num_bytes_wavepacket;
			}

			// load the requested bytes and init the corresponding instreams an decoders
			if (requested_wavepacket)
			{
				if (num_bytes_wavepacket != 0)
				{
					if (!instream.getBytes(bytes, num_bytes_wavepacket)) throw new EndOfStreamException();
					instream_wavepacket = new MemoryStream(bytes, 0, num_bytes_wavepacket);
					dec_wavepacket.init(instream_wavepacket);
					changed_wavepacket = true;
				}
				else
				{
					instream_wavepacket = new MemoryStream(0);
					changed_wavepacket = false;
				}
			}
			else
			{
				if (num_bytes_wavepacket != 0)
				{
					instream.Seek(num_bytes_wavepacket, SeekOrigin.Current);
				}
				changed_wavepacket = false;
			}

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = context; // all other items use context set by POINT14 reader

			// create and init models and decompressors
			createAndInitModelsAndDecompressors(current_context, item.wave_packet);

			return true;
		}

		public override void read(laszip_point item, ref uint context) // context is only read
		{
			// get last
			byte[] last_item = contexts[current_context].last_item;

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

			// decompress
			if (changed_wavepacket)
			{
				item.wave_packet[0] = (byte)(dec_wavepacket.decodeSymbol(contexts[current_context].m_packet_index));

				LASwavepacket13 this_item_m = new LASwavepacket13();
				LASwavepacket13 last_item_m = LASwavepacket13.unpack(last_item, 1);

				contexts[current_context].sym_last_offset_diff = dec_wavepacket.decodeSymbol(contexts[current_context].m_offset_diff[contexts[current_context].sym_last_offset_diff]);

				if (contexts[current_context].sym_last_offset_diff == 0)
				{
					this_item_m.offset = last_item_m.offset;
				}
				else if (contexts[current_context].sym_last_offset_diff == 1)
				{
					this_item_m.offset = last_item_m.offset + last_item_m.packet_size;
				}
				else if (contexts[current_context].sym_last_offset_diff == 2)
				{
					contexts[current_context].last_diff_32 = contexts[current_context].ic_offset_diff.decompress(contexts[current_context].last_diff_32);
					this_item_m.offset = (ulong)((long)last_item_m.offset + contexts[current_context].last_diff_32);
				}
				else
				{
					this_item_m.offset = dec_wavepacket.readInt64();
				}

				this_item_m.packet_size = (uint)contexts[current_context].ic_packet_size.decompress((int)last_item_m.packet_size);
				this_item_m.return_point.i32 = contexts[current_context].ic_return_point.decompress(last_item_m.return_point.i32);
				this_item_m.x.i32 = contexts[current_context].ic_xyz.decompress(last_item_m.x.i32, 0);
				this_item_m.y.i32 = contexts[current_context].ic_xyz.decompress(last_item_m.y.i32, 1);
				this_item_m.z.i32 = contexts[current_context].ic_xyz.decompress(last_item_m.z.i32, 2);

				this_item_m.pack(item.wave_packet, 1);

				item.wave_packet.CopyTo(last_item, 0);
			}
		}

		// not used as a decoder. just gives access to instream
		ArithmeticDecoder dec;

		MemoryStream instream_wavepacket;

		ArithmeticDecoder dec_wavepacket;

		bool changed_wavepacket;
		int num_bytes_wavepacket;
		bool requested_wavepacket;

		byte[] bytes;
		int num_bytes_allocated;

		uint current_context;
		readonly LAScontextWAVEPACKET14[] contexts =
		{
			new LAScontextWAVEPACKET14(),
			new LAScontextWAVEPACKET14(),
			new LAScontextWAVEPACKET14(),
			new LAScontextWAVEPACKET14()
		};

		bool createAndInitModelsAndDecompressors(uint context, byte[] item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models (if needed)
			if (requested_wavepacket)
			{
				if (contexts[context].m_packet_index == null)
				{
					contexts[context].m_packet_index = dec_wavepacket.createSymbolModel(256);
					contexts[context].m_offset_diff[0] = dec_wavepacket.createSymbolModel(4);
					contexts[context].m_offset_diff[1] = dec_wavepacket.createSymbolModel(4);
					contexts[context].m_offset_diff[2] = dec_wavepacket.createSymbolModel(4);
					contexts[context].m_offset_diff[3] = dec_wavepacket.createSymbolModel(4);
					contexts[context].ic_offset_diff = new IntegerCompressor(dec_wavepacket, 32);
					contexts[context].ic_packet_size = new IntegerCompressor(dec_wavepacket, 32);
					contexts[context].ic_return_point = new IntegerCompressor(dec_wavepacket, 32);
					contexts[context].ic_xyz = new IntegerCompressor(dec_wavepacket, 32, 3);
				}

				// then init entropy models
				dec_wavepacket.initSymbolModel(contexts[context].m_packet_index);
				dec_wavepacket.initSymbolModel(contexts[context].m_offset_diff[0]);
				dec_wavepacket.initSymbolModel(contexts[context].m_offset_diff[1]);
				dec_wavepacket.initSymbolModel(contexts[context].m_offset_diff[2]);
				dec_wavepacket.initSymbolModel(contexts[context].m_offset_diff[3]);
				contexts[context].ic_offset_diff.initDecompressor();
				contexts[context].ic_packet_size.initDecompressor();
				contexts[context].ic_return_point.initDecompressor();
				contexts[context].ic_xyz.initDecompressor();
			}

			// init current context from item
			contexts[context].last_diff_32 = 0;
			contexts[context].sym_last_offset_diff = 0;
			item.CopyTo(contexts[context].last_item, 0);

			contexts[context].unused = false;

			return true;
		}
	}
}
