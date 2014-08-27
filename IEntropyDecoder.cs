//===============================================================================
//
//  FILE:  ientropydecoder.cs
//
//  CONTENTS:
//
//    Interface for all entropy decoders.
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
	public interface IEntropyDecoder
	{
		// Manage decoding
		bool init(Stream instream);
		void done();

		// Manage an entropy model for a single bit
		IEntropyModel createBitModel();
		void initBitModel(IEntropyModel model);

		// Manage an entropy model for n symbols (table optional)
		IEntropyModel createSymbolModel(uint n);
		void initSymbolModel(IEntropyModel model, uint[] init=null);

		// Decode a bit with modelling
		uint decodeBit(IEntropyModel model);

		// Decode a symbol with modelling
		uint decodeSymbol(IEntropyModel model);

		// Decode a bit without modelling
		uint readBit();

		// Decode bits without modelling
		uint readBits(uint bits);

		// Decode an unsigned char without modelling
		byte readByte();

		// Decode an unsigned short without modelling
		ushort readShort();

		// Decode an unsigned int without modelling
		uint readInt();

		// Decode a float without modelling
		float readFloat();

		// Decode an unsigned 64 bit int without modelling
		ulong readInt64();

		// Decode a double without modelling
		double readDouble();
	}
}
