//===============================================================================
//
//  FILE:  lasreaditemcompressed_point14_v3.cs
//
//  CONTENTS:
//
//    Implementation of LASreadItemCompressed for POINT14 items (version 3).
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

using System.Diagnostics;
using System.IO;

namespace LASzip.Net
{
	class LASreadItemCompressed_POINT14_v3 : LASreadItemCompressed
	{
		const int LASZIP_GPSTIME_MULTI = 500;
		const int LASZIP_GPSTIME_MULTI_MINUS = -10;
		const int LASZIP_GPSTIME_MULTI_CODE_FULL = LASZIP_GPSTIME_MULTI - LASZIP_GPSTIME_MULTI_MINUS + 1;

		const int LASZIP_GPSTIME_MULTI_TOTAL = LASZIP_GPSTIME_MULTI - LASZIP_GPSTIME_MULTI_MINUS + 5;

		public LASreadItemCompressed_POINT14_v3(ArithmeticDecoder dec, LASZIP_DECOMPRESS_SELECTIVE decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL)
		{
			// not used as a decoder. just gives access to instream
			Debug.Assert(dec != null);
			this.dec = dec;

			// zero instreams and decoders
			instream_channel_returns_XY = null;
			instream_Z = null;
			instream_classification = null;
			instream_flags = null;
			instream_intensity = null;
			instream_scan_angle = null;
			instream_user_data = null;
			instream_point_source = null;
			instream_gps_time = null;

			dec_channel_returns_XY = null;
			dec_Z = null;
			dec_classification = null;
			dec_flags = null;
			dec_intensity = null;
			dec_scan_angle = null;
			dec_user_data = null;
			dec_point_source = null;
			dec_gps_time = null;

			// mark the four scanner channel contexts as uninitialized
			for (int c = 0; c < 4; c++)
			{
				contexts[c].m_changed_values[0] = null;
			}
			current_context = 0;

			// zero num_bytes and init booleany
			num_bytes_channel_returns_XY = 0;
			num_bytes_Z = 0;
			num_bytes_classification = 0;
			num_bytes_flags = 0;
			num_bytes_intensity = 0;
			num_bytes_scan_angle = 0;
			num_bytes_user_data = 0;
			num_bytes_point_source = 0;
			num_bytes_gps_time = 0;

			changed_Z = false;
			changed_classification = false;
			changed_flags = false;
			changed_intensity = false;
			changed_scan_angle = false;
			changed_user_data = false;
			changed_point_source = false;
			changed_gps_time = false;

			requested_Z = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.Z);
			requested_classification = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.CLASSIFICATION);
			requested_flags = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.FLAGS);
			requested_intensity = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.INTENSITY);
			requested_scan_angle = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.SCAN_ANGLE);
			requested_user_data = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.USER_DATA);
			requested_point_source = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.POINT_SOURCE);
			requested_gps_time = decompress_selective.HasFlag(LASZIP_DECOMPRESS_SELECTIVE.GPS_TIME);

			// init the bytes buffer to zero
			bytes = null;
			num_bytes_allocated = 0;
		}

		public override bool chunk_sizes()
		{
			// for layered compression 'dec' only hands over the stream
			var instream = dec.getByteStreamIn();

			// read bytes per layer
			if (!instream.get32bits(out num_bytes_channel_returns_XY)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_Z)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_classification)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_flags)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_intensity)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_scan_angle)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_user_data)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_point_source)) throw new EndOfStreamException();
			if (!instream.get32bits(out num_bytes_gps_time)) throw new EndOfStreamException();

			return true;
		}

		public override bool init(laszip_point item, ref uint context) // context is set
		{
			// for layered compression 'dec' only hands over the stream
			Stream instream = dec.getByteStreamIn();

			// on the first init create instreams and decoders
			if (instream_channel_returns_XY == null)
			{
				// create decoders
				dec_channel_returns_XY = new ArithmeticDecoder();
				dec_Z = new ArithmeticDecoder();
				dec_classification = new ArithmeticDecoder();
				dec_flags = new ArithmeticDecoder();
				dec_intensity = new ArithmeticDecoder();
				dec_scan_angle = new ArithmeticDecoder();
				dec_user_data = new ArithmeticDecoder();
				dec_point_source = new ArithmeticDecoder();
				dec_gps_time = new ArithmeticDecoder();
			}

			// how many bytes do we need to read
			int num_bytes = num_bytes_channel_returns_XY;
			if (requested_Z) num_bytes += num_bytes_Z;
			if (requested_classification) num_bytes += num_bytes_classification;
			if (requested_flags) num_bytes += num_bytes_flags;
			if (requested_intensity) num_bytes += num_bytes_intensity;
			if (requested_scan_angle) num_bytes += num_bytes_scan_angle;
			if (requested_user_data) num_bytes += num_bytes_user_data;
			if (requested_point_source) num_bytes += num_bytes_point_source;
			if (requested_gps_time) num_bytes += num_bytes_gps_time;

			// make sure the buffer is sufficiently large
			if (num_bytes > num_bytes_allocated)
			{
				try
				{
					bytes = new byte[num_bytes];
				}
				catch
				{
					return false;
				}
				num_bytes_allocated = num_bytes;
			}

			// load the requested bytes and init the corresponding instreams and decoders
			num_bytes = 0;
			if (!instream.getBytes(bytes, 0, num_bytes_channel_returns_XY)) throw new EndOfStreamException();
			instream_channel_returns_XY = new MemoryStream(bytes, 0, num_bytes_channel_returns_XY);
			dec_channel_returns_XY.init(instream_channel_returns_XY);
			num_bytes += num_bytes_channel_returns_XY;

			if (requested_Z)
			{
				if (num_bytes_Z != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_Z)) throw new EndOfStreamException();
					instream_Z = new MemoryStream(bytes, num_bytes, num_bytes_Z);
					dec_Z.init(instream_Z);
					num_bytes += num_bytes_Z;
					changed_Z = true;
				}
				else
				{
					instream_Z = new MemoryStream(0);
					changed_Z = false;
				}
			}
			else
			{
				if (num_bytes_Z != 0)
				{
					instream.Seek(num_bytes_Z, SeekOrigin.Current);
				}
				changed_Z = false;
			}

			if (requested_classification)
			{
				if (num_bytes_classification != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_classification)) throw new EndOfStreamException();
					instream_classification = new MemoryStream(bytes, num_bytes, num_bytes_classification);
					dec_classification.init(instream_classification);
					num_bytes += num_bytes_classification;
					changed_classification = true;
				}
				else
				{
					instream_classification = new MemoryStream(0);
					changed_classification = false;
				}
			}
			else
			{
				if (num_bytes_classification != 0)
				{
					instream.Seek(num_bytes_classification, SeekOrigin.Current);
				}
				changed_classification = false;
			}

			if (requested_flags)
			{
				if (num_bytes_flags != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_flags)) throw new EndOfStreamException();
					instream_flags = new MemoryStream(bytes, num_bytes, num_bytes_flags);
					dec_flags.init(instream_flags);
					num_bytes += num_bytes_flags;
					changed_flags = true;
				}
				else
				{
					instream_flags = new MemoryStream(0);
					changed_flags = false;
				}
			}
			else
			{
				if (num_bytes_flags != 0)
				{
					instream.Seek(num_bytes_flags, SeekOrigin.Current);
				}
				changed_flags = false;
			}

			if (requested_intensity)
			{
				if (num_bytes_intensity != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_intensity)) throw new EndOfStreamException();
					instream_intensity = new MemoryStream(bytes, num_bytes, num_bytes_intensity);
					dec_intensity.init(instream_intensity);
					num_bytes += num_bytes_intensity;
					changed_intensity = true;
				}
				else
				{
					instream_intensity = new MemoryStream(0);
					changed_intensity = false;
				}
			}
			else
			{
				if (num_bytes_intensity != 0)
				{
					instream.Seek(num_bytes_intensity, SeekOrigin.Current);
				}
				changed_intensity = false;
			}

			if (requested_scan_angle)
			{
				if (num_bytes_scan_angle != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_scan_angle)) throw new EndOfStreamException();
					instream_scan_angle = new MemoryStream(bytes, num_bytes, num_bytes_scan_angle);
					dec_scan_angle.init(instream_scan_angle);
					num_bytes += num_bytes_scan_angle;
					changed_scan_angle = true;
				}
				else
				{
					instream_scan_angle = new MemoryStream(0);
					changed_scan_angle = false;
				}
			}
			else
			{
				if (num_bytes_scan_angle != 0)
				{
					instream.Seek(num_bytes_scan_angle, SeekOrigin.Current);
				}
				changed_scan_angle = false;
			}

			if (requested_user_data)
			{
				if (num_bytes_user_data != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_user_data)) throw new EndOfStreamException();
					instream_user_data = new MemoryStream(bytes, num_bytes, num_bytes_user_data);
					dec_user_data.init(instream_user_data);
					num_bytes += num_bytes_user_data;
					changed_user_data = true;
				}
				else
				{
					instream_user_data = new MemoryStream(0);
					changed_user_data = false;
				}
			}
			else
			{
				if (num_bytes_user_data != 0)
				{
					instream.Seek(num_bytes_user_data, SeekOrigin.Current);
				}
				changed_user_data = false;
			}

			if (requested_point_source)
			{
				if (num_bytes_point_source != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_point_source)) throw new EndOfStreamException();
					instream_point_source = new MemoryStream(bytes, num_bytes, num_bytes_point_source);
					dec_point_source.init(instream_point_source);
					num_bytes += num_bytes_point_source;
					changed_point_source = true;
				}
				else
				{
					instream_point_source = new MemoryStream(0);
					changed_point_source = false;
				}
			}
			else
			{
				if (num_bytes_point_source != 0)
				{
					instream.Seek(num_bytes_point_source, SeekOrigin.Current);
				}
				changed_point_source = false;
			}

			if (requested_gps_time)
			{
				if (num_bytes_gps_time != 0)
				{
					if (!instream.getBytes(bytes, num_bytes, num_bytes_gps_time)) throw new EndOfStreamException();
					instream_gps_time = new MemoryStream(bytes, num_bytes, num_bytes_gps_time);
					dec_gps_time.init(instream_gps_time);
					num_bytes += num_bytes_gps_time;
					changed_gps_time = true;
				}
				else
				{
					instream_gps_time = new MemoryStream(0);
					changed_gps_time = false;
				}
			}
			else
			{
				if (num_bytes_gps_time != 0)
				{
					instream.Seek(num_bytes_gps_time, SeekOrigin.Current);
				}
				changed_gps_time = false;
			}

			// mark the four scanner channel contexts as unused
			for (int c = 0; c < 4; c++)
			{
				contexts[c].unused = true;
			}

			// set scanner channel as current context
			current_context = item.extended_scanner_channel;
			context = current_context; // the POINT14 reader sets context for all other items

			// create and init models and decompressors
			createAndInitModelsAndDecompressors(current_context, item);

			return true;
		}

		public override void read(laszip_point item, ref uint context) // context is set
		{
			// get last
			var last_item = contexts[current_context].last_item;

			////////////////////////////////////////
			// decompress returns_XY layer
			////////////////////////////////////////

			// create single (3) / first (1) / last (2) / intermediate (0) context from last point return
			int lpr = last_item.extended_return_number == 1 ? 1 : 0; // first?
			lpr += last_item.extended_return_number >= last_item.extended_number_of_returns ? 2 : 0; // last?

			// add info whether the GPS time changed in the last return to the context
			lpr += contexts[current_context].last_item_gps_time_change ? 4 : 0;

			// decompress which values have changed with last point return context
			int changed_values = (int)dec_channel_returns_XY.decodeSymbol(contexts[current_context].m_changed_values[lpr]);

			// if scanner channel has changed
			if ((changed_values & (1 << 6)) != 0)
			{
				uint scanner_channel_diff = dec_channel_returns_XY.decodeSymbol(contexts[current_context].m_scanner_channel); // curr = last + (sym + 1)
				uint scanner_channel = (current_context + scanner_channel_diff + 1) % 4;

				// maybe create and init entropy models and integer compressors
				if (contexts[scanner_channel].unused)
				{
					// create and init entropy models and integer decompressors
					createAndInitModelsAndDecompressors(scanner_channel, contexts[current_context].last_item);
				}

				// switch context to current scanner channel
				current_context = scanner_channel;
				context = current_context; // the POINT14 reader sets context for all other items

				// get last for new context
				last_item = contexts[current_context].last_item;
				last_item.extended_scanner_channel = (byte)scanner_channel;
			}

			// determine changed attributes
			bool point_source_change = (changed_values & (1 << 5)) != 0;
			bool gps_time_change = (changed_values & (1 << 4)) != 0;
			bool scan_angle_change = (changed_values & (1 << 3)) != 0;

			// get last return counts
			uint last_n = last_item.extended_number_of_returns;
			uint last_r = last_item.extended_return_number;

			// if number of returns is different we decompress it
			uint n;
			if ((changed_values & (1 << 2)) != 0)
			{
				if (contexts[current_context].m_number_of_returns[last_n] == null)
				{
					contexts[current_context].m_number_of_returns[last_n] = dec_channel_returns_XY.createSymbolModel(16);
					dec_channel_returns_XY.initSymbolModel(contexts[current_context].m_number_of_returns[last_n]);
				}
				n = dec_channel_returns_XY.decodeSymbol(contexts[current_context].m_number_of_returns[last_n]);
				last_item.extended_number_of_returns = (byte)n;
			}
			else
			{
				n = last_n;
			}

			// how is the return number different
			uint r;
			if ((changed_values & 3) == 0) // same return number
			{
				r = last_r;
			}
			else if ((changed_values & 3) == 1) // return number plus 1 mod 16
			{
				r = ((last_r + 1) % 16);
				last_item.extended_return_number = (byte)r;
			}
			else if ((changed_values & 3) == 2) // return number minus 1 mod 16
			{
				r = ((last_r + 15) % 16);
				last_item.extended_return_number = (byte)r;
			}
			else
			{
				// the return number difference is bigger than +1 / -1 so we decompress how it is different

				if (gps_time_change) // if the GPS time has changed
				{
					if (contexts[current_context].m_return_number[last_r] == null)
					{
						contexts[current_context].m_return_number[last_r] = dec_channel_returns_XY.createSymbolModel(16);
						dec_channel_returns_XY.initSymbolModel(contexts[current_context].m_return_number[last_r]);
					}
					r = dec_channel_returns_XY.decodeSymbol(contexts[current_context].m_return_number[last_r]);
				}
				else // if the GPS time has not changed
				{
					int sym = (int)dec_channel_returns_XY.decodeSymbol(contexts[current_context].m_return_number_gps_same);
					r = (uint)((last_r + (sym + 2)) % 16);
				}
				last_item.extended_return_number = (byte)r;
			}

			// set legacy return counts and number of returns
			if (n > 7)
			{
				if (r > 6)
				{
					if (r >= n)
					{
						last_item.return_number = 7;
					}
					else
					{
						last_item.return_number = 6;
					}
				}
				else
				{
					last_item.return_number = (byte)r;
				}
				last_item.number_of_returns = 7;
			}
			else
			{
				last_item.return_number = (byte)r;
				last_item.number_of_returns = (byte)n;
			}

			// get return map m and return level l context for current point
			uint m = Laszip_Common_v3.number_return_map_6ctx[n, r];
			uint l = Laszip_Common_v3.number_return_level_8ctx[n, r];

			// create single (3) / first (1) / last (2) / intermediate (0) return context for current point

			int cpr = (r == 1 ? 2 : 0); // first ?
			cpr += (r >= n ? 1 : 0); // last ?

			uint k_bits;
			int median, diff;

			// decompress X coordinate
			median = contexts[current_context].last_X_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].get();
			diff = contexts[current_context].ic_dX.decompress(median, n == 1 ? 1u : 0u);
			last_item.X += diff;
			contexts[current_context].last_X_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].add(diff);

			// decompress Y coordinate
			median = contexts[current_context].last_Y_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].get();
			k_bits = contexts[current_context].ic_dX.getK();
			diff = contexts[current_context].ic_dY.decompress(median, (n == 1 ? 1u : 0u) + (k_bits < 20 ? k_bits & 0xFE : 20)); // &0xFE round k_bits to next even number
			last_item.Y += diff;
			contexts[current_context].last_Y_diff_median5[(m << 1) | (gps_time_change ? 1u : 0u)].add(diff);

			////////////////////////////////////////
			// decompress Z layer (if changed and requested)
			////////////////////////////////////////

			if (changed_Z) // if the Z coordinate should be decompressed and changes within this chunk
			{
				k_bits = (contexts[current_context].ic_dX.getK() + contexts[current_context].ic_dY.getK()) / 2;
				last_item.Z = contexts[current_context].ic_Z.decompress(contexts[current_context].last_Z[l], (n == 1 ? 1u : 0u) + (k_bits < 18 ? k_bits & 0xFE : 18)); // &0xFE round k_bits to next even number
				contexts[current_context].last_Z[l] = last_item.Z;
			}

			////////////////////////////////////////
			// decompress classifications layer (if changed and requested)
			////////////////////////////////////////

			if (changed_classification) // if the classification should be decompressed and changes within this chunk
			{
				uint last_classification = last_item.extended_classification;
				uint ccc = ((last_classification & 0x1F) << 1) + (cpr == 3 ? 1u : 0u);
				if (contexts[current_context].m_classification[ccc] == null)
				{
					contexts[current_context].m_classification[ccc] = dec_classification.createSymbolModel(256);
					dec_classification.initSymbolModel(contexts[current_context].m_classification[ccc]);
				}
				last_item.extended_classification = (byte)dec_classification.decodeSymbol(contexts[current_context].m_classification[ccc]);

				// legacy copies
				if (last_item.extended_classification < 32)
				{
					last_item.classification = last_item.extended_classification;
				}
			}

			////////////////////////////////////////
			// decompress flags layer (if changed and requested)
			////////////////////////////////////////

			if (changed_flags) // if the flags should be decompressed and change within this chunk
			{
				int last_flags = (last_item.edge_of_flight_line << 5) | (last_item.scan_direction_flag << 4) | last_item.extended_classification_flags;
				if (contexts[current_context].m_flags[last_flags] == null)
				{
					contexts[current_context].m_flags[last_flags] = dec_flags.createSymbolModel(64);
					dec_flags.initSymbolModel(contexts[current_context].m_flags[last_flags]);
				}
				uint flags = dec_flags.decodeSymbol(contexts[current_context].m_flags[last_flags]);
				last_item.edge_of_flight_line = (byte)((flags & (1 << 5)) != 0 ? 1 : 0);
				last_item.scan_direction_flag = (byte)((flags & (1 << 4)) != 0 ? 1 : 0);
				last_item.extended_classification_flags = (byte)(flags & 0x0F);

				// legacy copies
				//was last_item.legacy_flags = (byte)(flags & 0x07);
				last_item.classification_and_classification_flags = (byte)(((uint)last_item.classification_and_classification_flags & 0x1F) | ((flags & 0x07) << 5));
			}

			////////////////////////////////////////
			// decompress intensity layer (if changed and requested)
			////////////////////////////////////////

			if (changed_intensity) // if the intensity should be decompressed and changes within this chunk
			{
				ushort intensity = (ushort)contexts[current_context].ic_intensity.decompress(contexts[current_context].last_intensity[(cpr << 1) | (gps_time_change ? 1 : 0)], (uint)cpr);
				contexts[current_context].last_intensity[(cpr << 1) | (gps_time_change ? 1 : 0)] = intensity;
				last_item.intensity = intensity;
			}

			////////////////////////////////////////
			// decompress scan_angle layer (if changed and requested)
			////////////////////////////////////////

			if (changed_scan_angle) // if the scan angle should be decompressed and changes within this chunk
			{
				if (scan_angle_change) // if the scan angle has actually changed
				{
					last_item.extended_scan_angle = (short)contexts[current_context].ic_scan_angle.decompress(last_item.extended_scan_angle, gps_time_change ? 1u : 0u); // if the GPS time has changed
					last_item.scan_angle_rank = MyDefs.I8_CLAMP(MyDefs.I16_QUANTIZE(0.006f * last_item.extended_scan_angle));
				}
			}

			////////////////////////////////////////
			// decompress user_data layer (if changed and requested)
			////////////////////////////////////////

			if (changed_user_data) // if the user data should be decompressed and changes within this chunk
			{
				if (contexts[current_context].m_user_data[last_item.user_data / 4] == null)
				{
					contexts[current_context].m_user_data[last_item.user_data / 4] = dec_user_data.createSymbolModel(256);
					dec_user_data.initSymbolModel(contexts[current_context].m_user_data[last_item.user_data / 4]);
				}
				last_item.user_data = (byte)dec_user_data.decodeSymbol(contexts[current_context].m_user_data[last_item.user_data / 4]);
			}

			////////////////////////////////////////
			// decompress point_source layer (if changed and requested)
			////////////////////////////////////////

			if (changed_point_source) // if the point source ID should be decompressed and changes within this chunk
			{
				if (point_source_change) // if the point source ID has actually changed
				{
					last_item.point_source_ID = (ushort)contexts[current_context].ic_point_source_ID.decompress(last_item.point_source_ID);
				}
			}

			////////////////////////////////////////
			// decompress gps_time layer (if changed and requested)
			////////////////////////////////////////

			if (changed_gps_time) // if the GPS time should be decompressed and changes within this chunk
			{
				if (gps_time_change) // if the GPS time has actually changed
				{
					read_gps_time();
					last_item.gps_time = contexts[current_context].last_gpstime[contexts[current_context].last].f64;
				}
			}

			// copy the last item
			item.X = last_item.X;
			item.Y = last_item.Y;
			item.Z = last_item.Z;
			item.intensity = last_item.intensity;
			item.flags = last_item.flags;
			item.classification_and_classification_flags = last_item.classification_and_classification_flags;
			item.scan_angle_rank = last_item.scan_angle_rank;
			item.user_data = last_item.user_data;
			item.point_source_ID = last_item.point_source_ID;
			item.extended_scan_angle = last_item.extended_scan_angle;
			item.extended_flags = last_item.extended_flags;
			item.extended_classification = last_item.extended_classification;
			item.extended_returns = last_item.extended_returns;
			item.gps_time = last_item.gps_time;

			// remember if the last point had a gps_time_change
			contexts[current_context].last_item_gps_time_change = gps_time_change;
		}

		// not used as a decoder. just gives access to instream
		ArithmeticDecoder dec;

		MemoryStream instream_channel_returns_XY;
		MemoryStream instream_Z;
		MemoryStream instream_classification;
		MemoryStream instream_flags;
		MemoryStream instream_intensity;
		MemoryStream instream_scan_angle;
		MemoryStream instream_user_data;
		MemoryStream instream_point_source;
		MemoryStream instream_gps_time;

		ArithmeticDecoder dec_channel_returns_XY;
		ArithmeticDecoder dec_Z;
		ArithmeticDecoder dec_classification;
		ArithmeticDecoder dec_flags;
		ArithmeticDecoder dec_intensity;
		ArithmeticDecoder dec_scan_angle;
		ArithmeticDecoder dec_user_data;
		ArithmeticDecoder dec_point_source;
		ArithmeticDecoder dec_gps_time;

		bool changed_Z;
		bool changed_classification;
		bool changed_flags;
		bool changed_intensity;
		bool changed_scan_angle;
		bool changed_user_data;
		bool changed_point_source;
		bool changed_gps_time;

		int num_bytes_channel_returns_XY;
		int num_bytes_Z;
		int num_bytes_classification;
		int num_bytes_flags;
		int num_bytes_intensity;
		int num_bytes_scan_angle;
		int num_bytes_user_data;
		int num_bytes_point_source;
		int num_bytes_gps_time;

		bool requested_Z;
		bool requested_classification;
		bool requested_flags;
		bool requested_intensity;
		bool requested_scan_angle;
		bool requested_user_data;
		bool requested_point_source;
		bool requested_gps_time;

		byte[] bytes;
		int num_bytes_allocated;

		uint current_context;
		readonly LAScontextPOINT14[] contexts =
		{
			new LAScontextPOINT14(),
			new LAScontextPOINT14(),
			new LAScontextPOINT14(),
			new LAScontextPOINT14()
		};

		bool createAndInitModelsAndDecompressors(uint context, laszip_point item)
		{
			// should only be called when context is unused
			Debug.Assert(contexts[context].unused);

			// first create all entropy models and integer decompressors (if needed)
			if (contexts[context].m_changed_values[0] == null)
			{
				// for the channel_returns_XY layer
				contexts[context].m_changed_values[0] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[1] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[2] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[3] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[4] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[5] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[6] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_changed_values[7] = dec_channel_returns_XY.createSymbolModel(128);
				contexts[context].m_scanner_channel = dec_channel_returns_XY.createSymbolModel(3);

				for (int i = 0; i < 16; i++)
				{
					contexts[context].m_number_of_returns[i] = null;
					contexts[context].m_return_number[i] = null;
				}

				contexts[context].m_return_number_gps_same = dec_channel_returns_XY.createSymbolModel(13);

				contexts[context].ic_dX = new IntegerCompressor(dec_channel_returns_XY, 32, 2);  // 32 bits, 2 context
				contexts[context].ic_dY = new IntegerCompressor(dec_channel_returns_XY, 32, 22); // 32 bits, 22 contexts

				// for the Z layer
				contexts[context].ic_Z = new IntegerCompressor(dec_Z, 32, 20);  // 32 bits, 20 contexts

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
				contexts[context].ic_intensity = new IntegerCompressor(dec_intensity, 16, 4);

				// for the scan_angle layer
				contexts[context].ic_scan_angle = new IntegerCompressor(dec_scan_angle, 16, 2);

				// for the point_source_ID layer
				contexts[context].ic_point_source_ID = new IntegerCompressor(dec_point_source, 16);

				// for the gps_time layer
				contexts[context].m_gpstime_multi = dec_gps_time.createSymbolModel(LASZIP_GPSTIME_MULTI_TOTAL);
				contexts[context].m_gpstime_0diff = dec_gps_time.createSymbolModel(5);
				contexts[context].ic_gpstime = new IntegerCompressor(dec_gps_time, 32, 9); // 32 bits, 9 contexts
			}

			// then init entropy models and integer compressors

			// for the channel_returns_XY layer
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[0]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[1]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[2]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[3]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[4]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[5]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[6]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_changed_values[7]);
			dec_channel_returns_XY.initSymbolModel(contexts[context].m_scanner_channel);

			for (int i = 0; i < 16; i++)
			{
				if (contexts[context].m_number_of_returns[i] != null) dec_channel_returns_XY.initSymbolModel(contexts[context].m_number_of_returns[i]);
				if (contexts[context].m_return_number[i] != null) dec_channel_returns_XY.initSymbolModel(contexts[context].m_return_number[i]);
			}

			dec_channel_returns_XY.initSymbolModel(contexts[context].m_return_number_gps_same);
			contexts[context].ic_dX.initDecompressor();
			contexts[context].ic_dY.initDecompressor();

			for (int i = 0; i < 12; i++)
			{
				contexts[context].last_X_diff_median5[i].init();
				contexts[context].last_Y_diff_median5[i].init();
			}

			// for the Z layer
			contexts[context].ic_Z.initDecompressor();
			for (int i = 0; i < 8; i++)
			{
				contexts[context].last_Z[i] = item.Z;
			}

			// for the classification layer
			// for the flags layer
			// for the user_data layer
			for (int i = 0; i < 64; i++)
			{
				if (contexts[context].m_classification[i] != null) dec_classification.initSymbolModel(contexts[context].m_classification[i]);
				if (contexts[context].m_flags[i] != null) dec_flags.initSymbolModel(contexts[context].m_flags[i]);
				if (contexts[context].m_user_data[i] != null) dec_user_data.initSymbolModel(contexts[context].m_user_data[i]);
			}

			// for the intensity layer
			contexts[context].ic_intensity.initDecompressor();
			for (int i = 0; i < 8; i++)
			{
				contexts[context].last_intensity[i] = item.intensity;
			}

			// for the scan_angle layer
			contexts[context].ic_scan_angle.initDecompressor();

			// for the point_source_ID layer
			contexts[context].ic_point_source_ID.initDecompressor();

			// for the gps_time layer
			dec_gps_time.initSymbolModel(contexts[context].m_gpstime_multi);
			dec_gps_time.initSymbolModel(contexts[context].m_gpstime_0diff);
			contexts[context].ic_gpstime.initDecompressor();
			contexts[context].last = 0;
			contexts[context].next = 0;
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

			// init current context from last item
			var last_item = contexts[context].last_item;
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

			contexts[context].last_item_gps_time_change = false;

			//Console.Error.WriteLine("INIT: current_context {0} last item {1:F14} {2} {3} {4} {5} {6} {7}", current_context, item.gps_time, item.X, item.Y, item.Z, item.intensity, item.extended_return_number, item.extended_number_of_returns);

			contexts[context].unused = false;

			return true;
		}

		void read_gps_time()
		{
			int multi;
			if (contexts[current_context].last_gpstime_diff[contexts[current_context].last] == 0) // if the last integer difference was zero
			{
				multi = (int)dec_gps_time.decodeSymbol(contexts[current_context].m_gpstime_0diff);
				if (multi == 0) // the difference can be represented with 32 bits
				{
					contexts[current_context].last_gpstime_diff[contexts[current_context].last] = contexts[current_context].ic_gpstime.decompress(0, 0);
					contexts[current_context].last_gpstime[contexts[current_context].last].i64 += contexts[current_context].last_gpstime_diff[contexts[current_context].last];
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				else if (multi == 1) // the difference is huge
				{
					contexts[current_context].next = (contexts[current_context].next + 1) & 3;
					contexts[current_context].last_gpstime[contexts[current_context].next].u64 = (ulong)contexts[current_context].ic_gpstime.decompress((int)(contexts[current_context].last_gpstime[contexts[current_context].last].u64 >> 32), 8);
					contexts[current_context].last_gpstime[contexts[current_context].next].u64 = contexts[current_context].last_gpstime[contexts[current_context].next].u64 << 32;
					contexts[current_context].last_gpstime[contexts[current_context].next].u64 |= dec_gps_time.readInt();
					contexts[current_context].last = contexts[current_context].next;
					contexts[current_context].last_gpstime_diff[contexts[current_context].last] = 0;
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				else // we switch to another sequence
				{
					contexts[current_context].last = (uint)(contexts[current_context].last + multi - 1) & 3;
					read_gps_time();
				}
			}
			else
			{
				multi = (int)dec_gps_time.decodeSymbol(contexts[current_context].m_gpstime_multi);
				if (multi == 1)
				{
					contexts[current_context].last_gpstime[contexts[current_context].last].i64 += contexts[current_context].ic_gpstime.decompress(contexts[current_context].last_gpstime_diff[contexts[current_context].last], 1); ;
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				else if (multi < LASZIP_GPSTIME_MULTI_CODE_FULL)
				{
					int gpstime_diff;
					if (multi == 0)
					{
						gpstime_diff = contexts[current_context].ic_gpstime.decompress(0, 7);
						contexts[current_context].multi_extreme_counter[contexts[current_context].last]++;
						if (contexts[current_context].multi_extreme_counter[contexts[current_context].last] > 3)
						{
							contexts[current_context].last_gpstime_diff[contexts[current_context].last] = gpstime_diff;
							contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
						}
					}
					else if (multi < LASZIP_GPSTIME_MULTI)
					{
						if (multi < 10)
							gpstime_diff = contexts[current_context].ic_gpstime.decompress(multi * contexts[current_context].last_gpstime_diff[contexts[current_context].last], 2);
						else
							gpstime_diff = contexts[current_context].ic_gpstime.decompress(multi * contexts[current_context].last_gpstime_diff[contexts[current_context].last], 3);
					}
					else if (multi == LASZIP_GPSTIME_MULTI)
					{
						gpstime_diff = contexts[current_context].ic_gpstime.decompress(LASZIP_GPSTIME_MULTI * contexts[current_context].last_gpstime_diff[contexts[current_context].last], 4);
						contexts[current_context].multi_extreme_counter[contexts[current_context].last]++;
						if (contexts[current_context].multi_extreme_counter[contexts[current_context].last] > 3)
						{
							contexts[current_context].last_gpstime_diff[contexts[current_context].last] = gpstime_diff;
							contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
						}
					}
					else
					{
						multi = LASZIP_GPSTIME_MULTI - multi;
						if (multi > LASZIP_GPSTIME_MULTI_MINUS)
						{
							gpstime_diff = contexts[current_context].ic_gpstime.decompress(multi * contexts[current_context].last_gpstime_diff[contexts[current_context].last], 5);
						}
						else
						{
							gpstime_diff = contexts[current_context].ic_gpstime.decompress(LASZIP_GPSTIME_MULTI_MINUS * contexts[current_context].last_gpstime_diff[contexts[current_context].last], 6);
							contexts[current_context].multi_extreme_counter[contexts[current_context].last]++;
							if (contexts[current_context].multi_extreme_counter[contexts[current_context].last] > 3)
							{
								contexts[current_context].last_gpstime_diff[contexts[current_context].last] = gpstime_diff;
								contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
							}
						}
					}
					contexts[current_context].last_gpstime[contexts[current_context].last].i64 += gpstime_diff;
				}
				else if (multi == LASZIP_GPSTIME_MULTI_CODE_FULL)
				{
					contexts[current_context].next = (contexts[current_context].next + 1) & 3;
					contexts[current_context].last_gpstime[contexts[current_context].next].u64 = (ulong)contexts[current_context].ic_gpstime.decompress((int)(contexts[current_context].last_gpstime[contexts[current_context].last].u64 >> 32), 8);
					contexts[current_context].last_gpstime[contexts[current_context].next].u64 = contexts[current_context].last_gpstime[contexts[current_context].next].u64 << 32;
					contexts[current_context].last_gpstime[contexts[current_context].next].u64 |= dec_gps_time.readInt();
					contexts[current_context].last = contexts[current_context].next;
					contexts[current_context].last_gpstime_diff[contexts[current_context].last] = 0;
					contexts[current_context].multi_extreme_counter[contexts[current_context].last] = 0;
				}
				else if (multi >= LASZIP_GPSTIME_MULTI_CODE_FULL)
				{
					contexts[current_context].last = (uint)(contexts[current_context].last + multi - LASZIP_GPSTIME_MULTI_CODE_FULL) & 3;
					read_gps_time();
				}
			}
		}
	}
}
