//===============================================================================
//
//  FILE:  arithmeticbitmodel.cs
//
//  CONTENTS:
//
//    C# port of a modular C++ wrapper for an adapted version of Amir Said's FastAC Code.
//    see: http://www.cipr.rpi.edu/~said/FastAC.html
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2014, martin isenburg, rapidlasso - tools to catch reality
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

// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
//                                                                           -
// Fast arithmetic coding implementation                                     -
// -> 32-bit variables, 32-bit product, periodic updates, table decoding     -
//                                                                           -
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
//                                                                           -
// Version 1.00  -  April 25, 2004                                           -
//                                                                           -
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
//                                                                           -
//                                  WARNING                                  -
//                                 =========                                 -
//                                                                           -
// The only purpose of this program is to demonstrate the basic principles   -
// of arithmetic coding. It is provided as is, without any express or        -
// implied warranty, without even the warranty of fitness for any particular -
// purpose, or that the implementations are correct.                         -
//                                                                           -
// Permission to copy and redistribute this code is hereby granted, provided -
// that this warning and copyright notices are not removed or altered.       -
//                                                                           -
// Copyright (c) 2004 by Amir Said (said@ieee.org) &                         -
//                       William A. Pearlman (pearlw@ecse.rpi.edu)           -
//                                                                           -
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
//                                                                           -
// A description of the arithmetic coding method used here is available in   -
//                                                                           -
// Lossless Compression Handbook, ed. K. Sayood                              -
// Chapter 5: Arithmetic Coding (A. Said), pp. 101-152, Academic Press, 2003 -
//                                                                           -
// A. Said, Introduction to Arithetic Coding Theory and Practice             -
// HP Labs report HPL-2004-76  -  http://www.hpl.hp.com/techreports/         -
//                                                                           -
// - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

namespace LASzip.Net
{
	class ArithmeticBitModel
	{
		public ArithmeticBitModel()
		{
			init();
		}

		public int init()
		{
			// initialization to equiprobable model
			bit_0_count=1;
			bit_count=2;
			bit_0_prob=1u<<(BM.LengthShift-1);

			// start with frequent updates
			update_cycle=bits_until_update=4;

			return 0;
		}

		internal void update()
		{
			// halve counts when a threshold is reached
			if((bit_count+=update_cycle)>BM.MaxCount)
			{
				bit_count=(bit_count+1)>>1;
				bit_0_count=(bit_0_count+1)>>1;
				if(bit_0_count==bit_count) ++bit_count;
			}

			// compute scaled bit 0 probability
			uint scale=0x80000000u/bit_count;
			bit_0_prob=(bit_0_count*scale)>>(31-BM.LengthShift);

			// set frequency of model updates
			update_cycle=(5*update_cycle)>>2;
			if(update_cycle>64) update_cycle=64;
			bits_until_update=update_cycle;
		}

		internal uint update_cycle, bits_until_update;
		internal uint bit_0_prob, bit_0_count, bit_count;
	}
}
