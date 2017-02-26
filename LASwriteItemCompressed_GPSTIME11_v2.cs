//===============================================================================
//
//  FILE:  laswriteitemcompressed_gpstime11_v2.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for GPSTIME11 items (version 2).
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

using System.Diagnostics;

namespace LASzip.Net
{
	class LASwriteItemCompressed_GPSTIME11_v2 : LASwriteItemCompressed
	{
		const int LASZIP_GPSTIME_MULTI=500;
		const int LASZIP_GPSTIME_MULTI_MINUS=-10;
		const int LASZIP_GPSTIME_MULTI_UNCHANGED=(LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS+1);
		const int LASZIP_GPSTIME_MULTI_CODE_FULL=(LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS+2);

		const int LASZIP_GPSTIME_MULTI_TOTAL=(LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS+6);

		public LASwriteItemCompressed_GPSTIME11_v2(ArithmeticEncoder enc)
		{
			// set encoder
			Debug.Assert(enc!=null);
			this.enc=enc;

			// create entropy models and integer compressors
			m_gpstime_multi=enc.createSymbolModel(LASZIP_GPSTIME_MULTI_TOTAL);
			m_gpstime_0diff=enc.createSymbolModel(6);
			ic_gpstime=new IntegerCompressor(enc, 32, 9); // 32 bits, 9 contexts
		}

		public override bool init(laszip.point item)
		{
			// init state
			last=0; next=0;
			last_gpstime_diff[0]=0;
			last_gpstime_diff[1]=0;
			last_gpstime_diff[2]=0;
			last_gpstime_diff[3]=0;
			multi_extreme_counter[0]=0;
			multi_extreme_counter[1]=0;
			multi_extreme_counter[2]=0;
			multi_extreme_counter[3]=0;

			// init models and integer compressors
			enc.initSymbolModel(m_gpstime_multi);
			enc.initSymbolModel(m_gpstime_0diff);
			ic_gpstime.initCompressor();

			// init last item
			last_gpstime[0].f64=item.gps_time;
			last_gpstime[1].u64=0;
			last_gpstime[2].u64=0;
			last_gpstime[3].u64=0;
			return true;
		}

		public override bool write(laszip.point item)
		{
			U64I64F64 this_gpstime=new U64I64F64();
			this_gpstime.f64=item.gps_time;

			if(last_gpstime_diff[last]==0) // if the last integer difference was zero
			{
				if(this_gpstime.i64==last_gpstime[last].i64)
				{
					enc.encodeSymbol(m_gpstime_0diff, 0); // the doubles have not changed
				}
				else
				{
					// calculate the difference between the two doubles as an integer
					long curr_gpstime_diff_64=this_gpstime.i64-last_gpstime[last].i64;
					int curr_gpstime_diff=(int)curr_gpstime_diff_64;
					if(curr_gpstime_diff_64==(long)(curr_gpstime_diff))
					{
						enc.encodeSymbol(m_gpstime_0diff, 1); // the difference can be represented with 32 bits
						ic_gpstime.compress(0, curr_gpstime_diff, 0);
						last_gpstime_diff[last]=curr_gpstime_diff;
						multi_extreme_counter[last]=0;
					}
					else // the difference is huge
					{
						// maybe the double belongs to another time sequence
						for(uint i=1; i<4; i++)
						{
							long other_gpstime_diff_64=this_gpstime.i64-last_gpstime[(last+i)&3].i64;
							int other_gpstime_diff=(int)other_gpstime_diff_64;
							if(other_gpstime_diff_64==(long)(other_gpstime_diff))
							{
								enc.encodeSymbol(m_gpstime_0diff, i+2); // it belongs to another sequence 
								last=(last+i)&3;
								return write(item);
							}
						}
						// no other sequence found. start new sequence.
						enc.encodeSymbol(m_gpstime_0diff, 2);
						ic_gpstime.compress((int)(last_gpstime[last].u64>>32), (int)(this_gpstime.u64>>32), 8);
						enc.writeInt((uint)(this_gpstime.u64));
						next=(next+1)&3;
						last=next;
						last_gpstime_diff[last]=0;
						multi_extreme_counter[last]=0;
					}
					last_gpstime[last].i64=this_gpstime.i64;
				}
			}
			else // the last integer difference was *not* zero
			{
				if(this_gpstime.i64==last_gpstime[last].i64)
				{
					// if the doubles have not changed use a special symbol
					enc.encodeSymbol(m_gpstime_multi, LASZIP_GPSTIME_MULTI_UNCHANGED);
				}
				else
				{
					// calculate the difference between the two doubles as an integer
					long curr_gpstime_diff_64=this_gpstime.i64-last_gpstime[last].i64;
					int curr_gpstime_diff=(int)curr_gpstime_diff_64;

					// if the current gpstime difference can be represented with 32 bits
					if(curr_gpstime_diff_64==(long)(curr_gpstime_diff))
					{
						// compute multiplier between current and last integer difference
						double multi_f=(double)curr_gpstime_diff/(double)(last_gpstime_diff[last]);
						int multi=MyDefs.I32_QUANTIZE(multi_f);

						// compress the residual curr_gpstime_diff in dependance on the multiplier
						if(multi==1)
						{
							// this is the case we assume we get most often for regular spaced pulses
							enc.encodeSymbol(m_gpstime_multi, 1);
							ic_gpstime.compress(last_gpstime_diff[last], curr_gpstime_diff, 1);
							multi_extreme_counter[last]=0;
						}
						else if(multi>0)
						{
							if(multi<LASZIP_GPSTIME_MULTI) // positive multipliers up to LASZIP_GPSTIME_MULTI are compressed directly
							{
								enc.encodeSymbol(m_gpstime_multi, (uint)multi);
								if(multi<10)
									ic_gpstime.compress(multi*last_gpstime_diff[last], curr_gpstime_diff, 2);
								else
									ic_gpstime.compress(multi*last_gpstime_diff[last], curr_gpstime_diff, 3);
							}
							else
							{
								enc.encodeSymbol(m_gpstime_multi, LASZIP_GPSTIME_MULTI);
								ic_gpstime.compress(LASZIP_GPSTIME_MULTI*last_gpstime_diff[last], curr_gpstime_diff, 4);
								multi_extreme_counter[last]++;
								if(multi_extreme_counter[last]>3)
								{
									last_gpstime_diff[last]=curr_gpstime_diff;
									multi_extreme_counter[last]=0;
								}
							}
						}
						else if(multi<0)
						{
							if(multi>LASZIP_GPSTIME_MULTI_MINUS) // negative multipliers larger than LASZIP_GPSTIME_MULTI_MINUS are compressed directly
							{
								enc.encodeSymbol(m_gpstime_multi, (uint)(LASZIP_GPSTIME_MULTI-multi));
								ic_gpstime.compress(multi*last_gpstime_diff[last], curr_gpstime_diff, 5);
							}
							else
							{
								enc.encodeSymbol(m_gpstime_multi, LASZIP_GPSTIME_MULTI-LASZIP_GPSTIME_MULTI_MINUS);
								ic_gpstime.compress(LASZIP_GPSTIME_MULTI_MINUS*last_gpstime_diff[last], curr_gpstime_diff, 6);
								multi_extreme_counter[last]++;
								if(multi_extreme_counter[last]>3)
								{
									last_gpstime_diff[last]=curr_gpstime_diff;
									multi_extreme_counter[last]=0;
								}
							}
						}
						else
						{
							enc.encodeSymbol(m_gpstime_multi, 0);
							ic_gpstime.compress(0, curr_gpstime_diff, 7);
							multi_extreme_counter[last]++;
							if(multi_extreme_counter[last]>3)
							{
								last_gpstime_diff[last]=curr_gpstime_diff;
								multi_extreme_counter[last]=0;
							}
						}
					}
					else // the difference is huge
					{
						// maybe the double belongs to another time sequence
						for(uint i=1; i<4; i++)
						{
							long other_gpstime_diff_64=this_gpstime.i64-last_gpstime[(last+i)&3].i64;
							int other_gpstime_diff=(int)other_gpstime_diff_64;
							if(other_gpstime_diff_64==(long)(other_gpstime_diff))
							{
								// it belongs to this sequence 
								enc.encodeSymbol(m_gpstime_multi, LASZIP_GPSTIME_MULTI_CODE_FULL+i);
								last=(last+i)&3;
								return write(item);
							}
						}
						// no other sequence found. start new sequence.
						enc.encodeSymbol(m_gpstime_multi, LASZIP_GPSTIME_MULTI_CODE_FULL);
						ic_gpstime.compress((int)(last_gpstime[last].u64>>32), (int)(this_gpstime.u64>>32), 8);
						enc.writeInt((uint)(this_gpstime.u64));
						next=(next+1)&3;
						last=next;
						last_gpstime_diff[last]=0;
						multi_extreme_counter[last]=0;
					}
					last_gpstime[last].i64=this_gpstime.i64;
				}
			}

			return true;
		}

		ArithmeticEncoder enc;
		uint last, next;
		U64I64F64[] last_gpstime=new U64I64F64[4];
		int[] last_gpstime_diff=new int[4];
		int[] multi_extreme_counter=new int[4];

		ArithmeticModel m_gpstime_multi;
		ArithmeticModel m_gpstime_0diff;
		IntegerCompressor ic_gpstime;
	}
}
