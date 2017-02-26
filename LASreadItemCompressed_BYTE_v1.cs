//===============================================================================
//
//  FILE:  lasreaditemcompressed_byte_v1.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for BYTE items (version 1).
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
	class LASreadItemCompressed_BYTE_v1 : LASreadItemCompressed
	{
		public LASreadItemCompressed_BYTE_v1(ArithmeticDecoder dec, uint number)
		{
			// set decoder
			Debug.Assert(dec!=null);
			this.dec=dec;
			Debug.Assert(number!=0);
			this.number=number;

			// create models and integer compressors
			ic_byte=new IntegerCompressor(dec, 8, number);

			// create last item
			last_item=new byte[number];
		}

		public override bool init(laszip.point item)
		{
			// init state

			// init models and integer compressors
			ic_byte.initDecompressor();

			// init last item
			Buffer.BlockCopy(item.extra_bytes, 0, last_item, 0, (int)number);

			return true;
		}

		public override void read(laszip.point item)
		{
			for(uint i=0; i<number; i++)
			{
				last_item[i]=item.extra_bytes[i]=(byte)(ic_byte.decompress(last_item[i], i));
			}
		}

		ArithmeticDecoder dec;
		uint number;
		byte[] last_item;

		IntegerCompressor ic_byte;
	}
}
