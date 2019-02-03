//===============================================================================
//
//  FILE:  laswriteitemcompressed_byte14_v4.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for BYTE14 items (version 4).
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
	class LASwriteItemCompressed_BYTE14_v4 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_BYTE14_v4(ArithmeticEncoder enc, uint number)
		{
			// not used as a encoder. just gives access to outstream
			Debug.Assert(enc != null);
			this.enc = enc;

			// must be more than one byte
			Debug.Assert(number != 0);
			this.number = number;

			// zero outstream and encoder pointer arrays
			outstream_Bytes = null;
			enc_Bytes = null;

			// number of bytes per layer
			num_bytes_Bytes = new uint[number];
			changed_Bytes = new bool[number];

			for (uint i = 0; i < number; i++)
			{
				num_bytes_Bytes[i] = 0;
				changed_Bytes[i] = false;
			}

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_bytes = null;
			}
			current_context = 0;
		}

		public override bool init(laszip_point item, ref uint context)
		{
			// on the first init create outstreams and encoders
			if (outstream_Bytes == null)
			{
				// create outstreams pointer array
				outstream_Bytes = new MemoryStream[number];

				// create outstreams
				for (uint i = 0; i < number; i++)
				{
					outstream_Bytes[i] = new MemoryStream();
				}

				// create encoder pointer array
				enc_Bytes = new ArithmeticEncoder[number];

				// create layer encoders
				for (uint i = 0; i < number; i++)
				{
					enc_Bytes[i] = new ArithmeticEncoder();
				}
			}
			else
			{
				// otherwise just seek back
				for (uint i = 0; i < number; i++)
				{
					outstream_Bytes[i].Seek(0, SeekOrigin.Begin);
				}
			}

			// init layer encoders
			for (uint i = 0; i < number; i++)
			{
				enc_Bytes[i].init(outstream_Bytes[i]);
			}

			// set changed booleans to FALSE
			for (uint i = 0; i < number; i++)
			{
				changed_Bytes[i] = false;
			}

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = context; // all other items use context set by POINT14 writer

			// create and init entropy models and integer compressors (and init context from item)
			createAndInitModelsAndCompressors(current_context, item.extra_bytes);

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

			// compress
			for (uint i = 0; i < number; i++)
			{
				int diff = item.extra_bytes[i] - last_item[i];
				enc_Bytes[i].encodeSymbol(contexts[current_context].m_bytes[i], (uint)MyDefs.U8_FOLD(diff));
				if (diff != 0)
				{
					changed_Bytes[i] = true;
					last_item[i] = item.extra_bytes[i];
				}
			}

			return true;
		}

		public override bool chunk_sizes()
		{
			Stream outstream = enc.getByteStreamOut();

			// output the sizes of all layer (i.e.. number of bytes per layer)
			for (uint i = 0; i < number; i++)
			{
				// finish the encoders
				enc_Bytes[i].done();

				uint num_bytes = 0;
				if (changed_Bytes[i])
				{
					num_bytes = (uint)outstream_Bytes[i].Position;
					num_bytes_Bytes[i] += num_bytes;
				}
				outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);
			}

			return true;
		}

		public override bool chunk_bytes()
		{
			Stream outstream = enc.getByteStreamOut();

			// output the bytes of all layers
			for (uint i = 0; i < number; i++)
			{
				if (changed_Bytes[i])
				{
					outstream.Write(outstream_Bytes[i].GetBuffer(), 0, (int)outstream_Bytes[i].Position);
				}
			}

			return true;
		}

		// not used as a encoder. just gives access to outstream
		ArithmeticEncoder enc;

		MemoryStream[] outstream_Bytes;

		ArithmeticEncoder[] enc_Bytes;

		uint[] num_bytes_Bytes;

		bool[] changed_Bytes;

		uint current_context;
		readonly LAScontextBYTE14[] contexts =
		{
			new LAScontextBYTE14(),
			new LAScontextBYTE14(),
			new LAScontextBYTE14(),
			new LAScontextBYTE14()
		};

		uint number;

		bool createAndInitModelsAndCompressors(uint context, byte[] item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models and last items (if needed)
			if (contexts[context].m_bytes == null)
			{
				contexts[context].m_bytes = new ArithmeticModel[number];
				for (uint i = 0; i < number; i++)
				{
					contexts[context].m_bytes[i] = enc_Bytes[i].createSymbolModel(256);
					enc_Bytes[i].initSymbolModel(contexts[context].m_bytes[i]);
				}

				// create last item
				contexts[context].last_item = new byte[number];
			}

			// then init entropy models
			for (uint i = 0; i < number; i++)
			{
				enc_Bytes[i].initSymbolModel(contexts[context].m_bytes[i]);
			}

			// init current context from item
			Array.Copy(item, contexts[context].last_item, number);

			contexts[context].unused = false;

			return true;
		}
	}
}
