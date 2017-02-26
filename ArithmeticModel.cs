//===============================================================================
//
//  FILE:  arithmeticmodel.cs
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
//    (c) 2005-2017, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2014-2017 by Shinta <shintadono@googlemail.com>
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
	class ArithmeticModel
	{
		public ArithmeticModel(uint symbols, bool compress)
		{
			this.symbols = symbols;
			this.compress = compress;
			distribution = null;
		}

		public int init(uint[] table = null)
		{
			if (distribution == null)
			{
				if ((symbols < 2) || (symbols > (1 << 11)))
					return -1; // invalid number of symbols

				try
				{
					last_symbol = symbols - 1;
					if ((!compress) && (symbols > 16))
					{
						int table_bits = 3;
						while (symbols > (1u << (table_bits + 2))) table_bits++;
						table_size = 1u << table_bits;
						table_shift = DM.LengthShift - table_bits;

						decoder_table = new uint[table_size + 2];
					}
					else // small alphabet: no table needed
					{
						decoder_table = null;
						table_size = 0; table_shift = 0;
					}

					distribution = new uint[symbols];
					symbol_count = new uint[symbols];
				}
				catch
				{
					return -1; // "cannot allocate model memory");
				}
			}

			total_count = 0;
			update_cycle = symbols;
			if (table != null) for (uint k = 0; k < symbols; k++) symbol_count[k] = table[k];
			else for (uint k = 0; k < symbols; k++) symbol_count[k] = 1;

			update();
			symbols_until_update = update_cycle = (symbols + 6) >> 1;

			return 0;
		}

		internal void update()
		{
			// halve counts when a threshold is reached
			if ((total_count += update_cycle) > DM.MaxCount)
			{
				total_count = 0;
				for (uint n = 0; n < symbols; n++)
				{
					total_count += (symbol_count[n] = (symbol_count[n] + 1) >> 1);
				}
			}

			// compute cumulative distribution, decoder table
			uint sum = 0, s = 0;
			uint scale = 0x80000000u / total_count;

			if (compress || (table_size == 0))
			{
				for (uint k = 0; k < symbols; k++)
				{
					distribution[k] = (scale * sum) >> (31 - DM.LengthShift);
					sum += symbol_count[k];
				}
			}
			else
			{
				for (uint k = 0; k < symbols; k++)
				{
					distribution[k] = (scale * sum) >> (31 - DM.LengthShift);
					sum += symbol_count[k];
					uint w = distribution[k] >> table_shift;
					while (s < w) decoder_table[++s] = k - 1;
				}
				decoder_table[0] = 0;
				while (s <= table_size) decoder_table[++s] = symbols - 1;
			}

			// set frequency of model updates
			update_cycle = (5 * update_cycle) >> 2;
			uint max_cycle = (symbols + 6) << 3;
			if (update_cycle > max_cycle) update_cycle = max_cycle;
			symbols_until_update = update_cycle;
		}

		internal uint[] distribution, symbol_count, decoder_table;
		internal uint total_count, update_cycle, symbols_until_update;
		internal uint symbols, last_symbol, table_size;
		internal int table_shift;
		bool compress;
	}
}
