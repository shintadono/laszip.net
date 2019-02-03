//===============================================================================
//
//  FILE:  laswriteitemcompressed_point14_v4.cs
//
//  CONTENTS:
//
//    Implementation of LASwriteItemCompressed for POINT14 items (version 4).
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2018-2019 by Shinta <shintadono@googlemail.com>
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
using System.IO;

namespace LASzip.Net
{
	class LASwriteItemCompressed_POINT14_v4 : LASwriteItemCompressed
	{
		const int LASZIP_GPSTIME_MULTI = 500;
		const int LASZIP_GPSTIME_MULTI_MINUS = -10;
		const int LASZIP_GPSTIME_MULTI_CODE_FULL = LASZIP_GPSTIME_MULTI - LASZIP_GPSTIME_MULTI_MINUS + 1;

		const int LASZIP_GPSTIME_MULTI_TOTAL = LASZIP_GPSTIME_MULTI - LASZIP_GPSTIME_MULTI_MINUS + 5;

		public LASwriteItemCompressed_POINT14_v4(ArithmeticEncoder enc)
		{
			// not used as a encoder. just gives access to outstream
			Debug.Assert(enc != null);
			this.enc = enc;

			// zero outstreams and encoders
			outstream_channel_returns_XY = null;
			outstream_Z = null;
			outstream_classification = null;
			outstream_flags = null;
			outstream_intensity = null;
			outstream_scan_angle = null;
			outstream_user_data = null;
			outstream_point_source = null;
			outstream_gps_time = null;

			enc_channel_returns_XY = null;
			enc_Z = null;
			enc_classification = null;
			enc_flags = null;
			enc_intensity = null;
			enc_scan_angle = null;
			enc_user_data = null;
			enc_point_source = null;
			enc_gps_time = null;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_changed_values[0] = null;
			}
			current_context = 0;

			// number of bytes per layer
			num_bytes_channel_returns_XY = 0;
			num_bytes_Z = 0;
			num_bytes_classification = 0;
			num_bytes_flags = 0;
			num_bytes_intensity = 0;
			num_bytes_scan_angle = 0;
			num_bytes_user_data = 0;
			num_bytes_point_source = 0;
			num_bytes_gps_time = 0;
		}

		public override bool init(laszip_point item, ref uint context)
		{
			// on the first init create outstreams and encoders
			if (outstream_channel_returns_XY == null)
			{
				outstream_channel_returns_XY = new MemoryStream();
				outstream_Z = new MemoryStream();
				outstream_classification = new MemoryStream();
				outstream_flags = new MemoryStream();
				outstream_intensity = new MemoryStream();
				outstream_scan_angle = new MemoryStream();
				outstream_user_data = new MemoryStream();
				outstream_point_source = new MemoryStream();
				outstream_gps_time = new MemoryStream();

				// create layer encoders
				enc_channel_returns_XY = new ArithmeticEncoder();
				enc_Z = new ArithmeticEncoder();
				enc_classification = new ArithmeticEncoder();
				enc_flags = new ArithmeticEncoder();
				enc_intensity = new ArithmeticEncoder();
				enc_scan_angle = new ArithmeticEncoder();
				enc_user_data = new ArithmeticEncoder();
				enc_point_source = new ArithmeticEncoder();
				enc_gps_time = new ArithmeticEncoder();
			}
			else
			{
				// otherwise just seek back
				outstream_channel_returns_XY.Seek(0, SeekOrigin.Begin);
				outstream_Z.Seek(0, SeekOrigin.Begin);
				outstream_classification.Seek(0, SeekOrigin.Begin);
				outstream_flags.Seek(0, SeekOrigin.Begin);
				outstream_intensity.Seek(0, SeekOrigin.Begin);
				outstream_scan_angle.Seek(0, SeekOrigin.Begin);
				outstream_user_data.Seek(0, SeekOrigin.Begin);
				outstream_point_source.Seek(0, SeekOrigin.Begin);
				outstream_gps_time.Seek(0, SeekOrigin.Begin);
			}

			// init layer encoders
			enc_channel_returns_XY.init(outstream_channel_returns_XY);
			enc_Z.init(outstream_Z);
			enc_classification.init(outstream_classification);
			enc_flags.init(outstream_flags);
			enc_intensity.init(outstream_intensity);
			enc_scan_angle.init(outstream_scan_angle);
			enc_user_data.init(outstream_user_data);
			enc_point_source.init(outstream_point_source);
			enc_gps_time.init(outstream_gps_time);

			// set changed booleans to FALSE
			changed_classification = false;
			changed_flags = false;
			changed_intensity = false;
			changed_scan_angle = false;
			changed_user_data = false;
			changed_point_source = false;
			changed_gps_time = false;

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = item.extended_scanner_channel;
			context = current_context; // the POINT14 writer sets context for all other items

			// create and init entropy models and integer compressors (and init context from item)
			createAndInitModelsAndCompressors(current_context, item);

			return true;
		}

		public override bool write(laszip_point item, ref uint context)
		{
			// get last
			laszip_point last_item = contexts[current_context].last_item;

			////////////////////////////////////////
			// compress returns_XY layer
			////////////////////////////////////////

			// create single (3) / first (1) / last (2) / intermediate (0) context from last point return
			int lpr = last_item.extended_return_number == 1 ? 1 : 0; // first?
			lpr += last_item.extended_return_number >= last_item.extended_number_of_returns ? 2 : 0; // last?

			// add info whether the GPS time changed in the last return to the context
			lpr += contexts[current_context].last_item_gps_time_change ? 4 : 0;

			// get the (potentially new) context
			uint scanner_channel = item.extended_scanner_channel;

			// if context has changed (and the new context already exists) get last for new context
			if (scanner_channel != current_context)
			{
				if (contexts[scanner_channel].unused == false)
				{
					last_item = contexts[scanner_channel].last_item;
				}
			}

			// get last and current return counts
			uint last_n = last_item.extended_number_of_returns;
			uint last_r = last_item.extended_return_number;

			uint n = item.extended_number_of_returns;
			uint r = item.extended_return_number;

			// determine changed attributes
			bool scanner_channel_change = scanner_channel != current_context;
			bool point_source_change = item.point_source_ID != last_item.point_source_ID;
			bool gps_time_change = item.gps_time != last_item.gps_time;
			bool scan_angle_change = item.extended_scan_angle != last_item.extended_scan_angle;
			bool number_change = n != last_n;

			// create the 7 bit mask that encodes various changes (its value ranges from 0 to 127)
			int changed_values = (scanner_channel_change ? 64 : 0) | // scanner channel compared to last point
								 (point_source_change ? 32 : 0) |   // point source ID compared to last point from *same* scanner channel
								 (gps_time_change ? 16 : 0) |       // GPS time stamp compared to last point from *same* scanner channel
								 (scan_angle_change ? 8 : 0) |      // scan angle compared to last point from *same* scanner channel
								 (number_change ? 4 : 0);           // number of returns compared to last point from *same* scanner channel

			// return number compared to last point of *same* scanner channel (same = 0 / plus one mod 16 = 1 / minus one mod 16 = 2 / other difference = 3)
			if (r != last_r)
			{
				if (r == ((last_r + 1) % 16))
				{
					changed_values |= 1;
				}
				else if (r == ((last_r + 15) % 16))
				{
					changed_values |= 2;
				}
				else
				{
					changed_values |= 3;
				}
			}

			// compress the 7 bit mask that encodes changes with last point return context
			enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_changed_values[lpr], (uint)changed_values);

			// if scanner channel has changed, record change
			if (scanner_channel_change)
			{
				int diff = (int)scanner_channel - (int)current_context;
				if (diff > 0)
				{
					enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_scanner_channel, (uint)(diff - 1)); // curr = last + (sym + 1)
				}
				else
				{
					enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_scanner_channel, (uint)(diff + 4 - 1)); // curr = (last + (sym + 1)) % 4
				}

				// maybe create and init entropy models and integer compressors
				if (contexts[scanner_channel].unused)
				{
					// create and init entropy models and integer compressors (and init context from last item)
					createAndInitModelsAndCompressors(scanner_channel, contexts[current_context].last_item);

					// get last for new context
					last_item = contexts[scanner_channel].last_item;
				}
				// switch context to current scanner channel
				current_context = scanner_channel;
			}
			context = current_context; // the POINT14 writer sets context for all other items

			// if number of returns is different we compress it
			if (number_change)
			{
				if (contexts[current_context].m_number_of_returns[last_n] == null)
				{
					contexts[current_context].m_number_of_returns[last_n] = enc_channel_returns_XY.createSymbolModel(16);
					enc_channel_returns_XY.initSymbolModel(contexts[current_context].m_number_of_returns[last_n]);
				}
				enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_number_of_returns[last_n], n);
			}

			// if return number is different and difference is bigger than +1 / -1 we compress how it is different
			if ((changed_values & 3) == 3)
			{
				if (gps_time_change) // if the GPS time has changed
				{
					if (contexts[current_context].m_return_number[last_r] == null)
					{
						contexts[current_context].m_return_number[last_r] = enc_channel_returns_XY.createSymbolModel(16);
						enc_channel_returns_XY.initSymbolModel(contexts[current_context].m_return_number[last_r]);
					}
					enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_return_number[last_r], r);
				}
				else // if the GPS time has not changed
				{
					int diff = (int)r - (int)last_r;
					if (diff > 1)
					{
						enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_return_number_gps_same, (uint)(diff - 2)); // r = last_r + (sym + 2) with sym = diff - 2
					}
					else
					{
						enc_channel_returns_XY.encodeSymbol(contexts[current_context].m_return_number_gps_same, (uint)(diff + 16 - 2)); // r = (last_r + (sym + 2)) % 16 with sym = diff + 16 - 2
					}
				}
			}

			// get return map m and return level l context for current point
			uint m = Laszip_Common_v3.number_return_map_6ctx[n, r];
			uint l = Laszip_Common_v3.number_return_level_8ctx[n, r];

			// create single (3) / first (1) / last (2) / intermediate (0) return context for current point
			int cpr = (r == 1 ? 2 : 0); // first ?
			cpr += (r >= n ? 1 : 0); // last ?

			{
				// compress X coordinate
				int median = contexts[current_context].last_X_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].get();
				int diff = item.X - last_item.X;
				contexts[current_context].ic_dX.compress(median, diff, n == 1 ? 1u : 0u);
				contexts[current_context].last_X_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].add(diff);

				// compress Y coordinate
				uint k_bits = contexts[current_context].ic_dX.getK();
				median = contexts[current_context].last_Y_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].get();
				diff = item.Y - last_item.Y;
				contexts[current_context].ic_dY.compress(median, diff, (n == 1 ? 1u : 0u) + (k_bits < 20 ? k_bits & 0xFEu : 20u));
				contexts[current_context].last_Y_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].add(diff);
			}

			////////////////////////////////////////
			// compress Z layer
			////////////////////////////////////////
			{
				uint k_bits = (contexts[current_context].ic_dX.getK() + contexts[current_context].ic_dY.getK()) / 2;
				contexts[current_context].ic_Z.compress(contexts[current_context].last_Z[l], item.Z, (n == 1 ? 1u : 0u) + (k_bits < 18 ? k_bits & 0xFEu : 18u));
				contexts[current_context].last_Z[l] = item.Z;
			}

			////////////////////////////////////////
			// compress classifications layer
			////////////////////////////////////////

			uint last_classification = last_item.extended_classification;
			uint classification = item.extended_classification;

			if (classification != last_classification)
			{
				changed_classification = true;
			}

			int ccc = (int)((last_classification & 0x1F) << 1) + (cpr == 3 ? 1 : 0);
			if (contexts[current_context].m_classification[ccc] == null)
			{
				contexts[current_context].m_classification[ccc] = enc_classification.createSymbolModel(256);
				enc_classification.initSymbolModel(contexts[current_context].m_classification[ccc]);
			}
			enc_classification.encodeSymbol(contexts[current_context].m_classification[ccc], classification);

			////////////////////////////////////////
			// compress flags layer
			////////////////////////////////////////

			uint last_flags = (uint)(last_item.edge_of_flight_line << 5) | (uint)(last_item.scan_direction_flag << 4) | last_item.extended_classification_flags;
			uint flags = (uint)(item.edge_of_flight_line << 5) | (uint)(item.scan_direction_flag << 4) | item.extended_classification_flags;

			if (flags != last_flags)
			{
				changed_flags = true;
			}

			if (contexts[current_context].m_flags[last_flags] == null)
			{
				contexts[current_context].m_flags[last_flags] = enc_flags.createSymbolModel(64);
				enc_flags.initSymbolModel(contexts[current_context].m_flags[last_flags]);
			}
			enc_flags.encodeSymbol(contexts[current_context].m_flags[last_flags], flags);

			////////////////////////////////////////
			// compress intensity layer
			////////////////////////////////////////

			if (item.intensity != last_item.intensity)
			{
				changed_intensity = true;
			}
			contexts[current_context].ic_intensity.compress(contexts[current_context].last_intensity[(cpr << 1) | (gps_time_change ? 1 : 0)], item.intensity, (uint)cpr);
			contexts[current_context].last_intensity[(cpr << 1) | (gps_time_change ? 1 : 0)] = item.intensity;

			////////////////////////////////////////
			// compress scan_angle layer
			////////////////////////////////////////

			if (scan_angle_change)
			{
				changed_scan_angle = true;
				contexts[current_context].ic_scan_angle.compress(last_item.extended_scan_angle, item.extended_scan_angle, gps_time_change ? 1u : 0u); // if the GPS time has changed
			}

			////////////////////////////////////////
			// compress user_data layer
			////////////////////////////////////////

			if (item.user_data != last_item.user_data)
			{
				changed_user_data = true;
			}
			if (contexts[current_context].m_user_data[last_item.user_data / 4] == null)
			{
				contexts[current_context].m_user_data[last_item.user_data / 4] = enc_user_data.createSymbolModel(256);
				enc_user_data.initSymbolModel(contexts[current_context].m_user_data[last_item.user_data / 4]);
			}
			enc_user_data.encodeSymbol(contexts[current_context].m_user_data[last_item.user_data / 4], item.user_data);

			////////////////////////////////////////
			// compress point_source layer
			////////////////////////////////////////

			if (point_source_change)
			{
				changed_point_source = true;
				contexts[current_context].ic_point_source_ID.compress(last_item.point_source_ID, item.point_source_ID);
			}

			////////////////////////////////////////
			// compress gps_time layer
			////////////////////////////////////////

			if (gps_time_change) // if the GPS time has changed
			{
				changed_gps_time = true;

				U64I64F64 gps_time = new U64I64F64();
				gps_time.f64 = item.gps_time;

				write_gps_time(gps_time);
			}

			// copy the last item
			last_item.X = item.X;
			last_item.Y = item.Y;
			last_item.Z = item.Z;
			last_item.intensity = item.intensity;
			last_item.flags = item.flags;
			last_item.classification_and_classification_flags = item.classification_and_classification_flags;
			last_item.scan_angle_rank = item.scan_angle_rank;
			last_item.user_data = item.user_data;
			last_item.point_source_ID = item.point_source_ID;
			last_item.extended_scan_angle = item.extended_scan_angle;
			last_item.extended_flags = item.extended_flags;
			last_item.extended_classification = item.extended_classification;
			last_item.extended_returns = item.extended_returns;
			last_item.gps_time = item.gps_time;

			// remember if the last point had a gps_time_change
			contexts[current_context].last_item_gps_time_change = gps_time_change;

			return true;
		}

		public override bool chunk_sizes()
		{
			Stream outstream = enc.getByteStreamOut();

			// finish the encoders
			enc_channel_returns_XY.done();
			enc_Z.done();
			if (changed_classification)
			{
				enc_classification.done();
			}
			if (changed_flags)
			{
				enc_flags.done();
			}
			if (changed_intensity)
			{
				enc_intensity.done();
			}
			if (changed_scan_angle)
			{
				enc_scan_angle.done();
			}
			if (changed_user_data)
			{
				enc_user_data.done();
			}
			if (changed_point_source)
			{
				enc_point_source.done();
			}
			if (changed_gps_time)
			{
				enc_gps_time.done();
			}

			// output the sizes of all layer (i.e.. number of bytes per layer)
			uint num_bytes = (uint)outstream_channel_returns_XY.Position;
			num_bytes_channel_returns_XY += num_bytes;
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			num_bytes = (uint)outstream_Z.Position;
			num_bytes_Z += num_bytes;
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_classification)
			{
				num_bytes = (uint)outstream_classification.Position;
				num_bytes_classification += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_flags)
			{
				num_bytes = (uint)outstream_flags.Position;
				num_bytes_flags += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_intensity)
			{
				num_bytes = (uint)outstream_intensity.Position;
				num_bytes_intensity += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_scan_angle)
			{
				num_bytes = (uint)outstream_scan_angle.Position;
				num_bytes_scan_angle += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_user_data)
			{
				num_bytes = (uint)outstream_user_data.Position;
				num_bytes_user_data += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_point_source)
			{
				num_bytes = (uint)outstream_point_source.Position;
				num_bytes_point_source += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			if (changed_gps_time)
			{
				num_bytes = (uint)outstream_gps_time.Position;
				num_bytes_gps_time += num_bytes;
			}
			else
			{
				num_bytes = 0;
			}
			outstream.Write(BitConverter.GetBytes(num_bytes), 0, 4);

			return true;
		}

		public override bool chunk_bytes()
		{
			Stream outstream = enc.getByteStreamOut();

			// output the bytes of all layers
			outstream.Write(outstream_channel_returns_XY.GetBuffer(), 0, (int)outstream_channel_returns_XY.Position);

			outstream.Write(outstream_Z.GetBuffer(), 0, (int)outstream_Z.Position);

			if (changed_classification)
			{
				outstream.Write(outstream_classification.GetBuffer(), 0, (int)outstream_classification.Position);
			}

			if (changed_flags)
			{
				outstream.Write(outstream_flags.GetBuffer(), 0, (int)outstream_flags.Position);
			}

			if (changed_intensity)
			{
				outstream.Write(outstream_intensity.GetBuffer(), 0, (int)outstream_intensity.Position);
			}

			if (changed_scan_angle)
			{
				outstream.Write(outstream_scan_angle.GetBuffer(), 0, (int)outstream_scan_angle.Position);
			}

			if (changed_user_data)
			{
				outstream.Write(outstream_user_data.GetBuffer(), 0, (int)outstream_user_data.Position);
			}

			if (changed_point_source)
			{
				outstream.Write(outstream_point_source.GetBuffer(), 0, (int)outstream_point_source.Position);
			}

			if (changed_gps_time)
			{
				outstream.Write(outstream_gps_time.GetBuffer(), 0, (int)outstream_gps_time.Position);
			}

			return true;
		}

		// not used as a encoder. just gives access to outstream
		ArithmeticEncoder enc;

		MemoryStream outstream_channel_returns_XY;
		MemoryStream outstream_Z;
		MemoryStream outstream_classification;
		MemoryStream outstream_flags;
		MemoryStream outstream_intensity;
		MemoryStream outstream_scan_angle;
		MemoryStream outstream_user_data;
		MemoryStream outstream_point_source;
		MemoryStream outstream_gps_time;

		ArithmeticEncoder enc_channel_returns_XY;
		ArithmeticEncoder enc_Z;
		ArithmeticEncoder enc_classification;
		ArithmeticEncoder enc_flags;
		ArithmeticEncoder enc_intensity;
		ArithmeticEncoder enc_scan_angle;
		ArithmeticEncoder enc_user_data;
		ArithmeticEncoder enc_point_source;
		ArithmeticEncoder enc_gps_time;

		bool changed_classification;
		bool changed_flags;
		bool changed_intensity;
		bool changed_scan_angle;
		bool changed_user_data;
		bool changed_point_source;
		bool changed_gps_time;

		uint num_bytes_channel_returns_XY;
		uint num_bytes_Z;
		uint num_bytes_classification;
		uint num_bytes_flags;
		uint num_bytes_intensity;
		uint num_bytes_scan_angle;
		uint num_bytes_user_data;
		uint num_bytes_point_source;
		uint num_bytes_gps_time;

		uint current_context;
		readonly LAScontextPOINT14[] contexts =
		{
			new LAScontextPOINT14(),
			new LAScontextPOINT14(),
			new LAScontextPOINT14(),
			new LAScontextPOINT14()
		};

		bool createAndInitModelsAndCompressors(uint context, laszip_point item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models and integer compressors (if needed)
			if (contexts[context].m_changed_values[0] == null)
			{
				// for the channel_returns_XY layer
				contexts[context].m_changed_values[0] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[1] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[2] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[3] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[4] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[5] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[6] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[7] = enc_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_scanner_channel = enc_channel_returns_XY.createSymbolModel(3);
				for (int i = 0; i < 16; i++)
				{
					contexts[context].m_number_of_returns[i] = null;
					contexts[context].m_return_number[i] = null;
				}
				contexts[context].m_return_number_gps_same = enc_channel_returns_XY.createSymbolModel(13);

				contexts[context].ic_dX = new IntegerCompressor(enc_channel_returns_XY, 32, 2);  // 32 bits, 2 context
				contexts[context].ic_dY = new IntegerCompressor(enc_channel_returns_XY, 32, 22); // 32 bits, 22 contexts

				// for the Z layer
				contexts[context].ic_Z = new IntegerCompressor(enc_Z, 32, 20);  // 32 bits, 20 contexts

				// for the classification layer
				// for the flags layer
				// for the user_data layer
				for (int i = 0; i < 64; i++)
				{
					contexts[context].m_classification[i] = null;
					contexts[context].m_flags[i] = null;
					contexts[context].m_user_data[i] = null;
				}

				// for the intensity layer
				contexts[context].ic_intensity = new IntegerCompressor(enc_intensity, 16, 4);

				// for the scan_angle layer
				contexts[context].ic_scan_angle = new IntegerCompressor(enc_scan_angle, 16, 2);

				// for the point_source_ID layer
				contexts[context].ic_point_source_ID = new IntegerCompressor(enc_point_source, 16);

				// for the gps_time layer
				contexts[context].m_gpstime_multi = enc_gps_time.createSymbolModel(LASZIP_GPSTIME_MULTI_TOTAL);
				contexts[context].m_gpstime_0diff = enc_gps_time.createSymbolModel(5);
				contexts[context].ic_gpstime = new IntegerCompressor(enc_gps_time, 32, 9); // 32 bits, 9 contexts
			}

			// then init entropy models and integer compressors

			// for the channel_returns_XY layer
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[0]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[1]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[2]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[3]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[4]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[5]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[6]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[7]);
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_scanner_channel);
			for (int i = 0; i < 16; i++)
			{
				if (contexts[context].m_number_of_returns[i] != null) enc_channel_returns_XY.initSymbolModel(contexts[context].m_number_of_returns[i]);
				if (contexts[context].m_return_number[i] != null) enc_channel_returns_XY.initSymbolModel(contexts[context].m_return_number[i]);
			}
			enc_channel_returns_XY.initSymbolModel(contexts[context].m_return_number_gps_same);
			contexts[context].ic_dX.initCompressor();
			contexts[context].ic_dY.initCompressor();
			for (int i = 0; i < 12; i++)
			{
				contexts[context].last_X_diff_median5[i].init();
				contexts[context].last_Y_diff_median5[i].init();
			}

			// for the Z layer
			contexts[context].ic_Z.initCompressor();
			for (int i = 0; i < 8; i++)
			{
				contexts[context].last_Z[i] = item.Z;
			}

			// for the classification layer
			// for the flags layer
			// for the user_data layer
			for (int i = 0; i < 64; i++)
			{
				if (contexts[context].m_classification[i] != null) enc_classification.initSymbolModel(contexts[context].m_classification[i]);
				if (contexts[context].m_flags[i] != null) enc_flags.initSymbolModel(contexts[context].m_flags[i]);
				if (contexts[context].m_user_data[i] != null) enc_user_data.initSymbolModel(contexts[context].m_user_data[i]);
			}

			// for the intensity layer
			contexts[context].ic_intensity.initCompressor();
			for (int i = 0; i < 8; i++)
			{
				contexts[context].last_intensity[i] = item.intensity;
			}

			// for the scan_angle layer
			contexts[context].ic_scan_angle.initCompressor();

			// for the point_source_ID layer
			contexts[context].ic_point_source_ID.initCompressor();

			// for the gps_time layer
			enc_gps_time.initSymbolModel(contexts[context].m_gpstime_multi);
			enc_gps_time.initSymbolModel(contexts[context].m_gpstime_0diff);
			contexts[context].ic_gpstime.initCompressor();
			contexts[context].last = 0; contexts[context].next = 0;
			contexts[context].last_gpstime_diff[0] = 0;
			contexts[context].last_gpstime_diff[1] = 0;
			contexts[context].last_gpstime_diff[2] = 0;
			contexts[context].last_gpstime_diff[3] = 0;
			contexts[context].multi_extreme_counter[0] = 0;
			contexts[context].multi_extreme_counter[1] = 0;
			contexts[context].multi_extreme_counter[2] = 0;
			contexts[context].multi_extreme_counter[3] = 0;
			contexts[context].last_gpstime[0].f64 = item.gps_time;
			contexts[context].last_gpstime[1].u64 = 0;
			contexts[context].last_gpstime[2].u64 = 0;
			contexts[context].last_gpstime[3].u64 = 0;

			// init current context from item
			contexts[context].last_item.X = item.X;
			contexts[context].last_item.Y = item.Y;
			contexts[context].last_item.Z = item.Z;
			contexts[context].last_item.intensity = item.intensity;
			contexts[context].last_item.flags = item.flags;
			contexts[context].last_item.classification_and_classification_flags = item.classification_and_classification_flags;
			contexts[context].last_item.scan_angle_rank = item.scan_angle_rank;
			contexts[context].last_item.user_data = item.user_data;
			contexts[context].last_item.point_source_ID = item.point_source_ID;
			contexts[context].last_item.extended_scan_angle = item.extended_scan_angle;
			contexts[context].last_item.extended_flags = item.extended_flags;
			contexts[context].last_item.extended_classification = item.extended_classification;
			contexts[context].last_item.extended_returns = item.extended_returns;
			contexts[context].last_item.gps_time = item.gps_time;

			contexts[context].last_item_gps_time_change = false;

			contexts[context].unused = false;

			return true;
		}

		void write_gps_time(U64I64F64 gps_time)
		{
			if (contexts[current_context].last_gpstime_diff[contexts[current_context].last] == 0) // if the last integer difference was zero
			{
				// calculate the difference between the two doubles as an integer
				long curr_gpstime_diff_64 = gps_time.i64 - contexts[current_context].last_gpstime[contexts[current_context].last].i64;
				int curr_gpstime_diff = (int)curr_gpstime_diff_64;
				if (curr_gpstime_diff_64 == curr_gpstime_diff)
				{
					enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_0diff, 0); // the difference can be represented with 32 bits
					contexts[current_context].ic_gpstime.compress(0, curr_gpstime_diff, 0);
					contexts[current_context].last_gpstime_diff[contexts[current_context].last] = curr_gpstime_diff;
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				else // the difference is huge
				{
					// maybe the double belongs to another time sequence
					for (uint i = 1; i < 4; i++)
					{
						long other_gpstime_diff_64 = gps_time.i64 - contexts[current_context].last_gpstime[(contexts[current_context].last + i) & 3].i64;
						int other_gpstime_diff = (int)other_gpstime_diff_64;
						if (other_gpstime_diff_64 == other_gpstime_diff)
						{
							enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_0diff, i + 1); // it belongs to another sequence
							contexts[current_context].last = (contexts[current_context].last + i) & 3;
							write_gps_time(gps_time);
							return;
						}
					}
					// no other sequence found. start new sequence.
					enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_0diff, 1);
					contexts[current_context].ic_gpstime.compress((int)(contexts[current_context].last_gpstime[contexts[current_context].last].u64 >> 32), (int)(gps_time.u64 >> 32), 8);
					enc_gps_time.writeInt((uint)gps_time.u64);
					contexts[current_context].next = (contexts[current_context].next + 1) & 3;
					contexts[current_context].last = contexts[current_context].next;
					contexts[current_context].last_gpstime_diff[contexts[current_context].last] = 0;
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				contexts[current_context].last_gpstime[contexts[current_context].last].i64 = gps_time.i64;
			}
			else // the last integer difference was *not* zero
			{
				// calculate the difference between the two doubles as an integer
				long curr_gpstime_diff_64 = gps_time.i64 - contexts[current_context].last_gpstime[contexts[current_context].last].i64;
				int curr_gpstime_diff = (int)curr_gpstime_diff_64;

				// if the current gpstime difference can be represented with 32 bits
				if (curr_gpstime_diff_64 == curr_gpstime_diff)
				{
					// compute multiplier between current and last integer difference
					double multi_f = curr_gpstime_diff / (double)(contexts[current_context].last_gpstime_diff[contexts[current_context].last]);
					int multi = MyDefs.I32_QUANTIZE(multi_f);

					// compress the residual curr_gpstime_diff in dependance on the multiplier
					if (multi == 1)
					{
						// this is the case we assume we get most often for regular spaced pulses
						enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, 1);
						contexts[current_context].ic_gpstime.compress(contexts[current_context].last_gpstime_diff[contexts[current_context].last], curr_gpstime_diff, 1);
						contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
					}
					else if (multi > 0)
					{
						if (multi < LASZIP_GPSTIME_MULTI) // positive multipliers up to LASZIP_GPSTIME_MULTI are compressed directly
						{
							enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, (uint)multi);
							if (multi < 10)
								contexts[current_context].ic_gpstime.compress(multi * contexts[current_context].last_gpstime_diff[contexts[current_context].last], curr_gpstime_diff, 2);
							else
								contexts[current_context].ic_gpstime.compress(multi * contexts[current_context].last_gpstime_diff[contexts[current_context].last], curr_gpstime_diff, 3);
						}
						else
						{
							enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, LASZIP_GPSTIME_MULTI);
							contexts[current_context].ic_gpstime.compress(LASZIP_GPSTIME_MULTI * contexts[current_context].last_gpstime_diff[contexts[current_context].last], curr_gpstime_diff, 4);
							contexts[current_context].multi_extreme_counter[contexts[current_context].last]++;
							if (contexts[current_context].multi_extreme_counter[contexts[current_context].last] > 3)
							{
								contexts[current_context].last_gpstime_diff[contexts[current_context].last] = curr_gpstime_diff;
								contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
							}
						}
					}
					else if (multi < 0)
					{
						if (multi > LASZIP_GPSTIME_MULTI_MINUS) // negative multipliers larger than LASZIP_GPSTIME_MULTI_MINUS are compressed directly
						{
							enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, (uint)(LASZIP_GPSTIME_MULTI - multi));
							contexts[current_context].ic_gpstime.compress(multi * contexts[current_context].last_gpstime_diff[contexts[current_context].last], curr_gpstime_diff, 5);
						}
						else
						{
							enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, LASZIP_GPSTIME_MULTI - LASZIP_GPSTIME_MULTI_MINUS);
							contexts[current_context].ic_gpstime.compress(LASZIP_GPSTIME_MULTI_MINUS * contexts[current_context].last_gpstime_diff[contexts[current_context].last], curr_gpstime_diff, 6);
							contexts[current_context].multi_extreme_counter[contexts[current_context].last]++;
							if (contexts[current_context].multi_extreme_counter[contexts[current_context].last] > 3)
							{
								contexts[current_context].last_gpstime_diff[contexts[current_context].last] = curr_gpstime_diff;
								contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
							}
						}
					}
					else
					{
						enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, 0);
						contexts[current_context].ic_gpstime.compress(0, curr_gpstime_diff, 7);
						contexts[current_context].multi_extreme_counter[contexts[current_context].last]++;
						if (contexts[current_context].multi_extreme_counter[contexts[current_context].last] > 3)
						{
							contexts[current_context].last_gpstime_diff[contexts[current_context].last] = curr_gpstime_diff;
							contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
						}
					}
				}
				else // the difference is huge
				{
					// maybe the double belongs to another time sequence
					for (uint i = 1; i < 4; i++)
					{
						long other_gpstime_diff_64 = gps_time.i64 - contexts[current_context].last_gpstime[(contexts[current_context].last + i) & 3].i64;
						int other_gpstime_diff = (int)other_gpstime_diff_64;
						if (other_gpstime_diff_64 == other_gpstime_diff)
						{
							// it belongs to this sequence 
							enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, LASZIP_GPSTIME_MULTI_CODE_FULL + i);
							contexts[current_context].last = (contexts[current_context].last + i) & 3;
							write_gps_time(gps_time);
							return;
						}
					}
					// no other sequence found. start new sequence.
					enc_gps_time.encodeSymbol(contexts[current_context].m_gpstime_multi, LASZIP_GPSTIME_MULTI_CODE_FULL);
					contexts[current_context].ic_gpstime.compress((int)(contexts[current_context].last_gpstime[contexts[current_context].last].u64 >> 32), (int)(gps_time.u64 >> 32), 8);
					enc_gps_time.writeInt((uint)gps_time.u64);
					contexts[current_context].next = (contexts[current_context].next + 1) & 3;
					contexts[current_context].last = contexts[current_context].next;
					contexts[current_context].last_gpstime_diff[contexts[current_context].last] = 0;
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				contexts[current_context].last_gpstime[contexts[current_context].last].i64 = gps_time.i64;
			}
		}
	}
}
