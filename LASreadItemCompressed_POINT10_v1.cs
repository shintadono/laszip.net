//===============================================================================
//
//  FILE:  lasreaditemcompressed_point10_v1.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for POINT10 items (version 1).
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

namespace LASzip.Net
{
	class LASreadItemCompressed_POINT10_v1 : LASreadItemCompressed
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		struct LASpoint10
		{
			public int x;
			public int y;
			public int z;
			public ushort intensity;

			// all the following bits combine to flags
			//public byte return_number : 3;
			//public byte number_of_returns : 3;
			//public byte scan_direction_flag : 1;
			//public byte edge_of_flight_line : 1;
			public byte flags;

			// all the following bits combine to classification_and_classification_flags
			//public byte classification : 5;
			//public byte synthetic_flag : 1;
			//public byte keypoint_flag : 1;
			//public byte withheld_flag : 1;
			public byte classification_and_classification_flags;
			public sbyte scan_angle_rank;
			public byte user_data;
			public ushort point_source_ID;
		}

		public LASreadItemCompressed_POINT10_v1(ArithmeticDecoder dec)
		{
			// set decoder
			Debug.Assert(dec!=null);
			this.dec=dec;

			// create models and integer compressors
			ic_dx=new IntegerCompressor(dec, 32); // 32 bits, 1 context
			ic_dy=new IntegerCompressor(dec, 32, 20); // 32 bits, 20 contexts
			ic_z=new IntegerCompressor(dec, 32, 20); // 32 bits, 20 contexts
			ic_intensity=new IntegerCompressor(dec, 16);
			ic_scan_angle_rank=new IntegerCompressor(dec, 8, 2);
			ic_point_source_ID=new IntegerCompressor(dec, 16);
			m_changed_values=dec.createSymbolModel(64);
			for(int i=0; i<256; i++)
			{
				m_bit_byte[i]=null;
				m_classification[i]=null;
				m_user_data[i]=null;
			}
		}

		public override bool init(laszip.point item)
		{
			// init state
			last_x_diff[0]=last_x_diff[1]=last_x_diff[2]=0;
			last_y_diff[0]=last_y_diff[1]=last_y_diff[2]=0;
			last_incr=0;

			// init models and integer compressors
			ic_dx.initDecompressor();
			ic_dy.initDecompressor();
			ic_z.initDecompressor();
			ic_intensity.initDecompressor();
			ic_scan_angle_rank.initDecompressor();
			ic_point_source_ID.initDecompressor();
			dec.initSymbolModel(m_changed_values);
			for(int i=0; i<256; i++)
			{
				if(m_bit_byte[i]!=null) dec.initSymbolModel(m_bit_byte[i]);
				if(m_classification[i]!=null) dec.initSymbolModel(m_classification[i]);
				if(m_user_data[i]!=null) dec.initSymbolModel(m_user_data[i]);
			}

			// init last item
			last.x=item.X;
			last.y=item.Y;
			last.z=item.Z;
			last.intensity=item.intensity;
			last.flags=item.flags;
			last.classification_and_classification_flags = item.classification_and_classification_flags;
			last.scan_angle_rank=item.scan_angle_rank;
			last.user_data=item.user_data;
			last.point_source_ID=item.point_source_ID;

			return true;
		}

		public override void read(laszip.point item)
		{
			// find median difference for x and y from 3 preceding differences
			int median_x;
			if(last_x_diff[0]<last_x_diff[1])
			{
				if(last_x_diff[1]<last_x_diff[2]) median_x=last_x_diff[1];
				else if(last_x_diff[0]<last_x_diff[2]) median_x=last_x_diff[2];
				else median_x=last_x_diff[0];
			}
			else
			{
				if(last_x_diff[0]<last_x_diff[2]) median_x=last_x_diff[0];
				else if(last_x_diff[1]<last_x_diff[2]) median_x=last_x_diff[2];
				else median_x=last_x_diff[1];
			}

			int median_y;
			if(last_y_diff[0]<last_y_diff[1])
			{
				if(last_y_diff[1]<last_y_diff[2]) median_y=last_y_diff[1];
				else if(last_y_diff[0]<last_y_diff[2]) median_y=last_y_diff[2];
				else median_y=last_y_diff[0];
			}
			else
			{
				if(last_y_diff[0]<last_y_diff[2]) median_y=last_y_diff[0];
				else if(last_y_diff[1]<last_y_diff[2]) median_y=last_y_diff[2];
				else median_y=last_y_diff[1];
			}

			// decompress x y z coordinates
			int x_diff=ic_dx.decompress(median_x);
			last.x+=x_diff;

			// we use the number k of bits corrector bits to switch contexts
			uint k_bits=ic_dx.getK();
			int y_diff=ic_dy.decompress(median_y, (k_bits<19?k_bits:19u));
			last.y+=y_diff;

			k_bits=(k_bits+ic_dy.getK())/2;
			last.z=ic_z.decompress(last.z, (k_bits<19?k_bits:19u));

			// decompress which other values have changed
			uint changed_values=dec.decodeSymbol(m_changed_values);

			if(changed_values!=0)
			{
				// decompress the intensity if it has changed
				if((changed_values&32)!=0)
				{
					last.intensity=(ushort)ic_intensity.decompress(last.intensity);
				}

				// decompress the edge_of_flight_line, scan_direction_flag, ... if it has changed
				if((changed_values&16)!=0)
				{
					if(m_bit_byte[last.flags]==null)
					{
						m_bit_byte[last.flags]=dec.createSymbolModel(256);
						dec.initSymbolModel(m_bit_byte[last.flags]);
					}
					last.flags=(byte)dec.decodeSymbol(m_bit_byte[last.flags]);
				}

				// decompress the classification ... if it has changed
				if((changed_values&8)!=0)
				{
					if(m_classification[last.classification_and_classification_flags] ==null)
					{
						m_classification[last.classification_and_classification_flags] =dec.createSymbolModel(256);
						dec.initSymbolModel(m_classification[last.classification_and_classification_flags]);
					}
					last.classification_and_classification_flags = (byte)dec.decodeSymbol(m_classification[last.classification_and_classification_flags]);
				}

				// decompress the scan_angle_rank ... if it has changed
				if((changed_values&4)!=0)
				{
					last.scan_angle_rank=(sbyte)(byte)ic_scan_angle_rank.decompress((byte)last.scan_angle_rank, k_bits<3?1u:0u);
				}

				// decompress the user_data ... if it has changed
				if((changed_values&2)!=0)
				{
					if(m_user_data[last.user_data]==null)
					{
						m_user_data[last.user_data]=dec.createSymbolModel(256);
						dec.initSymbolModel(m_user_data[last.user_data]);
					}
					last.user_data=(byte)dec.decodeSymbol(m_user_data[last.user_data]);
				}

				// decompress the point_source_ID ... if it has changed
				if((changed_values&1)!=0)
				{
					last.point_source_ID=(ushort)ic_point_source_ID.decompress(last.point_source_ID);
				}
			}

			// record the difference
			last_x_diff[last_incr]=x_diff;
			last_y_diff[last_incr]=y_diff;
			last_incr++;
			if(last_incr>2) last_incr=0;

			// copy the last point
			item.X=last.x;
			item.Y=last.y;
			item.Z=last.z;
			item.intensity=last.intensity;
			item.flags=last.flags;
			item.classification_and_classification_flags = last.classification_and_classification_flags;
			item.scan_angle_rank=last.scan_angle_rank;
			item.user_data=last.user_data;
			item.point_source_ID=last.point_source_ID;
		}

		ArithmeticDecoder dec;
		LASpoint10 last=new LASpoint10();

		int[] last_x_diff=new int[3];
		int[] last_y_diff=new int[3];
		int last_incr;
		IntegerCompressor ic_dx;
		IntegerCompressor ic_dy;
		IntegerCompressor ic_z;
		IntegerCompressor ic_intensity;
		IntegerCompressor ic_scan_angle_rank;
		IntegerCompressor ic_point_source_ID;

		ArithmeticModel m_changed_values;
		ArithmeticModel[] m_bit_byte=new ArithmeticModel[256];
		ArithmeticModel[] m_classification=new ArithmeticModel[256];
		ArithmeticModel[] m_user_data=new ArithmeticModel[256];
	}
}
