//===============================================================================
//
//  FILE:  integercompressor.cs
//
//  CONTENTS:
//
//    This compressor provides three different contexts for encoding integer
//    numbers whose range may lie anywhere between 1 and 31 bits, which is
//    specified with the SetPrecision function.
//
//    The compressor encodes two things:
//
//      (1) the number k of miss-predicted low-order bits and
//      (2) the k-bit number that corrects the missprediction
//
//    The k-bit number is usually coded broken in two chunks. The highest
//    bits are compressed using an arithmetic range table. The lower bits
//    are stored raw without predicive coding. How many of the higher bits
//    are compressed can be specified with bits_high. The default is 8.
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

using System.Diagnostics;

namespace LASzip.Net
{
	class IntegerCompressor
	{
		// Constructor & Deconstructor
		public IntegerCompressor(ArithmeticEncoder enc, uint bits = 16, uint contexts = 1, uint bits_high = 8, uint range = 0)
		{
			Debug.Assert(enc != null);
			this.enc = enc;
			this.dec = null;

			Init(bits, contexts, bits_high, range);
		}

		public IntegerCompressor(ArithmeticDecoder dec, uint bits = 16, uint contexts = 1, uint bits_high = 8, uint range = 0)
		{
			Debug.Assert(dec != null);
			this.enc = null;
			this.dec = dec;

			Init(bits, contexts, bits_high, range);
		}

		void Init(uint bits = 16, uint contexts = 1, uint bits_high = 8, uint range = 0)
		{
			this.bits = bits;
			this.contexts = contexts;
			this.bits_high = bits_high;
			this.range = range;

			if (range != 0) // the corrector's significant bits and range
			{
				corr_bits = 0;
				corr_range = range;
				while (range != 0)
				{
					range = range >> 1;
					corr_bits++;
				}
				if (corr_range == (1u << ((int)corr_bits - 1)))
				{
					corr_bits--;
				}
				// the corrector must fall into this interval
				corr_min = -((int)(corr_range / 2));
				corr_max = (int)(corr_min + corr_range - 1);
			}
			else if (bits != 0 && bits < 32)
			{
				corr_bits = bits;
				corr_range = 1u << (int)bits;
				// the corrector must fall into this interval
				corr_min = -((int)(corr_range / 2));
				corr_max = (int)(corr_min + corr_range - 1);
			}
			else
			{
				corr_bits = 32;
				corr_range = 0;
				// the corrector must fall into this interval
				corr_min = int.MinValue;
				corr_max = int.MaxValue;
			}

			k = 0;

			mBits = null;
			mCorrector = null;
		}

		// Manage Compressor
		public void initCompressor()
		{
			Debug.Assert(enc != null);

			// maybe create the models
			if (mBits == null)
			{
				mBits = new ArithmeticModel[contexts];
				for (uint i = 0; i < contexts; i++)
				{
					mBits[i] = enc.createSymbolModel(corr_bits + 1);
				}
#if !COMPRESS_ONLY_K
				mCorrector = new ArithmeticModel[corr_bits + 1];
				mCorrectorBit = enc.createBitModel();
				for (uint i = 1; i <= corr_bits; i++)
				{
					if (i <= bits_high)
					{
						mCorrector[i] = enc.createSymbolModel(1u << (int)i);
					}
					else
					{
						mCorrector[i] = enc.createSymbolModel(1u << (int)bits_high);
					}
				}
#endif
			}

			// certainly init the models
			for (uint i = 0; i < contexts; i++)
			{
				enc.initSymbolModel(mBits[i]);
			}

#if !COMPRESS_ONLY_K
			enc.initBitModel(mCorrectorBit);
			for (uint i = 1; i <= corr_bits; i++)
			{
				enc.initSymbolModel(mCorrector[i]);
			}
#endif
		}

		public void compress(int pred, int real, uint context = 0)
		{
			Debug.Assert(enc != null);

			// the corrector will be within the interval [ - (corr_range - 1)  ...  + (corr_range - 1) ]
			int corr = real - pred;

			// we fold the corrector into the interval [ corr_min  ...  corr_max ]
			if (corr < corr_min) corr += (int)corr_range;
			else if (corr > corr_max) corr -= (int)corr_range;
			writeCorrector(corr, mBits[context]);
		}

		// Manage Decompressor
		public void initDecompressor()
		{
			Debug.Assert(dec != null);

			// maybe create the models
			if (mBits == null)
			{
				mBits = new ArithmeticModel[contexts];
				for (uint i = 0; i < contexts; i++)
				{
					mBits[i] = dec.createSymbolModel(corr_bits + 1);
				}

#if !COMPRESS_ONLY_K
				mCorrector = new ArithmeticModel[corr_bits + 1];
				mCorrectorBit = dec.createBitModel();
				for (uint i = 1; i <= corr_bits; i++)
				{
					if (i <= bits_high)
					{
						mCorrector[i] = dec.createSymbolModel(1u << (int)i);
					}
					else
					{
						mCorrector[i] = dec.createSymbolModel(1u << (int)bits_high);
					}
				}
#endif
			}

			// certainly init the models
			for (uint i = 0; i < contexts; i++)
			{
				dec.initSymbolModel(mBits[i]);
			}

#if !COMPRESS_ONLY_K
			dec.initBitModel(mCorrectorBit);
			for (uint i = 1; i <= corr_bits; i++)
			{
				dec.initSymbolModel(mCorrector[i]);
			}
#endif
		}

		public int decompress(int pred, uint context = 0)
		{
			Debug.Assert(dec != null);

			int real = pred + readCorrector(mBits[context]);
			if (real < 0) real += (int)corr_range;
			else if ((uint)(real) >= corr_range) real -= (int)corr_range;
			return real;
		}

		// Get the k corrector bits from the last compress/decompress call
		public uint getK() { return k; }

		void writeCorrector(int c, ArithmeticModel model)
		{
			// find the tighest interval [ - (2^k - 1)  ...  + (2^k) ] that contains c
			k = 0;

			// do this by checking the absolute value of c (adjusted for the case that c is 2^k)
			uint c1 = (uint)(c <= 0 ? -c : c - 1);

			// this loop could be replaced with more efficient code
			while (c1 != 0)
			{
				c1 = c1 >> 1;
				k = k + 1;
			}

			// the number k is between 0 and corr_bits and describes the interval the corrector falls into
			// we can compress the exact location of c within this interval using k bits
			enc.encodeSymbol(model, k);

#if COMPRESS_ONLY_K
			if (k != 0) // then c is either smaller than 0 or bigger than 1
			{
				Debug.Assert((c != 0) && (c != 1));
				if (k < 32)
				{
					// translate the corrector c into the k-bit interval [ 0 ... 2^k - 1 ]
					if (c < 0) // then c is in the interval [ - (2^k - 1)  ...  - (2^(k-1)) ]
					{
						// so we translate c into the interval [ 0 ...  + 2^(k-1) - 1 ] by adding (2^k - 1)
						enc.writeBits((int)k, (uint)(c + ((1 << (int)k) - 1)));
					}
					else // then c is in the interval [ 2^(k-1) + 1  ...  2^k ]
					{
						// so we translate c into the interval [ 2^(k-1) ...  + 2^k - 1 ] by subtracting 1
						enc.writeBits((int)k, (uint)(c - 1));
					}
				}
			}
			else // then c is 0 or 1
			{
				Debug.Assert((c == 0) || (c == 1));
				enc.writeBit((uint)c);
			}
#else // COMPRESS_ONLY_K
			if (k != 0) // then c is either smaller than 0 or bigger than 1
			{
				Debug.Assert((c != 0) && (c != 1));
				if (k < 32)
				{
					// translate the corrector c into the k-bit interval [ 0 ... 2^k - 1 ]
					if (c < 0) // then c is in the interval [ - (2^k - 1)  ...  - (2^(k-1)) ]
					{
						// so we translate c into the interval [ 0 ...  + 2^(k-1) - 1 ] by adding (2^k - 1)
						c += ((1 << (int)k) - 1);
					}
					else // then c is in the interval [ 2^(k-1) + 1  ...  2^k ]
					{
						// so we translate c into the interval [ 2^(k-1) ...  + 2^k - 1 ] by subtracting 1
						c -= 1;
					}
					if (k <= bits_high) // for small k we code the interval in one step
					{
						// compress c with the range coder
						enc.encodeSymbol(mCorrector[k], (uint)c);
					}
					else // for larger k we need to code the interval in two steps
					{
						// figure out how many lower bits there are
						int k1 = (int)k - (int)bits_high;
						// c1 represents the lowest k-bits_high+1 bits
						c1 = (uint)(c & ((1 << k1) - 1));
						// c represents the highest bits_high bits
						c = c >> k1;
						// compress the higher bits using a context table
						enc.encodeSymbol(mCorrector[k], (uint)c);
						// store the lower k1 bits raw
						enc.writeBits(k1, c1);
					}
				}
			}
			else // then c is 0 or 1
			{
				Debug.Assert((c == 0) || (c == 1));
				enc.encodeBit(mCorrectorBit, (uint)c);
			}
#endif // COMPRESS_ONLY_K
		}

		int readCorrector(ArithmeticModel model)
		{
			int c;

			// decode within which interval the corrector is falling
			k = dec.decodeSymbol(model);

			// decode the exact location of the corrector within the interval

#if COMPRESS_ONLY_K
			if (k != 0) // then c is either smaller than 0 or bigger than 1
			{
				if (k < 32)
				{
					c = (int)dec.readBits(k);

					if (c >= (1 << ((int)k - 1))) // if c is in the interval [ 2^(k-1)  ...  + 2^k - 1 ]
					{
						// so we translate c back into the interval [ 2^(k-1) + 1  ...  2^k ] by adding 1 
						c += 1;
					}
					else // otherwise c is in the interval [ 0 ...  + 2^(k-1) - 1 ]
					{
						// so we translate c back into the interval [ - (2^k - 1)  ...  - (2^(k-1)) ] by subtracting (2^k - 1)
						c -= ((1 << (int)k) - 1);
					}
				}
				else
				{
					c = corr_min;
				}
			}
			else // then c is either 0 or 1
			{
				c = (int)dec.readBit();
			}
#else // COMPRESS_ONLY_K
			if (k != 0) // then c is either smaller than 0 or bigger than 1
			{
				if (k < 32)
				{
					if (k <= bits_high) // for small k we can do this in one step
					{
						// decompress c with the range coder
						c = (int)dec.decodeSymbol(mCorrector[k]);
					}
					else
					{
						// for larger k we need to do this in two steps
						uint k1 = k - bits_high;
						// decompress higher bits with table
						c = (int)dec.decodeSymbol(mCorrector[k]);
						// read lower bits raw
						int c1 = (int)dec.readBits(k1);
						// put the corrector back together
						c = (c << (int)k1) | c1;
					}
					// translate c back into its correct interval
					if (c >= (1 << ((int)k - 1))) // if c is in the interval [ 2^(k-1)  ...  + 2^k - 1 ]
					{
						// so we translate c back into the interval [ 2^(k-1) + 1  ...  2^k ] by adding 1 
						c += 1;
					}
					else // otherwise c is in the interval [ 0 ...  + 2^(k-1) - 1 ]
					{
						// so we translate c back into the interval [ - (2^k - 1)  ...  - (2^(k-1)) ] by subtracting (2^k - 1)
						c -= ((1 << (int)k) - 1);
					}
				}
				else
				{
					c = corr_min;
				}
			}
			else // then c is either 0 or 1
			{
				c = (int)dec.decodeBit(mCorrectorBit);
			}
#endif // COMPRESS_ONLY_K

			return c;
		}

		uint k;

		uint contexts;
		uint bits_high;

		uint bits;
		uint range;

		uint corr_bits;
		uint corr_range;
		int corr_min;
		int corr_max;

		ArithmeticEncoder enc;
		ArithmeticDecoder dec;

		ArithmeticModel[] mBits;
		ArithmeticModel[] mCorrector; // mCorrector[0] will always be null... the content of mCorrector[0] has been moved to mCorrectorBit
		ArithmeticBitModel mCorrectorBit;
	}
}
