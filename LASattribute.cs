//===============================================================================
//
//  FILE:  lasattribute.cs
//
//  CONTENTS:
//
//    This class assists with handling the "extra bytes" that allow storing
//    additional per point attributes.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2015, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2017-2018 by Shinta <shintadono@googlemail.com>
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
using System.Runtime.InteropServices;

namespace LASzip.Net
{
	public enum LAS_ATTRIBUTE
	{
		U8 = 0,
		I8 = 1,
		U16 = 2,
		I16 = 3,
		U32 = 4,
		I32 = 5,
		U64 = 6,
		I64 = 7,
		F32 = 8,
		F64 = 9
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LASattribute
	{
		static unsafe int strncpy(byte* dst, byte[] src, int count)
		{
			int len = 0;
			if (src != null) len = Math.Min(count, src.Length);
			for (int i = 0; i < len; i++) dst[i] = src[i];
			for (int i = len; i < count; i++) dst[i] = 0;
			return len;
		}

		static unsafe int strncpy(byte* dst, string src, int count)
		{
			if (src == null) return strncpy(dst, (byte[])null, count);
			return strncpy(dst, System.Text.Encoding.ASCII.GetBytes(src), count);
		}

		unsafe fixed byte reserved[2]; // 2 bytes
		public byte data_type; // 1 byte; LAS_ATTRIBUTE-1; 0 denotes a byte[options].
		public byte options; // 1 byte; Bitfield (no_data: 1, min: 2, max: 4, scale: 8, offset: 16) if data_type >= 0; otherwise, size of a byte[].
		unsafe fixed byte name[32]; // [32] bytes
		unsafe fixed byte unused[4]; // 4 bytes
		unsafe fixed ulong no_data[3]; // 24 = 3*8 bytes
		unsafe fixed ulong min[3]; // 24 = 3*8 bytes
		unsafe fixed ulong max[3]; // 24 = 3*8 bytes
		unsafe fixed double scale[3]; // 24 = 3*8 bytes
		unsafe fixed double offset[3]; // 24 = 3*8 bytes
		unsafe fixed byte description[32]; // [32] bytes

		public string Name
		{
			get { unsafe { fixed (byte* n = name) return new string((sbyte*)n); } }
			set { unsafe { fixed (byte* n = name) strncpy(n, value, 32); } }
		}

		public U64I64F64 NoData(int dim)
		{
			if (!has_no_data() || 0 > dim || dim >= get_dim()) return new U64I64F64 { u64 = 0 };
			unsafe { fixed (ulong* n = no_data) return new U64I64F64 { u64 = n[dim] }; }
		}

		public bool SetNoData(U64I64F64 no_data, int dim = 0)
		{
			if (data_type == 0 || 0 > dim || dim >= get_dim()) return false;

			unsafe { fixed (ulong* n = this.no_data) n[dim] = no_data.u64; }
			options |= 0x01;
			return true;
		}

		public U64I64F64 Min(int dim)
		{
			if (!has_min() || 0 > dim || dim >= get_dim()) return new U64I64F64 { u64 = 0 };
			unsafe { fixed (ulong* m = min) return new U64I64F64 { u64 = m[dim] }; }
		}

		public bool SetMin(U64I64F64 min, int dim = 0)
		{
			if (data_type == 0 || 0 > dim || dim >= get_dim()) return false;

			unsafe { fixed (ulong* m = this.min) m[dim] = min.u64; }
			options |= 0x02;
			return true;
		}

		public U64I64F64 Max(int dim)
		{
			if (!has_max() || 0 > dim || dim >= get_dim()) return new U64I64F64 { u64 = 0 };
			unsafe { fixed (ulong* m = max) return new U64I64F64 { u64 = m[dim] }; }
		}

		public bool SetMax(U64I64F64 max, int dim = 0)
		{
			if (data_type == 0 || 0 > dim || dim >= get_dim()) return false;

			unsafe { fixed (ulong* m = this.max) m[dim] = max.u64; }
			options |= 0x04;
			return true;
		}

		public double Scale(int dim)
		{
			if (!has_scale() || 0 > dim || dim >= get_dim()) return 1.0; // Default for scale is 1.0.
			unsafe { fixed (double* s = scale) return s[dim]; }
		}

		public double Offset(int dim)
		{
			if (!has_offset() || 0 > dim || dim >= get_dim()) return 0;
			unsafe { fixed (double* o = offset) return o[dim]; }
		}

		public string Description
		{
			get { unsafe { fixed (byte* d = description) return new string((sbyte*)d); } }
			set { unsafe { fixed (byte* d = description) strncpy(d, value, 32); } }
		}

		public LASattribute(byte size) : this()
		{
			if (size == 0) throw new ArgumentOutOfRangeException(nameof(size), "Must be greater zero (0).");

			options = size;
			unsafe { fixed (double* s = scale) s[0] = s[1] = s[2] = 1.0; }
		}

		public LASattribute(LAS_ATTRIBUTE type, string name, string description = null, int dim = 1) : this()
		{
			if (type > LAS_ATTRIBUTE.F64) throw new ArgumentOutOfRangeException(nameof(type), "Must be one of the enum values.");
			if ((dim < 1) || (dim > 3)) throw new ArgumentOutOfRangeException(nameof(dim), "Must be 1, 2, or 3.");
			if (name == null) throw new ArgumentNullException(nameof(name));

			unsafe { fixed (double* s = scale) s[0] = s[1] = s[2] = 1.0; }
			data_type = (byte)((dim - 1) * 10 + (int)type + 1);

			Name = name;
			Description = description;
		}

		public bool set_no_data(byte no_data, int dim = 0) { return 0 != get_type() ? false : SetNoData(new U64I64F64 { u64 = no_data }, dim); }
		public bool set_no_data(sbyte no_data, int dim = 0) { return 1 != get_type() ? false : SetNoData(new U64I64F64 { i64 = no_data }, dim); }
		public bool set_no_data(ushort no_data, int dim = 0) { return 2 != get_type() ? false : SetNoData(new U64I64F64 { u64 = no_data }, dim); }
		public bool set_no_data(short no_data, int dim = 0) { return 3 != get_type() ? false : SetNoData(new U64I64F64 { i64 = no_data }, dim); }
		public bool set_no_data(uint no_data, int dim = 0) { return 4 != get_type() ? false : SetNoData(new U64I64F64 { u64 = no_data }, dim); }
		public bool set_no_data(int no_data, int dim = 0) { return 5 != get_type() ? false : SetNoData(new U64I64F64 { i64 = no_data }, dim); }
		public bool set_no_data(ulong no_data, int dim = 0) { return 6 != get_type() ? false : SetNoData(new U64I64F64 { u64 = no_data }, dim); }
		public bool set_no_data(long no_data, int dim = 0) { return 7 != get_type() ? false : SetNoData(new U64I64F64 { i64 = no_data }, dim); }
		public bool set_no_data(float no_data, int dim = 0) { return 8 != get_type() ? false : SetNoData(new U64I64F64 { f64 = no_data }, dim); }
		public bool set_no_data(double no_data, int dim = 0)
		{
			switch (get_type())
			{
				case 0:
				case 2:
				case 4:
				case 6:
					return SetNoData(new U64I64F64 { u64 = (ulong)no_data }, dim);
				case 1:
				case 3:
				case 5:
				case 7:
					return SetNoData(new U64I64F64 { i64 = (long)no_data }, dim);
				case 8:
				case 9:
					return SetNoData(new U64I64F64 { f64 = no_data }, dim);
			}
			return false;
		}

		public void set_min(byte[] min, int dim = 0) { SetMin(cast(min), dim); }
		public void update_min(byte[] min, int dim = 0) { SetMin(smallest(cast(min), Min(dim)), dim); }
		public bool set_min(byte min, int dim = 0) { return 0 != get_type() ? false : SetMin(new U64I64F64 { u64 = min }, dim); }
		public bool set_min(sbyte min, int dim = 0) { return 1 != get_type() ? false : SetMin(new U64I64F64 { i64 = min }, dim); }
		public bool set_min(ushort min, int dim = 0) { return 2 != get_type() ? false : SetMin(new U64I64F64 { u64 = min }, dim); }
		public bool set_min(short min, int dim = 0) { return 3 != get_type() ? false : SetMin(new U64I64F64 { i64 = min }, dim); }
		public bool set_min(uint min, int dim = 0) { return 4 != get_type() ? false : SetMin(new U64I64F64 { u64 = min }, dim); }
		public bool set_min(int min, int dim = 0) { return 5 != get_type() ? false : SetMin(new U64I64F64 { i64 = min }, dim); }
		public bool set_min(ulong min, int dim = 0) { return 6 != get_type() ? false : SetMin(new U64I64F64 { u64 = min }, dim); }
		public bool set_min(long min, int dim = 0) { return 7 != get_type() ? false : SetMin(new U64I64F64 { i64 = min }, dim); }
		public bool set_min(float min, int dim = 0) { return 8 != get_type() ? false : SetMin(new U64I64F64 { f64 = min }, dim); }
		public bool set_min(double min, int dim = 0) { return 9 != get_type() ? false : SetMin(new U64I64F64 { f64 = min }, dim); }

		public void set_max(byte[] max, int dim = 0) { SetMax(cast(max), dim); }
		public void update_max(byte[] max, int dim = 0) { SetMax(biggest(cast(max), Max(dim)), dim); }
		public bool set_max(byte max, int dim = 0) { return 0 != get_type() ? false : SetMax(new U64I64F64 { u64 = max }, dim); }
		public bool set_max(sbyte max, int dim = 0) { return 1 != get_type() ? false : SetMax(new U64I64F64 { i64 = max }, dim); }
		public bool set_max(ushort max, int dim = 0) { return 2 != get_type() ? false : SetMax(new U64I64F64 { u64 = max }, dim); }
		public bool set_max(short max, int dim = 0) { return 3 != get_type() ? false : SetMax(new U64I64F64 { i64 = max }, dim); }
		public bool set_max(uint max, int dim = 0) { return 4 != get_type() ? false : SetMax(new U64I64F64 { u64 = max }, dim); }
		public bool set_max(int max, int dim = 0) { return 5 != get_type() ? false : SetMax(new U64I64F64 { i64 = max }, dim); }
		public bool set_max(ulong max, int dim = 0) { return 6 != get_type() ? false : SetMax(new U64I64F64 { u64 = max }, dim); }
		public bool set_max(long max, int dim = 0) { return 7 != get_type() ? false : SetMax(new U64I64F64 { i64 = max }, dim); }
		public bool set_max(float max, int dim = 0) { return 8 != get_type() ? false : SetMax(new U64I64F64 { f64 = max }, dim); }
		public bool set_max(double max, int dim = 0) { return 9 != get_type() ? false : SetMax(new U64I64F64 { f64 = max }, dim); }

		public bool set_scale(double scale, int dim = 0)
		{
			if (data_type == 0 || 0 > dim || dim >= get_dim()) return false;

			unsafe { fixed (double* s = this.scale) s[dim] = scale; }
			options |= 0x08;
			return true;
		}

		public bool set_offset(double offset, int dim = 0)
		{
			if (data_type == 0 || 0 > dim || dim >= get_dim()) return false;

			unsafe { fixed (double* o = this.offset) o[dim] = offset; }
			options |= 0x10;
			return true;
		}

		public bool has_no_data() { return data_type == 0 ? false : (options & 0x01) != 0; }
		public bool has_min() { return data_type == 0 ? false : (options & 0x02) != 0; }
		public bool has_max() { return data_type == 0 ? false : (options & 0x04) != 0; }
		public bool has_scale() { return data_type == 0 ? false : (options & 0x08) != 0; }
		public bool has_offset() { return data_type == 0 ? false : (options & 0x10) != 0; }

		public int get_size()
		{
			if (data_type == 0) return options;

			int dim = get_dim();
			switch (get_type())
			{
				default:
				case 0:
				case 1: return dim;
				case 2: case 3: return 2 * dim;
				case 4: case 5: case 8: return 4 * dim;
				case 6: case 7: case 9: return 8 * dim;
			}
		}

		public double get_value_as_float(byte[] value)
		{
			double casted_value;
			switch (get_type())
			{
				case 0: casted_value = value[0]; break;
				case 1: casted_value = (sbyte)value[0]; break;
				case 2: casted_value = BitConverter.ToUInt16(value, 0); break;
				case 3: casted_value = BitConverter.ToInt16(value, 0); break;
				case 4: casted_value = BitConverter.ToUInt32(value, 0); break;
				case 5: casted_value = BitConverter.ToInt32(value, 0); break;
				case 6: casted_value = BitConverter.ToUInt64(value, 0); break; // was: (long)BitConverter.ToUInt64(value, 0); don't know why.
				case 7: casted_value = BitConverter.ToInt64(value, 0); break;
				case 8: casted_value = BitConverter.ToSingle(value, 0); break;
				case 9: casted_value = BitConverter.ToDouble(value, 0); break;
				default: return 0;
			}
			unsafe { fixed (double* o = offset, s = scale) return o[0] + s[0] * casted_value; }
		}

		int get_type()
		{
			return (data_type - 1) % 10;
		}

		int get_dim()
		{
			return 1 + (data_type - 1) / 10;
		}

		U64I64F64 cast(byte[] value)
		{
			switch (get_type())
			{
				case 0: return new U64I64F64 { u64 = value[0] };
				case 1: return new U64I64F64 { i64 = (sbyte)value[0] };
				case 2: return new U64I64F64 { u64 = BitConverter.ToUInt16(value, 0) };
				case 3: return new U64I64F64 { i64 = BitConverter.ToInt16(value, 0) };
				case 4: return new U64I64F64 { u64 = BitConverter.ToUInt32(value, 0) };
				case 5: return new U64I64F64 { i64 = BitConverter.ToInt32(value, 0) };
				case 6: return new U64I64F64 { u64 = BitConverter.ToUInt64(value, 0) };
				case 7: return new U64I64F64 { i64 = BitConverter.ToInt64(value, 0) };
				case 8: return new U64I64F64 { f64 = BitConverter.ToSingle(value, 0) };
				case 9: return new U64I64F64 { f64 = BitConverter.ToDouble(value, 0) };
				default: return new U64I64F64();
			}
		}

		U64I64F64 smallest(U64I64F64 a, U64I64F64 b)
		{
			int type = get_type();
			if (type >= 8) return a.f64 < b.f64 ? a : b; // float compare
			if ((type & 1) != 0) return a.i64 < b.i64 ? a : b; // int compare
			return a.u64 < b.u64 ? a : b;
		}

		U64I64F64 biggest(U64I64F64 a, U64I64F64 b)
		{
			int type = get_type();
			if (type >= 8) return a.f64 > b.f64 ? a : b; // float compare
			if ((type & 1) != 0) return a.i64 > b.i64 ? a : b; // int compare
			return a.u64 > b.u64 ? a : b;
		}
	}
}
