//===============================================================================
//
//  FILE:  lasreaditemcompressed_byte_v4.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for BYTE14 items (version 4).
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
	class LASreadItemCompressed_BYTE14_v4 : LASreadItemCompressed
	{
		public LASreadItemCompressed_BYTE14_v4(ArithmeticDecoder dec, uint number, LASZIP_DECOMPRESS_SELECTIVE decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL)
		{
			// not used as a decoder. just gives access to instream
			Debug.Assert(dec != null);
			this.dec = dec;

			// must be more than one byte
			Debug.Assert(number != 0);
			this.number = number;

			// zero instream and decoder pointer arrays
			instream_Bytes = null;
			dec_Bytes = null;

			// create and init num_bytes and booleans arrays
			num_bytes_Bytes = new int[number];
			changed_Bytes = new bool[number];
			requested_Bytes = new bool[number];

			for (uint i = 0; i < number; i++)
			{
				num_bytes_Bytes[i] = 0;
				changed_Bytes[i] = false;
				requested_Bytes[i] = decompress_selective.HasFlag((LASZIP_DECOMPRESS_SELECTIVE)((uint)LASZIP_DECOMPRESS_SELECTIVE.BYTE0 << (int)i));
			}

			// init the bytes buffer to zero
			bytes = null;
			num_bytes_allocated = 0;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_bytes = null;
			}
			current_context = 0;
		}

		public override bool chunk_sizes()
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			for (uint i = 0; i < number; i++)
			{
				// read bytes per layer
				if (!instream.get32bits(out num_bytes_Bytes[i])) throw new EndOfStreamException();
			}

			return true;
		}

		public override bool init(laszip_point item, ref uint context) // context is only read
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			// on the first init create instreams and decoders
			if (instream_Bytes == null)
			{
				// create instream pointer array
				instream_Bytes = new MemoryStream[number];

				// create decoder pointer array
				dec_Bytes = new ArithmeticDecoder[number];

				// create layer decoders
				for (uint i = 0; i < number; i++)
				{
					dec_Bytes[i] = new ArithmeticDecoder();
				}
			}

			// how many bytes do we need to read
			int num_bytes = 0;

			for (uint i = 0; i < number; i++)
			{
				if (requested_Bytes[i]) num_bytes += num_bytes_Bytes[i];
			}

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
			for (uint i = 0; i < number; i++)
			{
				if (requested_Bytes[i])
				{
					if (num_bytes_Bytes[i] != 0)
					{
						if (!instream.getBytes(bytes, num_bytes, num_bytes_Bytes[i])) throw new EndOfStreamException();
						instream_Bytes[i] = new MemoryStream(bytes, num_bytes, num_bytes_Bytes[i]);
						dec_Bytes[i].init(instream_Bytes[i]);
						num_bytes += num_bytes_Bytes[i];
						changed_Bytes[i] = true;
					}
					else
					{
						dec_Bytes[i].init(null, false);
						changed_Bytes[i] = false;
					}
				}
				else
				{
					if (num_bytes_Bytes[i] != 0)
					{
						instream.Seek(num_bytes_Bytes[i], SeekOrigin.Current);
					}
					changed_Bytes[i] = false;
				}
			}

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = context; // all other items use context set by POINT14 reader

			// create and init models and decompressors
			createAndInitModelsAndDecompressors(current_context, item.extra_bytes);

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
				}
				last_item = contexts[current_context].last_item;
			}

			// decompress
			for (uint i = 0; i < number; i++)
			{
				if (changed_Bytes[i])
				{
					int value = (int)(last_item[i] + dec_Bytes[i].decodeSymbol(contexts[current_context].m_bytes[i]));
					item.extra_bytes[i] = (byte)MyDefs.U8_FOLD(value);
					last_item[i] = item.extra_bytes[i];
				}
				else
				{
					item.extra_bytes[i] = last_item[i];
				}
			}
		}

		// not used as a decoder. just gives access to instream
		ArithmeticDecoder dec;

		MemoryStream[] instream_Bytes;

		ArithmeticDecoder[] dec_Bytes;

		readonly int[] num_bytes_Bytes;
		readonly bool[] changed_Bytes;
		readonly bool[] requested_Bytes;

		byte[] bytes;
		int num_bytes_allocated;

		uint current_context;
		readonly LAScontextBYTE14[] contexts =
		{
			new LAScontextBYTE14(),
			new LAScontextBYTE14(),
			new LAScontextBYTE14(),
			new LAScontextBYTE14()
		};

		uint number;

		bool createAndInitModelsAndDecompressors(uint context, byte[] item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models and last items (if needed)
			if (contexts[context].m_bytes == null)
			{
				contexts[context].m_bytes = new ArithmeticModel[number];
				for (uint i = 0; i < number; i++)
				{
					contexts[context].m_bytes[i] = dec_Bytes[i].createSymbolModel(256);
					dec_Bytes[i].initSymbolModel(contexts[context].m_bytes[i]);
				}

				// create last item
				contexts[context].last_item = new byte[number];
			}

			// then init entropy models
			for (uint i = 0; i < number; i++)
			{
				dec_Bytes[i].initSymbolModel(contexts[context].m_bytes[i]);
			}

			// init current context from item
			item.CopyTo(contexts[context].last_item, 0);

			contexts[context].unused = false;

			return true;
		}
	}
}
