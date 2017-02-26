//===============================================================================
//
//  FILE:  arithmeticdecoder.cs
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

using System;
using System.Diagnostics;
using System.IO;

namespace LASzip.Net
{
	class ArithmeticDecoder
	{
		// Constructor & Destructor
		public ArithmeticDecoder()
		{
			instream=null;
		}

		// Manage decoding
		public bool init(Stream instream)
		{
			if(instream==null) return false;
			this.instream=instream;

			length=AC.MaxLength;
			value=(uint)instream.ReadByte()<<24;
			value|=(uint)instream.ReadByte()<<16;
			value|=(uint)instream.ReadByte()<<8;
			value|=(uint)instream.ReadByte();

			return true;
		}

		public void done()
		{
			instream=null;
		}

		// Manage an entropy model for a single bit
		public ArithmeticBitModel createBitModel()
		{
			return new ArithmeticBitModel();
		}

		public void initBitModel(ArithmeticBitModel m)
		{
			m.init();
		}

		// Manage an entropy model for n symbols (table optional)
		public ArithmeticModel createSymbolModel(uint n)
		{
			return new ArithmeticModel(n, false);
		}

		public void initSymbolModel(ArithmeticModel m, uint[] table=null)
		{
			m.init(table);
		}

		// Decode a bit with modelling
		public uint decodeBit(ArithmeticBitModel m)
		{
			Debug.Assert(m!=null);

			uint x=m.bit_0_prob*(length>>BM.LengthShift); // product l x p0
			uint sym=(value>=x)?1u:0u; // decision

			// update & shift interval
			if(sym==0)
			{
				length=x;
				++m.bit_0_count;
			}
			else
			{
				value-=x; // shifted interval base = 0
				length-=x;
			}

			if(length<AC.MinLength) renorm_dec_interval(); // renormalization
			if(--m.bits_until_update==0) m.update(); // periodic model update

			return sym; // return data bit value
		}

		// Decode a symbol with modelling
		public uint decodeSymbol(ArithmeticModel m)
		{
			uint n, sym, x, y=length;

			if(m.decoder_table!=null)
			{ // use table look-up for faster decoding

				uint dv=value/(length>>=DM.LengthShift);
				uint t=dv>>m.table_shift;

				sym=m.decoder_table[t]; // initial decision based on table look-up
				n=m.decoder_table[t+1]+1;

				while(n>sym+1)
				{ // finish with bisection search
					uint k=(sym+n)>>1;
					if(m.distribution[k]>dv) n=k; else sym=k;
				}

				// compute products
				x=m.distribution[sym]*length;
				if(sym!=m.last_symbol) y=m.distribution[sym+1]*length;
			}
			else
			{ // decode using only multiplications
				x=sym=0;
				length>>=DM.LengthShift;
				uint k=(n=m.symbols)>>1;

				// decode via bisection search
				do
				{
					uint z=length*m.distribution[k];
					if(z>value)
					{
						n=k;
						y=z; // value is smaller
					}
					else
					{
						sym=k;
						x=z; // value is larger or equal
					}
				} while((k=(sym+n)>>1)!=sym);
			}

			value-=x; // update interval
			length=y-x;

			if(length<AC.MinLength) renorm_dec_interval(); // renormalization

			++m.symbol_count[sym];
			if(--m.symbols_until_update==0) m.update(); // periodic model update

			Debug.Assert(sym<m.symbols);

			return sym;
		}

		// Decode a bit without modelling
		public uint readBit()
		{
			uint sym=value/(length>>=1); // decode symbol, change length
			value-=length*sym; // update interval

			if(length<AC.MinLength) renorm_dec_interval(); // renormalization

			Debug.Assert(sym<2);

			return sym;
		}

		// Decode bits without modelling
		public uint readBits(uint bits)
		{
			Debug.Assert(bits!=0&&(bits<=32));

			if(bits>19)
			{
				uint tmp=readShort();
				bits=bits-16;
				uint tmp1=readBits(bits)<<16;
				return (tmp1|tmp);
			}

			uint sym=value/(length>>=(int)bits); // decode symbol, change length
			value-=length*sym; // update interval

			if(length<AC.MinLength) renorm_dec_interval(); // renormalization

			Debug.Assert(sym<(1u<<(int)bits));

			if(sym>=(1u<<(int)bits)) throw new Exception("4711");

			return sym;
		}

		// Decode an unsigned char without modelling
		public byte readByte()
		{
			uint sym=value/(length>>=8); // decode symbol, change length
			value-=length*sym; // update interval

			if(length<AC.MinLength) renorm_dec_interval(); // renormalization

			Debug.Assert(sym<(1u<<8));

			if(sym>=(1u<<8)) throw new Exception("4711");

			return (byte)sym;
		}

		// Decode an unsigned short without modelling
		public ushort readShort()
		{
			uint sym=value/(length>>=16); // decode symbol, change length
			value-=length*sym; // update interval

			if(length<AC.MinLength) renorm_dec_interval(); // renormalization

			Debug.Assert(sym<(1u<<16));

			if(sym>=(1u<<16)) throw new Exception("4711");

			return (ushort)sym;
		}

		// Decode an unsigned int without modelling
		public uint readInt()
		{
			uint lowerInt=readShort();
			uint upperInt=readShort();
			return (upperInt<<16)|lowerInt;
		}

		// Decode a float without modelling
		public unsafe float readFloat() // danger in float reinterpretation
		{
			uint ret=readInt();
			return *(float*)&ret;
		}

		// Decode an unsigned 64 bit int without modelling
		public ulong readInt64()
		{
			ulong lowerInt=readInt();
			ulong upperInt=readInt();
			return (upperInt<<32)|lowerInt;
		}

		// Decode a double without modelling
		public unsafe double readDouble() // danger in float reinterpretation
		{
			ulong ret=readInt64();
			return *(double*)&ret;
		}

		Stream instream;

		void renorm_dec_interval()
		{
			do
			{ // read least-significant byte
				value=(value<<8)|(uint)instream.ReadByte();
			} while((length<<=8)<AC.MinLength); // length multiplied by 256
		}

		uint value, length;
	}
}
