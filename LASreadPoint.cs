//===============================================================================
//
//  FILE:  lasreadpoint.cs
//
//  CONTENTS:
//
//    Common interface for the classes that read points raw or compressed.
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

using System;
using System.Collections.Generic;
using System.IO;

namespace LASzip.Net
{
	class LASreadPoint
	{
		public LASreadPoint(LASZIP_DECOMPRESS_SELECTIVE decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL)
		{
			point_size = 0;
			instream = null;
			num_readers = 0;
			readers = null;
			readers_raw = null;
			readers_compressed = null;
			dec = null;
			layered_las14_compression = false;

			// used for chunking
			chunk_size = uint.MaxValue;
			chunk_count = 0;
			current_chunk = 0;
			number_chunks = 0;
			tabled_chunks = 0;
			chunk_totals = null;
			chunk_starts = null;

			// used for selective decompression (new LAS 1.4 point types only)
			this.decompress_selective = decompress_selective;

			// used for seeking
			point_start = 0;

			// used for error and warning reporting
			last_error = null;
			last_warning = null;
		}

		// should only be called *once*
		public bool setup(uint num_items, LASitem[] items, LASzip laszip = null)
		{
			// is laszip exists then we must use its items
			if (laszip != null)
			{
				if (num_items == 0) return false;
				if (items == null) return false;
				if (num_items != laszip.num_items) return false;
				if (items != laszip.items) return false;
			}

			// delete old entropy decoder
			if (dec != null)
			{
				dec = null;
				layered_las14_compression = false;
			}

			if (laszip != null && laszip.compressor != 0)
			{
				// create new entropy decoder (if requested)
				switch (laszip.coder)
				{
					case LASzip.CODER_ARITHMETIC: dec = new ArithmeticDecoder(); break;
					default: return false; // entropy decoder not supported
				}

				// maybe layered compression for LAS 1.4
				layered_las14_compression = laszip.compressor == LASzip.COMPRESSOR_LAYERED_CHUNKED;
			}

			// initizalize the readers
			readers = null;
			num_readers = num_items;

			// disable chunking
			chunk_size = uint.MaxValue;

			// always create the raw readers
			readers_raw = new LASreadItem[num_readers];
			for (int i = 0; i < num_readers; i++)
			{
				switch (items[i].type)
				{
					case LASitem.Type.POINT10: readers_raw[i] = new LASreadItemRaw_POINT10(); break;
					case LASitem.Type.GPSTIME11: readers_raw[i] = new LASreadItemRaw_GPSTIME11(); break;
					case LASitem.Type.RGB12: case LASitem.Type.RGB14: readers_raw[i] = new LASreadItemRaw_RGB12(); break;
					case LASitem.Type.BYTE: case LASitem.Type.BYTE14: readers_raw[i] = new LASreadItemRaw_BYTE(items[i].size); break;
					case LASitem.Type.POINT14: readers_raw[i] = new LASreadItemRaw_POINT14(); break;
					case LASitem.Type.RGBNIR14: readers_raw[i] = new LASreadItemRaw_RGBNIR14(); break;
					case LASitem.Type.WAVEPACKET13: case LASitem.Type.WAVEPACKET14: readers_raw[i] = new LASreadItemRaw_WAVEPACKET13(); break;
					default: return false;
				}
				point_size += items[i].size;
			}

			if (dec != null)
			{
				readers_compressed = new LASreadItem[num_readers];

				// seeks with compressed data need a seek point
				seek_point = new laszip_point();

				if (layered_las14_compression)
				{
					// because extended_point_type must be set
					seek_point.extended_point_type = 1;
				}

				for (int i = 0; i < num_readers; i++)
				{
					switch (items[i].type)
					{
						case LASitem.Type.POINT10:
							if (items[i].version == 1) readers_compressed[i] = new LASreadItemCompressed_POINT10_v1(dec);
							else if (items[i].version == 2) readers_compressed[i] = new LASreadItemCompressed_POINT10_v2(dec);
							else return false;
							break;
						case LASitem.Type.GPSTIME11:
							if (items[i].version == 1) readers_compressed[i] = new LASreadItemCompressed_GPSTIME11_v1(dec);
							else if (items[i].version == 2) readers_compressed[i] = new LASreadItemCompressed_GPSTIME11_v2(dec);
							else return false;
							break;
						case LASitem.Type.RGB12:
							if (items[i].version == 1) readers_compressed[i] = new LASreadItemCompressed_RGB12_v1(dec);
							else if (items[i].version == 2) readers_compressed[i] = new LASreadItemCompressed_RGB12_v2(dec);
							else return false;
							break;
						case LASitem.Type.BYTE:
							seek_point.extra_bytes = new byte[items[i].size];
							seek_point.num_extra_bytes = items[i].size;
							if (items[i].version == 1) readers_compressed[i] = new LASreadItemCompressed_BYTE_v1(dec, items[i].size);
							else if (items[i].version == 2) readers_compressed[i] = new LASreadItemCompressed_BYTE_v2(dec, items[i].size);
							else return false;
							break;
						case LASitem.Type.POINT14:
							if ((items[i].version == 3) || (items[i].version == 2)) readers_compressed[i] = new LASreadItemCompressed_POINT14_v3(dec, decompress_selective); // version == 2 from lasproto
							else if (items[i].version == 4) readers_compressed[i] = new LASreadItemCompressed_POINT14_v4(dec, decompress_selective);
							else return false;
							break;
						case LASitem.Type.RGB14:
							if ((items[i].version == 3) || (items[i].version == 2)) readers_compressed[i] = new LASreadItemCompressed_RGB14_v3(dec, decompress_selective); // version == 2 from lasproto
							else if (items[i].version == 4) readers_compressed[i] = new LASreadItemCompressed_RGB14_v4(dec, decompress_selective);
							else return false;
							break;
						case LASitem.Type.RGBNIR14:
							if ((items[i].version == 3) || (items[i].version == 2)) readers_compressed[i] = new LASreadItemCompressed_RGBNIR14_v3(dec, decompress_selective); // version == 2 from lasproto
							else if (items[i].version == 4) readers_compressed[i] = new LASreadItemCompressed_RGBNIR14_v4(dec, decompress_selective);
							else return false;
							break;
						case LASitem.Type.BYTE14:
							seek_point.extra_bytes = new byte[items[i].size];
							seek_point.num_extra_bytes = items[i].size;
							if ((items[i].version == 3) || (items[i].version == 2)) readers_compressed[i] = new LASreadItemCompressed_BYTE14_v3(dec, items[i].size, decompress_selective); // version == 2 from lasproto
							else if (items[i].version == 4) readers_compressed[i] = new LASreadItemCompressed_BYTE14_v4(dec, items[i].size, decompress_selective); // version == 2 from lasproto
							else return false;
							break;
						case LASitem.Type.WAVEPACKET13:
							if (items[i].version == 1) readers_compressed[i] = new LASreadItemCompressed_WAVEPACKET13_v1(dec);
							else return false;
							break;
						case LASitem.Type.WAVEPACKET14:
							if (items[i].version == 3) readers_compressed[i] = new LASreadItemCompressed_WAVEPACKET14_v3(dec, decompress_selective);
							else if (items[i].version == 4) readers_compressed[i] = new LASreadItemCompressed_WAVEPACKET14_v4(dec, decompress_selective);
							else return false;
							break;
						default: return false;
					}
				}

				if (laszip.compressor != LASzip.COMPRESSOR_POINTWISE)
				{
					if (laszip.chunk_size != 0) chunk_size = laszip.chunk_size;
					number_chunks = uint.MaxValue;
				}
			}

			return true;
		}

		public bool init(Stream instream)
		{
			if (instream == null) return false;
			this.instream = instream;

			for (int i = 0; i < num_readers; i++)
			{
				((LASreadItemRaw)(readers_raw[i])).init(instream);
			}

			if (dec != null)
			{
				chunk_count = chunk_size;
				point_start = 0;
				readers = null;
			}
			else
			{
				point_start = instream.Position;
				readers = readers_raw;
			}

			return true;
		}

		public bool seek(uint current, uint target)
		{
			if (!instream.CanSeek) return false;

			uint delta = 0;
			if (dec != null)
			{
				if (point_start == 0)
				{
					init_dec();
					chunk_count = 0;
				}

				if (chunk_starts != null)
				{
					uint target_chunk;
					if (chunk_totals != null)
					{
						target_chunk = search_chunk_table(target, 0, number_chunks);
						chunk_size = chunk_totals[target_chunk + 1] - chunk_totals[target_chunk];
						delta = target - chunk_totals[target_chunk];
					}
					else
					{
						target_chunk = target / chunk_size;
						delta = target % chunk_size;
					}
					if (target_chunk >= tabled_chunks)
					{
						if (current_chunk < (tabled_chunks - 1))
						{
							dec.done();
							current_chunk = (tabled_chunks - 1);
							instream.Seek(chunk_starts[(int)current_chunk], SeekOrigin.Begin);
							init_dec();
							chunk_count = 0;
						}
						delta += (chunk_size * (target_chunk - current_chunk) - chunk_count);
					}
					else if (current_chunk != target_chunk || current > target)
					{
						dec.done();
						current_chunk = target_chunk;
						instream.Seek(chunk_starts[(int)current_chunk], SeekOrigin.Begin);
						init_dec();
						chunk_count = 0;
					}
					else
					{
						delta = target - current;
					}
				}
				else if (current > target)
				{
					dec.done();
					instream.Seek(point_start, SeekOrigin.Begin);
					init_dec();
					delta = target;
				}
				else if (current < target)
				{
					delta = target - current;
				}

				while (delta != 0)
				{
					read(seek_point);
					delta--;
				}
			}
			else
			{
				if (current != target)
				{
					instream.Seek(point_start + (long)point_size * target, SeekOrigin.Begin);
				}
			}
			return true;
		}

		public bool read(laszip_point point)
		{
			uint context = 0;

			try
			{
				if (dec != null)
				{
					if (chunk_count == chunk_size)
					{
						if (point_start != 0)
						{
							dec.done();
							current_chunk++;
							// check integrity
							if (current_chunk < tabled_chunks)
							{
								long here = instream.Position;
								if (chunk_starts[(int)current_chunk] != here)
								{
									// previous chunk was corrupt
									current_chunk--;
									throw new Exception("4711");
								}
							}
						}
						init_dec();
						if (tabled_chunks == current_chunk) // no or incomplete chunk table?
						{
							// If there was no(!) chunk table, we haven't had the chance to create the chunk_starts list.
							if (tabled_chunks == 0 && chunk_starts == null) chunk_starts = new List<long>();

							chunk_starts.Add(point_start);
							number_chunks++;
							tabled_chunks++;
						}
						else if (chunk_totals != null) // variable sized chunks?
						{
							chunk_size = chunk_totals[current_chunk + 1] - chunk_totals[current_chunk];
						}
						chunk_count = 0;
					}
					chunk_count++;

					if (readers != null)
					{
						for (int i = 0; i < num_readers; i++)
						{
							readers[i].read(point, ref context);
						}
					}
					else
					{
						for (int i = 0; i < num_readers; i++)
						{
							readers_raw[i].read(point, ref context);
						}

						if (layered_las14_compression)
						{
							// for layered compression 'dec' only hands over the stream
							dec.init(instream, false);

							// read how many points are in the chunk
							uint count;
							if (!instream.get32bits(out count)) throw new EndOfStreamException();

							// read the sizes of all layers
							for (int i = 0; i < num_readers; i++)
							{
								((LASreadItemCompressed)(readers_compressed[i])).chunk_sizes();
							}
							for (int i = 0; i < num_readers; i++)
							{
								((LASreadItemCompressed)(readers_compressed[i])).init(point, ref context);
							}
						}
						else
						{
							for (int i = 0; i < num_readers; i++)
							{
								((LASreadItemCompressed)(readers_compressed[i])).init(point, ref context);
							}
							dec.init(instream);
						}

						readers = readers_compressed;
					}
				}
				else
				{
					for (int i = 0; i < num_readers; i++)
					{
						readers[i].read(point, ref context);
					}
				}
			}
			catch (EndOfStreamException)
			{
				// end-of-file
				if (dec != null) last_error = "end-of-file during chunk " + current_chunk;
				else last_error = "end-of-file";
				return false;
			}
			catch
			{
				// decompression error
				last_error = string.Format("chunk {0} of {1} is corrupt", current_chunk, tabled_chunks);
				// if we know where the next chunk starts ...
				if ((current_chunk + 1) < tabled_chunks)
				{
					// ... try to seek to the next chunk
					instream.Seek(chunk_starts[(int)current_chunk + 1], SeekOrigin.Begin);
					// ... ready for next LASreadPoint::read()
					chunk_count = chunk_size;
				}

				return false;
			}
			return true;
		}

		public bool check_end()
		{
			if (readers == readers_compressed)
			{
				if (dec != null)
				{
					dec.done();
					current_chunk++;
					// check integrity
					if (current_chunk < tabled_chunks)
					{
						long here = instream.Position;
						if (chunk_starts[(int)current_chunk] != here)
						{
							// create error string - last chunk was corrupt
							last_error = string.Format("chunk with index {0} of {1} is corrupt", current_chunk, tabled_chunks);
							return false;
						}
					}
				}
			}

			return true;
		}

		public bool done()
		{
			instream = null;

			return true;
		}

		public string error() { return last_error; }
		public string warning() { return last_warning; }

		Stream instream;
		uint num_readers;
		LASreadItem[] readers;
		LASreadItem[] readers_raw;
		LASreadItem[] readers_compressed;
		ArithmeticDecoder dec;
		bool layered_las14_compression;

		// used for chunking
		uint chunk_size;
		uint chunk_count;
		uint current_chunk;
		uint number_chunks;
		uint tabled_chunks;
		List<long> chunk_starts;
		uint[] chunk_totals;

		bool init_dec()
		{
			// maybe read chunk table (only if chunking enabled)
			if (number_chunks == uint.MaxValue)
			{
				if (!read_chunk_table()) return false;

				current_chunk = 0;
				if (chunk_totals != null) chunk_size = chunk_totals[1];
			}

			point_start = instream.Position;
			readers = null;

			return true;
		}

		bool read_chunk_table()
		{
			// read the 8 bytes that store the location of the chunk table
			long chunk_table_start_position;
			try
			{
				if (!instream.get64bits(out chunk_table_start_position)) throw new EndOfStreamException();
			}
			catch
			{
				return false;
			}

			// this is where the chunks start
			long chunks_start = instream.Position;

			// was compressor interrupted before getting a chance to write the chunk table?
			if ((chunk_table_start_position + 8) == chunks_start)
			{
				// no choice but to fail if adaptive chunking was used
				if (chunk_size == uint.MaxValue) return false;

				// otherwise we build the chunk table as we read the file
				number_chunks = 0;
				chunk_starts = new List<long>();
				chunk_starts.Add(chunks_start);
				number_chunks++;
				tabled_chunks = 1;
				return true;
			}

			// maybe the stream is not seekable
			if (!instream.CanSeek)
			{
				// no choice but to fail if adaptive chunking was used
				if (chunk_size == uint.MaxValue) return false;

				// then we cannot seek to the chunk table but won't need it anyways
				number_chunks = uint.MaxValue - 1;
				tabled_chunks = 0;
				return true;
			}

			if (chunk_table_start_position == -1)
			{
				// the compressor was writing to a non-seekable stream and wrote the chunk table start at the end
				if (instream.Seek(-8, SeekOrigin.End) == 0) return false;

				try
				{
					if (!instream.get64bits(out chunk_table_start_position)) throw new EndOfStreamException();
				}
				catch
				{
					return false;
				}
			}

			// read the chunk table
			try
			{
				instream.Seek(chunk_table_start_position, SeekOrigin.Begin);

				uint version;
				if (!instream.get32bits(out version)) throw new EndOfStreamException();
				if (version != 0) throw new Exception();

				if (!instream.get32bits(out number_chunks)) throw new EndOfStreamException();
				chunk_totals = null;
				chunk_starts = null;
				if (chunk_size == uint.MaxValue)
				{
					chunk_totals = new uint[number_chunks + 1];
					chunk_totals[0] = 0;
				}

				chunk_starts = new List<long>();
				chunk_starts.Add(chunks_start);
				tabled_chunks = 1;

				if (number_chunks > 0)
				{
					dec.init(instream);
					IntegerCompressor ic = new IntegerCompressor(dec, 32, 2);
					ic.initDecompressor();
					for (int i = 1; i <= number_chunks; i++)
					{
						if (chunk_size == uint.MaxValue) chunk_totals[i] = (uint)ic.decompress((i > 1 ? (int)chunk_totals[i - 1] : 0), 0);
						chunk_starts.Add(ic.decompress((i > 1 ? (int)(chunk_starts[i - 1]) : 0), 1));
						tabled_chunks++;
					}
					dec.done();
					for (int i = 1; i <= number_chunks; i++)
					{
						if (chunk_size == uint.MaxValue) chunk_totals[i] += chunk_totals[i - 1];
						chunk_starts[i] += chunk_starts[i - 1];
						if (chunk_starts[i] <= chunk_starts[i - 1]) throw new Exception();
					}
				}
			}
			catch
			{
				// something went wrong while reading the chunk table
				chunk_totals = null;

				// no choice but to fail if adaptive chunking was used
				if (chunk_size == uint.MaxValue) return false;

				// did we not even read the number of chunks
				if (number_chunks == uint.MaxValue)
				{
					// then compressor was interrupted before getting a chance to write the chunk table
					number_chunks = 0;
					chunk_starts = new List<long>();

					chunk_starts.Add(chunks_start);
					number_chunks++;
					tabled_chunks = 1;
				}
				else
				{
					// otherwise fix as many additional chunk_starts as possible
					for (int i = 1; i < tabled_chunks; i++)
					{
						chunk_starts[i] += chunk_starts[i - 1];
					}
				}
				// create & report warning string
				last_warning = "corrupt chunk table";
			}

			if (instream.Seek(chunks_start, SeekOrigin.Begin) == 0) return false;
			return true;
		}

		uint search_chunk_table(uint index, uint lower, uint upper)
		{
			if (lower + 1 == upper) return lower;
			uint mid = (lower + upper) / 2;
			if (index >= chunk_totals[mid])
				return search_chunk_table(index, mid, upper);
			else
				return search_chunk_table(index, lower, mid);
		}

		// used for selective decompression (new LAS 1.4 point types only)
		LASZIP_DECOMPRESS_SELECTIVE decompress_selective;

		// used for seeking
		long point_start;
		uint point_size;
		laszip_point seek_point = new laszip_point();

		// used for error and warning reporting
		string last_error;
		string last_warning;
	}
}
