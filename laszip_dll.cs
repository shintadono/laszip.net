//===============================================================================
//
//  FILE:  laszip_dll.cs // TODO rename to laszip_api.cs
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
//    (c) of the C# port 2014-2018 by Shinta <shintadono@googlemail.com>
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
		public readonly header curHeader = new header(); // TODO name
		long p_count = 0;
		long npoints = 0;
		public readonly point curPoint = new point();

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

		LASZIP_DECOMPRESS_SELECTIVE las14_decompress_selective = LASZIP_DECOMPRESS_SELECTIVE.CHANNEL_RETURNS_XY;
		bool m_preserve_generating_software = false;
		bool m_request_native_extension = false;
		bool m_request_compatibility_mode = false;
		bool m_compatibility_mode = false;

		uint m_set_chunk_size = 0;

		int start_scan_angle = 0;
		int start_extended_returns = 0;
		int start_classification = 0;
		int start_flags_and_channel = 0;
		int start_NIR_band = 0;

		Inventory inventory = null;

		readonly List<byte[]> buffers = new List<byte[]>();

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
				curHeader.file_source_ID = 0;
				curHeader.global_encoding = 0;
				curHeader.project_ID_GUID_data_1 = 0;
				curHeader.project_ID_GUID_data_2 = 0;
				curHeader.project_ID_GUID_data_3 = 0;
				Array.Clear(curHeader.project_ID_GUID_data_4, 0, curHeader.project_ID_GUID_data_4.Length);
				curHeader.version_major = 0;
				curHeader.version_minor = 0;
				Array.Clear(curHeader.system_identifier, 0, curHeader.system_identifier.Length);
				Array.Clear(curHeader.generating_software, 0, curHeader.generating_software.Length);
				curHeader.file_creation_day = 0;
				curHeader.file_creation_year = 0;
				curHeader.header_size = 0;
				curHeader.offset_to_point_data = 0;
				curHeader.number_of_variable_length_records = 0;
				curHeader.point_data_format = 0;
				curHeader.point_data_record_length = 0;
				curHeader.number_of_point_records = 0;
				Array.Clear(curHeader.number_of_points_by_return, 0, curHeader.number_of_points_by_return.Length);
				curHeader.x_scale_factor = 0;
				curHeader.y_scale_factor = 0;
				curHeader.z_scale_factor = 0;
				curHeader.x_offset = 0;
				curHeader.y_offset = 0;
				curHeader.z_offset = 0;
				curHeader.max_x = 0;
				curHeader.min_x = 0;
				curHeader.max_y = 0;
				curHeader.min_y = 0;
				curHeader.max_z = 0;
				curHeader.min_z = 0;
				curHeader.start_of_waveform_data_packet_record = 0;
				curHeader.start_of_first_extended_variable_length_record = 0;
				curHeader.number_of_extended_variable_length_records = 0;
				curHeader.extended_number_of_point_records = 0;
				Array.Clear(curHeader.extended_number_of_points_by_return, 0, curHeader.extended_number_of_points_by_return.Length);
				curHeader.user_data_in_header_size = 0;
				curHeader.user_data_in_header = null;
				curHeader.vlrs = null;
				curHeader.user_data_after_header_size = 0;
				curHeader.user_data_after_header = null;

				// dealloc and zero everything alloc in the  point
				curPoint.X = 0;
				curPoint.Y = 0;
				curPoint.Z = 0;
				curPoint.intensity = 0;
				curPoint.flags = 0; // return_number, number_of_returns, scan_direction_flag and edge_of_flight_line
				curPoint.classification_and_classification_flags = 0; // classification, synthetic_flag, keypoint_flag and withheld_flag
				curPoint.scan_angle_rank = 0;
				curPoint.user_data = 0;
				curPoint.point_source_ID = 0;
				curPoint.extended_flags = 0; // extended_point_type, extended_scanner_channel and extended_classification_flags
				curPoint.extended_classification = 0;
				curPoint.extended_returns = 0; // extended_return_number and extended_number_of_returns
				curPoint.extended_scan_angle = 0;
				curPoint.gps_time = 0;
				Array.Clear(curPoint.rgb, 0, 4);
				Array.Clear(curPoint.wave_packet, 0, 29);
				curPoint.num_extra_bytes = 0;
				curPoint.extra_bytes = null;

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

				// dealloc any data fields that were kept around in memory for others
				buffers.Clear();

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
				byte[] generatingSoftware = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(generatingSoftware, curHeader.generating_software, Math.Min(generatingSoftware.Length, 32));
				curHeader.version_major = 1;
				curHeader.version_minor = 2;
				curHeader.header_size = 227;
				curHeader.offset_to_point_data = 227;
				curHeader.point_data_format = 1;
				curHeader.point_data_record_length = 28;
				curHeader.x_scale_factor = 0.01;
				curHeader.y_scale_factor = 0.01;
				curHeader.z_scale_factor = 0.01;
			}
			catch
			{
				error = "internal error in laszip_clean";
				return 1;
			}

			return 0;
		}

		public header get_header_pointer()
		{
			error = warning = "";
			return curHeader;
		}

		public point get_point_pointer()
		{
			error = warning = "";
			return curPoint;
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

		// TODO remove?
		public int get_number_of_point(out long npoints)
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

		public int set_header(header header)
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
				curHeader.file_source_ID = header.file_source_ID;
				curHeader.global_encoding = header.global_encoding;
				curHeader.project_ID_GUID_data_1 = header.project_ID_GUID_data_1;
				curHeader.project_ID_GUID_data_2 = header.project_ID_GUID_data_2;
				curHeader.project_ID_GUID_data_3 = header.project_ID_GUID_data_3;
				Array.Copy(header.project_ID_GUID_data_4, curHeader.project_ID_GUID_data_4, 8);
				curHeader.version_major = header.version_major;
				curHeader.version_minor = header.version_minor;
				Array.Copy(header.system_identifier, curHeader.system_identifier, 32);
				Array.Copy(header.generating_software, curHeader.generating_software, 32);
				curHeader.file_creation_day = header.file_creation_day;
				curHeader.file_creation_year = header.file_creation_year;
				curHeader.header_size = header.header_size;
				curHeader.offset_to_point_data = header.offset_to_point_data;
				curHeader.number_of_variable_length_records = header.number_of_variable_length_records;
				curHeader.point_data_format = header.point_data_format;
				curHeader.point_data_record_length = header.point_data_record_length;
				curHeader.number_of_point_records = header.number_of_point_records;
				for (int i = 0; i < 5; i++) curHeader.number_of_points_by_return[i] = header.number_of_points_by_return[i];
				curHeader.x_scale_factor = header.x_scale_factor;
				curHeader.y_scale_factor = header.y_scale_factor;
				curHeader.z_scale_factor = header.z_scale_factor;
				curHeader.x_offset = header.x_offset;
				curHeader.y_offset = header.y_offset;
				curHeader.z_offset = header.z_offset;
				curHeader.max_x = header.max_x;
				curHeader.min_x = header.min_x;
				curHeader.max_y = header.max_y;
				curHeader.min_y = header.min_y;
				curHeader.max_z = header.max_z;
				curHeader.min_z = header.min_z;

				if (curHeader.version_minor >= 3)
				{
					curHeader.start_of_waveform_data_packet_record = header.start_of_first_extended_variable_length_record;
				}

				if (curHeader.version_minor >= 4)
				{
					curHeader.start_of_first_extended_variable_length_record = header.start_of_first_extended_variable_length_record;
					curHeader.number_of_extended_variable_length_records = header.number_of_extended_variable_length_records;
					curHeader.extended_number_of_point_records = header.extended_number_of_point_records;
					for (int i = 0; i < 15; i++) curHeader.extended_number_of_points_by_return[i] = header.extended_number_of_points_by_return[i];
				}

				curHeader.user_data_in_header_size = header.user_data_in_header_size;
				curHeader.user_data_in_header = null;

				if (header.user_data_in_header_size != 0)
				{
					if (header.user_data_in_header == null)
					{
						error = string.Format("header->user_data_in_header_size is {0} but header->user_data_in_header is NULL", header.user_data_in_header_size);
						return 1;
					}

					curHeader.user_data_in_header = new byte[header.user_data_in_header_size];
					Array.Copy(header.user_data_in_header, curHeader.user_data_in_header, header.user_data_in_header_size);
				}

				curHeader.vlrs = null;
				if (header.number_of_variable_length_records != 0)
				{
					curHeader.vlrs = new List<vlr>((int)header.number_of_variable_length_records);
					for (int i = 0; i < header.number_of_variable_length_records; i++)
					{
						curHeader.vlrs.Add(new vlr());
						curHeader.vlrs[i].reserved = header.vlrs[i].reserved;
						Array.Copy(header.vlrs[i].user_id, curHeader.vlrs[i].user_id, 16);
						curHeader.vlrs[i].record_id = header.vlrs[i].record_id;
						curHeader.vlrs[i].record_length_after_header = header.vlrs[i].record_length_after_header;
						Array.Copy(header.vlrs[i].description, curHeader.vlrs[i].description, 32);
						if (header.vlrs[i].record_length_after_header != 0)
						{
							if (header.vlrs[i].data == null)
							{
								error = string.Format("header->vlrs[{0}].record_length_after_header is {1} but header->vlrs[{2}].data is NULL", i, header.vlrs[i].record_length_after_header, i);
								return 1;
							}
							curHeader.vlrs[i].data = new byte[header.vlrs[i].record_length_after_header];
							Array.Copy(header.vlrs[i].data, curHeader.vlrs[i].data, header.vlrs[i].record_length_after_header);
						}
						else
						{
							curHeader.vlrs[i].data = null;
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

				curHeader.user_data_after_header_size = header.user_data_after_header_size;
				curHeader.user_data_after_header = null;
				if (header.user_data_after_header_size != 0)
				{
					if (header.user_data_after_header == null)
					{
						error=string.Format("header->user_data_after_header_size is {0} but header->user_data_after_header is NULL", header.user_data_after_header_size);
						return 1;
					}
					curHeader.user_data_after_header = new byte[header.user_data_after_header_size];
					Array.Copy(header.user_data_after_header, curHeader.user_data_after_header, header.user_data_after_header_size);
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
				curHeader.point_data_format = point_type;
				curHeader.point_data_record_length = point_size;
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
				int quant_min_x = MyDefs.I32_QUANTIZE((curHeader.min_x - curHeader.x_offset) / curHeader.x_scale_factor);
				int quant_max_x = MyDefs.I32_QUANTIZE((curHeader.max_x - curHeader.x_offset) / curHeader.x_scale_factor);
				int quant_min_y = MyDefs.I32_QUANTIZE((curHeader.min_y - curHeader.y_offset) / curHeader.y_scale_factor);
				int quant_max_y = MyDefs.I32_QUANTIZE((curHeader.max_y - curHeader.y_offset) / curHeader.y_scale_factor);
				int quant_min_z = MyDefs.I32_QUANTIZE((curHeader.min_z - curHeader.z_offset) / curHeader.z_scale_factor);
				int quant_max_z = MyDefs.I32_QUANTIZE((curHeader.max_z - curHeader.z_offset) / curHeader.z_scale_factor);

				double dequant_min_x = curHeader.x_scale_factor * quant_min_x + curHeader.x_offset;
				double dequant_max_x = curHeader.x_scale_factor * quant_max_x + curHeader.x_offset;
				double dequant_min_y = curHeader.y_scale_factor * quant_min_y + curHeader.y_offset;
				double dequant_max_y = curHeader.y_scale_factor * quant_max_y + curHeader.y_offset;
				double dequant_min_z = curHeader.z_scale_factor * quant_min_z + curHeader.z_offset;
				double dequant_max_z = curHeader.z_scale_factor * quant_max_z + curHeader.z_offset;

				// make sure that there is not sign flip (a 32-bit integer overflow) for the bounding box
				if ((curHeader.min_x > 0) != (dequant_min_x > 0))
				{
					error = string.Format("quantization sign flip for min_x from {0} to {1}. set scale factor for x coarser than {2}", curHeader.min_x, dequant_min_x, curHeader.x_scale_factor);
					return 1;
				}
				if ((curHeader.max_x > 0) != (dequant_max_x > 0))
				{
					error = string.Format("quantization sign flip for max_x from {0} to {1}. set scale factor for x coarser than {2}", curHeader.max_x, dequant_max_x, curHeader.x_scale_factor);
					return 1;
				}
				if ((curHeader.min_y > 0) != (dequant_min_y > 0))
				{
					error = string.Format("quantization sign flip for min_y from {0} to {1}. set scale factor for y coarser than {2}", curHeader.min_y, dequant_min_y, curHeader.y_scale_factor);
					return 1;
				}
				if ((curHeader.max_y > 0) != (dequant_max_y > 0))
				{
					error = string.Format("quantization sign flip for max_y from {0} to {1}. set scale factor for y coarser than {2}", curHeader.max_y, dequant_max_y, curHeader.y_scale_factor);
					return 1;
				}
				if ((curHeader.min_z > 0) != (dequant_min_z > 0))
				{
					error = string.Format("quantization sign flip for min_z from {0} to {1}. set scale factor for z coarser than {2}", curHeader.min_z, dequant_min_z, curHeader.z_scale_factor);
					return 1;
				}
				if ((curHeader.max_z > 0) != (dequant_max_z > 0))
				{
					error = string.Format("quantization sign flip for max_z from {0} to {1}. set scale factor for z coarser than {2}", curHeader.max_z, dequant_max_z, curHeader.z_scale_factor);
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
				double x_scale_factor = curHeader.x_scale_factor;
				double y_scale_factor = curHeader.y_scale_factor;
				double z_scale_factor = curHeader.z_scale_factor;

				if ((x_scale_factor <= 0) || double.IsInfinity(x_scale_factor))
				{
					error = string.Format("invalid x scale_factor {0} in header", curHeader.x_scale_factor);
					return 1;
				}

				if ((y_scale_factor <= 0) || double.IsInfinity(y_scale_factor))
				{
					error = string.Format("invalid y scale_factor {0} in header", curHeader.y_scale_factor);
					return 1;
				}

				if ((z_scale_factor <= 0) || double.IsInfinity(z_scale_factor))
				{
					error = string.Format("invalid z scale_factor {0} in header", curHeader.z_scale_factor);
					return 1;
				}

				double center_bb_x = (curHeader.min_x + curHeader.max_x) / 2;
				double center_bb_y = (curHeader.min_y + curHeader.max_y) / 2;
				double center_bb_z = (curHeader.min_z + curHeader.max_z) / 2;

				if (double.IsInfinity(center_bb_x))
				{
					error = string.Format("invalid x coordinate at center of bounding box (min: {0} max: {1})", curHeader.min_x, curHeader.max_x);
					return 1;
				}

				if (double.IsInfinity(center_bb_y))
				{
					error = string.Format("invalid y coordinate at center of bounding box (min: {0} max: {1})", curHeader.min_y, curHeader.max_y);
					return 1;
				}

				if (double.IsInfinity(center_bb_z))
				{
					error = string.Format("invalid z coordinate at center of bounding box (min: {0} max: {1})", curHeader.min_z, curHeader.max_z);
					return 1;
				}

				double x_offset = curHeader.x_offset;
				double y_offset = curHeader.y_offset;
				double z_offset = curHeader.z_offset;

				curHeader.x_offset = (MyDefs.I64_FLOOR(center_bb_x / x_scale_factor / 10000000)) * 10000000 * x_scale_factor;
				curHeader.y_offset = (MyDefs.I64_FLOOR(center_bb_y / y_scale_factor / 10000000)) * 10000000 * y_scale_factor;
				curHeader.z_offset = (MyDefs.I64_FLOOR(center_bb_z / z_scale_factor / 10000000)) * 10000000 * z_scale_factor;

				if (check_for_integer_overflow() != 0)
				{
					curHeader.x_offset = x_offset;
					curHeader.y_offset = y_offset;
					curHeader.z_offset = z_offset;
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

		public int set_point(point point)
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
				curPoint.classification_and_classification_flags = point.classification_and_classification_flags;
				curPoint.edge_of_flight_line = point.edge_of_flight_line;
				curPoint.extended_classification = point.extended_classification;
				curPoint.extended_classification_flags = point.extended_classification_flags;
				curPoint.extended_number_of_returns = point.extended_number_of_returns;
				curPoint.extended_point_type = point.extended_point_type;
				curPoint.extended_return_number = point.extended_return_number;
				curPoint.extended_scan_angle = point.extended_scan_angle;
				curPoint.extended_scanner_channel = point.extended_scanner_channel;
				curPoint.gps_time = point.gps_time;
				curPoint.intensity = point.intensity;
				curPoint.num_extra_bytes = point.num_extra_bytes;
				curPoint.number_of_returns = point.number_of_returns;
				curPoint.point_source_ID = point.point_source_ID;
				curPoint.return_number = point.return_number;
				Array.Copy(point.rgb, curPoint.rgb, 4);
				curPoint.scan_angle_rank = point.scan_angle_rank;
				curPoint.scan_direction_flag = point.scan_direction_flag;
				curPoint.user_data = point.user_data;
				curPoint.X = point.X;
				curPoint.Y = point.Y;
				curPoint.Z = point.Z;
				Array.Copy(point.wave_packet, curPoint.wave_packet, 29);

				if (curPoint.extra_bytes != null)
				{
					if (point.extra_bytes != null)
					{
						if (curPoint.num_extra_bytes == point.num_extra_bytes)
						{
							Array.Copy(point.extra_bytes, curPoint.extra_bytes, point.num_extra_bytes);
						}
						else
						{
							error = string.Format("target point has {0} extra bytes but source point has {1}", curPoint.num_extra_bytes, point.num_extra_bytes);
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
				curPoint.X = MyDefs.I32_QUANTIZE((coordinates[0] - curHeader.x_offset) / curHeader.x_scale_factor);
				curPoint.Y = MyDefs.I32_QUANTIZE((coordinates[1] - curHeader.y_offset) / curHeader.y_scale_factor);
				curPoint.Z = MyDefs.I32_QUANTIZE((coordinates[2] - curHeader.z_offset) / curHeader.z_scale_factor);
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
				coordinates[0] = curHeader.x_scale_factor * curPoint.X + curHeader.x_offset;
				coordinates[1] = curHeader.y_scale_factor * curPoint.Y + curHeader.y_offset;
				coordinates[2] = curHeader.z_scale_factor * curPoint.Z + curHeader.z_offset;
			}
			catch
			{
				error = "internal error in laszip_get_coordinates";
				return 1;
			}

			error = warning = "";
			return 0;
		}

		public unsafe int set_geokeys(ushort number, geokey[] key_entries)
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
				byte[] buffer = new byte[sizeof(geokey) * (number + 1)];

				fixed (byte* pBuffer = buffer)
				{
					geokey* key_entries_plus_one = (geokey*)pBuffer;

					key_entries_plus_one[0].key_id = 1;            // aka key_directory_version
					key_entries_plus_one[0].tiff_tag_location = 1; // aka key_revision
					key_entries_plus_one[0].count = 0;             // aka minor_revision
					key_entries_plus_one[0].value_offset = number; // aka number_of_keys
					for (int i = 0; i < number; i++) key_entries_plus_one[i + 1] = key_entries[i];
				}

				// fill a VLR
				vlr vlr = new vlr();
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
				vlr vlr = new vlr();
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
				vlr vlr = new vlr();
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
				lasattribute.set_scale(scale, 0);
				lasattribute.set_offset(offset, 0);

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
				vlr vlr = new vlr();
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

		public int add_vlr(vlr vlr)
		{
			if (vlr == null)
			{
				error = "laszip_vlr_struct pointer 'vlr' is zero";
				return 1;
			}

			if ((vlr.record_length_after_header > 0) && (vlr.data == null))
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
				if (curHeader.vlrs.Count > 0)
				{
					// remove existing VLRs with same record and user id
					for (int i = (int)curHeader.number_of_variable_length_records - 1; i >= 0; i--)
					{
						if (curHeader.vlrs[i].record_id == vlr.record_id && strncmp(curHeader.vlrs[i].user_id, vlr.user_id, 16))
						{
							if (curHeader.vlrs[i].record_length_after_header != 0)
								curHeader.offset_to_point_data -= curHeader.vlrs[i].record_length_after_header;

							curHeader.offset_to_point_data -= 54;
							curHeader.vlrs.RemoveAt(i);
						}
					}
				}

				if (vlr.description[0] == 0)
				{
					// description field must be a null-terminate string, so we don't copy more than 31 characters
					byte[] v = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
					Array.Copy(v, vlr.description, Math.Min(v.Length, 31));
				}

				curHeader.vlrs.Add(vlr);
				curHeader.number_of_variable_length_records = (uint)curHeader.vlrs.Count;
				curHeader.offset_to_point_data += 54;

				// copy the VLR
				curHeader.offset_to_point_data += vlr.record_length_after_header;
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
				if (curHeader.number_of_variable_length_records != 0)
				{
					bool found = false;
					for (int i = (int)curHeader.number_of_variable_length_records - 1; i >= 0; i--)
					{
						if (curHeader.vlrs[i].record_id == record_id && strncmp(curHeader.vlrs[i].user_id, user_id, 16))
						{
							found = true;

							if (curHeader.vlrs[i].record_length_after_header != 0)
								curHeader.offset_to_point_data -= curHeader.vlrs[i].record_length_after_header;

							curHeader.offset_to_point_data -= 54;
							curHeader.vlrs.RemoveAt(i);
						}
					}

					if (!found)
					{
						error = string.Format("cannot find VLR with user_id '{0}' and record_id {1} among the {2} VLRs in the header", Encoding.ASCII.GetString(user_id), record_id, curHeader.number_of_variable_length_records);
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
				if (curHeader.number_of_variable_length_records != 0)
				{
					bool found = false;
					for (int i = (int)curHeader.number_of_variable_length_records - 1; i >= 0; i--)
					{
						if (curHeader.vlrs[i].record_id == record_id && strcmp(curHeader.vlrs[i].user_id, user_id))
						{
							found = true;
							if (curHeader.vlrs[i].record_length_after_header != 0)
								curHeader.offset_to_point_data -= curHeader.vlrs[i].record_length_after_header;

							curHeader.offset_to_point_data -= 54;
							curHeader.vlrs.RemoveAt(i);
						}
					}

					if (!found)
					{
						error = string.Format("cannot find VLR with user_id '{0}' and record_id {1} among the {2} VLRs in the header", user_id, record_id, curHeader.number_of_variable_length_records);
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
			if ((curHeader.version_major != 1) || (curHeader.version_minor > 4))
			{
				error = string.Format("unknown LAS version {0}.{1}", curHeader.version_major, curHeader.version_minor);
				return 1;
			}

			// check counters
			if (curHeader.point_data_format > 5)
			{
				// legacy counters are zero for new point types

				curHeader.number_of_point_records = 0;
				for (int i = 0; i < 5; i++)
				{
					curHeader.number_of_points_by_return[i] = 0;
				}
			}
			else if (curHeader.version_minor > 3)
			{
				// legacy counters must be zero or consistent for old point types
				if (curHeader.number_of_point_records != curHeader.extended_number_of_point_records)
				{
					if (curHeader.number_of_point_records != 0)
					{
						error = string.Format("inconsistent number_of_point_records {0} and extended_number_of_point_records {1}", curHeader.number_of_point_records, curHeader.extended_number_of_point_records);
						return 1;
					}
					else if (curHeader.extended_number_of_point_records <= uint.MaxValue)
					{
						curHeader.number_of_point_records = (uint)curHeader.extended_number_of_point_records;
					}
				}

				for (int i = 0; i < 5; i++)
				{
					if (curHeader.number_of_points_by_return[i] != curHeader.extended_number_of_points_by_return[i])
					{
						if (curHeader.number_of_points_by_return[i] != 0)
						{
							error = string.Format("inconsistent number_of_points_by_return[{0}] {1} and extended_number_of_points_by_return[{0}] {2}", i, curHeader.number_of_points_by_return[i], curHeader.extended_number_of_points_by_return[i]);
							return 1;
						}
						else if (curHeader.extended_number_of_points_by_return[i] <= uint.MaxValue)
						{
							curHeader.number_of_points_by_return[i] = (uint)curHeader.extended_number_of_points_by_return[i];
						}
					}
				}
			}

			return 0;
		}

		//------- next to check
		//int prepare_point_for_write(bool compress)

		int prepare_vlrs_for_write()
		{
			uint vlrs_size = 0;

			if (curHeader.number_of_variable_length_records != 0)
			{
				if (curHeader.vlrs == null)
				{
					error = string.Format("number_of_variable_length_records is {0} but vlrs pointer is zero", curHeader.number_of_variable_length_records);
					return 1;
				}

				for (int i = 0; i < curHeader.number_of_variable_length_records; i++)
				{
					vlrs_size += 54;
					if (curHeader.vlrs[i].record_length_after_header != 0)
					{
						if (curHeader.vlrs == null)
						{
							error = string.Format("vlrs[{0}].record_length_after_header is {1} but vlrs[{0}].data pointer is zero", i, curHeader.vlrs[i].record_length_after_header);
							return 1;
						}
						vlrs_size += curHeader.vlrs[i].record_length_after_header;
					}
				}
			}

			if ((vlrs_size + curHeader.header_size + curHeader.user_data_after_header_size) != curHeader.offset_to_point_data)
			{
				error = string.Format("header_size ({0}) plus vlrs_size ({1}) plus user_data_after_header_size ({2}) does not equal offset_to_point_data ({3})", 
					curHeader.header_size, vlrs_size, curHeader.user_data_after_header_size, curHeader.offset_to_point_data);
				return 1;
			}

			return 0;
		}

		static uint vrl_payload_size(LASzip laszip)
		{
			return 34u + (6u * laszip.num_items);
		}

		int write_laszip_vlr_header(LASzip laszip)
		{
			// write the LASzip VLR header
			ushort reserved = 0;
			try { streamout.Write(BitConverter.GetBytes(reserved), 0, 2); }
			catch { error = "writing LASzip VLR header.reserved"; return 1; }

			byte[] user_id1 = Encoding.ASCII.GetBytes("laszip encoded");
			byte[] user_id = new byte[16];
			Array.Copy(user_id1, user_id, Math.Min(16, user_id1.Length));
			try { streamout.Write(user_id, 0, 16); }
			catch { error = "writing LASzip VLR header.user_id"; return 1; }

			ushort record_id = 22204;
			try { streamout.Write(BitConverter.GetBytes(record_id), 0, 2); }
			catch { error = "writing LASzip VLR header.record_id"; return 1; }

			ushort record_length_after_header = (ushort)vrl_payload_size(laszip);
			try { streamout.Write(BitConverter.GetBytes(record_length_after_header), 0, 2); }
			catch { error = "writing LASzip VLR header.record_length_after_header"; return 1; }

			// description field must be a null-terminate string, so we don't copy more than 31 characters
			byte[] description1 = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
			byte[] description = new byte[32];
			Array.Copy(description1, description, Math.Min(31, description1.Length));

			try { streamout.Write(description, 0, 32); }
			catch { error = "writing LASzip VLR header.description"; return 1; }

			return 0;
		}

		int write_laszip_vlr_payload(LASzip laszip)
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

			return 0;
		}

		int laszip_write_header(LASzip laszip, bool compress)
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
				error = "writing Header.file_signature";
				return 1;
			}

			try { streamout.Write(BitConverter.GetBytes(curHeader.file_source_ID), 0, 2); }
			catch { error = "writing Header.file_source_ID"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.global_encoding), 0, 2); }
			catch { error = "writing Header.global_encoding"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.project_ID_GUID_data_1), 0, 4); }
			catch { error = "writing Header.project_ID_GUID_data_1"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.project_ID_GUID_data_2), 0, 2); }
			catch { error = "writing Header.project_ID_GUID_data_2"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.project_ID_GUID_data_3), 0, 2); }
			catch { error = "writing Header.project_ID_GUID_data_3"; return 1; }

			try { streamout.Write(curHeader.project_ID_GUID_data_4, 0, 8); }
			catch { error = "writing Header.project_ID_GUID_data_4"; return 1; }

			try { streamout.WriteByte(curHeader.version_major); }
			catch { error = "writing Header.version_major"; return 1; }

			try { streamout.WriteByte(curHeader.version_minor); }
			catch { error = "writing Header.version_minor"; return 1; }

			try { streamout.Write(curHeader.system_identifier, 0, 32); }
			catch { error = "writing Header.system_identifier"; return 1; }

			if (!m_preserve_generating_software)
			{
				byte[] generatingSoftware = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(generatingSoftware, curHeader.generating_software, Math.Min(generatingSoftware.Length, 32));
			}

			try { streamout.Write(curHeader.generating_software, 0, 32); }
			catch { error = "writing Header.generating_software"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.file_creation_day), 0, 2); }
			catch { error = "writing Header.file_creation_day"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.file_creation_year), 0, 2); }
			catch { error = "writing Header.file_creation_year"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.header_size), 0, 2); }
			catch { error = "writing Header.header_size"; return 1; }

			if (compress) curHeader.offset_to_point_data += 54 + vrl_payload_size(laszip);

			try { streamout.Write(BitConverter.GetBytes(curHeader.offset_to_point_data), 0, 4); }
			catch { error = "writing Header.offset_to_point_data"; return 1; }

			if (compress)
			{
				curHeader.offset_to_point_data -= 54 + vrl_payload_size(laszip);
				curHeader.number_of_variable_length_records += 1;
			}

			try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_variable_length_records), 0, 4); }
			catch { error = "writing Header.number_of_variable_length_records"; return 1; }

			if (compress)
			{
				curHeader.number_of_variable_length_records -= 1;
				curHeader.point_data_format |= 128;
			}

			try { streamout.WriteByte(curHeader.point_data_format); }
			catch { error = "writing Header.point_data_format"; return 1; }

			if (compress) curHeader.point_data_format &= 127;

			try { streamout.Write(BitConverter.GetBytes(curHeader.point_data_record_length), 0, 2); }
			catch { error = "writing Header.point_data_record_length"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_point_records), 0, 4); }
			catch { error = "writing Header.number_of_point_records"; return 1; }

			for (uint i = 0; i < 5; i++)
			{
				try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_points_by_return[i]), 0, 4); }
				catch { error = string.Format("writing Header.number_of_points_by_return {0}", i); return 1; }
			}

			try { streamout.Write(BitConverter.GetBytes(curHeader.x_scale_factor), 0, 8); }
			catch { error = "writing Header.x_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.y_scale_factor), 0, 8); }
			catch { error = "writing Header.y_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.z_scale_factor), 0, 8); }
			catch { error = "writing Header.z_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.x_offset), 0, 8); }
			catch { error = "writing Header.x_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.y_offset), 0, 8); }
			catch { error = "writing Header.y_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.z_offset), 0, 8); }
			catch { error = "writing Header.z_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.max_x), 0, 8); }
			catch { error = "writing Header.max_x"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.min_x), 0, 8); }
			catch { error = "writing Header.min_x"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.max_y), 0, 8); }
			catch { error = "writing Header.max_y"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.min_y), 0, 8); }
			catch { error = "writing Header.min_y"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.max_z), 0, 8); }
			catch { error = "writing Header.max_z"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.min_z), 0, 8); }
			catch { error = "writing Header.min_z"; return 1; }

			#region special handling for LAS 1.3+
			if (curHeader.version_major == 1 && curHeader.version_minor >= 3)
			{
				if (curHeader.header_size < 235)
				{
					error = string.Format("for LAS 1.{0} header_size should at least be 235 but it is only {1}", curHeader.version_minor, curHeader.header_size);
					return 1;
				}

				if (curHeader.start_of_waveform_data_packet_record != 0)
				{
					warning=string.Format("header.start_of_waveform_data_packet_record is {0}. writing 0 instead.", curHeader.start_of_waveform_data_packet_record);
					curHeader.start_of_waveform_data_packet_record = 0;
				}

				try { streamout.Write(BitConverter.GetBytes(curHeader.start_of_waveform_data_packet_record), 0, 8); }
				catch { error = "writing Header.start_of_waveform_data_packet_record"; return 1; }

				curHeader.user_data_in_header_size = curHeader.header_size - 235u;
			}
			else curHeader.user_data_in_header_size = curHeader.header_size - 227u;
			#endregion

			#region special handling for LAS 1.4+
			if (curHeader.version_major == 1 && curHeader.version_minor >= 4)
			{
				if (curHeader.header_size < 375)
				{
					error = string.Format("for LAS 1.{0} header_size should at least be 375 but it is only {1}", curHeader.version_minor, curHeader.header_size);
					return 1;
				}

				try { streamout.Write(BitConverter.GetBytes(curHeader.start_of_first_extended_variable_length_record), 0, 8); }
				catch { error = "writing Header.start_of_first_extended_variable_length_record"; return 1; }

				try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_extended_variable_length_records), 0, 4); }
				catch { error = "writing Header.number_of_extended_variable_length_records"; return 1; }

				try { streamout.Write(BitConverter.GetBytes(curHeader.extended_number_of_point_records), 0, 8); }
				catch { error = "writing Header.extended_number_of_point_records"; return 1; }

				for (uint i = 0; i < 15; i++)
				{
					try { streamout.Write(BitConverter.GetBytes(curHeader.extended_number_of_points_by_return[i]), 0, 8); }
					catch { error = string.Format("writing Header.extended_number_of_points_by_return[{0}]", i); return 1; }
				}

				curHeader.user_data_in_header_size = curHeader.header_size - 375u;
			}
			#endregion

			#region write any number of user-defined bytes that might have been added to the header
			if (curHeader.user_data_in_header_size != 0)
			{
				try { streamout.Write(curHeader.user_data_in_header, 0, (int)curHeader.user_data_in_header_size); }
				catch { error = string.Format("writing {0} bytes of data into Header.user_data_in_header", curHeader.user_data_in_header_size); return 1; }
			}
			#endregion

			#region write variable length records into the header
			if (curHeader.number_of_variable_length_records != 0)
			{
				for (int i = 0; i < curHeader.number_of_variable_length_records; i++)
				{
					// write variable length records variable after variable (to avoid alignment issues)
					try { streamout.Write(BitConverter.GetBytes(curHeader.vlrs[i].reserved), 0, 2); }
					catch { error = string.Format("writing header.vlrs[{0}].reserved", i); return 1; }

					try { streamout.Write(curHeader.vlrs[i].user_id, 0, 16); }
					catch { error = string.Format("writing header.vlrs[{0}].user_id", i); return 1; }

					try { streamout.Write(BitConverter.GetBytes(curHeader.vlrs[i].record_id), 0, 2); }
					catch { error = string.Format("writing header.vlrs[{0}].record_id", i); return 1; }

					try { streamout.Write(BitConverter.GetBytes(curHeader.vlrs[i].record_length_after_header), 0, 2); }
					catch { error = string.Format("writing header.vlrs[{0}].record_length_after_header", i); return 1; }

					try { streamout.Write(curHeader.vlrs[i].description, 0, 32); }
					catch { error = string.Format("writing header.vlrs[{0}].description", i); return 1; }

					// write data following the header of the variable length record
					if (curHeader.vlrs[i].record_length_after_header != 0)
					{
						try { streamout.Write(curHeader.vlrs[i].data, 0, curHeader.vlrs[i].record_length_after_header); }
						catch { error = string.Format("writing {0} bytes of data into header.vlrs[{1}].data", curHeader.vlrs[i].record_length_after_header, i); return 1; }
					}
				}
			}

			if (compress)
			{
				// write the LASzip VLR header
				if (write_laszip_vlr_header(laszip) != 0) return 1;

				// write the LASzip VLR payload
				if (write_laszip_vlr_payload(laszip) != 0) return 1;
			}
			#endregion

			#region write any number of user-defined bytes that might have been added after the header
			if (curHeader.user_data_after_header_size != 0)
			{
				try { streamout.Write(curHeader.user_data_after_header, 0, (int)curHeader.user_data_after_header_size); }
				catch { error = string.Format("writing {0} bytes of data into header.user_data_after_header", curHeader.user_data_after_header_size); return 1; }
			}
			#endregion

			#endregion

			return 0;
		}

		int create_point_writer(LASzip laszip)
		{
			// create the point writer
			writer = new LASwritePoint();
			if (writer == null)
			{
				error = "could not alloc LASwritePoint";
				return 1;
			}

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

		//int setup_laszip_items(LASzip laszip, bool compress)

		// TODO move
		static int CheckHeaderAndSetup(header curHeader, bool compress, out LASzip laszip, point curPoint, out uint laszip_vrl_payload_size, out string error)
		{
			laszip = null;
			laszip_vrl_payload_size = 0;
			error = null;

			#region check header and prepare point

			uint vlrs_size = 0;

			if (curHeader.version_major != 1)
			{
				error = string.Format("unknown LAS version {0}.{1}", curHeader.version_major, curHeader.version_minor);
				return 1;
			}

			if (compress && (curHeader.point_data_format > 5))
			{
				error = string.Format("compressor does not yet support point data format {1}", curHeader.point_data_format);
				return 1;
			}

			if (curHeader.number_of_variable_length_records != 0)
			{
				if (curHeader.vlrs == null)
				{
					error = string.Format("number_of_variable_length_records is {0} but vlrs pointer is zero", curHeader.number_of_variable_length_records);
					return 1;
				}

				for (int i = 0; i < curHeader.number_of_variable_length_records; i++)
				{
					vlrs_size += 54;
					if (curHeader.vlrs[i].record_length_after_header != 0)
					{
						if (curHeader.vlrs == null)
						{
							error = string.Format("vlrs[{0}].record_length_after_header is {1} but vlrs[{0}].data pointer is zero", i, curHeader.vlrs[i].record_length_after_header);
							return 1;
						}
						vlrs_size += curHeader.vlrs[i].record_length_after_header;
					}
				}
			}

			if ((vlrs_size + curHeader.header_size + curHeader.user_data_after_header_size) != curHeader.offset_to_point_data)
			{
				error = string.Format("header_size ({0}) plus vlrs_size ({1}) plus user_data_after_header_size ({2}) does not equal offset_to_point_data ({3})", curHeader.header_size, vlrs_size, curHeader.user_data_after_header_size, curHeader.offset_to_point_data);
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

			if (!laszip.setup(curHeader.point_data_format, curHeader.point_data_record_length, LASzip.COMPRESSOR_NONE))
			{
				error = string.Format("invalid combination of point_data_format {0} and point_data_record_length {1}", curHeader.point_data_format, curHeader.point_data_record_length);
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
						curPoint.num_extra_bytes = laszip.items[i].size;
						curPoint.extra_bytes = new byte[curPoint.num_extra_bytes];
						break;
					default:
						error = string.Format("unknown LASitem type {0}", laszip.items[i].type);
						return 1;
				}
			}
			#endregion

			if (compress)
			{
				if (!laszip.setup(curHeader.point_data_format, curHeader.point_data_record_length, LASzip.COMPRESSOR_DEFAULT))
				{
					error = string.Format("cannot compress point_data_format {0} with point_data_record_length {1}", curHeader.point_data_format, curHeader.point_data_record_length);
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

		public int open_writer(Stream streamout, bool compress, bool leaveOpen = false)
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

				int err = CheckHeaderAndSetup(curHeader, compress, out laszip, curPoint, out laszip_vrl_payload_size, out error);
				if (err != 0) return err;

				this.streamout = streamout;
				leaveStreamOutOpen = leaveOpen;

				return open_writer_stream(compress, laszip, laszip_vrl_payload_size);
			}
			catch
			{
				error = string.Format("internal error in laszip_open_writer (Stream)");
				return 1;
			}
		}

		public int open_writer(string file_name, bool compress)
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

				int err = CheckHeaderAndSetup(curHeader, compress, out laszip, curPoint, out laszip_vrl_payload_size, out error);
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

				return open_writer_stream(compress, laszip, laszip_vrl_payload_size);
			}
			catch
			{
				error = string.Format("internal error in laszip_open_writer '{0}'", file_name);
				return 1;
			}
		}

		// TODO move?
		int open_writer_stream(bool compress, LASzip laszip, uint laszip_vrl_payload_size)
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
				error = "writing Header.file_signature";
				return 1;
			}

			try { streamout.Write(BitConverter.GetBytes(curHeader.file_source_ID), 0, 2); }
			catch { error = "writing Header.file_source_ID"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.global_encoding), 0, 2); }
			catch { error = "writing Header.global_encoding"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.project_ID_GUID_data_1), 0, 4); }
			catch { error = "writing Header.project_ID_GUID_data_1"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.project_ID_GUID_data_2), 0, 2); }
			catch { error = "writing Header.project_ID_GUID_data_2"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.project_ID_GUID_data_3), 0, 2); }
			catch { error = "writing Header.project_ID_GUID_data_3"; return 1; }

			try { streamout.Write(curHeader.project_ID_GUID_data_4, 0, 8); }
			catch { error = "writing Header.project_ID_GUID_data_4"; return 1; }

			try { streamout.WriteByte(curHeader.version_major); }
			catch { error = "writing Header.version_major"; return 1; }

			try { streamout.WriteByte(curHeader.version_minor); }
			catch { error = "writing Header.version_minor"; return 1; }

			try { streamout.Write(curHeader.system_identifier, 0, 32); }
			catch { error = "writing Header.system_identifier"; return 1; }

			if (curHeader.generating_software == null || curHeader.generating_software.Length != 32)
			{
				byte[] generatingSoftware = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				Array.Copy(generatingSoftware, curHeader.generating_software, Math.Min(generatingSoftware.Length, 32));
			}

			try { streamout.Write(curHeader.generating_software, 0, 32); }
			catch { error = "writing Header.generating_software"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.file_creation_day), 0, 2); }
			catch { error = "writing Header.file_creation_day"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.file_creation_year), 0, 2); }
			catch { error = "writing Header.file_creation_year"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.header_size), 0, 2); }
			catch { error = "writing Header.header_size"; return 1; }

			if (compress) curHeader.offset_to_point_data += (54 + laszip_vrl_payload_size);

			try { streamout.Write(BitConverter.GetBytes(curHeader.offset_to_point_data), 0, 4); }
			catch { error = "writing Header.offset_to_point_data"; return 1; }

			if (compress)
			{
				curHeader.offset_to_point_data -= (54 + laszip_vrl_payload_size);
				curHeader.number_of_variable_length_records += 1;
			}

			try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_variable_length_records), 0, 4); }
			catch { error = "writing Header.number_of_variable_length_records"; return 1; }

			if (compress)
			{
				curHeader.number_of_variable_length_records -= 1;
				curHeader.point_data_format |= 128;
			}

			try { streamout.WriteByte(curHeader.point_data_format); }
			catch { error = "writing Header.point_data_format"; return 1; }

			if (compress) curHeader.point_data_format &= 127;

			try { streamout.Write(BitConverter.GetBytes(curHeader.point_data_record_length), 0, 2); }
			catch { error = "writing Header.point_data_record_length"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_point_records), 0, 4); }
			catch { error = "writing Header.number_of_point_records"; return 1; }

			for (uint i = 0; i < 5; i++)
			{
				try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_points_by_return[i]), 0, 4); }
				catch { error = string.Format("writing Header.number_of_points_by_return {0}", i); return 1; }
			}

			try { streamout.Write(BitConverter.GetBytes(curHeader.x_scale_factor), 0, 8); }
			catch { error = "writing Header.x_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.y_scale_factor), 0, 8); }
			catch { error = "writing Header.y_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.z_scale_factor), 0, 8); }
			catch { error = "writing Header.z_scale_factor"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.x_offset), 0, 8); }
			catch { error = "writing Header.x_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.y_offset), 0, 8); }
			catch { error = "writing Header.y_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.z_offset), 0, 8); }
			catch { error = "writing Header.z_offset"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.max_x), 0, 8); }
			catch { error = "writing Header.max_x"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.min_x), 0, 8); }
			catch { error = "writing Header.min_x"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.max_y), 0, 8); }
			catch { error = "writing Header.max_y"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.min_y), 0, 8); }
			catch { error = "writing Header.min_y"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.max_z), 0, 8); }
			catch { error = "writing Header.max_z"; return 1; }

			try { streamout.Write(BitConverter.GetBytes(curHeader.min_z), 0, 8); }
			catch { error = "writing Header.min_z"; return 1; }

			#region special handling for LAS 1.3+
			if (curHeader.version_major == 1 && curHeader.version_minor >= 3)
			{
				if (curHeader.header_size < 235)
				{
					error = string.Format("for LAS 1.{0} header_size should at least be 235 but it is only {1}", curHeader.version_minor, curHeader.header_size);
					return 1;
				}

				try { streamout.Write(BitConverter.GetBytes(curHeader.start_of_waveform_data_packet_record), 0, 8); }
				catch { error = "writing Header.start_of_waveform_data_packet_record"; return 1; }

				curHeader.user_data_in_header_size = curHeader.header_size - 235u;
			}
			else curHeader.user_data_in_header_size = curHeader.header_size - 227u;
			#endregion

			#region special handling for LAS 1.4+
			if (curHeader.version_major == 1 && curHeader.version_minor >= 4)
			{
				if (curHeader.header_size < 375)
				{
					error = string.Format("for LAS 1.{0} header_size should at least be 375 but it is only {1}", curHeader.version_minor, curHeader.header_size);
					return 1;
				}

				try { streamout.Write(BitConverter.GetBytes(curHeader.start_of_first_extended_variable_length_record), 0, 8); }
				catch { error = "writing Header.start_of_first_extended_variable_length_record"; return 1; }

				try { streamout.Write(BitConverter.GetBytes(curHeader.number_of_extended_variable_length_records), 0, 4); }
				catch { error = "writing Header.number_of_extended_variable_length_records"; return 1; }

				try { streamout.Write(BitConverter.GetBytes(curHeader.extended_number_of_point_records), 0, 8); }
				catch { error = "writing Header.extended_number_of_point_records"; return 1; }

				for (uint i = 0; i < 15; i++)
				{
					try { streamout.Write(BitConverter.GetBytes(curHeader.extended_number_of_points_by_return[i]), 0, 8); }
					catch { error = string.Format("writing Header.extended_number_of_points_by_return[{0}]", i); return 1; }
				}

				curHeader.user_data_in_header_size = curHeader.header_size - 375u;
			}
			#endregion

			#region write any number of user-defined bytes that might have been added to the header
			if (curHeader.user_data_in_header_size != 0)
			{
				try { streamout.Write(curHeader.user_data_in_header, 0, (int)curHeader.user_data_in_header_size); }
				catch { error = string.Format("writing {0} bytes of data into Header.user_data_in_header", curHeader.user_data_in_header_size); return 1; }
			}
			#endregion

			#region write variable length records into the header
			if (curHeader.number_of_variable_length_records != 0)
			{
				for (int i = 0; i < curHeader.number_of_variable_length_records; i++)
				{
					// write variable length records variable after variable (to avoid alignment issues)
					try { streamout.Write(BitConverter.GetBytes(curHeader.vlrs[i].reserved), 0, 2); }
					catch { error = string.Format("writing Header.vlrs[{0}].reserved", i); return 1; }

					try { streamout.Write(curHeader.vlrs[i].user_id, 0, 16); }
					catch { error = string.Format("writing Header.vlrs[{0}].user_id", i); return 1; }

					try { streamout.Write(BitConverter.GetBytes(curHeader.vlrs[i].record_id), 0, 2); }
					catch { error = string.Format("writing Header.vlrs[{0}].record_id", i); return 1; }

					try { streamout.Write(BitConverter.GetBytes(curHeader.vlrs[i].record_length_after_header), 0, 2); }
					catch { error = string.Format("writing Header.vlrs[{0}].record_length_after_header", i); return 1; }

					try { streamout.Write(curHeader.vlrs[i].description, 0, 32); }
					catch { error = string.Format("writing Header.vlrs[{0}].description", i); return 1; }

					// write data following the header of the variable length record
					if (curHeader.vlrs[i].record_length_after_header != 0)
					{
						try { streamout.Write(curHeader.vlrs[i].data, 0, curHeader.vlrs[i].record_length_after_header); }
						catch { error = string.Format("writing {0} bytes of data into Header.vlrs[{1}].data", curHeader.vlrs[i].record_length_after_header, i); return 1; }
					}
				}
			}

			if (compress)
			{
				#region write the LASzip VLR header
				//uint i = curHeader.number_of_variable_length_records;

				//ushort reserved = 0xAABB;
				//try { streamout.Write(BitConverter.GetBytes(reserved), 0, 2); }
				//catch { error = string.Format("writing Header.vlrs[{0}].reserved", i); return 1; }

				//byte[] user_id1 = Encoding.ASCII.GetBytes("laszip encoded");
				//byte[] user_id = new byte[16];
				//Array.Copy(user_id1, user_id, Math.Min(16, user_id1.Length));
				//try { streamout.Write(user_id, 0, 16); }
				//catch { error = string.Format("writing Header.vlrs[{0}].user_id", i); return 1; }

				//ushort record_id = 22204;
				//try { streamout.Write(BitConverter.GetBytes(record_id), 0, 2); }
				//catch { error = string.Format("writing Header.vlrs[{0}].record_id", i); return 1; }

				//ushort record_length_after_header = (ushort)laszip_vrl_payload_size;
				//try { streamout.Write(BitConverter.GetBytes(record_length_after_header), 0, 2); }
				//catch { error = string.Format("writing Header.vlrs[{0}].record_length_after_header", i); return 1; }

				//// description field must be a null-terminate string, so we don't copy more than 31 characters
				//byte[] description1 = Encoding.ASCII.GetBytes(string.Format("LASzip.net DLL {0}.{1} r{2} ({3})", LASzip.VERSION_MAJOR, LASzip.VERSION_MINOR, LASzip.VERSION_REVISION, LASzip.VERSION_BUILD_DATE));
				//byte[] description = new byte[32];
				//Array.Copy(description1, description, Math.Min(31, description1.Length));

				//try { streamout.Write(description, 0, 32); }
				//catch { error = string.Format("writing Header.vlrs[{0}].description", i); return 1; }

				if (write_laszip_vlr_header(laszip) != 0) return 1;

				//// write the LASzip VLR payload

				////     U16  compressor                2 bytes
				////     U32  coder                     2 bytes
				////     U8   version_major             1 byte
				////     U8   version_minor             1 byte
				////     U16  version_revision          2 bytes
				////     U32  options                   4 bytes
				////     I32  chunk_size                4 bytes
				////     I64  number_of_special_evlrs   8 bytes
				////     I64  offset_to_special_evlrs   8 bytes
				////     U16  num_items                 2 bytes
				////        U16 type                2 bytes * num_items
				////        U16 size                2 bytes * num_items
				////        U16 version             2 bytes * num_items
				//// which totals 34+6*num_items

				//try { streamout.Write(BitConverter.GetBytes(laszip.compressor), 0, 2); }
				//catch { error = string.Format("writing compressor {0}", laszip.compressor); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.coder), 0, 2); }
				//catch { error = string.Format("writing coder {0}", laszip.coder); return 1; }

				//try { streamout.WriteByte(laszip.version_major); }
				//catch { error = string.Format("writing version_major {0}", laszip.version_major); return 1; }

				//try { streamout.WriteByte(laszip.version_minor); }
				//catch { error = string.Format("writing version_minor {0}", laszip.version_minor); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.version_revision), 0, 2); }
				//catch { error = string.Format("writing version_revision {0}", laszip.version_revision); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.options), 0, 4); }
				//catch { error = string.Format("writing options {0}", laszip.options); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.chunk_size), 0, 4); }
				//catch { error = string.Format("writing chunk_size {0}", laszip.chunk_size); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.number_of_special_evlrs), 0, 8); }
				//catch { error = string.Format("writing number_of_special_evlrs {0}", laszip.number_of_special_evlrs); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.offset_to_special_evlrs), 0, 8); }
				//catch { error = string.Format("writing offset_to_special_evlrs {0}", laszip.offset_to_special_evlrs); return 1; }

				//try { streamout.Write(BitConverter.GetBytes(laszip.num_items), 0, 2); }
				//catch { error = string.Format("writing num_items {0}", laszip.num_items); return 1; }

				//for (uint j = 0; j < laszip.num_items; j++)
				//{
				//	ushort type = (ushort)laszip.items[j].type;
				//	try { streamout.Write(BitConverter.GetBytes(type), 0, 2); }
				//	catch { error = string.Format("writing type {0} of item {1}", laszip.items[j].type, j); return 1; }

				//	try { streamout.Write(BitConverter.GetBytes(laszip.items[j].size), 0, 2); }
				//	catch { error = string.Format("writing size {0} of item {1}", laszip.items[j].size, j); return 1; }

				//	try { streamout.Write(BitConverter.GetBytes(laszip.items[j].version), 0, 2); }
				//	catch { error = string.Format("writing version {0} of item {1}", laszip.items[j].version, j); return 1; }
				//}

				if (write_laszip_vlr_payload(laszip) != 0) return 1;
				#endregion
			}
			#endregion

			#region write any number of user-defined bytes that might have been added after the header
			if (curHeader.user_data_after_header_size != 0)
			{
				try { streamout.Write(curHeader.user_data_after_header, 0, (int)curHeader.user_data_after_header_size); }
				catch { error = string.Format("writing {0} bytes of data into Header.user_data_after_header", curHeader.user_data_after_header_size); return 1; }
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
			npoints = curHeader.number_of_point_records;
			p_count = 0;

			error = null;
			return 0;
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
				// write the point
				if (!writer.write(curPoint))
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

		// TODO
		//public int write_indexed_point();
		//public int update_inventory();

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

		// TODO
		//public int exploit_spatial_index(bool exploit);

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

		public int open_reader(Stream streamin, ref bool is_compressed, bool leaveOpen = false)
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

			return open_reader_stream(ref is_compressed);
		}

		public int open_reader(string file_name, ref bool is_compressed)
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

			return open_reader_stream(ref is_compressed);
		}

		// TODO move?
		int open_reader_stream(ref bool is_compressed)
		{
			try
			{
				byte[] buffer = new byte[32];

				#region read the header variable after variable
				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading Header.file_signature";
					return 1;
				}

				if (buffer[0] != 'L' && buffer[1] != 'A' && buffer[2] != 'S' && buffer[3] != 'F')
				{
					error = "wrong file_signature. not a LAS/LAZ file.";
					return 1;
				}

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.file_source_ID";
					return 1;
				}
				curHeader.file_source_ID = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.global_encoding";
					return 1;
				}
				curHeader.global_encoding = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading Header.project_ID_GUID_data_1";
					return 1;
				}
				curHeader.project_ID_GUID_data_1 = BitConverter.ToUInt32(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.project_ID_GUID_data_2";
					return 1;
				}
				curHeader.project_ID_GUID_data_2 = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.project_ID_GUID_data_3";
					return 1;
				}
				curHeader.project_ID_GUID_data_3 = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(curHeader.project_ID_GUID_data_4, 0, 8) != 8)
				{
					error = "reading Header.project_ID_GUID_data_4";
					return 1;
				}

				if (streamin.Read(buffer, 0, 1) != 1)
				{
					error = "reading Header.version_major";
					return 1;
				}
				curHeader.version_major = buffer[0];

				if (streamin.Read(buffer, 0, 1) != 1)
				{
					error = "reading Header.version_minor";
					return 1;
				}
				curHeader.version_minor = buffer[0];

				if (streamin.Read(curHeader.system_identifier, 0, 32) != 32)
				{
					error = "reading Header.system_identifier";
					return 1;
				}

				if (streamin.Read(curHeader.generating_software, 0, 32) != 32)
				{
					error = "reading Header.generating_software";
					return 1;
				}

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.file_creation_day";
					return 1;
				}
				curHeader.file_creation_day = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.file_creation_year";
					return 1;
				}
				curHeader.file_creation_year = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.header_size";
					return 1;
				}
				curHeader.header_size = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading Header.offset_to_point_data";
					return 1;
				}
				curHeader.offset_to_point_data = BitConverter.ToUInt32(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading Header.number_of_variable_length_records";
					return 1;
				}
				curHeader.number_of_variable_length_records = BitConverter.ToUInt32(buffer, 0);

				if (streamin.Read(buffer, 0, 1) != 1)
				{
					error = "reading Header.point_data_format";
					return 1;
				}
				curHeader.point_data_format = buffer[0];

				if (streamin.Read(buffer, 0, 2) != 2)
				{
					error = "reading Header.point_data_record_length";
					return 1;
				}
				curHeader.point_data_record_length = BitConverter.ToUInt16(buffer, 0);

				if (streamin.Read(buffer, 0, 4) != 4)
				{
					error = "reading Header.number_of_point_records";
					return 1;
				}
				curHeader.number_of_point_records = BitConverter.ToUInt32(buffer, 0);

				for (int i = 0; i < 5; i++)
				{
					if (streamin.Read(buffer, 0, 4) != 4)
					{
						error = string.Format("reading Header.number_of_points_by_return {0}", i);
						return 1;
					}
					curHeader.number_of_points_by_return[i] = BitConverter.ToUInt32(buffer, 0);
				}

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.x_scale_factor";
					return 1;
				}
				curHeader.x_scale_factor = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.y_scale_factor";
					return 1;
				}
				curHeader.y_scale_factor = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.z_scale_factor";
					return 1;
				}
				curHeader.z_scale_factor = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.x_offset";
					return 1;
				}
				curHeader.x_offset = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.y_offset";
					return 1;
				}
				curHeader.y_offset = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.z_offset";
					return 1;
				}
				curHeader.z_offset = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.max_x";
					return 1;
				}
				curHeader.max_x = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.min_x";
					return 1;
				}
				curHeader.min_x = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.max_y";
					return 1;
				}
				curHeader.max_y = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.min_y";
					return 1;
				}
				curHeader.min_y = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.max_z";
					return 1;
				}
				curHeader.max_z = BitConverter.ToDouble(buffer, 0);

				if (streamin.Read(buffer, 0, 8) != 8)
				{
					error = "reading Header.min_z";
					return 1;
				}
				curHeader.min_z = BitConverter.ToDouble(buffer, 0);

				// special handling for LAS 1.3
				if ((curHeader.version_major == 1) && (curHeader.version_minor >= 3))
				{
					if (curHeader.header_size < 235)
					{
						error = string.Format("for LAS 1.{0} header_size should at least be 235 but it is only {1}", curHeader.version_minor, curHeader.header_size);
						return 1;
					}
					else
					{
						if (streamin.Read(buffer, 0, 8) != 8)
						{
							error = "reading Header.start_of_waveform_data_packet_record";
							return 1;
						}
						curHeader.start_of_waveform_data_packet_record = BitConverter.ToUInt64(buffer, 0);
						curHeader.user_data_in_header_size = (uint)curHeader.header_size - 235;
					}
				}
				else
				{
					curHeader.user_data_in_header_size = (uint)curHeader.header_size - 227;
				}

				// special handling for LAS 1.4
				if ((curHeader.version_major == 1) && (curHeader.version_minor >= 4))
				{
					if (curHeader.header_size < 375)
					{
						error = string.Format("for LAS 1.{0} header_size should at least be 375 but it is only {1}", curHeader.version_minor, curHeader.header_size);
						return 1;
					}
					else
					{
						if (streamin.Read(buffer, 0, 8) != 8)
						{
							error = "reading Header.start_of_first_extended_variable_length_record";
							return 1;
						}
						curHeader.start_of_first_extended_variable_length_record = BitConverter.ToUInt64(buffer, 0);

						if (streamin.Read(buffer, 0, 4) != 4)
						{
							error = "reading Header.number_of_extended_variable_length_records";
							return 1;
						}
						curHeader.number_of_extended_variable_length_records = BitConverter.ToUInt32(buffer, 0);

						if (streamin.Read(buffer, 0, 8) != 8)
						{
							error = "reading Header.extended_number_of_point_records";
							return 1;
						}
						curHeader.extended_number_of_point_records = BitConverter.ToUInt64(buffer, 0);

						for (int i = 0; i < 15; i++)
						{
							if (streamin.Read(buffer, 0, 8) != 8)
							{
								error = string.Format("reading Header.extended_number_of_points_by_return[{0}]", i);
								return 1;
							}
							curHeader.extended_number_of_points_by_return[i] = BitConverter.ToUInt64(buffer, 0);
						}
						curHeader.user_data_in_header_size = (uint)curHeader.header_size - 375;
					}
				}

				// load any number of user-defined bytes that might have been added to the header
				if (curHeader.user_data_in_header_size != 0)
				{
					curHeader.user_data_in_header = new byte[curHeader.user_data_in_header_size];

					if (streamin.Read(curHeader.user_data_in_header, 0, (int)curHeader.user_data_in_header_size) != curHeader.user_data_in_header_size)
					{
						error = string.Format("reading {0} bytes of data into Header.user_data_in_header", curHeader.user_data_in_header_size);
						return 1;
					}
				}
				#endregion

				#region read variable length records into the header
				uint vlrs_size = 0;
				LASzip laszip = null;

				if (curHeader.number_of_variable_length_records != 0)
				{
					curHeader.vlrs = new List<vlr>();

					for (int i = 0; i < curHeader.number_of_variable_length_records; i++)
					{
						curHeader.vlrs.Add(new vlr());

						// make sure there are enough bytes left to read a variable length record before the point block starts
						if (((int)curHeader.offset_to_point_data - vlrs_size - curHeader.header_size) < 54)
						{
							warning = string.Format("only {0} bytes until point block after reading {1} of {2} vlrs. skipping remaining vlrs ...", (int)curHeader.offset_to_point_data - vlrs_size - curHeader.header_size, i, curHeader.number_of_variable_length_records);
							curHeader.number_of_variable_length_records = (uint)i;
							break;
						}

						// read variable length records variable after variable (to avoid alignment issues)
						if (streamin.Read(buffer, 0, 2) != 2)
						{
							error = string.Format("reading Header.vlrs[{0}].reserved", i);
							return 1;
						}
						curHeader.vlrs[i].reserved = BitConverter.ToUInt16(buffer, 0);

						if (streamin.Read(curHeader.vlrs[i].user_id, 0, 16) != 16)
						{
							error = string.Format("reading Header.vlrs[{0}].user_id", i);
							return 1;
						}

						if (streamin.Read(buffer, 0, 2) != 2)
						{
							error = string.Format("reading Header.vlrs[{0}].record_id", i);
							return 1;
						}
						curHeader.vlrs[i].record_id = BitConverter.ToUInt16(buffer, 0);

						if (streamin.Read(buffer, 0, 2) != 2)
						{
							error = string.Format("reading Header.vlrs[{0}].record_length_after_header", i);
							return 1;
						}
						curHeader.vlrs[i].record_length_after_header = BitConverter.ToUInt16(buffer, 0);

						if (streamin.Read(curHeader.vlrs[i].description, 0, 32) != 32)
						{
							error = string.Format("reading Header.vlrs[{0}].description", i);
							return 1;
						}

						// keep track on the number of bytes we have read so far
						vlrs_size += 54;

						// check variable length record contents
						if (curHeader.vlrs[i].reserved != 0xAABB)
						{
							warning = string.Format("wrong Header.vlrs[{0}].reserved: {1} != 0xAABB", i, curHeader.vlrs[i].reserved);
						}

						// make sure there are enough bytes left to read the data of the variable length record before the point block starts
						if (((int)curHeader.offset_to_point_data - vlrs_size - curHeader.header_size) < curHeader.vlrs[i].record_length_after_header)
						{
							warning = string.Format("only {0} bytes until point block when trying to read {1} bytes into Header.vlrs[{2}].data", (int)curHeader.offset_to_point_data - vlrs_size - curHeader.header_size, curHeader.vlrs[i].record_length_after_header, i);
							curHeader.vlrs[i].record_length_after_header = (ushort)(curHeader.offset_to_point_data - vlrs_size - curHeader.header_size);
						}

						// load data following the header of the variable length record
						if (curHeader.vlrs[i].record_length_after_header != 0)
						{
							if (strcmp(curHeader.vlrs[i].user_id, "laszip encoded"))
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
								curHeader.vlrs[i].data = new byte[curHeader.vlrs[i].record_length_after_header];
								if (streamin.Read(curHeader.vlrs[i].data, 0, curHeader.vlrs[i].record_length_after_header) != curHeader.vlrs[i].record_length_after_header)
								{
									error = string.Format("reading {0} bytes of data into Header.vlrs[{1}].data", curHeader.vlrs[i].record_length_after_header, i);
									return 1;
								}
							}
						}
						else
						{
							curHeader.vlrs[i].data = null;
						}

						// keep track on the number of bytes we have read so far
						vlrs_size += curHeader.vlrs[i].record_length_after_header;

						// special handling for LASzip VLR
						if (strcmp(curHeader.vlrs[i].user_id, "laszip encoded"))
						{
							// we take our the VLR for LASzip away
							curHeader.offset_to_point_data -= (uint)(54 + curHeader.vlrs[i].record_length_after_header);
							vlrs_size -= (uint)(54 + curHeader.vlrs[i].record_length_after_header);
							curHeader.vlrs.RemoveAt(i);
							i--;
							curHeader.number_of_variable_length_records--;
						}
					}
				}
				#endregion

				// load any number of user-defined bytes that might have been added after the header
				curHeader.user_data_after_header_size = curHeader.offset_to_point_data - vlrs_size - curHeader.header_size;
				if (curHeader.user_data_after_header_size != 0)
				{
					curHeader.user_data_after_header = new byte[curHeader.user_data_after_header_size];

					if (streamin.Read(curHeader.user_data_after_header, 0, (int)curHeader.user_data_after_header_size) != curHeader.user_data_after_header_size)
					{
						error = string.Format("reading {0} bytes of data into Header.user_data_after_header", curHeader.user_data_after_header_size);
						return 1;
					}
				}

				// remove extra bits in point data type
				if ((curHeader.point_data_format & 128) != 0 || (curHeader.point_data_format & 64) != 0)
				{
					if (laszip == null)
					{
						error = "this file was compressed with an experimental version of LASzip. contact 'martin.isenburg@rapidlasso.com' for assistance";
						return 1;
					}
					curHeader.point_data_format &= 127;
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
					if (!laszip.setup(curHeader.point_data_format, curHeader.point_data_record_length, LASzip.COMPRESSOR_NONE))
					{
						error = string.Format("invalid combination of point_data_format {0} and point_data_record_length {1}", curHeader.point_data_format, curHeader.point_data_record_length);
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
							curPoint.num_extra_bytes = laszip.items[i].size;
							curPoint.extra_bytes = new byte[curPoint.num_extra_bytes];
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
				npoints = curHeader.number_of_point_records;
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

		// TODO
		//public int has_spatial_index(bool[]/ref bool is_indexed, bool[]/ref bool is_appended);

		//public int inside_rectangle(double min_x, double min_y, double max_x, double max_y, out bool is_empty);

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

			error = null;
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
				if (!reader.read(curPoint))
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

		// TODO
		//public int read_inside_point(out bool is_done);

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
			}
			catch
			{
				error = "internal error in laszip_close_reader";
				return 1;
			}

			error = null;
			return 0;
		}

		public int open_reader_stream(Stream streamin, ref bool is_compressed, bool leaveOpen = false)
		{
			return open_reader(streamin, ref is_compressed, leaveOpen);
		}

		//public int open_writer_stream(Stream streamout, bool compress, bool do_not_write_header, bool leaveOpen = false);

		// make LASzip VLR for point type and point size already specified earlier
		//public int create_laszip_vlr(byte** vlr, uint* vlr_size);
	}
}
