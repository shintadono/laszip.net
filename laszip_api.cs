//===============================================================================
//
//  FILE:  laszip_api.cs
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
//    (c) 2007-2017, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2014-2019 by Shinta <shintadono@googlemail.com>
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

namespace LASzip.Net
{
	public partial class laszip
	{
		public readonly laszip_header header = new laszip_header();
		long p_count = 0;
		long npoints = 0;
		public readonly laszip_point point = new laszip_point();

		Stream streamin = null;
		bool leaveStreamInOpen = false;
		LASreadPoint reader = null;

		Stream streamout = null;
		bool leaveStreamOutOpen = false;
		LASwritePoint writer = null;

		LASattributer attributer = null;

		string error = "";
		string warning = "";

		LASindex lax_index = null;
		double lax_r_min_x = 0.0;
		double lax_r_min_y = 0.0;
		double lax_r_max_x = 0.0;
		double lax_r_max_y = 0.0;
		string lax_file_name = null;
		bool lax_create = false;
		bool lax_append = false;
		bool lax_exploit = false;

		LASZIP_DECOMPRESS_SELECTIVE las14_decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL;
		bool m_preserve_generating_software = false;
		bool m_request_native_extension = true;
		bool m_request_compatibility_mode = false;
		bool m_compatibility_mode = false;

		uint m_set_chunk_size = LASzip.CHUNK_SIZE_DEFAULT;

		int start_scan_angle = 0;
		int start_extended_returns = 0;
		int start_classification = 0;
		int start_flags_and_channel = 0;
		int start_NIR_band = 0;

		Inventory inventory = null;

		public List<laszip_evlr> evlrs = null;

		static unsafe List<LASattribute> ToLASattributeList(byte[] data)
		{
			int count = data.Length / sizeof(LASattribute);

			List<LASattribute> ret = new List<LASattribute>(count);
			fixed (byte* _att = data)
			{
				LASattribute* att = (LASattribute*)_att;
				for (int i = 0; i < count; i++)
				{
					ret.Add(*att);
					att++;
				}
			}

			return ret;
		}

		static unsafe byte[] ToByteArray(List<LASattribute> attributes)
		{
			int bytes = attributes.Count * sizeof(LASattribute);

			try
			{
				byte[] ret = new byte[bytes];
				fixed (byte* _ret = ret)
				{
					LASattribute* att = (LASattribute*)_ret;
					for (int i = 0; i < attributes.Count; i++)
					{
						*att = attributes[i];
						att++;
					}
				}

				return ret;
			}
			catch
			{
				return null;
			}
		}

		static bool strcmp(byte[] a, string b)
		{
			if (a.Length < b.Length) return false;

			for (int i = 0; i < b.Length; i++)
			{
				if (a[i] != b[i]) return false;
				if (a[i] == 0) return true;
			}

			return true;
		}

		static bool strncmp(byte[] a, byte[] b, int num)
		{
			if (a.Length != num || b.Length != num) return false;

			for (int i = 0; i < num; i++)
			{
				if (a[i] != b[i]) return false;
				if (a[i] == 0) return true;
			}

			return true;
		}

		public static int get_version(out byte version_major, out byte version_minor, out ushort version_revision, out uint version_build)
		{
			version_major = LASzip.VERSION_MAJOR;
			version_minor = LASzip.VERSION_MINOR;
			version_revision = LASzip.VERSION_REVISION;
			version_build = LASzip.VERSION_BUILD_DATE;

			return 0;
		}

		public static laszip create()
		{
			laszip ret = new laszip();
			ret.clean();
			return ret;
		}

		public string get_error()
		{
			return error;
		}

		public string get_warning()
		{
			return warning;
		}

		public int clean()
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

				// dealloc and zero everything alloc in the header
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
				header.vlrs.Clear();
				header.user_data_after_header_size = 0;
				header.user_data_after_header = null;

				// dealloc and zero everything alloc in the  point
				point.X = 0;
				point.Y = 0;
				point.Z = 0;
				point.intensity = 0;
				point.flags = 0; // return_number, number_of_returns, scan_direction_flag and edge_of_flight_line
				point.classification_and_classification_flags = 0; // classification, synthetic_flag, keypoint_flag and withheld_flag
				point.scan_angle_rank = 0;
				point.user_data = 0;
				point.point_source_ID = 0;
				point.extended_flags = 0; // extended_point_type, extended_scanner_channel and extended_classification_flags
				point.extended_classification = 0;
				point.extended_returns = 0; // extended_return_number and extended_number_of_returns
				point.extended_scan_angle = 0;
				point.gps_time = 0;
				Array.Clear(point.rgb, 0, 4);
				Array.Clear(point.wave_packet, 0, 29);
				point.num_extra_bytes = 0;
				point.extra_bytes = null;

				// dealloc streamin although close_reader() call should have done this already
				streamin = null;
				leaveStreamInOpen = false;
				reader = null;

				// dealloc streamout although close_writer() call should have done this already
				streamout = null;
				leaveStreamOutOpen = false;
				writer = null;

				// dealloc the attributer
				attributer = null;

				// dealloc lax_index although close_reader() / close_writer() call should have done this already
				lax_index = null;

				// dealloc lax_file_name although close_writer() call should have done this already
				lax_file_name = null;

				// dealloc the inventory although close_writer() call should have done this already
				inventory = null;

				// default everything else of the laszip struct
				p_count = npoints = 0;
				error = warning = "";
				lax_r_min_x = lax_r_min_y = lax_r_max_x = lax_r_max_y = 0.0;
				lax_create = lax_append = lax_exploit = false;
				m_set_chunk_size = LASzip.CHUNK_SIZE_DEFAULT;
				las14_decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.ALL;
				m_request_native_extension = true;
				m_preserve_generating_software = m_request_compatibility_mode = m_compatibility_mode = false;
				start_scan_angle = start_extended_returns = start_classification = start_flags_and_channel = start_NIR_band = 0;

				// create default header
				header.setDefault();
			}
			catch
			{
				error = "internal error in laszip_clean";
				return 1;
			}

			return 0;
		}

		public laszip_header get_header_pointer()
		{
			error = warning = "";
			return header;
		}

		public laszip_point get_point_pointer()
		{
			error = warning = "";
			return point;
		}

		public int get_point_count(out long count)
		{
			count = 0;
			if (reader == null && writer == null)
			{
				error = "getting count before reader or writer was opened";
				return 1;
			}

			count = p_count;

			error = warning = "";
			return 0;
		}

		public int get_number_of_point(out long npoints)
		{
			npoints = 0;
			if (reader == null && writer == null)
			{
				error = "getting count before reader or writer was opened";
				return 1;
			}

			npoints = this.npoints;

			error = warning = "";
			return 0;
		}

		public int set_header(laszip_header header)
		{
			if (header == null)
			{
				error = "laszip_header_struct pointer 'header' is zero";
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
				// dealloc the attributer (if needed)
				attributer = null;

				// populate the header
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
					if (header.user_data_in_header == null)
					{
						error = string.Format("header->user_data_in_header_size is {0} but header->user_data_in_header is NULL", header.user_data_in_header_size);
						return 1;
					}

					this.header.user_data_in_header = new byte[header.user_data_in_header_size];
					Array.Copy(header.user_data_in_header, this.header.user_data_in_header, header.user_data_in_header_size);
				}

				this.header.vlrs = null;
				if (header.number_of_variable_length_records != 0)
				{
					this.header.vlrs = new List<laszip_vlr>((int)header.number_of_variable_length_records);
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
							if (header.vlrs[i].data == null)
							{
								error = string.Format("header->vlrs[{0}].record_length_after_header is {1} but header->vlrs[{2}].data is NULL", i, header.vlrs[i].record_length_after_header, i);
								return 1;
							}
							this.header.vlrs[i].data = new byte[header.vlrs[i].record_length_after_header];
							Array.Copy(header.vlrs[i].data, this.header.vlrs[i].data, header.vlrs[i].record_length_after_header);
						}
						else
						{
							this.header.vlrs[i].data = null;
						}

						// populate the attributer if needed
						if (strcmp(header.vlrs[i].user_id, "LASF_Spec") && header.vlrs[i].record_id == 4)
						{
							if (attributer == null)
							{
								try
								{
									attributer = new LASattributer();
								}
								catch
								{
									error = "cannot allocate LASattributer";
									return 1;
								}
							}
							attributer.init_attributes(ToLASattributeList(header.vlrs[i].data));
						}
					}
				}

				this.header.user_data_after_header_size = header.user_data_after_header_size;
				this.header.user_data_after_header = null;
				if (header.user_data_after_header_size != 0)
				{
					if (header.user_data_after_header == null)
					{
						error = string.Format("header->user_data_after_header_size is {0} but header->user_data_after_header is NULL", header.user_data_after_header_size);
						return 1;
					}
					this.header.user_data_after_header = new byte[header.user_data_after_header_size];
					Array.Copy(header.user_data_after_header, this.header.user_data_after_header, header.user_data_after_header_size);
				}
			}
			catch
			{
				error = "internal error in laszip_set_header";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int set_point_type_and_size(byte point_type, ushort point_size)
		{
			try
			{
				if (reader != null)
				{
					error = "cannot set point format and point size after reader was opened";
					return 1;
				}

				if (writer != null)
				{
					error = "cannot set point format and point size after writer was opened";
					return 1;
				}

				// check if point type and type are supported
				if (!new LASzip().setup(point_type, point_size, LASzip.COMPRESSOR_NONE))
				{
					error = string.Format("invalid combination of point_type {0} and point_size {1}", point_type, point_size);
					return 1;
				}

				// set point type and point size
				header.point_data_format = point_type;
				header.point_data_record_length = point_size;
			}
			catch
			{
				error = "internal error in laszip_set_point_type_and_size";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int check_for_integer_overflow()
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

			error = warning = "";
			return 0;
		}

		public int auto_offset()
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

				if (check_for_integer_overflow() != 0)
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

			error = warning = "";
			return 0;
		}

		public int set_point(laszip_point point)
		{
			if (point == null)
			{
				error = "laszip_point_struct pointer 'point' is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot set point for reader";
				return 1;
			}

			try
			{
				this.point.classification_and_classification_flags = point.classification_and_classification_flags;
				this.point.edge_of_flight_line = point.edge_of_flight_line;
				this.point.extended_classification = point.extended_classification;
				this.point.extended_classification_flags = point.extended_classification_flags;
				this.point.extended_number_of_returns = point.extended_number_of_returns;
				this.point.extended_point_type = point.extended_point_type;
				this.point.extended_return_number = point.extended_return_number;
				this.point.extended_scan_angle = point.extended_scan_angle;
				this.point.extended_scanner_channel = point.extended_scanner_channel;
				this.point.gps_time = point.gps_time;
				this.point.intensity = point.intensity;
				this.point.num_extra_bytes = point.num_extra_bytes;
				this.point.number_of_returns = point.number_of_returns;
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
					else if (!m_compatibility_mode)
					{
						error = "target point has extra bytes but source point does not";
						return 1;
					}
				}
				//else
				//{
				//	if (point.extra_bytes != null)
				//	{
				//		error = "source point has extra bytes but target point does not";
				//		return 1;
				//	}
				//}
			}
			catch
			{
				error = "internal error in laszip_set_point";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int set_coordinates(double[] coordinates)
		{
			if (coordinates == null)
			{
				error = "laszip_F64 pointer 'coordinates' is zero";
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

			error = warning = "";
			return 0;
		}

		public int get_coordinates(double[] coordinates)
		{
			if (coordinates == null)
			{
				error = "laszip_F64 pointer 'coordinates' is zero";
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

			error = warning = "";
			return 0;
		}

		public unsafe int set_geokeys(ushort number, laszip_geokey[] key_entries)
		{
			if (number == 0)
			{
				error = "number of key_entries is zero";
				return 1;
			}

			if (key_entries == null)
			{
				error = "laszip_geokey_struct pointer 'key_entries' is zero";
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
				vlr.reserved = 0;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 34735;
				vlr.record_length_after_header = (ushort)(8 + number * 8);
				vlr.description[0] = 0; // add_vlr will fill the description.
				vlr.data = buffer;

				// add the VLR
				if (add_vlr(vlr) != 0)
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

			error = warning = "";
			return 0;
		}

		public int set_geodouble_params(ushort number, double[] geodouble_params)
		{
			if (number == 0)
			{
				error = "number of geodouble_params is zero";
				return 1;
			}

			if (geodouble_params == null)
			{
				error = "laszip_F64 pointer 'geodouble_params' is zero";
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
				vlr.reserved = 0;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 34736;
				vlr.record_length_after_header = (ushort)(number * 8);
				vlr.description[0] = 0; // add_vlr will fill the description.

				byte[] buffer = new byte[number * 8];
				Buffer.BlockCopy(geodouble_params, 0, buffer, 0, number * 8);
				vlr.data = buffer;

				// add the VLR
				if (add_vlr(vlr) != 0)
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

			error = warning = "";
			return 0;
		}

		public int set_geoascii_params(ushort number, byte[] geoascii_params)
		{
			if (number == 0)
			{
				error = "number of geoascii_params is zero";
				return 1;
			}

			if (geoascii_params == null)
			{
				error = "laszip_CHAR pointer 'geoascii_params' is zero";
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
				vlr.reserved = 0;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 34737;
				vlr.record_length_after_header = number;
				vlr.description[0] = 0; // add_vlr will fill the description.
				vlr.data = geoascii_params;

				// add the VLR
				if (add_vlr(vlr) != 0)
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

			error = warning = "";
			return 0;
		}

		public int add_attribute(LAS_ATTRIBUTE type, string name, string description, double scale, double offset)
		{
			if (type > LAS_ATTRIBUTE.F64)
			{
				error = string.Format("laszip_U32 'type' is {0} but needs to be between {1} and {2}", type, LAS_ATTRIBUTE.U8, LAS_ATTRIBUTE.F64);
				return 1;
			}

			if (string.IsNullOrEmpty(name))
			{
				error = "laszip_CHAR pointer 'name' is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot add attribute after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot add attribute after writer was opened";
				return 1;
			}

			try
			{
				LASattribute lasattribute = new LASattribute(type, name, description);
				lasattribute.set_scale(scale);
				lasattribute.set_offset(offset);

				if (attributer == null)
				{
					try
					{
						attributer = new LASattributer();
					}
					catch
					{
						error = "cannot allocate LASattributer";
						return 1;
					}
				}

				if (attributer.add_attribute(lasattribute) == -1)
				{
					error = string.Format("cannot add attribute '{0}' to attributer", name);
					return 1;
				}

				// fill a VLR
				laszip_vlr vlr = new laszip_vlr();
				vlr.reserved = 0;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Spec");
				Array.Copy(user_id, vlr.user_id, Math.Min(user_id.Length, 16));
				vlr.record_id = 4;
				unsafe { vlr.record_length_after_header = (ushort)(attributer.number_attributes * sizeof(LASattribute)); }
				vlr.description[0] = 0; // add_vlr will fill the description.
				vlr.data = ToByteArray(attributer.attributes);

				// add the VLR
				if (add_vlr(vlr) != 0)
				{
					error = string.Format("adding the new extra bytes VLR with the additional attribute '{0}'", name);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_add_attribute";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int add_vlr(laszip_vlr vlr)
		{
			if (vlr == null)
			{
				error = "laszip_vlr_struct pointer 'vlr' is zero";
				return 1;
			}

			if (vlr.record_length_after_header > 0 && vlr.data == null)
			{
				error = string.Format("VLR record_length_after_header is {0} but VLR data pointer is zero", vlr.record_length_after_header);
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
					// remove existing VLRs with same record and user id
					for (int i = (int)header.number_of_variable_length_records - 1; i >= 0; i--)
					{
						if (header.vlrs[i].record_id == vlr.record_id && strncmp(header.vlrs[i].user_id, vlr.user_id, 16))
						{
							if (header.vlrs[i].record_length_after_header != 0)
								header.offset_to_point_data -= header.vlrs[i].record_length_after_header;

							header.offset_to_point_data -= 54;
							header.vlrs.RemoveAt(i);
						}
					}
				}

				if (vlr.description[0] == 0)
				{
					// description field must be a null-terminate string, so we don't copy more than 31 characters
					byte[] v = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
					Array.Copy(v, vlr.description, Math.Min(v.Length, 31));
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

			error = warning = "";
			return 0;
		}

		public int remove_vlr(byte[] user_id, ushort record_id)
		{
			if (user_id == null)
			{
				error = "laszip_CHAR pointer 'user_id' is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot remove vlr after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot remove vlr after writer was opened";
				return 1;
			}

			try
			{
				if (header.number_of_variable_length_records != 0)
				{
					bool found = false;
					for (int i = (int)header.number_of_variable_length_records - 1; i >= 0; i--)
					{
						if (header.vlrs[i].record_id == record_id && strncmp(header.vlrs[i].user_id, user_id, 16))
						{
							found = true;

							if (header.vlrs[i].record_length_after_header != 0)
								header.offset_to_point_data -= header.vlrs[i].record_length_after_header;

							header.offset_to_point_data -= 54;
							header.vlrs.RemoveAt(i);
						}
					}

					if (!found)
					{
						error = string.Format("cannot find VLR with user_id '{0}' and record_id {1} among the {2} VLRs in the header", Encoding.ASCII.GetString(user_id), record_id, header.number_of_variable_length_records);
						return 1;
					}
				}
				else
				{
					error = string.Format("cannot remove VLR with user_id '{0}' and record_id {1} because header has no VLRs", Encoding.ASCII.GetString(user_id), record_id);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_add_vlr";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int remove_vlr(string user_id, ushort record_id)
		{
			if (string.IsNullOrEmpty(user_id))
			{
				error = "laszip_CHAR pointer 'user_id' is zero";
				return 1;
			}

			if (reader != null)
			{
				error = "cannot remove vlr after reader was opened";
				return 1;
			}

			if (writer != null)
			{
				error = "cannot remove vlr after writer was opened";
				return 1;
			}

			try
			{
				if (header.number_of_variable_length_records != 0)
				{
					bool found = false;
					for (int i = (int)header.number_of_variable_length_records - 1; i >= 0; i--)
					{
						if (header.vlrs[i].record_id == record_id && strcmp(header.vlrs[i].user_id, user_id))
						{
							found = true;
							if (header.vlrs[i].record_length_after_header != 0)
								header.offset_to_point_data -= header.vlrs[i].record_length_after_header;

							header.offset_to_point_data -= 54;
							header.vlrs.RemoveAt(i);
						}
					}

					if (!found)
					{
						error = string.Format("cannot find VLR with user_id '{0}' and record_id {1} among the {2} VLRs in the header", user_id, record_id, header.number_of_variable_length_records);
						return 1;
					}
				}
				else
				{
					error = string.Format("cannot remove VLR with user_id '{0}' and record_id {1} because header has no VLRs", user_id, record_id);
					return 1;
				}
			}
			catch
			{
				error = "internal error in laszip_add_vlr";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int create_spatial_index(bool create, bool append)
		{
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

			if (append)
			{
				error = "appending of spatial index not (yet) supported in this version";
				return 1;
			}

			lax_create = create;
			lax_append = append;

			error = warning = "";
			return 0;
		}

		public int preserve_generating_software(bool preserve)
		{
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

			m_preserve_generating_software = preserve;

			error = warning = "";
			return 0;
		}

		public int request_native_extension(bool request)
		{
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

			m_request_native_extension = request;

			error = warning = "";
			return 0;
		}

		public int request_compatibility_mode(bool request)
		{
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

			m_request_compatibility_mode = request;

			if (request) // only one should be on
			{
				m_request_native_extension = false;
			}

			error = warning = "";
			return 0;
		}

		public int set_chunk_size(uint chunk_size)
		{
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

			m_set_chunk_size = chunk_size;

			error = warning = "";
			return 0;
		}

		int prepare_header_for_write()
		{
			if ((header.version_major != 1) || (header.version_minor > 4))
			{
				error = string.Format("unknown LAS version {0}.{1}", header.version_major, header.version_minor);
				return 1;
			}

			// check counters
			if (header.point_data_format > 5)
			{
				// legacy counters are zero for new point types

				header.number_of_point_records = 0;
				for (int i = 0; i < 5; i++)
				{
					header.number_of_points_by_return[i] = 0;
				}
			}
			else if (header.version_minor > 3)
			{
				// legacy counters must be zero or consistent for old point types
				if (header.number_of_point_records != header.extended_number_of_point_records)
				{
					if (header.number_of_point_records != 0)
					{
						error = string.Format("inconsistent number_of_point_records {0} and extended_number_of_point_records {1}", header.number_of_point_records, header.extended_number_of_point_records);
						return 1;
					}
					else if (header.extended_number_of_point_records <= uint.MaxValue)
					{
						header.number_of_point_records = (uint)header.extended_number_of_point_records;
					}
				}

				for (int i = 0; i < 5; i++)
				{
					if (header.number_of_points_by_return[i] != header.extended_number_of_points_by_return[i])
					{
						if (header.number_of_points_by_return[i] != 0)
						{
							error = string.Format("inconsistent number_of_points_by_return[{0}] {1} and extended_number_of_points_by_return[{0}] {2}", i, header.number_of_points_by_return[i], header.extended_number_of_points_by_return[i]);
							return 1;
						}
						else if (header.extended_number_of_points_by_return[i] <= uint.MaxValue)
						{
							header.number_of_points_by_return[i] = (uint)header.extended_number_of_points_by_return[i];
						}
					}
				}
			}

			return 0;
		}

		unsafe int prepare_point_for_write(bool compress)
		{
			if (header.point_data_format > 5)
			{
				// must be set for the new point types 6 or higher ...
				point.extended_point_type = 1;

				if (m_request_native_extension)
				{
					// we are *not* operating in compatibility mode
					m_compatibility_mode = false;
				}
				else if (m_request_compatibility_mode)
				{
					// we are *not* using the native extension
					m_request_native_extension = false;

					// make sure there are no more than U32_MAX points
					if (header.extended_number_of_point_records > uint.MaxValue)
					{
						error = string.Format("extended_number_of_point_records of {0} is too much for 32-bit counters of compatibility mode", header.extended_number_of_point_records);
						return 1;
					}

					// copy 64-bit extended counters back into 32-bit legacy counters
					header.number_of_point_records = (uint)header.extended_number_of_point_records;
					for (int i = 0; i < 5; i++)
					{
						header.number_of_points_by_return[i] = (uint)header.extended_number_of_points_by_return[i];
					}

					// are there any "extra bytes" already ... ?
					int number_of_existing_extrabytes = 0;

					switch (header.point_data_format)
					{
						case 6: number_of_existing_extrabytes = header.point_data_record_length - 30; break;
						case 7: number_of_existing_extrabytes = header.point_data_record_length - 36; break;
						case 8: number_of_existing_extrabytes = header.point_data_record_length - 38; break;
						case 9: number_of_existing_extrabytes = header.point_data_record_length - 59; break;
						case 10: number_of_existing_extrabytes = header.point_data_record_length - 67; break;
						default: error = string.Format("unknown point_data_format {0}", header.point_data_format); return 1;
					}

					if (number_of_existing_extrabytes < 0)
					{
						error = string.Format("bad point_data_format {0} point_data_record_length {1} combination", header.point_data_format, header.point_data_record_length);
						return 1;
					}

					// downgrade to LAS 1.2 or LAS 1.3
					if (header.point_data_format <= 8)
					{
						header.version_minor = 2;
						// LAS 1.2 header is 148 bytes less than LAS 1.4+ header
						header.header_size -= 148;
						header.offset_to_point_data -= 148;
					}
					else
					{
						header.version_minor = 3;
						// LAS 1.3 header is 140 bytes less than LAS 1.4+ header
						header.header_size -= 140;
						header.offset_to_point_data -= 140;
					}

					// turn off the bit indicating the presence of the OGC WKT
					header.global_encoding &= 0xFFEF; // ~(1 << 4)

					// old point type is two bytes shorter
					header.point_data_record_length -= 2;
					// but we add 5 bytes of attributes
					header.point_data_record_length += 5;

					// create 2+2+4+148 bytes payload for compatibility VLR
					MemoryStream outStream = new MemoryStream();

					// write control info
					ushort laszip_version = unchecked((ushort)LASzip.VERSION_BUILD_DATE);
					outStream.Write(BitConverter.GetBytes(laszip_version), 0, 2);

					ushort compatible_version = 3;
					outStream.Write(BitConverter.GetBytes(compatible_version), 0, 2);
					uint unused = 0;
					outStream.Write(BitConverter.GetBytes(unused), 0, 4);

					// write the 148 bytes of the extended LAS 1.4 header
					ulong start_of_waveform_data_packet_record = header.start_of_waveform_data_packet_record;
					if (start_of_waveform_data_packet_record != 0)
					{
						Console.Error.WriteLine("WARNING: header->start_of_waveform_data_packet_record is {0}. writing 0 instead.", start_of_waveform_data_packet_record);
						start_of_waveform_data_packet_record = 0;
					}
					outStream.Write(BitConverter.GetBytes(start_of_waveform_data_packet_record), 0, 8);

					ulong start_of_first_extended_variable_length_record = header.start_of_first_extended_variable_length_record;
					if (start_of_first_extended_variable_length_record != 0)
					{
						Console.Error.WriteLine("WARNING: EVLRs not supported. header->start_of_first_extended_variable_length_record is {0}. writing 0 instead.", start_of_first_extended_variable_length_record);
						start_of_first_extended_variable_length_record = 0;
					}
					outStream.Write(BitConverter.GetBytes(start_of_first_extended_variable_length_record), 0, 8);

					uint number_of_extended_variable_length_records = header.number_of_extended_variable_length_records;
					if (number_of_extended_variable_length_records != 0)
					{
						Console.Error.WriteLine("WARNING: EVLRs not supported. header->number_of_extended_variable_length_records is {0}. writing 0 instead.", number_of_extended_variable_length_records);
						number_of_extended_variable_length_records = 0;
					}
					outStream.Write(BitConverter.GetBytes(number_of_extended_variable_length_records), 0, 4);

					ulong extended_number_of_point_records;
					if (header.number_of_point_records != 0)
						extended_number_of_point_records = header.number_of_point_records;
					else
						extended_number_of_point_records = header.extended_number_of_point_records;
					outStream.Write(BitConverter.GetBytes(extended_number_of_point_records), 0, 8);

					ulong extended_number_of_points_by_return;
					for (int i = 0; i < 15; i++)
					{
						if (i < 5 && header.number_of_points_by_return[i] != 0)
							extended_number_of_points_by_return = header.number_of_points_by_return[i];
						else
							extended_number_of_points_by_return = header.extended_number_of_points_by_return[i];
						outStream.Write(BitConverter.GetBytes(extended_number_of_points_by_return), 0, 8);
					}

					// add the compatibility VLR
					byte[] user_id = Encoding.ASCII.GetBytes("lascompatible");
					laszip_vlr lascompatible = new laszip_vlr() { reserved = 0, record_id = 22204, record_length_after_header = 2 + 2 + 4 + 148, data = outStream.ToArray() };
					Array.Copy(user_id, lascompatible.user_id, Math.Min(user_id.Length, 16));

					if (add_vlr(lascompatible) != 0)
					{
						error = "adding the compatibility VLR";
						return 1;
					}
					outStream.Close();

					// if needed create an attributer to describe the "extra bytes"
					if (attributer == null)
					{
						try
						{
							attributer = new LASattributer();
						}
						catch
						{
							error = "cannot allocate LASattributer";
							return 1;
						}
					}

					// were there any pre-existing extra bytes
					if (number_of_existing_extrabytes > 0)
					{
						// make sure the existing "extra bytes" are documented
						if (attributer.get_attributes_size() > number_of_existing_extrabytes)
						{
							error = string.Format("bad \"extra bytes\" VLR describes {0} bytes more than points actually have", attributer.get_attributes_size() - number_of_existing_extrabytes);
							return 1;
						}
						else if (attributer.get_attributes_size() < number_of_existing_extrabytes)
						{
							// maybe the existing "extra bytes" are documented in a VLR
							if (header.vlrs != null)
							{
								for (int i = 0; i < header.number_of_variable_length_records; i++)
								{
									if (strcmp(header.vlrs[i].user_id, "LASF_Spec") && header.vlrs[i].record_id == 4)
									{

										attributer.init_attributes(ToLASattributeList(header.vlrs[i].data));
									}
								}
							}

							// describe any undocumented "extra bytes" as "unknown" U8 attributes
							for (int i = attributer.get_attributes_size(); i < number_of_existing_extrabytes; i++)
							{
								string unknown_name = string.Format("unknown {0}", i);
								LASattribute lasattribute_unknown = new LASattribute(LAS_ATTRIBUTE.U8, unknown_name, unknown_name);
								if (attributer.add_attribute(lasattribute_unknown) == -1)
								{
									error = string.Format("cannot add unknown U8 attribute '{0}' of {1} to attributer", unknown_name, number_of_existing_extrabytes);
									return 1;
								}
							}
						}
					}

					// create the "extra bytes" that store the newer LAS 1.4 point attributes

					// scan_angle (difference or remainder) is stored as a I16
					LASattribute lasattribute_scan_angle = new LASattribute(LAS_ATTRIBUTE.I16, "LAS 1.4 scan angle", "additional attributes");
					lasattribute_scan_angle.set_scale(0.006);
					int index_scan_angle = attributer.add_attribute(lasattribute_scan_angle);
					start_scan_angle = attributer.get_attribute_start(index_scan_angle);
					// extended returns stored as a U8
					LASattribute lasattribute_extended_returns = new LASattribute(LAS_ATTRIBUTE.U8, "LAS 1.4 extended returns", "additional attributes");
					int index_extended_returns = attributer.add_attribute(lasattribute_extended_returns);
					start_extended_returns = attributer.get_attribute_start(index_extended_returns);
					// classification stored as a U8
					LASattribute lasattribute_classification = new LASattribute(LAS_ATTRIBUTE.U8, "LAS 1.4 classification", "additional attributes");
					int index_classification = attributer.add_attribute(lasattribute_classification);
					start_classification = attributer.get_attribute_start(index_classification);
					// flags and channel stored as a U8
					LASattribute lasattribute_flags_and_channel = new LASattribute(LAS_ATTRIBUTE.U8, "LAS 1.4 flags and channel", "additional attributes");
					int index_flags_and_channel = attributer.add_attribute(lasattribute_flags_and_channel);
					start_flags_and_channel = attributer.get_attribute_start(index_flags_and_channel);
					// maybe store the NIR band as a U16
					if (header.point_data_format == 8 || header.point_data_format == 10)
					{
						// the NIR band is stored as a U16
						LASattribute lasattribute_NIR_band = new LASattribute(LAS_ATTRIBUTE.U16, "LAS 1.4 NIR band", "additional attributes");
						int index_NIR_band = attributer.add_attribute(lasattribute_NIR_band);
						start_NIR_band = attributer.get_attribute_start(index_NIR_band);
					}
					else
					{
						start_NIR_band = -1;
					}

					// add the extra bytes VLR with the additional attributes
					user_id = Encoding.ASCII.GetBytes("LASF_Spec");
					laszip_vlr attributes = new laszip_vlr() { reserved = 0, record_id = 4, record_length_after_header = (ushort)(attributer.number_attributes * sizeof(LASattribute)), data = ToByteArray(attributer.attributes) };
					Array.Copy(user_id, attributes.user_id, Math.Min(user_id.Length, 16));

					if (add_vlr(attributes) != 0)
					{
						error = "adding the extra bytes VLR with the additional attributes";
						return 1;
					}

					// update point type
					if (header.point_data_format == 6)
					{
						header.point_data_format = 1;
					}
					else if (header.point_data_format <= 8)
					{
						header.point_data_format = 3;
					}
					else // 9->4 and 10->5
					{
						header.point_data_format -= 5;
					}

					// we are operating in compatibility mode
					m_compatibility_mode = true;
				}
				else if (compress)
				{
					error = string.Format("LASzip DLL {0}.{1} r{2} ({3}) cannot compress point data format {4} without requesting 'compatibility mode'",
						LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE, header.point_data_format);
					return 1;
				}
			}
			else
			{
				// must *not* be set for the old point type 5 or lower
				point.extended_point_type = 0;

				// we are *not* operating in compatibility mode
				m_compatibility_mode = false;
			}

			return 0;
		}

		int prepare_vlrs_for_write()
		{
			uint vlrs_size = 0;

			if (header.number_of_variable_length_records != 0)
			{
				if (header.vlrs == null || header.vlrs.Count == 0)
				{
					error = string.Format("number_of_variable_length_records is {0} but vlrs pointer is zero", header.number_of_variable_length_records);
					return 1;
				}

				for (int i = 0; i < header.number_of_variable_length_records; i++)
				{
					vlrs_size += 54;
					if (header.vlrs[i].record_length_after_header != 0)
					{
						if (header.vlrs[i] == null)
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
				error = string.Format("header_size ({0}) plus vlrs_size ({1}) plus user_data_after_header_size ({2}) does not equal offset_to_point_data ({3})",
					header.header_size, vlrs_size, header.user_data_after_header_size, header.offset_to_point_data);
				return 1;
			}

			return 0;
		}

		static uint vrl_payload_size(LASzip laszip)
		{
			return 34u + (6u * laszip.num_items);
		}

		int write_laszip_vlr_header(LASzip laszip, Stream outStream)
		{
			// write the LASzip VLR header
			ushort reserved = 0;
			try { outStream.Write(BitConverter.GetBytes(reserved), 0, 2); }
			catch { error = "writing LASzip VLR header.reserved"; return 1; }

			byte[] user_id1 = Encoding.ASCII.GetBytes("laszip encoded");
			byte[] user_id = new byte[16];
			Array.Copy(user_id1, user_id, Math.Min(16, user_id1.Length));
			try { outStream.Write(user_id, 0, 16); }
			catch { error = "writing LASzip VLR header.user_id"; return 1; }

			ushort record_id = 22204;
			try { outStream.Write(BitConverter.GetBytes(record_id), 0, 2); }
			catch { error = "writing LASzip VLR header.record_id"; return 1; }

			ushort record_length_after_header = (ushort)vrl_payload_size(laszip);
			try { outStream.Write(BitConverter.GetBytes(record_length_after_header), 0, 2); }
			catch { error = "writing LASzip VLR header.record_length_after_header"; return 1; }

			// description field must be a null-terminate string, so we don't copy more than 31 characters
			byte[] description1 = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
			byte[] description = new byte[32];
			Array.Copy(description1, description, Math.Min(31, description1.Length));

			try { outStream.Write(description, 0, 32); }
			catch { error = "writing LASzip VLR header.description"; return 1; }

			return 0;
		}

		int write_laszip_vlr_payload(LASzip laszip, Stream outStream)
		{
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

			try { outStream.Write(BitConverter.GetBytes(laszip.compressor), 0, 2); }
			catch { error = string.Format("writing compressor {0}", laszip.compressor); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.coder), 0, 2); }
			catch { error = string.Format("writing coder {0}", laszip.coder); return 1; }

			try { outStream.WriteByte(laszip.version_major); }
			catch { error = string.Format("writing version_major {0}", laszip.version_major); return 1; }

			try { outStream.WriteByte(laszip.version_minor); }
			catch { error = string.Format("writing version_minor {0}", laszip.version_minor); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.version_revision), 0, 2); }
			catch { error = string.Format("writing version_revision {0}", laszip.version_revision); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.options), 0, 4); }
			catch { error = string.Format("writing options {0}", laszip.options); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.chunk_size), 0, 4); }
			catch { error = string.Format("writing chunk_size {0}", laszip.chunk_size); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.number_of_special_evlrs), 0, 8); }
			catch { error = string.Format("writing number_of_special_evlrs {0}", laszip.number_of_special_evlrs); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.offset_to_special_evlrs), 0, 8); }
			catch { error = string.Format("writing offset_to_special_evlrs {0}", laszip.offset_to_special_evlrs); return 1; }

			try { outStream.Write(BitConverter.GetBytes(laszip.num_items), 0, 2); }
			catch { error = string.Format("writing num_items {0}", laszip.num_items); return 1; }

			for (uint j = 0; j < laszip.num_items; j++)
			{
				ushort type = (ushort)laszip.items[j].type;
				try { outStream.Write(BitConverter.GetBytes(type), 0, 2); }
				catch { error = string.Format("writing type {0} of item {1}", laszip.items[j].type, j); return 1; }

				try { outStream.Write(BitConverter.GetBytes(laszip.items[j].size), 0, 2); }
				catch { error = string.Format("writing size {0} of item {1}", laszip.items[j].size, j); return 1; }

				try { outStream.Write(BitConverter.GetBytes(laszip.items[j].version), 0, 2); }
				catch { error = string.Format("writing version {0} of item {1}", laszip.items[j].version, j); return 1; }
			}

			return 0;
		}

		int write_header(LASzip laszip, bool compress)
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

			if (!m_preserve_generating_software)
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

			if (compress) header.offset_to_point_data += 54 + vrl_payload_size(laszip);

			try { streamout.Write(BitConverter.GetBytes(header.offset_to_point_data), 0, 4); }
			catch { error = "writing header.offset_to_point_data"; return 1; }

			if (compress)
			{
				header.offset_to_point_data -= 54 + vrl_payload_size(laszip);
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

				if (header.start_of_waveform_data_packet_record != 0)
				{
					warning = string.Format("header.start_of_waveform_data_packet_record is {0}. writing 0 instead.", header.start_of_waveform_data_packet_record);
					header.start_of_waveform_data_packet_record = 0;
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
				// write the LASzip VLR header
				if (write_laszip_vlr_header(laszip, streamout) != 0) return 1;

				// write the LASzip VLR payload
				if (write_laszip_vlr_payload(laszip, streamout) != 0) return 1;
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

			return 0;
		}

		int create_point_writer(LASzip laszip)
		{
			// create the point writer
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

			return 0;
		}

		int setup_laszip_items(LASzip laszip, bool compress)
		{
			byte point_type = header.point_data_format;
			ushort point_size = header.point_data_record_length;

			if (compress && point_type > 5 && m_request_compatibility_mode)
			{
				if (!laszip.request_compatibility_mode(1))
				{
					error = "requesting 'compatibility mode' has failed";
					return 1;
				}
			}

			// create point items in the LASzip structure from point format and size
			if (!laszip.setup(point_type, point_size, LASzip.COMPRESSOR_NONE))
			{
				error = string.Format("invalid combination of point_type {0} and point_size {1}", point_type, point_size);
				return 1;
			}

			// compute offsets (or points item pointers) for data transfer from the point items
			for (int i = 0; i < laszip.num_items; i++)
			{
				switch (laszip.items[i].type)
				{
					case LASitem.Type.POINT10:
					case LASitem.Type.POINT14:
					case LASitem.Type.GPSTIME11:
					case LASitem.Type.RGB12:
					case LASitem.Type.RGB14:
					case LASitem.Type.RGBNIR14:
					case LASitem.Type.WAVEPACKET13:
					case LASitem.Type.WAVEPACKET14: break;
					case LASitem.Type.BYTE:
					case LASitem.Type.BYTE14:
						point.num_extra_bytes = laszip.items[i].size;
						point.extra_bytes = new byte[point.num_extra_bytes];
						break;
					default:
						error = string.Format("unknown LASitem type {0}", laszip.items[i].type);
						return 1;
				}
			}

			if (compress)
			{
				if (point_type > 5 && m_request_native_extension)
				{
					if (!laszip.setup(point_type, point_size, LASzip.COMPRESSOR_LAYERED_CHUNKED))
					{
						error = string.Format("cannot compress point_type {0} with point_size {1} using native", point_type, point_size);
						return 1;
					}
				}
				else
				{
					if (!laszip.setup(point_type, point_size, LASzip.COMPRESSOR_DEFAULT))
					{
						error = string.Format("cannot compress point_type {0} with point_size {1}", point_type, point_size);
						return 1;
					}
				}

				// request version (old point types only, new point types always use version 3)
				laszip.request_version(2);

				// maybe we should change the chunk size
				if (m_set_chunk_size != LASzip.CHUNK_SIZE_DEFAULT)
				{
					if (!laszip.set_chunk_size(m_set_chunk_size))
					{
						error = string.Format("setting chunk size {0} has failed", m_set_chunk_size);
						return 1;
					}
				}
			}
			else
			{
				laszip.request_version(0);
			}

			return 0;
		}

		// The stream writer also supports software that writes the LAS header on its
		// own simply by setting the BOOL 'do_not_write_header' to TRUE. This function
		// should then be called just prior to writing points as data is then written
		// to the current stream position
		public int open_writer_stream(Stream streamout, bool compress, bool do_not_write_header, bool leaveOpen = false)
		{
			if (streamout == null || !streamout.CanWrite)
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
				// create the outstream
				this.streamout = streamout;
				leaveStreamOutOpen = leaveOpen;

				// setup the items that make up the point
				LASzip laszip = new LASzip();
				if (setup_laszip_items(laszip, compress) != 0)
				{
					return 1;
				}

				// this supports software that writes the LAS header on its own
				if (do_not_write_header == false)
				{
					// prepare header
					if (prepare_header_for_write() != 0)
					{
						return 1;
					}

					// prepare point
					if (prepare_point_for_write(compress) != 0)
					{
						return 1;
					}

					// prepare VLRs
					if (prepare_vlrs_for_write() != 0)
					{
						return 1;
					}

					// write header variable after variable
					if (write_header(laszip, compress) != 0)
					{
						return 1;
					}
				}

				// create the point writer
				if (create_point_writer(laszip) != 0)
				{
					return 1;
				}

				// set the point number and point count
				npoints = (long)(header.number_of_point_records != 0 ? header.number_of_point_records : header.extended_number_of_point_records);
				p_count = 0;
			}
			catch
			{
				error = "internal error in laszip_open_writer_stream.";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int open_writer(string file_name, bool compress)
		{
			if (file_name == null || file_name.Length == 0)
			{
				error = "string 'file_name' is zero";
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
				Stream stream;
				try
				{
					stream = new FileStream(file_name, FileMode.Create, FileAccess.Write, FileShare.Read);
				}
				catch
				{
					error = string.Format("cannot open file '{0}'", file_name);
					return 1;
				}

				if (lax_create)
				{
					// create spatial indexing information using cell_size = 100.0f and threshold = 1000
					LASquadtree lasquadtree = new LASquadtree();
					lasquadtree.setup(header.min_x, header.max_x, header.min_y, header.max_y, 100.0f);

					lax_index = new LASindex();
					lax_index.prepare(lasquadtree, 1000);

					// copy the file name for later
					lax_file_name = file_name;
				}

				return open_writer_stream(stream, compress, false, false);
			}
			catch
			{
				error = string.Format("internal error in laszip_open_writer '{0}'", file_name);
				return 1;
			}
		}

		public int write_point()
		{
			if (writer == null)
			{
				error = "writing points before writer was opened";
				return 1;
			}

			try
			{
				// temporary fix to avoid corrupt LAZ files
				if (point.extended_point_type != 0)
				{
					// make sure legacy flags and extended flags are identical
					if ((point.extended_classification_flags & 0x7) != (point.classification_and_classification_flags >> 5))
					{
						error = "legacy flags and extended flags are not identical";
						return 1;
					}
				}

				// special recoding of points (in compatibility mode only)
				if (m_compatibility_mode)
				{
					// distill extended attributes
					point.scan_angle_rank = MyDefs.I8_CLAMP(MyDefs.I16_QUANTIZE(0.006 * point.extended_scan_angle));
					int scan_angle_remainder = point.extended_scan_angle - MyDefs.I16_QUANTIZE(point.scan_angle_rank / 0.006);

					if (point.extended_number_of_returns <= 7)
					{
						point.number_of_returns = point.extended_number_of_returns;
						if (point.extended_return_number <= 7)
						{
							point.return_number = point.extended_return_number;
						}
						else
						{
							point.return_number = 7;
						}
					}
					else
					{
						point.number_of_returns = 7;
						if (point.extended_return_number <= 4)
						{
							point.return_number = point.extended_return_number;
						}
						else
						{
							int return_count_difference = point.extended_number_of_returns - point.extended_return_number;
							if (return_count_difference <= 0)
							{
								point.return_number = 7;
							}
							else if (return_count_difference >= 3)
							{
								point.return_number = 4;
							}
							else
							{
								point.return_number = (byte)(7 - return_count_difference);
							}
						}
					}

					int return_number_increment = point.extended_return_number - point.return_number;
					int number_of_returns_increment = point.extended_number_of_returns - point.number_of_returns;

					if (point.extended_classification > 31)
					{
						point.classification = 0;
					}
					else
					{
						point.extended_classification = 0;
					}

					int scanner_channel = point.extended_scanner_channel;
					int overlap_bit = (point.extended_classification_flags >> 3);

					// write distilled extended attributes into extra bytes
					point.extra_bytes[start_scan_angle] = (byte)scan_angle_remainder;
					point.extra_bytes[start_scan_angle + 1] = (byte)(scan_angle_remainder >> 8);
					point.extra_bytes[start_extended_returns] = (byte)((return_number_increment << 4) | number_of_returns_increment);
					point.extra_bytes[start_classification] = point.extended_classification;
					point.extra_bytes[start_flags_and_channel] = (byte)((scanner_channel << 1) | overlap_bit);
					if (start_NIR_band != -1)
					{
						point.extra_bytes[start_NIR_band] = (byte)point.rgb[3];
						point.extra_bytes[start_NIR_band + 1] = (byte)(point.rgb[3] >> 8);
					}
				}

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

			error = warning = "";
			return 0;
		}

		public int write_indexed_point()
		{
			try
			{
				// write the point
				if (!writer.write(point))
				{
					error = string.Format("writing point {0} of {1} total points", p_count, npoints);
					return 1;
				}

				// index the point
				double x = header.x_scale_factor * point.X + header.x_offset;
				double y = header.y_scale_factor * point.Y + header.y_offset;
				lax_index.add(x, y, (uint)p_count);
				p_count++;
			}
			catch
			{
				error = "internal error in laszip_write_indexed_point";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int update_inventory()
		{
			try
			{
				if (inventory == null)
				{
					inventory = new Inventory();
				}

				inventory.add(point);
			}
			catch
			{
				error = "internal error in laszip_update_inventory";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int close_writer()
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

				// maybe update the header
				if (inventory != null)
				{
					if (header.point_data_format <= 5) // only update legacy counters for old point types
					{
						streamout.Seek(107, SeekOrigin.Begin);
						try { streamout.Write(BitConverter.GetBytes(inventory.number_of_point_records), 0, 4); }
						catch { error = "updating laszip_dll->inventory->number_of_point_records"; return 1; }

						for (int i = 0; i < 5; i++)
						{
							try { streamout.Write(BitConverter.GetBytes(inventory.number_of_points_by_return[i + 1]), 0, 4); }
							catch { error = string.Format("updating laszip_dll->inventory->number_of_points_by_return[{0}]", i); return 1; }
						}
					}

					streamout.Seek(179, SeekOrigin.Begin);

					double value = header.x_scale_factor * inventory.max_X + header.x_offset;
					try { streamout.Write(BitConverter.GetBytes(value), 0, 8); }
					catch { error = "updating laszip_dll->inventory->max_X"; return 1; }

					value = header.x_scale_factor * inventory.min_X + header.x_offset;
					try { streamout.Write(BitConverter.GetBytes(value), 0, 8); }
					catch { error = "updating laszip_dll->inventory->min_X"; return 1; }

					value = header.y_scale_factor * inventory.max_Y + header.y_offset;
					try { streamout.Write(BitConverter.GetBytes(value), 0, 8); }
					catch { error = "updating laszip_dll->inventory->max_Y"; return 1; }

					value = header.y_scale_factor * inventory.min_Y + header.y_offset;
					try { streamout.Write(BitConverter.GetBytes(value), 0, 8); }
					catch { error = "updating laszip_dll->inventory->min_Y"; return 1; }

					value = header.z_scale_factor * inventory.max_Z + header.z_offset;
					try { streamout.Write(BitConverter.GetBytes(value), 0, 8); }
					catch { error = "updating laszip_dll->inventory->max_Z"; return 1; }

					value = header.z_scale_factor * inventory.min_Z + header.z_offset;
					try { streamout.Write(BitConverter.GetBytes(value), 0, 8); }
					catch { error = "updating laszip_dll->inventory->min_Z"; return 1; }

					if (header.version_minor >= 4) // only update extended counters for LAS 1.4
					{
						streamout.Seek(247, SeekOrigin.Begin);
						long number = inventory.number_of_point_records;
						try { streamout.Write(BitConverter.GetBytes(number), 0, 8); }
						catch { error = "updating laszip_dll->inventory->extended_number_of_point_records"; return 1; }

						for (int i = 0; i < 15; i++)
						{
							number = inventory.number_of_points_by_return[i + 1];
							try { streamout.Write(BitConverter.GetBytes(number), 0, 8); }
							catch { error = string.Format("updating laszip_dll->inventory->extended_number_of_points_by_return[{0}]", i); return 1; }
						}
					}

					streamout.Seek(0, SeekOrigin.End);

					inventory = null;
				}

				if (lax_index != null)
				{
					lax_index.complete(100000, -20, false);

					if (!lax_index.write(lax_file_name))
					{
						error = string.Format("writing LAX file to '{0}'", lax_file_name);
						return 1;
					}

					lax_file_name = null;
					lax_index = null;
				}

				if (!leaveStreamOutOpen) streamout.Close();
				streamout = null;
			}
			catch
			{
				error = "internal error in laszip_writer_close";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int exploit_spatial_index(bool exploit)
		{
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

			lax_exploit = exploit;

			error = warning = "";
			return 0;
		}

		public int decompress_selective(LASZIP_DECOMPRESS_SELECTIVE decompress_selective)
		{
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

			las14_decompress_selective = decompress_selective;

			error = warning = "";
			return 0;
		}

		unsafe int read_header(out bool is_compressed)
		{
			is_compressed = false;

			try
			{
				byte[] signature = new byte[32];

				#region read the header variable after variable
				if (!streamin.getBytes(signature, 4))
				{
					error = "reading header.file_signature";
					return 1;
				}

				if (signature[0] != 'L' && signature[1] != 'A' && signature[2] != 'S' && signature[3] != 'F')
				{
					error = "wrong file_signature. not a LAS/LAZ file.";
					return 1;
				}

				if (!streamin.get16bits(out header.file_source_ID))
				{
					error = "reading header.file_source_ID";
					return 1;
				}

				if (!streamin.get16bits(out header.global_encoding))
				{
					error = "reading header.global_encoding";
					return 1;
				}

				if (!streamin.get32bits(out header.project_ID_GUID_data_1))
				{
					error = "reading header.project_ID_GUID_data_1";
					return 1;
				}

				if (!streamin.get16bits(out header.project_ID_GUID_data_2))
				{
					error = "reading header.project_ID_GUID_data_2";
					return 1;
				}

				if (!streamin.get16bits(out header.project_ID_GUID_data_3))
				{
					error = "reading header.project_ID_GUID_data_3";
					return 1;
				}

				if (!streamin.getBytes(header.project_ID_GUID_data_4, 8))
				{
					error = "reading header.project_ID_GUID_data_4";
					return 1;
				}

				if (!streamin.get8bits(out header.version_major))
				{
					error = "reading header.version_major";
					return 1;
				}

				if (!streamin.get8bits(out header.version_minor))
				{
					error = "reading header.version_minor";
					return 1;
				}

				if (!streamin.getBytes(header.system_identifier, 32))
				{
					error = "reading header.system_identifier";
					return 1;
				}

				if (!streamin.getBytes(header.generating_software, 32))
				{
					error = "reading header.generating_software";
					return 1;
				}

				if (!streamin.get16bits(out header.file_creation_day))
				{
					error = "reading header.file_creation_day";
					return 1;
				}

				if (!streamin.get16bits(out header.file_creation_year))
				{
					error = "reading header.file_creation_year";
					return 1;
				}

				if (!streamin.get16bits(out header.header_size))
				{
					error = "reading header.header_size";
					return 1;
				}

				if (!streamin.get32bits(out header.offset_to_point_data))
				{
					error = "reading header.offset_to_point_data";
					return 1;
				}

				if (!streamin.get32bits(out header.number_of_variable_length_records))
				{
					error = "reading header.number_of_variable_length_records";
					return 1;
				}

				if (!streamin.get8bits(out header.point_data_format))
				{
					error = "reading header.point_data_format";
					return 1;
				}

				if (!streamin.get16bits(out header.point_data_record_length))
				{
					error = "reading header.point_data_record_length";
					return 1;
				}

				if (!streamin.get32bits(out header.number_of_point_records))
				{
					error = "reading header.number_of_point_records";
					return 1;
				}

				for (int i = 0; i < 5; i++)
				{
					if (!streamin.get32bits(out header.number_of_points_by_return[i]))
					{
						error = string.Format("reading header.number_of_points_by_return {0}", i);
						return 1;
					}
				}

				if (!streamin.get64bits(out header.x_scale_factor))
				{
					error = "reading header.x_scale_factor";
					return 1;
				}

				if (!streamin.get64bits(out header.y_scale_factor))
				{
					error = "reading header.y_scale_factor";
					return 1;
				}

				if (!streamin.get64bits(out header.z_scale_factor))
				{
					error = "reading header.z_scale_factor";
					return 1;
				}

				if (!streamin.get64bits(out header.x_offset))
				{
					error = "reading header.x_offset";
					return 1;
				}

				if (!streamin.get64bits(out header.y_offset))
				{
					error = "reading header.y_offset";
					return 1;
				}

				if (!streamin.get64bits(out header.z_offset))
				{
					error = "reading header.z_offset";
					return 1;
				}

				if (!streamin.get64bits(out header.max_x))
				{
					error = "reading header.max_x";
					return 1;
				}

				if (!streamin.get64bits(out header.min_x))
				{
					error = "reading header.min_x";
					return 1;
				}

				if (!streamin.get64bits(out header.max_y))
				{
					error = "reading header.max_y";
					return 1;
				}

				if (!streamin.get64bits(out header.min_y))
				{
					error = "reading header.min_y";
					return 1;
				}

				if (!streamin.get64bits(out header.max_z))
				{
					error = "reading header.max_z";
					return 1;
				}

				if (!streamin.get64bits(out header.min_z))
				{
					error = "reading header.min_z";
					return 1;
				}

				// special handling for LAS 1.3
				if (header.version_major == 1 && header.version_minor >= 3)
				{
					if (header.header_size < 235)
					{
						error = string.Format("for LAS 1.{0} header_size should at least be 235 but it is only {1}", header.version_minor, header.header_size);
						return 1;
					}
					else
					{
						if (!streamin.get64bits(out header.start_of_waveform_data_packet_record))
						{
							error = "reading header.start_of_waveform_data_packet_record";
							return 1;
						}
						header.user_data_in_header_size = (uint)header.header_size - 235;
					}
				}
				else
				{
					header.user_data_in_header_size = (uint)header.header_size - 227;
				}

				// special handling for LAS 1.4
				if (header.version_major == 1 && header.version_minor >= 4)
				{
					if (header.header_size < 375)
					{
						error = string.Format("for LAS 1.{0} header_size should at least be 375 but it is only {1}", header.version_minor, header.header_size);
						return 1;
					}
					else
					{
						if (!streamin.get64bits(out header.start_of_first_extended_variable_length_record))
						{
							error = "reading header.start_of_first_extended_variable_length_record";
							return 1;
						}

						if (!streamin.get32bits(out header.number_of_extended_variable_length_records))
						{
							error = "reading header.number_of_extended_variable_length_records";
							return 1;
						}

						if (!streamin.get64bits(out header.extended_number_of_point_records))
						{
							error = "reading header.extended_number_of_point_records";
							return 1;
						}

						for (int i = 0; i < 15; i++)
						{
							if (!streamin.get64bits(out header.extended_number_of_points_by_return[i]))
							{
								error = string.Format("reading header.extended_number_of_points_by_return[{0}]", i);
								return 1;
							}
						}
						header.user_data_in_header_size = (uint)header.header_size - 375;
					}
				}

				// load any number of user-defined bytes that might have been added to the header
				if (header.user_data_in_header_size != 0)
				{
					header.user_data_in_header = new byte[header.user_data_in_header_size];

					if (!streamin.getBytes(header.user_data_in_header, (int)header.user_data_in_header_size))
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
					try { header.vlrs = new List<laszip_vlr>(); }
					catch { error = string.Format("allocating {0} VLRs", header.number_of_variable_length_records); return 1; }

					for (int i = 0; i < header.number_of_variable_length_records; i++)
					{
						try { header.vlrs.Add(new laszip_vlr()); }
						catch { error = string.Format("allocating VLR #{0}", i); return 1; }

						// make sure there are enough bytes left to read a variable length record before the point block starts
						if (((int)header.offset_to_point_data - vlrs_size - header.header_size) < 54)
						{
							warning = string.Format("only {0} bytes until point block after reading {1} of {2} vlrs. skipping remaining vlrs ...", (int)header.offset_to_point_data - vlrs_size - header.header_size, i, header.number_of_variable_length_records);
							header.number_of_variable_length_records = (uint)i;
							break;
						}

						// read variable length records variable after variable (to avoid alignment issues)
						if (!streamin.get16bits(out header.vlrs[i].reserved))
						{
							error = string.Format("reading header.vlrs[{0}].reserved", i);
							return 1;
						}

						if (!streamin.getBytes(header.vlrs[i].user_id, 16))
						{
							error = string.Format("reading header.vlrs[{0}].user_id", i);
							return 1;
						}

						if (!streamin.get16bits(out header.vlrs[i].record_id))
						{
							error = string.Format("reading header.vlrs[{0}].record_id", i);
							return 1;
						}

						if (!streamin.get16bits(out header.vlrs[i].record_length_after_header))
						{
							error = string.Format("reading header.vlrs[{0}].record_length_after_header", i);
							return 1;
						}

						if (!streamin.getBytes(header.vlrs[i].description, 32))
						{
							error = string.Format("reading header.vlrs[{0}].description", i);
							return 1;
						}

						// keep track on the number of bytes we have read so far
						vlrs_size += 54;

						// check variable length record contents
						if (header.vlrs[i].reserved != 0xAABB && header.vlrs[i].reserved != 0x0)
						{
							warning = string.Format("wrong header.vlrs[{0}].reserved: {1} != 0xAABB and {1} != 0x0", i, header.vlrs[i].reserved);
						}

						// make sure there are enough bytes left to read the data of the variable length record before the point block starts
						if (((int)header.offset_to_point_data - vlrs_size - header.header_size) < header.vlrs[i].record_length_after_header)
						{
							warning = string.Format("only {0} bytes until point block when trying to read {1} bytes into header.vlrs[{2}].data", (int)header.offset_to_point_data - vlrs_size - header.header_size, header.vlrs[i].record_length_after_header, i);
							header.vlrs[i].record_length_after_header = (ushort)(header.offset_to_point_data - vlrs_size - header.header_size);
						}

						// load data following the header of the variable length record
						if (header.vlrs[i].record_length_after_header != 0)
						{
							if (strcmp(header.vlrs[i].user_id, "laszip encoded") && header.vlrs[i].record_id == 22204)
							{
								try { laszip = new LASzip(); }
								catch { error = "could not alloc LASzip"; return 1; }

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

								if (!streamin.get16bits(out laszip.compressor))
								{
									error = "reading compressor";
									return 1;
								}

								if (!streamin.get16bits(out laszip.coder))
								{
									error = "reading coder";
									return 1;
								}

								if (!streamin.get8bits(out laszip.version_major))
								{
									error = "reading version_major";
									return 1;
								}

								if (!streamin.get8bits(out laszip.version_minor))
								{
									error = "reading version_minor";
									return 1;
								}

								if (!streamin.get16bits(out laszip.version_revision))
								{
									error = "reading version_revision";
									return 1;
								}

								if (!streamin.get32bits(out laszip.options))
								{
									error = "reading options";
									return 1;
								}

								if (!streamin.get32bits(out laszip.chunk_size))
								{
									error = "reading chunk_size";
									return 1;
								}

								if (!streamin.get64bits(out laszip.number_of_special_evlrs))
								{
									error = "reading number_of_special_evlrs";
									return 1;
								}

								if (!streamin.get64bits(out laszip.offset_to_special_evlrs))
								{
									error = "reading offset_to_special_evlrs";
									return 1;
								}

								if (!streamin.get16bits(out laszip.num_items))
								{
									error = "reading num_items";
									return 1;
								}

								laszip.items = new LASitem[laszip.num_items];
								for (int j = 0; j < laszip.num_items; j++)
								{
									laszip.items[j] = new LASitem();

									ushort type;
									if (!streamin.get16bits(out type))
									{
										error = string.Format("reading type of item {0}", j);
										return 1;
									}
									laszip.items[j].type = (LASitem.Type)type;

									if (!streamin.get16bits(out laszip.items[j].size))
									{
										error = string.Format("reading size of item {0}", j);
										return 1;
									}

									if (!streamin.get16bits(out laszip.items[j].version))
									{
										error = string.Format("reading version of item {0}", j);
										return 1;
									}
								}
							}
							else
							{
								header.vlrs[i].data = new byte[header.vlrs[i].record_length_after_header];
								if (!streamin.getBytes(header.vlrs[i].data, header.vlrs[i].record_length_after_header))
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
						if (strcmp(header.vlrs[i].user_id, "laszip encoded") && header.vlrs[i].record_id == 22204)
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

				#region load any number of user-defined bytes that might have been added after the header
				header.user_data_after_header_size = header.offset_to_point_data - vlrs_size - header.header_size;
				if (header.user_data_after_header_size != 0)
				{
					header.user_data_after_header = new byte[header.user_data_after_header_size];

					if (!streamin.getBytes(header.user_data_after_header, (int)header.user_data_after_header_size))
					{
						error = string.Format("reading {0} bytes of data into header.user_data_after_header", header.user_data_after_header_size);
						return 1;
					}
				}
				#endregion

				#region remove extra bits in point data type
				if ((header.point_data_format & 128) != 0 || (header.point_data_format & 64) != 0)
				{
					if (laszip == null)
					{
						error = "this file was compressed with an experimental version of LASzip. contact 'martin.isenburg@rapidlasso.com' for assistance";
						return 1;
					}
					header.point_data_format &= 127;
				}
				#endregion

				#region check if file is compressed
				if (laszip != null)
				{
					// yes. check the compressor state
					is_compressed = true;
					if (!laszip.check(header.point_data_record_length))
					{
						error = string.Format("{0} upgrade to the latest release of LAStools (with LASzip) or contact 'martin.isenburg@rapidlasso.com' for assistance", laszip.get_error());
						return 1;
					}
				}
				else
				{
					// no. setup an un-compressed read
					is_compressed = false;
					try { laszip = new LASzip(); }
					catch { error = "could not alloc LASzip"; return 1; }
					if (!laszip.setup(header.point_data_format, header.point_data_record_length, LASzip.COMPRESSOR_NONE))
					{
						error = string.Format("invalid combination of point_data_format {0} and point_data_record_length {1}", header.point_data_format, header.point_data_record_length);
						return 1;
					}
				}
				#endregion

				#region create point's item pointers
				for (int i = 0; i < laszip.num_items; i++)
				{
					switch (laszip.items[i].type)
					{
						case LASitem.Type.POINT14:
						case LASitem.Type.POINT10:
						case LASitem.Type.GPSTIME11:
						case LASitem.Type.RGB12:
						case LASitem.Type.RGB14:
						case LASitem.Type.RGBNIR14:
						case LASitem.Type.WAVEPACKET13:
						case LASitem.Type.WAVEPACKET14:
							break;
						case LASitem.Type.BYTE:
						case LASitem.Type.BYTE14:
							point.num_extra_bytes = laszip.items[i].size;
							point.extra_bytes = new byte[point.num_extra_bytes];
							break;
						default:
							error = string.Format("unknown LASitem type {0}", laszip.items[i].type);
							return 1;
					}
				}
				#endregion

				#region did the user request to recode the compatibility mode points?
				m_compatibility_mode = false;

				if (m_request_compatibility_mode && header.version_minor < 4)
				{
					// does this file contain compatibility mode recoded LAS 1.4 content
					laszip_vlr compatibility_VLR = null;

					if (header.point_data_format == 1 || header.point_data_format == 3 || header.point_data_format == 4 || header.point_data_format == 5)
					{
						// if we find the compatibility VLR
						for (int i = 0; i < header.number_of_variable_length_records; i++)
						{
							if (strcmp(header.vlrs[i].user_id, "lascompatible") && header.vlrs[i].record_id == 22204)
							{
								if (header.vlrs[i].record_length_after_header == 2 + 2 + 4 + 148)
								{
									compatibility_VLR = header.vlrs[i];
									break;
								}
							}
						}

						if (compatibility_VLR != null)
						{
							// and we also find the extra bytes VLR with the right attributes
							LASattributer attributer = new LASattributer();
							for (int i = 0; i < header.number_of_variable_length_records; i++)
							{
								if (strcmp(header.vlrs[i].user_id, "LASF_Spec") && header.vlrs[i].record_id == 4)
								{
									attributer.init_attributes(ToLASattributeList(header.vlrs[i].data));
									start_scan_angle = attributer.get_attribute_start("LAS 1.4 scan angle");
									start_extended_returns = attributer.get_attribute_start("LAS 1.4 extended returns");
									start_classification = attributer.get_attribute_start("LAS 1.4 classification");
									start_flags_and_channel = attributer.get_attribute_start("LAS 1.4 flags and channel");
									start_NIR_band = attributer.get_attribute_start("LAS 1.4 NIR band");
									break;
								}
							}

							// can we do it ... ?
							if (start_scan_angle != -1 && start_extended_returns != -1 && start_classification != -1 && start_flags_and_channel != -1)
							{
								// yes ... so let's fix the header (using the content from the compatibility VLR)
								MemoryStream inStream = new MemoryStream(compatibility_VLR.data, 0, compatibility_VLR.record_length_after_header);

								// read control info
								ushort laszip_version;
								inStream.get16bits(out laszip_version);
								ushort compatible_version;
								inStream.get16bits(out compatible_version);
								uint unused;
								inStream.get32bits(out unused);

								// read the 148 bytes of the extended LAS 1.4 header
								ulong start_of_waveform_data_packet_record;
								inStream.get64bits(out start_of_waveform_data_packet_record);
								if (start_of_waveform_data_packet_record != 0)
								{
									Console.Error.WriteLine("WARNING: start_of_waveform_data_packet_record is {0}. reading 0 instead.", start_of_waveform_data_packet_record);
								}
								header.start_of_waveform_data_packet_record = 0;

								ulong start_of_first_extended_variable_length_record;
								inStream.get64bits(out start_of_first_extended_variable_length_record);
								if (start_of_first_extended_variable_length_record != 0)
								{
									Console.Error.WriteLine("WARNING: EVLRs not supported. start_of_first_extended_variable_length_record is {0}. reading 0 instead.", start_of_first_extended_variable_length_record);
								}
								header.start_of_first_extended_variable_length_record = 0;

								uint number_of_extended_variable_length_records;
								inStream.get32bits(out number_of_extended_variable_length_records);
								if (number_of_extended_variable_length_records != 0)
								{
									Console.Error.WriteLine("WARNING: EVLRs not supported. number_of_extended_variable_length_records is {0}. reading 0 instead.", number_of_extended_variable_length_records);
								}
								header.number_of_extended_variable_length_records = 0;

								ulong extended_number_of_point_records;
								inStream.get64bits(out extended_number_of_point_records);
								if (header.number_of_point_records != 0 && header.number_of_point_records != extended_number_of_point_records)
								{
									Console.Error.WriteLine("WARNING: number_of_point_records is {0}. but extended_number_of_point_records is {1}.", header.number_of_point_records, extended_number_of_point_records);
								}
								header.extended_number_of_point_records = extended_number_of_point_records;

								ulong extended_number_of_points_by_return;
								for (int r = 0; r < 15; r++)
								{
									inStream.get64bits(out extended_number_of_points_by_return);
									if (r < 5 && header.number_of_points_by_return[r] != 0 && header.number_of_points_by_return[r] != extended_number_of_points_by_return)
									{
										Console.Error.WriteLine("WARNING: number_of_points_by_return[{0}] is {1}. but extended_number_of_points_by_return[{0}] is {2}.", r, header.number_of_points_by_return[r], extended_number_of_points_by_return);
									}
									header.extended_number_of_points_by_return[r] = extended_number_of_points_by_return;
								}
								inStream.Close();

								// remove the compatibility VLR
								if (remove_vlr("lascompatible", 22204) != 0)
								{

									error = "removing the compatibility VLR";
									return 1;
								}

								// remove the LAS 1.4 attributes from the "extra bytes" description
								if (start_NIR_band != -1) attributer.remove_attribute("LAS 1.4 NIR band");
								attributer.remove_attribute("LAS 1.4 flags and channel");
								attributer.remove_attribute("LAS 1.4 classification");
								attributer.remove_attribute("LAS 1.4 extended returns");
								attributer.remove_attribute("LAS 1.4 scan angle");

								// either rewrite or remove the "extra bytes" VLR
								if (attributer.number_attributes != 0)
								{
									laszip_vlr attributes = new laszip_vlr() { reserved = 0, record_id = 4, record_length_after_header = (ushort)(attributer.number_attributes * sizeof(LASattribute)), data = ToByteArray(attributer.attributes) };
									Encoding.ASCII.GetBytes("LASF_Spec").CopyTo(attributes.user_id, 0);

									if (add_vlr(attributes) != 0)
									{

										error = "rewriting the extra bytes VLR without 'LAS 1.4 compatibility mode' attributes";
										return 1;
									}
								}
								else
								{
									if (remove_vlr("LASF_Spec", 4) != 0)
									{

										error = "removing the LAS 1.4 attribute VLR";
										return 1;
									}
								}

								// upgrade to LAS 1.4
								if (header.version_minor < 3)
								{
									// LAS 1.2 header is 148 bytes less than LAS 1.4+ header
									header.header_size += 148;
									header.offset_to_point_data += 148;
								}
								else
								{
									// LAS 1.3 header is 140 bytes less than LAS 1.4+ header
									header.header_size += 140;
									header.offset_to_point_data += 140;
								}
								header.version_minor = 4;

								// maybe turn on the bit indicating the presence of the OGC WKT
								for (int i = 0; i < header.number_of_variable_length_records; i++)
								{
									if (strcmp(header.vlrs[i].user_id, "LASF_Projection") && header.vlrs[i].record_id == 2112)
									{
										header.global_encoding |= (1 << 4);
										break;
									}
								}

								// update point type and size
								point.extended_point_type = 1;

								if (header.point_data_format == 1)
								{
									header.point_data_format = 6;
									header.point_data_record_length -= 5 - 2; // record is 2 bytes larger but minus 5 extra bytes
								}
								else if (header.point_data_format == 3)
								{
									if (start_NIR_band == -1)
									{
										header.point_data_format = 7;
										header.point_data_record_length -= 5 - 2; // record is 2 bytes larger but minus 5 extra bytes
									}
									else
									{
										header.point_data_format = 8;
										header.point_data_record_length -= 7 - 4; // record is 4 bytes larger but minus 7 extra bytes
									}
								}
								else
								{
									if (start_NIR_band == -1)
									{
										header.point_data_format = 9;
										header.point_data_record_length -= 5 - 2; // record is 2 bytes larger but minus 5 extra bytes
									}
									else
									{
										header.point_data_format = 10;
										header.point_data_record_length -= 7 - 4; // record is 4 bytes larger but minus 7 extra bytes
									}
								}

								// we are operating in compatibility mode
								m_compatibility_mode = true;
							}
						}
					}
				}
				else if (header.point_data_format > 5)
				{
					point.extended_point_type = 1;
				}
				#endregion

				#region create the point reader
				try { reader = new LASreadPoint(las14_decompress_selective); }
				catch { error = "could not alloc LASreadPoint"; return 1; }

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
				#endregion

				// set the point number and point count
				npoints = (long)(header.number_of_point_records != 0 ? header.number_of_point_records : header.extended_number_of_point_records);
				p_count = 0;
			}
			catch
			{
				error = "internal error in laszip_open_reader";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int open_reader_stream(Stream streamin, out bool is_compressed, bool leaveOpen = false)
		{
			is_compressed = false;

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

			return read_header(out is_compressed);
		}

		public int open_reader(string file_name, out bool is_compressed)
		{
			is_compressed = false;

			if (file_name == null || file_name.Length == 0)
			{
				error = "string 'file_name' is zero";
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

			try
			{
				// open the file
				Stream stream;
				try
				{
					stream = File.OpenRead(file_name);
				}
				catch
				{
					error = string.Format("cannot open file '{0}'", file_name);
					return 1;
				}

				if (open_reader_stream(stream, out is_compressed, false) != 0)
				{
					return 1;
				}

				// should we try to exploit existing spatial indexing information
				if (lax_exploit)
				{
					lax_index = new LASindex();
					if (!lax_index.read(file_name))
					{
						lax_index = null;
					}
				}
			}
			catch
			{
				error = "internal error in laszip_open_reader";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int has_spatial_index(out bool is_indexed, out bool is_appended)
		{
			is_indexed = is_appended = false;

			try
			{
				if (reader == null)
				{
					error = "reader is not open";
					return 1;
				}

				if (writer != null)
				{

					error = "writer is already open";
					return 1;
				}

				if (lax_exploit == false)
				{

					error = "exploiting of spatial indexing not enabled before opening reader";
					return 1;
				}

				// check if reader found spatial indexing information when opening file
				is_indexed = lax_index != null;

				// inform whether spatial index is appended to LAZ file or in separate LAX file
				is_appended = false;
			}
			catch
			{
				error = "internal error in laszip_have_spatial_index";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int inside_rectangle(double min_x, double min_y, double max_x, double max_y, out bool is_empty)
		{
			is_empty = false;

			try
			{
				if (reader == null)
				{
					error = "reader is not open";
					return 1;
				}

				if (lax_exploit == false)
				{
					error = "exploiting of spatial indexing not enabled before opening reader";
					return 1;
				}

				lax_r_min_x = min_x;
				lax_r_min_y = min_y;
				lax_r_max_x = max_x;
				lax_r_max_y = max_y;

				if (lax_index != null)
				{
					if (lax_index.intersect_rectangle(min_x, min_y, max_x, max_y))
					{
						is_empty = false;
					}
					else
					{
						// no overlap between spatial indexing cells and query reactangle
						is_empty = true;
					}
				}
				else
				{
					if (header.min_x > max_x || header.min_y > max_y || header.max_x < min_x || header.max_y < min_y)
					{
						// no overlap between header bouding box and query reactangle
						is_empty = true;
					}
					else
					{
						is_empty = false;
					}
				}
			}
			catch
			{
				error = "internal error in laszip_inside_rectangle";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int seek_point(long index)
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

			error = warning = "";
			return 0;
		}

		public int read_point()
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

				// special recoding of points (in compatibility mode only)
				if (m_compatibility_mode)
				{
					// instill extended attributes
					var point = this.point;

					// get extended attributes from extra bytes
					short scan_angle_remainder = (short)((point.extra_bytes[start_scan_angle + 1] << 8) | point.extra_bytes[start_scan_angle]);
					byte extended_returns = point.extra_bytes[start_extended_returns];
					byte classification = point.extra_bytes[start_classification];
					byte flags_and_channel = point.extra_bytes[start_flags_and_channel];
					if (start_NIR_band != -1)
					{
						point.rgb[3] = (ushort)((point.extra_bytes[start_NIR_band + 1] << 8) | point.extra_bytes[start_NIR_band]);
					}

					// decompose into individual attributes
					int return_number_increment = (extended_returns >> 4) & 0x0F;
					int number_of_returns_increment = extended_returns & 0x0F;
					int scanner_channel = (flags_and_channel >> 1) & 0x03;
					int overlap_bit = flags_and_channel & 0x01;

					// instill into point
					point.extended_scan_angle = (short)(scan_angle_remainder + MyDefs.I16_QUANTIZE(point.scan_angle_rank / 0.006));
					point.extended_return_number = (byte)(return_number_increment + point.return_number);
					point.extended_number_of_returns = (byte)(number_of_returns_increment + point.number_of_returns);
					point.extended_classification = (byte)(classification + point.classification);
					point.extended_scanner_channel = (byte)scanner_channel;
					point.extended_classification_flags = (byte)((overlap_bit << 3) | ((point.withheld_flag) << 2) | ((point.keypoint_flag) << 1) | (point.synthetic_flag));
				}

				p_count++;
			}
			catch
			{
				error = "internal error in laszip_read_point";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int read_inside_point(out bool is_done)
		{
			is_done = true;

			try
			{
				if (lax_index != null)
				{
					while (lax_index.seek_next(reader, ref p_count))
					{
						if (reader.read(point))
						{
							p_count++;
							double xy = header.x_scale_factor * point.X + header.x_offset;
							if (xy < lax_r_min_x || xy >= lax_r_max_x) continue;
							xy = header.y_scale_factor * point.Y + header.y_offset;
							if (xy < lax_r_min_y || xy >= lax_r_max_y) continue;
							is_done = false;
							break;
						}
					}
				}
				else
				{
					while (reader.read(point))
					{
						p_count++;
						double xy = header.x_scale_factor * point.X + header.x_offset;
						if (xy < lax_r_min_x || xy >= lax_r_max_x) continue;
						xy = header.y_scale_factor * point.Y + header.y_offset;
						if (xy < lax_r_min_y || xy >= lax_r_max_y) continue;
						is_done = false;
						break;
					}

					if (is_done)
					{
						if (p_count < npoints)
						{
							error = string.Format("reading point {0} of {1} total points", p_count, npoints);
							return 1;
						}
					}
				}
			}
			catch
			{
				error = "internal error in laszip_read_inside_point";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public int close_reader()
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
				lax_index = null;
			}
			catch
			{
				error = "internal error in laszip_close_reader";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		// make LASzip VLR for point type and point size already specified earlier
		public int create_laszip_vlr(out byte[] vlr)
		{
			vlr = null;

			LASzip laszip = new LASzip();
			if (setup_laszip_items(laszip, true) != 0)
			{
				return 1;
			}

			MemoryStream outStream;

			try
			{
				outStream = new MemoryStream();
			}
			catch
			{
				error = "could not alloc ByteStreamOutArray";
				return 1;
			}

			if (write_laszip_vlr_header(laszip, outStream) != 0)
			{
				return 1;
			}

			if (write_laszip_vlr_payload(laszip, outStream) != 0)
			{
				return 1;
			}

			vlr = outStream.ToArray();

			error = warning = "";
			return 0;
		}

		public int read_evlrs()
		{
			if (reader == null)
			{
				error = "reading EVLRs before reader was opened";
				return 1;
			}

			if (!streamin.CanSeek)
			{
				error = "stream unable to seek";
				return 1;
			}

			if (header.number_of_extended_variable_length_records == 0)
			{
				error = "";
				warning = "not EVLRs in file";
				return 0;
			}

			try
			{
				long previousPosition = streamin.Position;

				streamin.Position = (long)header.start_of_first_extended_variable_length_record;

				try { evlrs = new List<laszip_evlr>((int)header.number_of_extended_variable_length_records); }
				catch { error = string.Format("allocating {0} EVLRs", header.number_of_extended_variable_length_records); return 1; }

				for (int i = 0; i < header.number_of_extended_variable_length_records; i++)
				{
					try { evlrs.Add(new laszip_evlr()); }
					catch { error = string.Format("allocating EVLR #{0}", i); return 1; }

					// make sure there are enough bytes left to read a extended variable length record before the end of the stream
					if ((streamin.Length - streamin.Position) < 60)
					{
						warning = string.Format("only {0} bytes until end of stream reading {1} of {2} EVLRs. skipping remaining EVLRs ...", streamin.Length - streamin.Position, i, header.number_of_extended_variable_length_records);
						header.number_of_extended_variable_length_records = (uint)i;
						break;
					}

					// read extended variable length records variable after variable (to avoid alignment issues)
					if (!streamin.get16bits(out evlrs[i].reserved))
					{
						error = string.Format("reading EVLRs[{0}].reserved", i);
						return 1;
					}

					if (!streamin.getBytes(evlrs[i].user_id, 16))
					{
						error = string.Format("reading EVLRs[{0}].user_id", i);
						return 1;
					}

					if (!streamin.get16bits(out evlrs[i].record_id))
					{
						error = string.Format("reading EVLRs[{0}].record_id", i);
						return 1;
					}

					if (!streamin.get64bits(out evlrs[i].record_length_after_header))
					{
						error = string.Format("reading EVLRs[{0}].record_length_after_header", i);
						return 1;
					}

					if (!streamin.getBytes(evlrs[i].description, 32))
					{
						error = string.Format("reading EVLRs[{0}].description", i);
						return 1;
					}

					// check extended variable length record contents
					if (evlrs[i].reserved != 0xAABB && evlrs[i].reserved != 0x0)
					{
						warning = string.Format("wrong EVLRs[{0}].reserved: {1} != 0xAABB and {1} != 0x0", i, evlrs[i].reserved);
					}

					// make sure there are enough bytes left to read the data of the extended variable length record before the point block starts
					if ((streamin.Length - streamin.Position) < (long)evlrs[i].record_length_after_header)
					{
						warning = string.Format("only {0} bytes until end of stream when trying to read {1} bytes into EVLRs[{2}].data", streamin.Length - streamin.Position, evlrs[i].record_length_after_header, i);
						evlrs[i].record_length_after_header = (ulong)(streamin.Length - streamin.Position);
					}

					// load data following the header of the extended variable length record
					if (evlrs[i].record_length_after_header != 0)
					{
						evlrs[i].data = new byte[(int)evlrs[i].record_length_after_header];
						if (!streamin.getBytes(evlrs[i].data, (int)evlrs[i].record_length_after_header))
						{
							error = string.Format("reading {0} bytes of data into EVLRs[{1}].data", evlrs[i].record_length_after_header, i);
							return 1;
						}
					}
					else
					{
						evlrs[i].data = null;
					}
				}

				streamin.Position = previousPosition;
			}
			catch
			{
				error = "internal error in read_evlrs";
				return 1;
			}

			error = warning = "";
			return 0;
		}
	}
}
