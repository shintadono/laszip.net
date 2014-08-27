//===============================================================================
//
//  FILE:  ientropyencoder.cs
//
//  CONTENTS:
//
//    Interface for all entropy encoders.
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

using System.IO;

namespace laszip.net
{
	public interface IEntropyEncoder
	{
		// Manage decoding
		bool init(Stream outstream);
		void done();

		// Manage an entropy model for a single bit
		IEntropyModel createBitModel();
		void initBitModel(IEntropyModel model);

		// Manage an entropy model for n symbols (table optional)
		IEntropyModel createSymbolModel(uint n);
		void initSymbolModel(IEntropyModel model, uint[] init=null);

		// Encode a bit with modelling
		void encodeBit(IEntropyModel model, uint bit);

		// Encode a symbol with modelling
		void encodeSymbol(IEntropyModel model, uint sym);

		// Encode a bit without modelling
		void writeBit(uint sym);

		// Encode bits without modelling
		void writeBits(int bits, uint sym);

		// Encode an unsigned char without modelling
		void writeByte(byte sym);

		// Encode an unsigned short without modelling
		void writeShort(ushort sym);

		// Encode an unsigned int without modelling
		void writeInt(uint sym);

		// Encode a float without modelling
		void writeFloat(float sym);

		// Encode an unsigned 64 bit int without modelling
		void writeInt64(ulong sym);

		// Encode a double without modelling
		void writeDouble(double sym);
	}
}
