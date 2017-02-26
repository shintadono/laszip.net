//===============================================================================
//
//  FILE:  laswriteitemcompressed_byte_v2.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for BYTE items (version 2).
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
	class LASwriteItemCompressed_BYTE_v2 : LASwriteItemCompressed
	{
		public LASwriteItemCompressed_BYTE_v2(ArithmeticEncoder enc, uint number)
		{
			// set encoder
			Debug.Assert(enc!=null);
			this.enc=enc;
			Debug.Assert(number>0);
			this.number=number;

			// create models and integer compressors
			m_byte=new ArithmeticModel[number];
			for(uint i=0; i<number; i++)
			{
				m_byte[i]=enc.createSymbolModel(256);
			}

			// create last item
			last_item=new byte[number];
		}

		public override bool init(laszip.point item)
		{
			// init state

			// init models and integer compressors
			for(uint i=0; i<number; i++)
			{
				enc.initSymbolModel(m_byte[i]);
			}

			// init last item
			Buffer.BlockCopy(item.extra_bytes, 0, last_item, 0, (int)number);

			return true;
		}

		public override bool write(laszip.point item)
		{
			for(uint i=0; i<number; i++)
			{
				int diff=item.extra_bytes[i]-last_item[i];
				enc.encodeSymbol(m_byte[i], (byte)MyDefs.U8_FOLD(diff));
			}

			Buffer.BlockCopy(item.extra_bytes, 0, last_item, 0, (int)number);
			return true;
		}

		ArithmeticEncoder enc;
		uint number;
		byte[] last_item;

		ArithmeticModel[] m_byte;
	}
}
