//===============================================================================
//
//  FILE:  laswriteitemcompressed_rgb12_v2.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for RGB12 items (version 2).
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
using System.Diagnostics;

namespace LASzip.Net
{
	class LASwriteItemCompressed_RGB12_v2 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_RGB12_v2(ArithmeticEncoder enc)
		{
			// set encoder
			Debug.Assert(enc!=null);
			this.enc=enc;

			// create models and integer compressors
			m_byte_used=enc.createSymbolModel(128);
			m_rgb_diff_0=enc.createSymbolModel(256);
			m_rgb_diff_1=enc.createSymbolModel(256);
			m_rgb_diff_2=enc.createSymbolModel(256);
			m_rgb_diff_3=enc.createSymbolModel(256);
			m_rgb_diff_4=enc.createSymbolModel(256);
			m_rgb_diff_5=enc.createSymbolModel(256);
		}

		public override bool init(laszip.point item)
		{
			// init state

			// init models and integer compressors
			enc.initSymbolModel(m_byte_used);
			enc.initSymbolModel(m_rgb_diff_0);
			enc.initSymbolModel(m_rgb_diff_1);
			enc.initSymbolModel(m_rgb_diff_2);
			enc.initSymbolModel(m_rgb_diff_3);
			enc.initSymbolModel(m_rgb_diff_4);
			enc.initSymbolModel(m_rgb_diff_5);

			// init last item
			Buffer.BlockCopy(item.rgb, 0, last_item, 0, 6);
			return true;
		}

		public override bool write(laszip.point item)
		{
			int diff_l=0;
			int diff_h=0;

			uint sym=0;

			bool rl=(last_item[0]&0x00FF)!=(item.rgb[0]&0x00FF); if(rl) sym|=1;
			bool rh=(last_item[0]&0xFF00)!=(item.rgb[0]&0xFF00); if(rh) sym|=2;
			bool gl=(last_item[1]&0x00FF)!=(item.rgb[1]&0x00FF); if(gl) sym|=4;
			bool gh=(last_item[1]&0xFF00)!=(item.rgb[1]&0xFF00); if(gh) sym|=8;
			bool bl=(last_item[2]&0x00FF)!=(item.rgb[2]&0x00FF); if(bl) sym|=16;
			bool bh=(last_item[2]&0xFF00)!=(item.rgb[2]&0xFF00); if(bh) sym|=32;
			
			bool allColors=((item.rgb[0]&0x00FF)!=(item.rgb[1]&0x00FF))||((item.rgb[0]&0x00FF)!=(item.rgb[2]&0x00FF))||
				((item.rgb[0]&0xFF00)!=(item.rgb[1]&0xFF00))||((item.rgb[0]&0xFF00)!=(item.rgb[2]&0xFF00));
			if(allColors) sym|=64;

			enc.encodeSymbol(m_byte_used, sym);
			if(rl)
			{
				diff_l=((int)(item.rgb[0]&255))-(last_item[0]&255);
				enc.encodeSymbol(m_rgb_diff_0, (byte)MyDefs.U8_FOLD(diff_l));
			}
			if(rh)
			{
				diff_h=((int)(item.rgb[0]>>8))-(last_item[0]>>8);
				enc.encodeSymbol(m_rgb_diff_1, (byte)MyDefs.U8_FOLD(diff_h));
			}

			if(allColors)
			{
				if(gl)
				{
					int corr=((int)(item.rgb[1]&255))-MyDefs.U8_CLAMP(diff_l+(last_item[1]&255));
					enc.encodeSymbol(m_rgb_diff_2, (byte)MyDefs.U8_FOLD(corr));
				}
				if(bl)
				{
					diff_l=(diff_l+(item.rgb[1]&255)-(last_item[1]&255))/2;
					int corr=((int)(item.rgb[2]&255))-MyDefs.U8_CLAMP(diff_l+(last_item[2]&255));
					enc.encodeSymbol(m_rgb_diff_4, (byte)MyDefs.U8_FOLD(corr));
				}
				if(gh)
				{
					int corr=((int)(item.rgb[1]>>8))-MyDefs.U8_CLAMP(diff_h+(last_item[1]>>8));
					enc.encodeSymbol(m_rgb_diff_3, (byte)MyDefs.U8_FOLD(corr));
				}
				if(bh)
				{
					diff_h=(diff_h+(item.rgb[1]>>8)-(last_item[1]>>8))/2;
					int corr=((int)(item.rgb[2]>>8))-MyDefs.U8_CLAMP(diff_h+(last_item[2]>>8));
					enc.encodeSymbol(m_rgb_diff_5, (byte)MyDefs.U8_FOLD(corr));
				}
			}

			last_item[0]=item.rgb[0];
			last_item[1]=item.rgb[1];
			last_item[2]=item.rgb[2];

			return true;
		}

		ArithmeticEncoder enc;
		ushort[] last_item=new ushort[3];

		ArithmeticModel m_byte_used;
		ArithmeticModel m_rgb_diff_0;
		ArithmeticModel m_rgb_diff_1;
		ArithmeticModel m_rgb_diff_2;
		ArithmeticModel m_rgb_diff_3;
		ArithmeticModel m_rgb_diff_4;
		ArithmeticModel m_rgb_diff_5;
	}
}
