//===============================================================================
//
//  FILE:  laszip_dll.cs
//
//  CONTENTS:
//
//    C# port of a simple DLL interface to LASzip.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2012, martin isenburg, rapidlasso - tools to catch reality
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace laszip.net
{
	public class laszip_dll
	{
		public laszip_header header = new laszip_header();
		long p_count;
		long npoints;
		public laszip_point point = new laszip_point();

		Stream streamin;
		bool leaveStreamInOpen;
		LASreadPoint reader;

		Stream streamout;
		bool leaveStreamOutOpen;
		LASwritePoint writer;

		string error;
		string warning;

		static int laszip_get_version(out byte version_major, out byte version_minor, out ushort version_revision, out uint version_build)
		{
			version_major = LASzip.VERSION_MAJOR;
			version_minor = LASzip.VERSION_MINOR;
			version_revision = LASzip.VERSION_REVISION;
			version_build = LASzip.VERSION_BUILD_DATE;

			return 0;
		}

		public string laszip_get_error()
		{
			return error;
		}

		public string laszip_get_warning()
		{
			return warning;
		}

		public static laszip_dll laszip_create()
		{
			laszip_dll ret = new laszip_dll();
			ret.laszip_clean();
			return ret;
		}

		public int laszip_clean()
		{
			try
			{
				if (reader != null)
				{
					error = "cannot clean while reader is open.";
					return 1;
				}

				if (writer != null)
				{
					error = "cannot clean while writer is open.";
					return 1;
				}

				// zero everything
				header.file_source_ID = 0;
				header.global_encoding = 0;
				header.project_ID_GUID_data_1 = 0;
				header.project_ID_GUID_data_2 = 0;
				header.project_ID_GUID_data_3 = 0;
				Array.Clear(header.project_ID_GUID_data_4, 0, header.project_ID_GUID_data_4.Length);
				header.version_major = 0;
				header.version_minor = 0;
				Array.Clear(header.system_identifier, 0, header.system_identifier.Length);
				Array.Clear(header.generating_software, 0, header.generating_software.Length);
				header.file_creation_day = 0;
				header.file_creation_year = 0;
				header.header_size = 0;
				header.offset_to_point_data = 0;
				header.number_of_variable_length_records = 0;
				header.point_data_format = 0;
				header.point_data_record_length = 0;
				header.number_of_point_records = 0;
				Array.Clear(header.number_of_points_by_return, 0, header.number_of_points_by_return.Length);
				header.x_scale_factor = 0;
				header.y_scale_factor = 0;
				header.z_scale_factor = 0;
				header.x_offset = 0;
				header.y_offset = 0;
				header.z_offset = 0;
				header.max_x = 0;
				header.min_x = 0;
				header.max_y = 0;
				header.min_y = 0;
				header.max_z = 0;
				header.min_z = 0;
				header.start_of_waveform_data_packet_record = 0;
				header.start_of_first_extended_variable_length_record = 0;
				header.number_of_extended_variable_length_records = 0;
				header.extended_number_of_point_records = 0;
				Array.Clear(header.extended_number_of_points_by_return, 0, header.extended_number_of_points_by_return.Length);
				header.user_data_in_header_size = 0;
				header.user_data_in_header = null;
				header.vlrs = null;
				header.user_data_after_header_size = 0;
				header.user_data_after_header = null;

				p_count = 0;
				npoints = 0;

				point.X = 0;
				point.Y = 0;
				point.Z = 0;
				point.intensity = 0;
				point.return_number = 0;// : 3;
				point.number_of_returns_of_given_pulse = 0;// : 3;
				point.scan_direction_flag = 0;// : 1;
				point.edge_of_flight_line = 0;// : 1;
				point.classification = 0;
				point.scan_angle_rank = 0;
				point.user_data = 0;
				point.point_source_ID = 0;
				point.gps_time = 0;
				point.rgb = new ushort[4];
				point.wave_packet = new byte[29];
				point.extended_point_type = 0;// : 2;
				point.extended_scanner_channel = 0;// : 2;
				point.extended_classification_flags = 0;// : 4;
				point.extended_classification = 0;
				point.extended_return_number = 0;// : 4;
				point.extended_number_of_returns_of_given_pulse = 0;// : 4;
				point.extended_scan_angle = 0;
				point.num_extra_bytes = 0;
				point.extra_bytes = null;

				streamin = null;
				reader = null;

				streamout = null;
				writer = null;

				error = null;
				warning = null;

				// create default header
				byte[] generatingSoftware = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(generatingSoftware, header.generating_software, Math.Min(generatingSoftware.Length, 32));
				header.version_major = 1;
				header.version_minor = 2;
				header.header_size = 227;
				header.offset_to_point_data = 227;
				header.point_data_format = 1;
				header.point_data_record_length = 28;
				header.x_scale_factor = 0.01;
				header.y_scale_factor = 0.01;
				header.z_scale_factor = 0.01;
			}
			catch
			{
				error = "internal error in laszip_clean";
				return 1;
			}

			return 0;
		}

		[Obsolete]
		public laszip_header laszip_get_header_pointer()
		{
			return header;
		}

		[Obsolete]
		public laszip_point laszip_get_point_pointer()
		{
			return point;
		}

		public int laszip_get_point_count(out long count)
		{
			count = 0;
			if (reader == null && writer == null)
			{
				error = "getting count before reader or writer was opened";
				return 1;
			}

			count = p_count;

			error = null;
			return 0;
		}

		public int laszip_get_number_of_point(out long npoints)
		{
			npoints = 0;
			if (reader == null && writer == null)
			{
				error = "getting count before reader or writer was opened";
				return 1;
			}

			npoints = this.npoints;

			error = null;
			return 0;
		}

		public int laszip_set_header(laszip_header header)
		{
			if (header == null)
			{
				error = "laszip_header_struct pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set header after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot set header after writer was opened";
				return 1;
			}

			try
			{
				this.header.file_source_ID = header.file_source_ID;
				this.header.global_encoding = header.global_encoding;
				this.header.project_ID_GUID_data_1 = header.project_ID_GUID_data_1;
				this.header.project_ID_GUID_data_2 = header.project_ID_GUID_data_2;
				this.header.project_ID_GUID_data_3 = header.project_ID_GUID_data_3;
				Array.Copy(header.project_ID_GUID_data_4, this.header.project_ID_GUID_data_4, 8);
				this.header.version_major = header.version_major;
				this.header.version_minor = header.version_minor;
				Array.Copy(header.system_identifier, this.header.system_identifier, 32);
				Array.Copy(header.generating_software, this.header.generating_software, 32);
				this.header.file_creation_day = header.file_creation_day;
				this.header.file_creation_year = header.file_creation_year;
				this.header.header_size = header.header_size;
				this.header.offset_to_point_data = header.offset_to_point_data;
				this.header.number_of_variable_length_records = header.number_of_variable_length_records;
				this.header.point_data_format = header.point_data_format;
				this.header.point_data_record_length = header.point_data_record_length;
				this.header.number_of_point_records = header.number_of_point_records;
				for (int i = 0; i < 5; i++) this.header.number_of_points_by_return[i] = header.number_of_points_by_return[i];
				this.header.x_scale_factor = header.x_scale_factor;
				this.header.y_scale_factor = header.y_scale_factor;
				this.header.z_scale_factor = header.z_scale_factor;
				this.header.x_offset = header.x_offset;
				this.header.y_offset = header.y_offset;
				this.header.z_offset = header.z_offset;
				this.header.max_x = header.max_x;
				this.header.min_x = header.min_x;
				this.header.max_y = header.max_y;
				this.header.min_y = header.min_y;
				this.header.max_z = header.max_z;
				this.header.min_z = header.min_z;

				if (this.header.version_minor >= 3)
				{
					this.header.start_of_waveform_data_packet_record = header.start_of_first_extended_variable_length_record;
				}

				if (this.header.version_minor >= 4)
				{
					this.header.start_of_first_extended_variable_length_record = header.start_of_first_extended_variable_length_record;
					this.header.number_of_extended_variable_length_records = header.number_of_extended_variable_length_records;
					this.header.extended_number_of_point_records = header.extended_number_of_point_records;
					for (int i = 0; i < 15; i++) this.header.extended_number_of_points_by_return[i] = header.extended_number_of_points_by_return[i];
				}

				this.header.user_data_in_header_size = header.user_data_in_header_size;
				this.header.user_data_in_header = null;

				if (header.user_data_in_header_size != 0)
				{
					this.header.user_data_in_header = new byte[header.user_data_in_header_size];
					Array.Copy(header.user_data_in_header, this.header.user_data_in_header, header.user_data_in_header_size);
				}

				this.header.vlrs = new List<laszip_vlr>();
				if (header.number_of_variable_length_records != 0)
				{
					for (int i = 0; i < header.number_of_variable_length_records; i++)
					{
						this.header.vlrs.Add(new laszip_vlr());
						this.header.vlrs[i].reserved = header.vlrs[i].reserved;
						Array.Copy(header.vlrs[i].user_id, this.header.vlrs[i].user_id, 16);
						this.header.vlrs[i].record_id = header.vlrs[i].record_id;
						this.header.vlrs[i].record_length_after_header = header.vlrs[i].record_length_after_header;
						Array.Copy(header.vlrs[i].description, this.header.vlrs[i].description, 32);
						if (header.vlrs[i].record_length_after_header != 0)
						{
							this.header.vlrs[i].data = new byte[header.vlrs[i].record_length_after_header];
							Array.Copy(header.vlrs[i].data, this.header.vlrs[i].data, header.vlrs[i].record_length_after_header);
						}
						else
						{
							this.header.vlrs[i].data = null;
						}
					}
				}

				this.header.user_data_after_header_size = header.user_data_after_header_size;
				this.header.user_data_after_header = null;
				if (header.user_data_after_header_size != 0)
				{
					this.header.user_data_after_header = new byte[header.user_data_after_header_size];
					Array.Copy(header.user_data_after_header, this.header.user_data_after_header, header.user_data_after_header_size);
				}
			}
			catch
			{
				error = "internal error in laszip_set_header";
				return 1;
			}

			error = null;
			return 0;
		}

		int laszip_check_for_integer_overflow()
		{
			try
			{
				// quantize and dequantize the bounding box with current scale_factor and offset
				int quant_min_x = MyDefs.I32_QUANTIZE((header.min_x - header.x_offset) / header.x_scale_factor);
				int quant_max_x = MyDefs.I32_QUANTIZE((header.max_x - header.x_offset) / header.x_scale_factor);
				int quant_min_y = MyDefs.I32_QUANTIZE((header.min_y - header.y_offset) / header.y_scale_factor);
				int quant_max_y = MyDefs.I32_QUANTIZE((header.max_y - header.y_offset) / header.y_scale_factor);
				int quant_min_z = MyDefs.I32_QUANTIZE((header.min_z - header.z_offset) / header.z_scale_factor);
				int quant_max_z = MyDefs.I32_QUANTIZE((header.max_z - header.z_offset) / header.z_scale_factor);

				double dequant_min_x = header.x_scale_factor * quant_min_x + header.x_offset;
				double dequant_max_x = header.x_scale_factor * quant_max_x + header.x_offset;
				double dequant_min_y = header.y_scale_factor * quant_min_y + header.y_offset;
				double dequant_max_y = header.y_scale_factor * quant_max_y + header.y_offset;
				double dequant_min_z = header.z_scale_factor * quant_min_z + header.z_offset;
				double dequant_max_z = header.z_scale_factor * quant_max_z + header.z_offset;

				// make sure that there is not sign flip (a 32-bit integer overflow) for the bounding box
				if ((header.min_x > 0) != (dequant_min_x > 0))
				{
					error = string.Format("quantization sign flip for min_x from {0} to {1}. set scale factor for x coarser than {2}", header.min_x, dequant_min_x, header.x_scale_factor);
					return 1;
				}
				if ((header.max_x > 0) != (dequant_max_x > 0))
				{
					error = string.Format("quantization sign flip for max_x from {0} to {1}. set scale factor for x coarser than {2}", header.max_x, dequant_max_x, header.x_scale_factor);
					return 1;
				}
				if ((header.min_y > 0) != (dequant_min_y > 0))
				{
					error = string.Format("quantization sign flip for min_y from {0} to {1}. set scale factor for y coarser than {2}", header.min_y, dequant_min_y, header.y_scale_factor);
					return 1;
				}
				if ((header.max_y > 0) != (dequant_max_y > 0))
				{
					error = string.Format("quantization sign flip for max_y from {0} to {1}. set scale factor for y coarser than {2}", header.max_y, dequant_max_y, header.y_scale_factor);
					return 1;
				}
				if ((header.min_z > 0) != (dequant_min_z > 0))
				{
					error = string.Format("quantization sign flip for min_z from {0} to {1}. set scale factor for z coarser than {2}", header.min_z, dequant_min_z, header.z_scale_factor);
					return 1;
				}
				if ((header.max_z > 0) != (dequant_max_z > 0))
				{
					error = string.Format("quantization sign flip for max_z from {0} to {1}. set scale factor for z coarser than {2}", header.max_z, dequant_max_z, header.z_scale_factor);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_auto_offset";
				return 1;
			}

			error = null;
			return 0;
		}

		int laszip_auto_offset()
		{
			try
			{
				if (reader != null)
				{
					error = "cannot auto offset after reader was opened";
					return 1;
				}

				if (writer != null)
				{
					error = "cannot auto offset after writer was opened";
					return 1;
				}

				// check scale factor
				double x_scale_factor = header.x_scale_factor;
				double y_scale_factor = header.y_scale_factor;
				double z_scale_factor = header.z_scale_factor;

				if ((x_scale_factor <= 0) || double.IsInfinity(x_scale_factor))
				{
					error = string.Format("invalid x scale_factor {0} in header", header.x_scale_factor);
					return 1;
				}

				if ((y_scale_factor <= 0) || double.IsInfinity(y_scale_factor))
				{
					error = string.Format("invalid y scale_factor {0} in header", header.y_scale_factor);
					return 1;
				}

				if ((z_scale_factor <= 0) || double.IsInfinity(z_scale_factor))
				{
					error = string.Format("invalid z scale_factor {0} in header", header.z_scale_factor);
					return 1;
				}

				double center_bb_x = (header.min_x + header.max_x) / 2;
				double center_bb_y = (header.min_y + header.max_y) / 2;
				double center_bb_z = (header.min_z + header.max_z) / 2;

				if (double.IsInfinity(center_bb_x))
				{
					error = string.Format("invalid x coordinate at center of bounding box (min: {0} max: {1})", header.min_x, header.max_x);
					return 1;
				}

				if (double.IsInfinity(center_bb_y))
				{
					error = string.Format("invalid y coordinate at center of bounding box (min: {0} max: {1})", header.min_y, header.max_y);
					return 1;
				}

				if (double.IsInfinity(center_bb_z))
				{
					error = string.Format("invalid z coordinate at center of bounding box (min: {0} max: {1})", header.min_z, header.max_z);
					return 1;
				}

				double x_offset = header.x_offset;
				double y_offset = header.y_offset;
				double z_offset = header.z_offset;

				header.x_offset = (MyDefs.I64_FLOOR(center_bb_x / x_scale_factor / 10000000)) * 10000000 * x_scale_factor;
				header.y_offset = (MyDefs.I64_FLOOR(center_bb_y / y_scale_factor / 10000000)) * 10000000 * y_scale_factor;
				header.z_offset = (MyDefs.I64_FLOOR(center_bb_z / z_scale_factor / 10000000)) * 10000000 * z_scale_factor;

				if (laszip_check_for_integer_overflow() != 0)
				{
					header.x_offset = x_offset;
					header.y_offset = y_offset;
					header.z_offset = z_offset;
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_auto_offset";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_set_point(laszip_point point)
		{
			if (point == null)
			{
				error = "laszip_point_struct pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set point for reader";
				return 1;
			}

			try
			{
				this.point.classification = point.classification;
				this.point.edge_of_flight_line = point.edge_of_flight_line;
				this.point.extended_classification = point.extended_classification;
				this.point.extended_classification_flags = point.extended_classification_flags;
				this.point.extended_number_of_returns_of_given_pulse = point.extended_number_of_returns_of_given_pulse;
				this.point.extended_point_type = point.extended_point_type;
				this.point.extended_return_number = point.extended_return_number;
				this.point.extended_scan_angle = point.extended_scan_angle;
				this.point.extended_scanner_channel = point.extended_scanner_channel;
				this.point.gps_time = point.gps_time;
				this.point.intensity = point.intensity;
				this.point.num_extra_bytes = point.num_extra_bytes;
				this.point.number_of_returns_of_given_pulse = point.number_of_returns_of_given_pulse;
				this.point.point_source_ID = point.point_source_ID;
				this.point.return_number = point.return_number;
				Array.Copy(point.rgb, this.point.rgb, 4);
				this.point.scan_angle_rank = point.scan_angle_rank;
				this.point.scan_direction_flag = point.scan_direction_flag;
				this.point.user_data = point.user_data;
				this.point.X = point.X;
				this.point.Y = point.Y;
				this.point.Z = point.Z;
				Array.Copy(point.wave_packet, this.point.wave_packet, 29);

				if (this.point.extra_bytes != null)
				{
					if (point.extra_bytes != null)
					{
						if (this.point.num_extra_bytes == point.num_extra_bytes)
						{
							Array.Copy(point.extra_bytes, this.point.extra_bytes, point.num_extra_bytes);
						}
						else
						{
							error = string.Format("target point has {0} extra bytes but source point has {1}", this.point.num_extra_bytes, point.num_extra_bytes);
							return 1;
						}
					}
					else
					{
						error = "target point has extra bytes but source point does not";
						return 1;
					}
				}
				else
				{
					if (point.extra_bytes != null)
					{
						error = "source point has extra bytes but target point does not";
						return 1;
					}
				}
			}
			catch
			{
				error = "internal error in laszip_set_point";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_set_coordinates(double[] coordinates)
		{
			if (coordinates == null)
			{
				error = "laszip_F64 coordinates pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set coordinates for reader";
				return 1;
			}

			try
			{
				// set the coordinates
				point.X = MyDefs.I32_QUANTIZE((coordinates[0] - header.x_offset) / header.x_scale_factor);
				point.Y = MyDefs.I32_QUANTIZE((coordinates[1] - header.y_offset) / header.y_scale_factor);
				point.Z = MyDefs.I32_QUANTIZE((coordinates[2] - header.z_offset) / header.z_scale_factor);
			}
			catch
			{
				error = "internal error in laszip_set_coordinates";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_get_coordinates(double[] coordinates)
		{
			if (coordinates == null)
			{
				error = "laszip_F64 coordinates pointer is zero";
				return 1;
			}

			try
			{
				// get the coordinates
				coordinates[0] = header.x_scale_factor * point.X + header.x_offset;
				coordinates[1] = header.y_scale_factor * point.Y + header.y_offset;
				coordinates[2] = header.z_scale_factor * point.Z + header.z_offset;
			}
			catch
			{
				error = "internal error in laszip_get_coordinates";
				return 1;
			}

			error = null;
			return 0;
		}

		public unsafe int laszip_set_geokeys(ushort number, laszip_geokey[] key_entries)
		{
			if (number == 0)
			{
				error = "number of key_entries is zero";
				return 1;
			}

			if (key_entries == null)
			{
				error = "key_entries pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set geokeys after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot set geokeys after writer was opened";
				return 1;
			}

			try
			{
				// create the geokey directory
				byte[] buffer = new byte[sizeof(laszip_geokey) * (number + 1)];

				fixed (byte* pBuffer = buffer)
				{
					laszip_geokey* key_entries_plus_one = (laszip_geokey*)pBuffer;

					key_entries_plus_one[0].key_id = 1;            // aka key_directory_version
					key_entries_plus_one[0].tiff_tag_location = 1; // aka key_revision
					key_entries_plus_one[0].count = 0;             // aka minor_revision
					key_entries_plus_one[0].value_offset = number; // aka number_of_keys
					for (int i = 0; i < number; i++) key_entries_plus_one[i + 1] = key_entries[i];
				}

				// fill a VLR
				laszip_vlr vlr = new laszip_vlr();
				vlr.reserved = 0xAABB;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 34735;
				vlr.record_length_after_header = (ushort)(8 + number * 8);

				// description field must be a null-terminate string, so we don't copy more than 31 characters
				byte[] v = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(v, vlr.description, Math.Min(v.Length, 31));

				vlr.data = buffer;

				// add the VLR
				if (laszip_add_vlr(vlr) != 0)
				{
					error = string.Format("setting {0} geokeys", number);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_set_geokey_entries";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_set_geodouble_params(ushort number, double[] geodouble_params)
		{
			if (number == 0)
			{
				error = "number of geodouble_params is zero";
				return 1;
			}

			if (geodouble_params == null)
			{
				error = "geodouble_params pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set geodouble_params after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot set geodouble_params after writer was opened";
				return 1;
			}

			try
			{
				// fill a VLR
				laszip_vlr vlr = new laszip_vlr();
				vlr.reserved = 0xAABB;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 34736;
				vlr.record_length_after_header = (ushort)(number * 8);

				// description field must be a null-terminate string, so we don't copy more than 31 characters
				byte[] v = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(v, vlr.description, Math.Min(v.Length, 31));

				byte[] buffer = new byte[number * 8];
				Buffer.BlockCopy(geodouble_params, 0, buffer, 0, number * 8);
				vlr.data = buffer;

				// add the VLR
				if (laszip_add_vlr(vlr) != 0)
				{
					error = string.Format("setting {0} geodouble_params", number);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_set_geodouble_params";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_set_geoascii_params(ushort number, byte[] geoascii_params)
		{
			if (number == 0)
			{
				error = "number of geoascii_params is zero";
				return 1;
			}

			if (geoascii_params == null)
			{
				error = "geoascii_params pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set geoascii_params after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot set geoascii_params after writer was opened";
				return 1;
			}

			try
			{
				// fill a VLR
				laszip_vlr vlr = new laszip_vlr();
				vlr.reserved = 0xAABB;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 34737;
				vlr.record_length_after_header = number;

				// description field must be a null-terminate string, so we don't copy more than 31 characters
				byte[] v = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(v, vlr.description, Math.Min(v.Length, 31));

				vlr.data = geoascii_params;

				// add the VLR
				if (laszip_add_vlr(vlr) != 0)
				{
					error = string.Format("setting {0} geoascii_params", number);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_set_geoascii_params";
				return 1;
			}

			error = null;
			return 0;
		}

		static bool ArrayCompare(byte[] a, byte[] b)
		{
			int len = Math.Min(a.Length, b.Length);
			int i = 0;
			for (; i < len; i++)
			{
				if (a[i] != b[i]) return false;
				if (a[i] == 0) break;
			}

			if (i < len - 1) return true;
			return a.Length == b.Length;
		}

		public int laszip_add_vlr(laszip_vlr vlr)
		{
			if (vlr == null)
			{
				error = "laszip_vlr_struct vlr pointer is zero";
				return 1;
			}

			if ((vlr.record_length_after_header > 0) && (vlr.data == null))
			{
				error = string.Format("VLR has record_length_after_header of {0} but VLR data pointer is zero", vlr.record_length_after_header);
				return 1;
			}

			if (reader != null)
			{
				error = "cannot add vlr after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot add vlr after writer was opened";
				return 1;
			}

			try
			{
				if (header.vlrs.Count > 0)
				{
					// overwrite existing VLR ?
					for (int i = (int)header.number_of_variable_length_records - 1; i >= 0; i++)
					{
						if (header.vlrs[i].record_id == vlr.record_id && !ArrayCompare(header.vlrs[i].user_id, vlr.user_id))
						{
							if (header.vlrs[i].record_length_after_header != 0)
								header.offset_to_point_data -= header.vlrs[i].record_length_after_header;

							header.vlrs.RemoveAt(i);
						}
					}
				}

				header.vlrs.Add(vlr);
				header.number_of_variable_length_records = (uint)header.vlrs.Count;
				header.offset_to_point_data += 54;

				// copy the VLR
				header.offset_to_point_data += vlr.record_length_after_header;
			}
			catch
			{
				error = "internal error in laszip_add_vlr";
				return 1;
			}

			error = null;
			return 0;
		}

		static int CheckHeaderAndSetup(laszip_header header, bool compress, out LASzip laszip, ref laszip_point point, out uint laszip_vrl_payload_size, out string error)
		{
			laszip = null;
			laszip_vrl_payload_size = 0;
			error = null;

			#region check header and prepare point

			uint vlrs_size = 0;

			if (header.version_major != 1)
			{
				error = string.Format("unknown LAS version {0}.{1}", header.version_major, header.version_minor);
				return 1;
			}

			if (compress && (header.point_data_format > 5))
			{
				error = string.Format("compressor does not yet support point data format {1}", header.point_data_format);
				return 1;
			}

			if (header.number_of_variable_length_records != 0)
			{
				if (header.vlrs == null)
				{
					error = string.Format("number_of_variable_length_records is {0} but vlrs pointer is zero", header.number_of_variable_length_records);
					return 1;
				}

				for (int i = 0; i < header.number_of_variable_length_records; i++)
				{
					vlrs_size += 54;
					if (header.vlrs[i].record_length_after_header != 0)
					{
						if (header.vlrs == null)
						{
							error = string.Format("vlrs[{0}].record_length_after_header is {1} but vlrs[{0}].data pointer is zero", i, header.vlrs[i].record_length_after_header);
							return 1;
						}
						vlrs_size += header.vlrs[i].record_length_after_header;
					}
				}
			}

			if ((vlrs_size + header.header_size + header.user_data_after_header_size) != header.offset_to_point_data)
			{
				error = string.Format("header_size ({0}) plus vlrs_size ({1}) plus user_data_after_header_size ({2}) does not equal offset_to_point_data ({3})", header.header_size, vlrs_size, header.user_data_after_header_size, header.offset_to_point_data);
				return 1;
			}

			try
			{
				laszip = new LASzip();
			}
			catch
			{
				error = "could not alloc LASzip";
				return 1;
			}

			if (!laszip.setup(header.point_data_format, header.point_data_record_length, LASzip.COMPRESSOR_NONE))
			{
				error = string.Format("invalid combination of point_data_format {0} and point_data_record_length {1}", header.point_data_format, header.point_data_record_length);
				return 1;
			}

			#region create point's item pointers
			for (uint i = 0; i < laszip.num_items; i++)
			{
				switch (laszip.items[i].type)
				{
					case LASitem.Type.POINT14:
					case LASitem.Type.POINT10:
					case LASitem.Type.GPSTIME11:
					case LASitem.Type.RGBNIR14:
					case LASitem.Type.RGB12:
					case LASitem.Type.WAVEPACKET13: break;
					case LASitem.Type.BYTE:
						point.num_extra_bytes = laszip.items[i].size;
						point.extra_bytes = new byte[point.num_extra_bytes];
						break;
					default:
						error = string.Format("unknown LASitem type {0}", laszip.items[i].type);
						return 1;
				}
			}
			#endregion

			if (compress)
			{
				if (!laszip.setup(header.point_data_format, header.point_data_record_length, LASzip.COMPRESSOR_DEFAULT))
				{
					error = string.Format("cannot compress point_data_format {0} with point_data_record_length {1}", header.point_data_format, header.point_data_record_length);
					return 1;
				}
				laszip.request_version(2);
				laszip_vrl_payload_size = 34u + 6u * laszip.num_items;
			}
			else
			{
				laszip.request_version(0);
			}
			#endregion

			return 0;
		}

		public int laszip_open_writer(Stream streamout, bool compress, bool leaveOpen = false)
		{
			if (!streamout.CanWrite)
			{
				error = "can not write output stream";
				return 1;
			}

			if (reader != null)
			{
				error = "reader is already open";
				return 1;
			}

			if (writer != null)
			{
				error = "writer is already open";
				return 1;
			}

			try
			{
				LASzip laszip;
				uint laszip_vrl_payload_size;

				int err = CheckHeaderAndSetup(header, compress, out laszip, ref point, out laszip_vrl_payload_size, out error);
				if (err != 0) return err;

				this.streamout = streamout;
				leaveStreamOutOpen = leaveOpen;

				return laszip_open_writer_stream(compress, laszip, laszip_vrl_payload_size);
			}
			catch
			{
				error = string.Format("internal error in laszip_open_writer (Stream)");
				return 1;
			}
		}

		public int laszip_open_writer(string file_name, bool compress)
		{
			if (file_name == null || file_name.Length == 0)
			{
				error = "string file_name pointer is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "reader is already open";
				return 1;
			}

			if (writer != null)
			{
				error = "writer is already open";
				return 1;
			}

			try
			{
				LASzip laszip;
				uint laszip_vrl_payload_size;

				int err = CheckHeaderAndSetup(header, compress, out laszip, ref point, out laszip_vrl_payload_size, out error);
				if (err != 0) return err;

				#region open the file
				try
				{
					streamout = new FileStream(file_name, FileMode.Create, FileAccess.Write, FileShare.Read);
					leaveStreamOutOpen = false;
				}
				catch
				{
					error = string.Format("cannot open file '{0}'", file_name);
					return 1;
				}
				#endregion

				return laszip_open_writer_stream(compress, laszip, laszip_vrl_payload_size);
			}
			catch
			{
				error = string.Format("internal error in laszip_open_writer '{0}'", file_name);
				return 1;
			}
		}

		int laszip_open_writer_stream(bool compress, LASzip laszip, uint laszip_vrl_payload_size)
		{
			#region write the header variable after variable
			try
			{
				streamout.WriteByte((byte)'L');
				streamout.WriteByte((byte)'A');
				streamout.WriteByte((byte)'S');
				streamout.WriteByte((byte)'F');
			}
			catch
			{
				error = "writing header.file_signature";
				return 1;
			}

			try { streamout.Write(BitConverter.GetBytes(header.file_source_ID), 0, 2); }
			catch { error = "writing header.file_source_ID"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.global_encoding), 0, 2); }
			catch { error = "writing header.global_encoding"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.project_ID_GUID_data_1), 0, 4); }
			catch { error = "writing header.project_ID_GUID_data_1"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.project_ID_GUID_data_2), 0, 2); }
			catch { error = "writing header.project_ID_GUID_data_2"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.project_ID_GUID_data_3), 0, 2); }
			catch { error = "writing header.project_ID_GUID_data_3"; return 1; }

			try { streamout.Write(header.project_ID_GUID_data_4, 0, 8); }
			catch { error = "writing header.project_ID_GUID_data_4"; return 1; }

			try { streamout.WriteByte(header.version_major); }
			catch { error = "writing header.version_major"; return 1; }

			try { streamout.WriteByte(header.version_minor); }
			catch { error = "writing header.version_minor"; return 1; }

			try { streamout.Write(header.system_identifier, 0, 32); }
			catch { error = "writing header.system_identifier"; return 1; }

			if (header.generating_software == null || header.generating_software.Length != 32)
			{
				byte[] generatingSoftware = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(generatingSoftware, header.generating_software, Math.Min(generatingSoftware.Length, 32));
			}

			try { streamout.Write(header.generating_software, 0, 32); }
			catch { error = "writing header.generating_software"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.file_creation_day), 0, 2); }
			catch { error = "writing header.file_creation_day"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.file_creation_year), 0, 2); }
			catch { error = "writing header.file_creation_year"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.header_size), 0, 2); }
			catch { error = "writing header.header_size"; return 1; }

			if (compress) header.offset_to_point_data += (54 + laszip_vrl_payload_size);

			try { streamout.Write(BitConverter.GetBytes(header.offset_to_point_data), 0, 4); }
			catch { error = "writing header.offset_to_point_data"; return 1; }

			if (compress)
			{
				header.offset_to_point_data -= (54 + laszip_vrl_payload_size);
				header.number_of_variable_length_records += 1;
			}

			try { streamout.Write(BitConverter.GetBytes(header.number_of_variable_length_records), 0, 4); }
			catch { error = "writing header.number_of_variable_length_records"; return 1; }

			if (compress)
			{
				header.number_of_variable_length_records -= 1;
				header.point_data_format |= 128;
			}

			try { streamout.WriteByte(header.point_data_format); }
			catch { error = "writing header.point_data_format"; return 1; }

			if (compress) header.point_data_format &= 127;

			try { streamout.Write(BitConverter.GetBytes(header.point_data_record_length), 0, 2); }
			catch { error = "writing header.point_data_record_length"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.number_of_point_records), 0, 4); }
			catch { error = "writing header.number_of_point_records"; return 1; }

			for (uint i = 0; i < 5; i++)
			{
				try { streamout.Write(BitConverter.GetBytes(header.number_of_points_by_return[i]), 0, 4); }
				catch { error = string.Format("writing header.number_of_points_by_return {0}", i); return 1; }
			}

			try { streamout.Write(BitConverter.GetBytes(header.x_scale_factor), 0, 8); }
			catch { error = "writing header.x_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.y_scale_factor), 0, 8); }
			catch { error = "writing header.y_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.z_scale_factor), 0, 8); }
			catch { error = "writing header.z_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.x_offset), 0, 8); }
			catch { error = "writing header.x_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.y_offset), 0, 8); }
			catch { error = "writing header.y_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.z_offset), 0, 8); }
			catch { error = "writing header.z_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.max_x), 0, 8); }
			catch { error = "writing header.max_x"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.min_x), 0, 8); }
			catch { error = "writing header.min_x"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.max_y), 0, 8); }
			catch { error = "writing header.max_y"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.min_y), 0, 8); }
			catch { error = "writing header.min_y"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.max_z), 0, 8); }
			catch { error = "writing header.max_z"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(header.min_z), 0, 8); }
			catch { error = "writing header.min_z"; return 1; }

			#region special handling for LAS 1.3+
			if (header.version_major == 1 && header.version_minor >= 3)
			{
				if (header.header_size < 235)
				{
					error = string.Format("for LAS 1.{0} header_size should at least be 235 but it is only {1}", header.version_minor, header.header_size);
					return 1;
				}

				try { streamout.Write(BitConverter.GetBytes(header.start_of_waveform_data_packet_record), 0, 8); }
				catch { error = "writing header.start_of_waveform_data_packet_record"; return 1; }

				header.user_data_in_header_size = header.header_size - 235u;
			}
			else header.user_data_in_header_size = header.header_size - 227u;
			#endregion

			#region special handling for LAS 1.4+
			if (header.version_major == 1 && header.version_minor >= 4)
			{
				if (header.header_size < 375)
				{
					error = string.Format("for LAS 1.{0} header_size should at least be 375 but it is only {1}", header.version_minor, header.header_size);
					return 1;
				}

				try { streamout.Write(BitConverter.GetBytes(header.start_of_first_extended_variable_length_record), 0, 8); }
				catch { error = "writing header.start_of_first_extended_variable_length_record"; return 1; }

				try { streamout.Write(BitConverter.GetBytes(header.number_of_extended_variable_length_records), 0, 4); }
				catch { error = "writing header.number_of_extended_variable_length_records"; return 1; }

				try { streamout.Write(BitConverter.GetBytes(header.extended_number_of_point_records), 0, 8); }
				catch { error = "writing header.extended_number_of_point_records"; return 1; }

				for (uint i = 0; i < 15; i++)
				{
					try { streamout.Write(BitConverter.GetBytes(header.extended_number_of_points_by_return[i]), 0, 8); }
					catch { error = string.Format("writing header.extended_number_of_points_by_return[{0}]", i); return 1; }
				}

				header.user_data_in_header_size = header.header_size - 375u;
			}
			#endregion

			#region write any number of user-defined bytes that might have been added to the header
			if (header.user_data_in_header_size != 0)
			{
				try { streamout.Write(header.user_data_in_header, 0, (int)header.user_data_in_header_size); }
				catch { error = string.Format("writing {0} bytes of data into header.user_data_in_header", header.user_data_in_header_size); return 1; }
			}
			#endregion

			#region write variable length records into the header
			if (header.number_of_variable_length_records != 0)
			{
				for (int i = 0; i < header.number_of_variable_length_records; i++)
				{
					// write variable length records variable after variable (to avoid alignment issues)
					try { streamout.Write(BitConverter.GetBytes(header.vlrs[i].reserved), 0, 2); }
					catch { error = string.Format("writing header.vlrs[{0}].reserved", i); return 1; }

					try { streamout.Write(header.vlrs[i].user_id, 0, 16); }
					catch { error = string.Format("writing header.vlrs[{0}].user_id", i); return 1; }

					try { streamout.Write(BitConverter.GetBytes(header.vlrs[i].record_id), 0, 2); }
					catch { error = string.Format("writing header.vlrs[{0}].record_id", i); return 1; }

					try { streamout.Write(BitConverter.GetBytes(header.vlrs[i].record_length_after_header), 0, 2); }
					catch { error = string.Format("writing header.vlrs[{0}].record_length_after_header", i); return 1; }

					try { streamout.Write(header.vlrs[i].description, 0, 32); }
					catch { error = string.Format("writing header.vlrs[{0}].description", i); return 1; }

					// write data following the header of the variable length record
					if (header.vlrs[i].record_length_after_header != 0)
					{
						try { streamout.Write(header.vlrs[i].data, 0, header.vlrs[i].record_length_after_header); }
						catch { error = string.Format("writing {0} bytes of data into header.vlrs[{1}].data", header.vlrs[i].record_length_after_header, i); return 1; }
					}
				}
			}

			if (compress)
			{
				#region write the LASzip VLR header
				uint i = header.number_of_variable_length_records;

				ushort reserved = 0xAABB;
				try { streamout.Write(BitConverter.GetBytes(reserved), 0, 2); }
				catch { error = string.Format("writing header.vlrs[{0}].reserved", i); return 1; }

				byte[] user_id1 = Encoding.ASCII.GetBytes("laszip encoded");
				byte[] user_id = new byte[16];
				Array.Copy(user_id1, user_id, Math.Min(16, user_id1.Length));
				try { streamout.Write(user_id, 0, 16); }
				catch { error = string.Format("writing header.vlrs[{0}].user_id", i); return 1; }

				ushort record_id = 22204;
				try { streamout.Write(BitConverter.GetBytes(record_id), 0, 2); }
				catch { error = string.Format("writing header.vlrs[{0}].record_id", i); return 1; }

				ushort record_length_after_header = (ushort)laszip_vrl_payload_size;
				try { streamout.Write(BitConverter.GetBytes(record_length_after_header), 0, 2); }
				catch { error = string.Format("writing header.vlrs[{0}].record_length_after_header", i); return 1; }

				// description field must be a null-terminate string, so we don't copy more than 31 characters
				byte[] description1 = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				byte[] description = new byte[32];
				Array.Copy(description1, description, Math.Min(31, description1.Length));

				try { streamout.Write(description, 0, 32); }
				catch { error = string.Format("writing header.vlrs[{0}].description", i); return 1; }

				// write the LASzip VLR payload

				//     U16  compressor                2 bytes
				//     U32  coder                     2 bytes
				//     U8   version_major             1 byte
				//     U8   version_minor             1 byte
				//     U16  version_revision          2 bytes
				//     U32  options                   4 bytes
				//     I32  chunk_size                4 bytes
				//     I64  number_of_special_evlrs   8 bytes
				//     I64  offset_to_special_evlrs   8 bytes
				//     U16  num_items                 2 bytes
				//        U16 type                2 bytes * num_items
				//        U16 size                2 bytes * num_items
				//        U16 version             2 bytes * num_items
				// which totals 34+6*num_items

				try { streamout.Write(BitConverter.GetBytes(laszip.compressor), 0, 2); }
				catch { error = string.Format("writing compressor {0}", laszip.compressor); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.coder), 0, 2); }
				catch { error = string.Format("writing coder {0}", laszip.coder); return 1; }

				try { streamout.WriteByte(laszip.version_major); }
				catch { error = string.Format("writing version_major {0}", laszip.version_major); return 1; }

				try { streamout.WriteByte(laszip.version_minor); }
				catch { error = string.Format("writing version_minor {0}", laszip.version_minor); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.version_revision), 0, 2); }
				catch { error = string.Format("writing version_revision {0}", laszip.version_revision); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.options), 0, 4); }
				catch { error = string.Format("writing options {0}", laszip.options); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.chunk_size), 0, 4); }
				catch { error = string.Format("writing chunk_size {0}", laszip.chunk_size); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.number_of_special_evlrs), 0, 8); }
				catch { error = string.Format("writing number_of_special_evlrs {0}", laszip.number_of_special_evlrs); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.offset_to_special_evlrs), 0, 8); }
				catch { error = string.Format("writing offset_to_special_evlrs {0}", laszip.offset_to_special_evlrs); return 1; }

				try { streamout.Write(BitConverter.GetBytes(laszip.num_items), 0, 2); }
				catch { error = string.Format("writing num_items {0}", laszip.num_items); return 1; }

				for (uint j = 0; j < laszip.num_items; j++)
				{
					ushort type = (ushort)laszip.items[j].type;
					try { streamout.Write(BitConverter.GetBytes(type), 0, 2); }
					catch { error = string.Format("writing type {0} of item {1}", laszip.items[j].type, j); return 1; }

					try { streamout.Write(BitConverter.GetBytes(laszip.items[j].size), 0, 2); }
					catch { error = string.Format("writing size {0} of item {1}", laszip.items[j].size, j); return 1; }

					try { streamout.Write(BitConverter.GetBytes(laszip.items[j].version), 0, 2); }
					catch { error = string.Format("writing version {0} of item {1}", laszip.items[j].version, j); return 1; }
				}
				#endregion
			}
			#endregion

			#region write any number of user-defined bytes that might have been added after the header
			if (header.user_data_after_header_size != 0)
			{
				try { streamout.Write(header.user_data_after_header, 0, (int)header.user_data_after_header_size); }
				catch { error = string.Format("writing {0} bytes of data into header.user_data_after_header", header.user_data_after_header_size); return 1; }
			}
			#endregion

			#endregion

			#region create the point writer
			try { writer = new LASwritePoint(); }
			catch { error = "could not alloc LASwritePoint"; return 1; }

			if (!writer.setup(laszip.num_items, laszip.items, laszip))
			{
				error = "setup of LASwritePoint failed";
				return 1;
			}

			if (!writer.init(streamout))
			{
				error = "init of LASwritePoint failed";
				return 1;
			}
			#endregion

			// set the point number and point count
			npoints = header.number_of_point_records;
			p_count = 0;

			error = null;
			return 0;
		}

		public int laszip_write_point()
		{
			if (writer == null)
			{
				error = "writing points before writer was opened";
				return 1;
			}

			try
			{
				// write the point
				if (!writer.write(point))
				{
					error = string.Format("writing point with index {0} of {1} total points", p_count, npoints);
					return 1;
				}

				p_count++;
			}
			catch
			{
				error = "internal error in laszip_write_point";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_close_writer()
		{
			if (writer == null)
			{
				error = "closing writer before it was opened";
				return 1;
			}

			try
			{
				if (!writer.done())
				{
					error = "done of LASwritePoint failed";
					return 1;
				}

				writer = null;
				if (!leaveStreamOutOpen) streamout.Close();
				streamout = null;
			}
			catch
			{
				error = "internal error in laszip_writer_close";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_open_reader(Stream streamin, ref bool is_compressed, bool leaveOpen = false)
		{
			if (!streamin.CanRead)
			{
				error = "can not read input stream";
				return 1;
			}

			if (streamin.Length <= 0)
			{
				error = "input stream is empty : nothing to read";
				return 1;
			}

			if (writer != null)
			{
				error = "writer is already open";
				return 1;
			}

			if (reader != null)
			{
				error = "reader is already open";
				return 1;
			}

			this.streamin = streamin;
			leaveStreamInOpen = leaveOpen;

			return laszip_open_reader_stream(ref is_compressed);
		}

		public int laszip_open_reader(string file_name, ref bool is_compressed)
		{
			if (file_name == null || file_name.Length == 0)
			{
				error = "file_name pointer is zero";
				return 1;
			}

			if (writer != null)
			{
				error = "writer is already open";
				return 1;
			}

			if (reader != null)
			{
				error = "reader is already open";
				return 1;
			}

			// open the file
			try
			{
				streamin = File.OpenRead(file_name);
				leaveStreamInOpen = false;
			}
			catch
			{
				error = string.Format("cannot open file '{0}'", file_name);
				return 1;
			}

			return laszip_open_reader_stream(ref is_compressed);
		}

		int laszip_open_reader_stream(ref bool is_compressed)
		{
			try
			{
				byte[] buffer = new byte[32];

				#region read the header variable after variable
				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading header.file_signature";
					return 1;
				}

				if (buffer[0] != 'L' && buffer[1] != 'A' && buffer[2] != 'S' && buffer[3] != 'F')
				{
					error = "wrong file_signature. not a LAS/LAZ file.";
					return 1;
				}

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.file_source_ID";
					return 1;
				}
				header.file_source_ID = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.global_encoding";
					return 1;
				}
				header.global_encoding = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading header.project_ID_GUID_data_1";
					return 1;
				}
				header.project_ID_GUID_data_1 = BitConverter.ToUInt32(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.project_ID_GUID_data_2";
					return 1;
				}
				header.project_ID_GUID_data_2 = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.project_ID_GUID_data_3";
					return 1;
				}
				header.project_ID_GUID_data_3 = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(header.project_ID_GUID_data_4, 0, 8) != 8)
				{
					error = "reading header.project_ID_GUID_data_4";
					return 1;
				}

				if (streamin.Read(buffer, 0, 1) != 1)
				{
					error = "reading header.version_major";
					return 1;
				}
				header.version_major = buffer[0];

				if (streamin.Read(buffer, 0, 1) != 1)
				{
					error = "reading header.version_minor";
					return 1;
				}
				header.version_minor = buffer[0];

				if (streamin.Read(header.system_identifier, 0, 32) != 32)
				{
					error = "reading header.system_identifier";
					return 1;
				}

				if (streamin.Read(header.generating_software, 0, 32) != 32)
				{
					error = "reading header.generating_software";
					return 1;
				}

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.file_creation_day";
					return 1;
				}
				header.file_creation_day = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.file_creation_year";
					return 1;
				}
				header.file_creation_year = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.header_size";
					return 1;
				}
				header.header_size = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading header.offset_to_point_data";
					return 1;
				}
				header.offset_to_point_data = BitConverter.ToUInt32(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading header.number_of_variable_length_records";
					return 1;
				}
				header.number_of_variable_length_records = BitConverter.ToUInt32(buffer, 0);

				if (streamin.Read(buffer, 0, 1) != 1)
				{
					error = "reading header.point_data_format";
					return 1;
				}
				header.point_data_format = buffer[0];

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading header.point_data_record_length";
					return 1;
				}
				header.point_data_record_length = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading header.number_of_point_records";
					return 1;
				}
				header.number_of_point_records = BitConverter.ToUInt32(buffer, 0);

				for (int i = 0; i < 5; i++)
				{
					if (streamin.Read(buffer, 0, 4) != 4)
					{
						error = string.Format("reading header.number_of_points_by_return {0}", i);
						return 1;
					}
					header.number_of_points_by_return[i] = BitConverter.ToUInt32(buffer, 0);
				}

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.x_scale_factor";
					return 1;
				}
				header.x_scale_factor = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.y_scale_factor";
					return 1;
				}
				header.y_scale_factor = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.z_scale_factor";
					return 1;
				}
				header.z_scale_factor = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.x_offset";
					return 1;
				}
				header.x_offset = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.y_offset";
					return 1;
				}
				header.y_offset = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.z_offset";
					return 1;
				}
				header.z_offset = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.max_x";
					return 1;
				}
				header.max_x = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.min_x";
					return 1;
				}
				header.min_x = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.max_y";
					return 1;
				}
				header.max_y = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.min_y";
					return 1;
				}
				header.min_y = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.max_z";
					return 1;
				}
				header.max_z = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading header.min_z";
					return 1;
				}
				header.min_z = BitConverter.ToDouble(buffer, 0);

				// special handling for LAS 1.3
				if ((header.version_major == 1) && (header.version_minor >= 3))
				{
					if (header.header_size < 235)
					{
						error = string.Format("for LAS 1.{0} header_size should at least be 235 but it is only {1}", header.version_minor, header.header_size);
						return 1;
					}
					else
					{
						if (streamin.Read(buffer, 0, 8) != 8)
						{
							error = "reading header.start_of_waveform_data_packet_record";
							return 1;
						}
						header.start_of_waveform_data_packet_record = BitConverter.ToUInt64(buffer, 0);
						header.user_data_in_header_size = (uint)header.header_size - 235;
					}
				}
				else
				{
					header.user_data_in_header_size = (uint)header.header_size - 227;
				}

				// special handling for LAS 1.4
				if ((header.version_major == 1) && (header.version_minor >= 4))
				{
					if (header.header_size < 375)
					{
						error = string.Format("for LAS 1.{0} header_size should at least be 375 but it is only {1}", header.version_minor, header.header_size);
						return 1;
					}
					else
					{
						if (streamin.Read(buffer, 0, 8) != 8)
						{
							error = "reading header.start_of_first_extended_variable_length_record";
							return 1;
						}
						header.start_of_first_extended_variable_length_record = BitConverter.ToUInt64(buffer, 0);

						if (streamin.Read(buffer, 0, 4) != 4)
						{
							error = "reading header.number_of_extended_variable_length_records";
							return 1;
						}
						header.number_of_extended_variable_length_records = BitConverter.ToUInt32(buffer, 0);

						if (streamin.Read(buffer, 0, 8) != 8)
						{
							error = "reading header.extended_number_of_point_records";
							return 1;
						}
						header.extended_number_of_point_records = BitConverter.ToUInt64(buffer, 0);

						for (int i = 0; i < 15; i++)
						{
							if (streamin.Read(buffer, 0, 8) != 8)
							{
								error = string.Format("reading header.extended_number_of_points_by_return[{0}]", i);
								return 1;
							}
							header.extended_number_of_points_by_return[i] = BitConverter.ToUInt64(buffer, 0);
						}
						header.user_data_in_header_size = (uint)header.header_size - 375;
					}
				}

				// load any number of user-defined bytes that might have been added to the header
				if (header.user_data_in_header_size != 0)
				{
					header.user_data_in_header = new byte[header.user_data_in_header_size];

					if (streamin.Read(header.user_data_in_header, 0, (int)header.user_data_in_header_size) != header.user_data_in_header_size)
					{
						error = string.Format("reading {0} bytes of data into header.user_data_in_header", header.user_data_in_header_size);
						return 1;
					}
				}
				#endregion

				#region read variable length records into the header
				uint vlrs_size = 0;
				LASzip laszip = null;

				if (header.number_of_variable_length_records != 0)
				{
					header.vlrs = new List<laszip_vlr>();

					for (int i = 0; i < header.number_of_variable_length_records; i++)
					{
						header.vlrs.Add(new laszip_vlr());

						// make sure there are enough bytes left to read a variable length record before the point block starts
						if (((int)header.offset_to_point_data - vlrs_size - header.header_size) < 54)
						{
							warning = string.Format("only {0} bytes until point block after reading {1} of {2} vlrs. skipping remaining vlrs ...", (int)header.offset_to_point_data - vlrs_size - header.header_size, i, header.number_of_variable_length_records);
							header.number_of_variable_length_records = (uint)i;
							break;
						}

						// read variable length records variable after variable (to avoid alignment issues)
						if (streamin.Read(buffer, 0, 2) != 2)
						{
							error = string.Format("reading header.vlrs[{0}].reserved", i);
							return 1;
						}
						header.vlrs[i].reserved = BitConverter.ToUInt16(buffer, 0);

						if (streamin.Read(header.vlrs[i].user_id, 0, 16) != 16)
						{
							error = string.Format("reading header.vlrs[{0}].user_id", i);
							return 1;
						}

						if (streamin.Read(buffer, 0, 2) != 2)
						{
							error = string.Format("reading header.vlrs[{0}].record_id", i);
							return 1;
						}
						header.vlrs[i].record_id = BitConverter.ToUInt16(buffer, 0);

						if (streamin.Read(buffer, 0, 2) != 2)
						{
							error = string.Format("reading header.vlrs[{0}].record_length_after_header", i);
							return 1;
						}
						header.vlrs[i].record_length_after_header = BitConverter.ToUInt16(buffer, 0);

						if (streamin.Read(header.vlrs[i].description, 0, 32) != 32)
						{
							error = string.Format("reading header.vlrs[{0}].description", i);
							return 1;
						}

						// keep track on the number of bytes we have read so far
						vlrs_size += 54;

						// check variable length record contents
						if (header.vlrs[i].reserved != 0xAABB)
						{
							warning = string.Format("wrong header.vlrs[{0}].reserved: {1} != 0xAABB", i, header.vlrs[i].reserved);
						}

						// make sure there are enough bytes left to read the data of the variable length record before the point block starts
						if (((int)header.offset_to_point_data - vlrs_size - header.header_size) < header.vlrs[i].record_length_after_header)
						{
							warning = string.Format("only {0} bytes until point block when trying to read {1} bytes into header.vlrs[{2}].data", (int)header.offset_to_point_data - vlrs_size - header.header_size, header.vlrs[i].record_length_after_header, i);
							header.vlrs[i].record_length_after_header = (ushort)(header.offset_to_point_data - vlrs_size - header.header_size);
						}

						string userid = "";
						for (int a = 0; a < header.vlrs[i].user_id.Length; a++)
						{
							if (header.vlrs[i].user_id[a] == 0) break;
							userid += (char)header.vlrs[i].user_id[a];
						}

						// load data following the header of the variable length record
						if (header.vlrs[i].record_length_after_header != 0)
						{
							if (userid == "laszip encoded")
							{
								laszip = new LASzip();

								// read the LASzip VLR payload

								//     U16  compressor                2 bytes 
								//     U32  coder                     2 bytes 
								//     U8   version_major             1 byte 
								//     U8   version_minor             1 byte
								//     U16  version_revision          2 bytes
								//     U32  options                   4 bytes 
								//     I32  chunk_size                4 bytes
								//     I64  number_of_special_evlrs   8 bytes
								//     I64  offset_to_special_evlrs   8 bytes
								//     U16  num_items                 2 bytes
								//        U16 type                2 bytes * num_items
								//        U16 size                2 bytes * num_items
								//        U16 version             2 bytes * num_items
								// which totals 34+6*num_items

								if (streamin.Read(buffer, 0, 2) != 2)
								{
									error = "reading compressor";
									return 1;
								}
								laszip.compressor = BitConverter.ToUInt16(buffer, 0);

								if (streamin.Read(buffer, 0, 2) != 2)
								{
									error = "reading coder";
									return 1;
								}
								laszip.coder = BitConverter.ToUInt16(buffer, 0);

								if (streamin.Read(buffer, 0, 1) != 1)
								{
									error = "reading version_major";
									return 1;
								}
								laszip.version_major = buffer[0];

								if (streamin.Read(buffer, 0, 1) != 1)
								{
									error = "reading version_minor";
									return 1;
								}
								laszip.version_minor = buffer[0];

								if (streamin.Read(buffer, 0, 2) != 2)
								{
									error = "reading version_revision";
									return 1;
								}
								laszip.version_revision = BitConverter.ToUInt16(buffer, 0);

								if (streamin.Read(buffer, 0, 4) != 4)
								{
									error = "reading options";
									return 1;
								}
								laszip.options = BitConverter.ToUInt32(buffer, 0);

								if (streamin.Read(buffer, 0, 4) != 4)
								{
									error = "reading chunk_size";
									return 1;
								}
								laszip.chunk_size = BitConverter.ToUInt32(buffer, 0);

								if (streamin.Read(buffer, 0, 8) != 8)
								{
									error = "reading number_of_special_evlrs";
									return 1;
								}
								laszip.number_of_special_evlrs = BitConverter.ToInt64(buffer, 0);

								if (streamin.Read(buffer, 0, 8) != 8)
								{
									error = "reading offset_to_special_evlrs";
									return 1;
								}
								laszip.offset_to_special_evlrs = BitConverter.ToInt64(buffer, 0);

								if (streamin.Read(buffer, 0, 2) != 2)
								{
									error = "reading num_items";
									return 1;
								}
								laszip.num_items = BitConverter.ToUInt16(buffer, 0);

								laszip.items = new LASitem[laszip.num_items];
								for (int j = 0; j < laszip.num_items; j++)
								{
									laszip.items[j] = new LASitem();

									if (streamin.Read(buffer, 0, 2) != 2)
									{
										error = string.Format("reading type of item {0}", j);
										return 1;
									}
									laszip.items[j].type = (LASitem.Type)BitConverter.ToUInt16(buffer, 0);

									if (streamin.Read(buffer, 0, 2) != 2)
									{
										error = string.Format("reading size of item {0}", j);
										return 1;
									}
									laszip.items[j].size = BitConverter.ToUInt16(buffer, 0);

									if (streamin.Read(buffer, 0, 2) != 2)
									{
										error = string.Format("reading version of item {0}", j);
										return 1;
									}
									laszip.items[j].version = BitConverter.ToUInt16(buffer, 0);
								}
							}
							else
							{
								header.vlrs[i].data = new byte[header.vlrs[i].record_length_after_header];
								if (streamin.Read(header.vlrs[i].data, 0, header.vlrs[i].record_length_after_header) != header.vlrs[i].record_length_after_header)
								{
									error = string.Format("reading {0} bytes of data into header.vlrs[{1}].data", header.vlrs[i].record_length_after_header, i);
									return 1;
								}
							}
						}
						else
						{
							header.vlrs[i].data = null;
						}

						// keep track on the number of bytes we have read so far
						vlrs_size += header.vlrs[i].record_length_after_header;

						// special handling for LASzip VLR
						if (userid == "laszip encoded")
						{
							// we take our the VLR for LASzip away
							header.offset_to_point_data -= (uint)(54 + header.vlrs[i].record_length_after_header);
							vlrs_size -= (uint)(54 + header.vlrs[i].record_length_after_header);
							header.vlrs.RemoveAt(i);
							i--;
							header.number_of_variable_length_records--;
						}
					}
				}
				#endregion

				// load any number of user-defined bytes that might have been added after the header
				header.user_data_after_header_size = header.offset_to_point_data - vlrs_size - header.header_size;
				if (header.user_data_after_header_size != 0)
				{
					header.user_data_after_header = new byte[header.user_data_after_header_size];

					if (streamin.Read(header.user_data_after_header, 0, (int)header.user_data_after_header_size) != header.user_data_after_header_size)
					{
						error = string.Format("reading {0} bytes of data into header.user_data_after_header", header.user_data_after_header_size);
						return 1;
					}
				}

				// remove extra bits in point data type
				if ((header.point_data_format & 128) != 0 || (header.point_data_format & 64) != 0)
				{
					if (laszip == null)
					{
						error = "this file was compressed with an experimental version of LASzip. contact 'martin.isenburg@rapidlasso.com' for assistance";
						return 1;
					}
					header.point_data_format &= 127;
				}

				// check if file is compressed
				if (laszip != null)
				{
					// yes. check the compressor state
					is_compressed = true;
					if (!laszip.check())
					{
						error = string.Format("{0} upgrade to the latest release of LAStools (with LASzip) or contact 'martin.isenburg@rapidlasso.com' for assistance", laszip.get_error());
						return 1;
					}
				}
				else
				{
					// no. setup an un-compressed read
					is_compressed = false;
					laszip = new LASzip();
					if (!laszip.setup(header.point_data_format, header.point_data_record_length, LASzip.COMPRESSOR_NONE))
					{
						error = string.Format("invalid combination of point_data_format {0} and point_data_record_length {1}", header.point_data_format, header.point_data_record_length);
						return 1;
					}
				}

				// create point's item pointers
				for (int i = 0; i < laszip.num_items; i++)
				{
					switch (laszip.items[i].type)
					{
						case LASitem.Type.POINT14:
						case LASitem.Type.POINT10:
						case LASitem.Type.GPSTIME11:
						case LASitem.Type.RGBNIR14:
						case LASitem.Type.RGB12:
						case LASitem.Type.WAVEPACKET13:
							break;
						case LASitem.Type.BYTE:
							point.num_extra_bytes = laszip.items[i].size;
							point.extra_bytes = new byte[point.num_extra_bytes];
							break;
						default:
							error = string.Format("unknown LASitem type {0}", laszip.items[i].type);
							return 1;
					}
				}

				// create the point reader
				reader = new LASreadPoint();
				if (!reader.setup(laszip.num_items, laszip.items, laszip))
				{
					error = "setup of LASreadPoint failed";
					return 1;
				}

				if (!reader.init(streamin))
				{
					error = "init of LASreadPoint failed";
					return 1;
				}

				laszip = null;

				// set the point number and point count
				npoints = header.number_of_point_records;
				p_count = 0;
			}
			catch
			{
				error = "internal error in laszip_open_reader";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_seek_point(long index)
		{
			try
			{
				// seek to the point
				if (!reader.seek((uint)p_count, (uint)index))
				{
					error = string.Format("seeking from index {0} to index {1} for file with {2} points", p_count, index, npoints);
					return 1;
				}
				p_count = index;
			}
			catch
			{
				error = "internal error in laszip_seek_point";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_read_point()
		{
			if (reader == null)
			{
				error = "reading points before reader was opened";
				return 1;
			}

			try
			{
				// read the point
				if (!reader.read(point))
				{
					error = string.Format("reading point with index {0} of {1} total points", p_count, npoints);
					return 1;
				}

				p_count++;
			}
			catch
			{
				error = "internal error in laszip_read_point";
				return 1;
			}

			error = null;
			return 0;
		}

		public int laszip_close_reader()
		{
			if (reader == null)
			{
				error = "closing reader before it was opened";
				return 1;
			}

			try
			{
				if (!reader.done())
				{
					error = "done of LASreadPoint failed";
					return 1;
				}

				reader = null;
				if (!leaveStreamInOpen) streamin.Close();
				streamin = null;
			}
			catch
			{
				error = "internal error in laszip_close_reader";
				return 1;
			}

			error = null;
			return 0;
		}
	}
}
