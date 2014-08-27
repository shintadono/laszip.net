//===============================================================================
//
//  FILE:  laswriteitemcompressed_wavepacket13_v1.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for WAVEPACKET13 items (version 1).
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
using System.Runtime.InteropServices;

namespace laszip.net
{
	class LASwriteItemCompressed_WAVEPACKET13_v1 : LASwriteItemCompressed
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		struct LASwavepacket13
		{
			public ulong offset;
			public uint packet_size;
			public U32I32F32 return_point;
			public U32I32F32 x;
			public U32I32F32 y;
			public U32I32F32 z;
		}

		public LASwriteItemCompressed_WAVEPACKET13_v1(IEntropyEncoder enc)
		{
			// set encoder
			Debug.Assert(enc!=null);
			this.enc=enc;

			// create models and integer compressors
			m_packet_index=enc.createSymbolModel(256);
			m_offset_diff[0]=enc.createSymbolModel(4);
			m_offset_diff[1]=enc.createSymbolModel(4);
			m_offset_diff[2]=enc.createSymbolModel(4);
			m_offset_diff[3]=enc.createSymbolModel(4);
			ic_offset_diff=new IntegerCompressor(enc, 32);
			ic_packet_size=new IntegerCompressor(enc, 32);
			ic_return_point=new IntegerCompressor(enc, 32);
			ic_xyz=new IntegerCompressor(enc, 32, 3);
		}

		public unsafe override bool init(laszip_point item)
		{
			// init state
			last_diff_32=0;
			sym_last_offset_diff=0;

			// init models and integer compressors
			enc.initSymbolModel(m_packet_index);
			enc.initSymbolModel(m_offset_diff[0]);
			enc.initSymbolModel(m_offset_diff[1]);
			enc.initSymbolModel(m_offset_diff[2]);
			enc.initSymbolModel(m_offset_diff[3]);
			ic_offset_diff.initCompressor();
			ic_packet_size.initCompressor();
			ic_return_point.initCompressor();
			ic_xyz.initCompressor();

			// init last item
			fixed(byte* pItem=item.wave_packet)
			{
				last_item=*(LASwavepacket13*)(pItem+1);
			}

			return true;
		}

		public unsafe override bool write(laszip_point item)
		{
			enc.encodeSymbol(m_packet_index, item.wave_packet[0]);

			fixed(byte* pItem=item.wave_packet)
			{
				LASwavepacket13* wave=(LASwavepacket13*)(pItem+1);

				// calculate the difference between the two offsets
				long curr_diff_64=(long)(wave->offset-last_item.offset);
				int curr_diff_32=(int)curr_diff_64;

				// if the current difference can be represented with 32 bits
				if(curr_diff_64==(long)(curr_diff_32))
				{
					if(curr_diff_32==0) // current difference is zero
					{
						enc.encodeSymbol(m_offset_diff[sym_last_offset_diff], 0);
						sym_last_offset_diff=0;
					}
					else if(curr_diff_32==(int)last_item.packet_size) // current difference is size of last packet
					{
						enc.encodeSymbol(m_offset_diff[sym_last_offset_diff], 1);
						sym_last_offset_diff=1;
					}
					else // 
					{
						enc.encodeSymbol(m_offset_diff[sym_last_offset_diff], 2);
						sym_last_offset_diff=2;
						ic_offset_diff.compress(last_diff_32, curr_diff_32);
						last_diff_32=curr_diff_32;
					}
				}
				else
				{
					enc.encodeSymbol(m_offset_diff[sym_last_offset_diff], 3);
					sym_last_offset_diff=3;
					enc.writeInt64(wave->offset);
				}

				ic_packet_size.compress((int)last_item.packet_size, (int)wave->packet_size);
				ic_return_point.compress(last_item.return_point.i32, wave->return_point.i32);
				ic_xyz.compress(last_item.x.i32, wave->x.i32, 0);
				ic_xyz.compress(last_item.y.i32, wave->y.i32, 1);
				ic_xyz.compress(last_item.z.i32, wave->z.i32, 2);

				last_item=*wave;
			}

			return true;
		}

		IEntropyEncoder enc;
		LASwavepacket13 last_item;

		int last_diff_32;
		uint sym_last_offset_diff;
		IEntropyModel m_packet_index;
		IEntropyModel[] m_offset_diff=new IEntropyModel[4];
		IntegerCompressor ic_offset_diff;
		IntegerCompressor ic_packet_size;
		IntegerCompressor ic_return_point;
		IntegerCompressor ic_xyz;
	}
}
