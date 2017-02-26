//===============================================================================
//
//  FILE:  laswritepoint.cs
//
//  CONTENTS:
//
//    Common interface for the classes that write points raw or compressed.
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

using System;
using System.Collections.Generic;
using System.IO;

namespace LASzip.Net
{
	class LASwritePoint
	{
		public LASwritePoint()
		{
			outstream=null;
			num_writers=0;
			writers=null;
			writers_raw=null;
			writers_compressed=null;
			enc=null;

			// used for chunking
			init_chunking=false;
			chunk_size=uint.MaxValue;
			chunk_count=0;
			chunk_table_start_position=0;
			chunk_start_position=0;
		}

		// should only be called *once*
		public bool setup(uint num_items, LASitem[] items, LASzip laszip=null)
		{
			// is laszip exists then we must use its items
			if(laszip!=null)
			{
				if(num_items!=laszip.num_items) return false;
				if(items!=laszip.items) return false;
			}

			// create entropy encoder (if requested)
			enc=null;
			if(laszip!=null&&laszip.compressor!=0)
			{
				switch(laszip.coder)
				{
					case LASzip.CODER_ARITHMETIC: enc=new ArithmeticEncoder(); break;
					default: return false; // entropy decoder not supported
				}
			}

			// initizalize the writers
			writers=null;
			num_writers=num_items;

			// disable chunking
			chunk_size=uint.MaxValue;

			// always create the raw writers
			writers_raw=new LASwriteItem[num_writers];

			for(uint i=0; i<num_writers; i++)
			{
				switch(items[i].type)
				{
					case LASitem.Type.POINT10: writers_raw[i]=new LASwriteItemRaw_POINT10(); break;
					case LASitem.Type.GPSTIME11: writers_raw[i]=new LASwriteItemRaw_GPSTIME11(); break;
					case LASitem.Type.RGB12: writers_raw[i]=new LASwriteItemRaw_RGB12(); break;
					case LASitem.Type.WAVEPACKET13: writers_raw[i]=new LASwriteItemRaw_WAVEPACKET13(); break;
					case LASitem.Type.BYTE: writers_raw[i]=new LASwriteItemRaw_BYTE(items[i].size); break;
					case LASitem.Type.POINT14: writers_raw[i]=new LASwriteItemRaw_POINT14(); break;
					case LASitem.Type.RGBNIR14: writers_raw[i]=new LASwriteItemRaw_RGBNIR14(); break;
					default: return false;
				}
			}

			// if needed create the compressed writers and set versions
			if(enc!=null)
			{
				writers_compressed=new LASwriteItem[num_writers];

				for(uint i=0; i<num_writers; i++)
				{
					switch(items[i].type)
					{
						case LASitem.Type.POINT10:
							if(items[i].version==1) throw new NotSupportedException("Version 1 POINT10 is no longer supported, use version 2.");
							else if(items[i].version==2) writers_compressed[i]=new LASwriteItemCompressed_POINT10_v2(enc);
							else return false;
							break;
						case LASitem.Type.GPSTIME11:
							if(items[i].version==1) throw new NotSupportedException("Version 1 GPSTIME11 is no longer supported, use version 2.");
							else if(items[i].version==2) writers_compressed[i]=new LASwriteItemCompressed_GPSTIME11_v2(enc);
							else return false;
							break;
						case LASitem.Type.RGB12:
							if(items[i].version==1) throw new NotSupportedException("Version 1 RGB12 is no longer supported, use version 2.");
							else if(items[i].version==2) writers_compressed[i]=new LASwriteItemCompressed_RGB12_v2(enc);
							else return false;
							break;
						case LASitem.Type.WAVEPACKET13:
							if(items[i].version==1) writers_compressed[i]=new LASwriteItemCompressed_WAVEPACKET13_v1(enc);
							else return false;
							break;
						case LASitem.Type.BYTE:
							if(items[i].version==1) throw new NotSupportedException("Version 1 BYTE is no longer supported, use version 2.");
							else if(items[i].version==2) writers_compressed[i]=new LASwriteItemCompressed_BYTE_v2(enc, items[i].size);
							else return false;
							break;
						default: return false;
					}
				}

				if(laszip.compressor==LASzip.COMPRESSOR_POINTWISE_CHUNKED)
				{
					if(laszip.chunk_size!=0) chunk_size=laszip.chunk_size;
					chunk_count=0;
					init_chunking=true;
				}
			}

			return true;
		}

		public bool init(Stream outstream)
		{
			if(outstream==null) return false;
			this.outstream=outstream;

			// if chunking is enabled
			if(init_chunking)
			{
				init_chunking=false;
				if(outstream.CanSeek) chunk_table_start_position=outstream.Position;
				else chunk_table_start_position=-1;

				outstream.Write(BitConverter.GetBytes(chunk_table_start_position), 0, 8);

				chunk_start_position=outstream.Position;
			}

			for(uint i=0; i<num_writers; i++)
			{
				((LASwriteItemRaw)(writers_raw[i])).init(outstream);
			}

			if(enc!=null) writers=null;
			else writers=writers_raw;

			return true;
		}

		public bool write(laszip.point point)
		{
			if(chunk_count==chunk_size)
			{
				enc.done();
				add_chunk_to_table();
				init(outstream);
				chunk_count=0;
			}
			chunk_count++;

			if(writers!=null)
			{
				for(uint i=0; i<num_writers; i++)
				{
					writers[i].write(point);
				}
			}
			else
			{
				for(uint i=0; i<num_writers; i++)
				{
					writers_raw[i].write(point);
					((LASwriteItemCompressed)(writers_compressed[i])).init(point);
				}
				writers=writers_compressed;
				enc.init(outstream);
			}

			return true;
		}

		public bool chunk()
		{
			if(chunk_start_position==0||chunk_size!=uint.MaxValue)
				return false;

			enc.done();
			add_chunk_to_table();
			init(outstream);
			chunk_count=0;

			return true;
		}

		public bool done()
		{
			if(writers==writers_compressed)
			{
				enc.done();
				if(chunk_start_position!=0)
				{
					if(chunk_count!=0) add_chunk_to_table();
					return write_chunk_table();
				}
			}
			else if(writers==null)
			{
				if(chunk_start_position!=0)
				{
					return write_chunk_table();
				}
			}

			return true;
		}

		Stream outstream;
		uint num_writers;
		LASwriteItem[] writers;
		LASwriteItem[] writers_raw;
		LASwriteItem[] writers_compressed;
		ArithmeticEncoder enc;

		// used for chunking
		bool init_chunking;
		uint chunk_size;
		uint chunk_count;
		List<uint> chunk_sizes=new List<uint>();
		List<uint> chunk_bytes=new List<uint>();
		long chunk_start_position;
		long chunk_table_start_position;

		bool add_chunk_to_table()
		{
			long position=outstream.Position;
			if(chunk_size==uint.MaxValue) chunk_sizes.Add(chunk_count);
			chunk_bytes.Add((uint)(position-chunk_start_position));
			chunk_start_position=position;

			return true;
		}

		bool write_chunk_table()
		{
			long position=outstream.Position;

			if(chunk_table_start_position!=-1) // stream is seekable
			{
				try
				{
					outstream.Seek(chunk_table_start_position, SeekOrigin.Begin);
					outstream.Write(BitConverter.GetBytes(position), 0, 8);
					outstream.Seek(position, SeekOrigin.Begin);
				}
				catch
				{
					return false;
				}
			}

			try
			{
				uint version=0;
				outstream.Write(BitConverter.GetBytes(version), 0, 4);
				outstream.Write(BitConverter.GetBytes(chunk_bytes.Count), 0, 4);
			}
			catch
			{
				return false;
			}

			if(chunk_bytes.Count>0)
			{
				enc.init(outstream);

				IntegerCompressor ic=new IntegerCompressor(enc, 32, 2);
				ic.initCompressor();

				for(int i=0; i<chunk_bytes.Count; i++)
				{
					if(chunk_size==uint.MaxValue) ic.compress((i!=0?(int)chunk_sizes[i-1]:0), (int)chunk_sizes[i], 0);
					ic.compress((i!=0?(int)chunk_bytes[i-1]:0), (int)chunk_bytes[i], 1);
				}

				enc.done();
			}

			if(chunk_table_start_position==-1) // stream is not-seekable
			{
				try
				{
					outstream.Write(BitConverter.GetBytes(position), 0, 8);
				}
				catch
				{
					return false;
				}
			}

			return true;
		}
	}
}
