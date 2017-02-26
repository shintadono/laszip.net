//===============================================================================
//
//  FILE:  arithmeticencoder.cs
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

using System.Diagnostics;
using System.IO;

namespace LASzip.Net
{
	class ArithmeticEncoder
	{
		// Constructor & Destructor
		public ArithmeticEncoder()
		{
			outbuffer=new byte[2*AC.BUFFER_SIZE];
			endbuffer=2*AC.BUFFER_SIZE;
		}

		// Manage encoding
		public bool init(Stream outstream)
		{
			if(outstream==null) return false;
			this.outstream=outstream;
			interval_base=0;
			length=AC.MaxLength;
			outbyte=0;
			endbyte=endbuffer;
			return true;
		}

		public void done()
		{
			uint init_interval_base=interval_base; // done encoding: set final data bytes
			bool another_byte=true;

			if(length>2*AC.MinLength)
			{
				interval_base+=AC.MinLength; // base offset
				length=AC.MinLength>>1; // set new length for 1 more byte
			}
			else
			{
				interval_base+=AC.MinLength>>1; // interval base offset
				length=AC.MinLength>>9; // set new length for 2 more bytes
				another_byte=false;
			}

			if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			renorm_enc_interval(); // renormalization = output last bytes

			if(endbyte!=endbuffer)
			{
				Debug.Assert(outbyte<AC.BUFFER_SIZE);
				outstream.Write(outbuffer, AC.BUFFER_SIZE, AC.BUFFER_SIZE);
			}

			if(outbyte!=0) outstream.Write(outbuffer, 0, outbyte);

			// write two or three zero bytes to be in sync with the decoder's byte reads
			outstream.WriteByte(0);
			outstream.WriteByte(0);
			if(another_byte) outstream.WriteByte(0);

			outstream=null;
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
			return new ArithmeticModel(n, true);
		}

		public void initSymbolModel(ArithmeticModel m, uint[] table=null)
		{
			m.init(table);
		}

		// Encode a bit with modelling
		public void encodeBit(ArithmeticBitModel m, uint bit)
		{
			Debug.Assert(m!=null&&(bit<=1));

			uint x=m.bit_0_prob*(length>>BM.LengthShift); // product l x p0
			// update interval
			if(bit==0)
			{
				length=x;
				++m.bit_0_count;
			}
			else
			{
				uint init_interval_base=interval_base;
				interval_base+=x;
				length-=x;
				if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			}

			if(length<AC.MinLength) renorm_enc_interval(); // renormalization
			if(--m.bits_until_update==0) m.update(); // periodic model update
		}

		// Encode a symbol with modelling
		public void encodeSymbol(ArithmeticModel m, uint sym)
		{
			Debug.Assert(m!=null);

			Debug.Assert(sym<=m.last_symbol);
			uint x, init_interval_base=interval_base;

			// compute products
			if(sym==m.last_symbol)
			{
				x=m.distribution[sym]*(length>>DM.LengthShift);
				interval_base+=x; // update interval
				length-=x; // no product needed
			}
			else
			{
				x=m.distribution[sym]*(length>>=DM.LengthShift);
				interval_base+=x; // update interval
				length=m.distribution[sym+1]*length-x;
			}

			if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			if(length<AC.MinLength) renorm_enc_interval(); // renormalization

			++m.symbol_count[sym];
			if(--m.symbols_until_update==0) m.update(); // periodic model update
		}

		// Encode a bit without modelling
		public void writeBit(uint bit)
		{
			Debug.Assert(bit<2);

			uint init_interval_base=interval_base;
			interval_base+=bit*(length>>=1); // new interval base and length

			if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			if(length<AC.MinLength) renorm_enc_interval(); // renormalization
		}

		// Encode bits without modelling
		public void writeBits(int bits, uint sym)
		{
			Debug.Assert(bits!=0&&(bits<=32)&&(sym<(1u<<bits)));

			if(bits>19)
			{
				writeShort((ushort)sym);
				sym=sym>>16;
				bits=bits-16;
			}

			uint init_interval_base=interval_base;
			interval_base+=sym*(length>>=bits); // new interval base and length

			if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			if(length<AC.MinLength) renorm_enc_interval(); // renormalization
		}

		// Encode an unsigned char without modelling
		public void writeByte(byte sym)
		{
			uint init_interval_base=interval_base;
			interval_base+=(uint)(sym)*(length>>=8); // new interval base and length

			if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			if(length<AC.MinLength) renorm_enc_interval(); // renormalization
		}

		// Encode an unsigned short without modelling
		public void writeShort(ushort sym)
		{
			uint init_interval_base=interval_base;
			interval_base+=(uint)(sym)*(length>>=16); // new interval base and length

			if(init_interval_base>interval_base) propagate_carry(); // overflow = carry
			if(length<AC.MinLength) renorm_enc_interval(); // renormalization
		}

		// Encode an unsigned int without modelling
		public void writeInt(uint sym)
		{
			writeShort((ushort)(sym&0xFFFF)); // lower 16 bits
			writeShort((ushort)(sym>>16)); // UPPER 16 bits
		}

		// Encode a float without modelling
		public unsafe void writeFloat(float sym) // danger in float reinterpretation
		{
			writeInt(*(uint*)&sym);
		}

		// Encode an unsigned 64 bit int without modelling
		public void writeInt64(ulong sym)
		{
			writeInt((uint)(sym&0xFFFFFFFF)); // lower 32 bits
			writeInt((uint)(sym>>32)); // UPPER 32 bits
		}

		// Encode a double without modelling
		public unsafe void writeDouble(double sym) // danger in float reinterpretation
		{
			writeInt64(*(ulong*)&sym);
		}

		Stream outstream;

		void propagate_carry()
		{
			int p;
			if(outbyte==0) p=endbuffer-1;
			else p=outbyte-1;

			while(outbuffer[p]==0xFFU)
			{
				outbuffer[p]=0;
				if(p==0) p=endbuffer-1;
				else p--;

				Debug.Assert(p>=0);
				Debug.Assert(p<endbuffer);
				Debug.Assert(outbyte<endbuffer);
			}
			outbuffer[p]++;
		}

		void renorm_enc_interval()
		{
			do
			{ // output and discard top byte
				Debug.Assert(outbyte>=0);
				Debug.Assert(outbyte<endbuffer);
				Debug.Assert(outbyte<endbyte);
				outbuffer[outbyte++]=(byte)(interval_base>>24);
				if(outbyte==endbyte) manage_outbuffer();
				interval_base<<=8;
			} while((length<<=8)<AC.MinLength); // length multiplied by 256
		}

		void manage_outbuffer()
		{
			if(outbyte==endbuffer) outbyte=0;
			outstream.Write(outbuffer, outbyte, AC.BUFFER_SIZE);
			endbyte=outbyte+AC.BUFFER_SIZE;
			Debug.Assert(endbyte>outbyte);
			Debug.Assert(outbyte<endbuffer);
		}

		byte[] outbuffer;
		int endbuffer;
		int outbyte;
		int endbyte;
		uint interval_base, length;
	}
}
