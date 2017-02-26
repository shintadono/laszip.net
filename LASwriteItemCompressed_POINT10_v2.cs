//===============================================================================
//
//  FILE:  laswriteitemcompressed_point10_v2.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for POINT10 items (version 2).
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
	class LASwriteItemCompressed_POINT10_v2 : LASwriteItemCompressed
	{
		[StructLayout(LayoutKind.Sequential, Pack=1)]
		struct LASpoint10
		{
			public int x;
			public int y;
			public int z;
			public ushort intensity;

			// all these bits combine to flags
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

		public LASwriteItemCompressed_POINT10_v2(ArithmeticEncoder enc)
		{
			// set encoder
			Debug.Assert(enc!=null);
			this.enc=enc;

			// create models and integer compressors
			ic_dx=new IntegerCompressor(enc, 32, 2); // 32 bits, 2 context
			ic_dy=new IntegerCompressor(enc, 32, 22); // 32 bits, 22 contexts
			ic_z=new IntegerCompressor(enc, 32, 20); // 32 bits, 20 contexts
			ic_intensity=new IntegerCompressor(enc, 16, 4);
			m_scan_angle_rank[0]=enc.createSymbolModel(256);
			m_scan_angle_rank[1]=enc.createSymbolModel(256);
			ic_point_source_ID=new IntegerCompressor(enc, 16);
			m_changed_values=enc.createSymbolModel(64);
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
			for(int i=0; i<16; i++)
			{
				last_x_diff_median5[i].init();
				last_y_diff_median5[i].init();
				last_intensity[i]=0;
				last_height[i/2]=0;
			}

			// init models and integer compressors
			ic_dx.initCompressor();
			ic_dy.initCompressor();
			ic_z.initCompressor();
			ic_intensity.initCompressor();
			enc.initSymbolModel(m_scan_angle_rank[0]);
			enc.initSymbolModel(m_scan_angle_rank[1]);
			ic_point_source_ID.initCompressor();
			enc.initSymbolModel(m_changed_values);
			for(int i=0; i<256; i++)
			{
				if(m_bit_byte[i]!=null) enc.initSymbolModel(m_bit_byte[i]);
				if(m_classification[i]!=null) enc.initSymbolModel(m_classification[i]);
				if(m_user_data[i]!=null) enc.initSymbolModel(m_user_data[i]);
			}

			// init last item
			last.x=item.X;
			last.y=item.Y;
			last.z=item.Z;
			last.intensity=0; // but set intensity to zero
			last.flags=item.flags;
			last.classification_and_classification_flags = item.classification_and_classification_flags;
			last.scan_angle_rank=item.scan_angle_rank;
			last.user_data=item.user_data;
			last.point_source_ID=item.point_source_ID;

			return true;
		}

		public override bool write(laszip.point item)
		{
			uint r=item.return_number;
			uint n=item.number_of_returns;
			uint m=Laszip_Common_v2.number_return_map[n, r];
			uint l=Laszip_Common_v2.number_return_level[n, r];

			// compress which other values have changed
			uint changed_values=0;
			
			bool needFlags=last.flags!=item.flags; if(needFlags) changed_values|=32; // bit_byte
			bool needIntensity=last_intensity[m]!=item.intensity; if(needIntensity) changed_values|=16;
			bool needClassification=last.classification_and_classification_flags != item.classification_and_classification_flags; if(needClassification) changed_values|=8;
			bool needScanAngleRank=last.scan_angle_rank!=item.scan_angle_rank; if(needScanAngleRank) changed_values|=4;
			bool needUserData=last.user_data!=item.user_data; if(needUserData) changed_values|=2;
			bool needPointSourceID=last.point_source_ID!=item.point_source_ID; if(needPointSourceID) changed_values|=1;

			enc.encodeSymbol(m_changed_values, changed_values);

			// compress the bit_byte (edge_of_flight_line, scan_direction_flag, returns, ...) if it has changed
			if(needFlags)
			{
				if(m_bit_byte[last.flags]==null)
				{
					m_bit_byte[last.flags]=enc.createSymbolModel(256);
					enc.initSymbolModel(m_bit_byte[last.flags]);
				}
				enc.encodeSymbol(m_bit_byte[last.flags], item.flags);
			}

			// compress the intensity if it has changed
			if(needIntensity)
			{
				ic_intensity.compress(last_intensity[m], item.intensity, (m<3?m:3u));
				last_intensity[m]=item.intensity;
			}

			// compress the classification ... if it has changed
			if(needClassification)
			{
				if(m_classification[last.classification_and_classification_flags] ==null)
				{
					m_classification[last.classification_and_classification_flags] =enc.createSymbolModel(256);
					enc.initSymbolModel(m_classification[last.classification_and_classification_flags]);
				}
				enc.encodeSymbol(m_classification[last.classification_and_classification_flags], item.classification_and_classification_flags);
			}

			// compress the scan_angle_rank ... if it has changed
			if(needScanAngleRank)
			{
				enc.encodeSymbol(m_scan_angle_rank[item.scan_direction_flag], (uint)MyDefs.U8_FOLD(item.scan_angle_rank-last.scan_angle_rank));
			}

			// compress the user_data ... if it has changed
			if(needUserData)
			{
				if(m_user_data[last.user_data]==null)
				{
					m_user_data[last.user_data]=enc.createSymbolModel(256);
					enc.initSymbolModel(m_user_data[last.user_data]);
				}
				enc.encodeSymbol(m_user_data[last.user_data], item.user_data);
			}

			// compress the point_source_ID ... if it has changed
			if(needPointSourceID)
			{
				ic_point_source_ID.compress(last.point_source_ID, item.point_source_ID);
			}

			// compress x coordinate
			int median=last_x_diff_median5[m].get();
			int diff=item.X-last.x;
			ic_dx.compress(median, diff, n==1?1u:0u);
			last_x_diff_median5[m].add(diff);

			// compress y coordinate
			uint k_bits=ic_dx.getK();
			median=last_y_diff_median5[m].get();
			diff=item.Y-last.y;
			ic_dy.compress(median, diff, (n==1?1u:0u)+(k_bits<20?k_bits&0xFEu:20u)); // &0xFE round k_bits to next even number
			last_y_diff_median5[m].add(diff);

			// compress z coordinate
			k_bits=(ic_dx.getK()+ic_dy.getK())/2;
			ic_z.compress(last_height[l], item.Z, (n==1?1u:0u)+(k_bits<18?k_bits&0xFEu:18u)); // &0xFE round k_bits to next even number
			last_height[l]=item.Z;

			// copy the last point
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

		ArithmeticEncoder enc;
		LASpoint10 last=new LASpoint10();

		ushort[] last_intensity=new ushort[16];
		StreamingMedian5[] last_x_diff_median5=new StreamingMedian5[16];
		StreamingMedian5[] last_y_diff_median5=new StreamingMedian5[16];
		int[] last_height=new int[8];

		IntegerCompressor ic_dx;
		IntegerCompressor ic_dy;
		IntegerCompressor ic_z;
		IntegerCompressor ic_intensity;
		IntegerCompressor ic_point_source_ID;
		ArithmeticModel m_changed_values;
		ArithmeticModel[] m_scan_angle_rank=new ArithmeticModel[2];
		ArithmeticModel[] m_bit_byte=new ArithmeticModel[256];
		ArithmeticModel[] m_classification=new ArithmeticModel[256];
		ArithmeticModel[] m_user_data=new ArithmeticModel[256];
	}
}
