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
//    (c) 2005-2017, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2017-2017 by Shinta <shintadono@googlemail.com>
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

	public class LASattribute
	{
		//readonly byte reserved[2]; // 2 bytes
		public byte data_type; // 1 byte; LAS_ATTRIBUTE-1; 0 denotes a byte[options].
		public byte options; // 1 byte; Bitfield (no_data: 1, min: 2, max: 4, scale: 8, offset: 16) if data_type >= 0; otherwise, size of a byte[].
		public string name; // [32] bytes
							//readonly byte unused[4]; // 4 bytes
		public readonly U64I64F64[] no_data = new U64I64F64[3]; // 24 = 3*8 bytes
		public readonly U64I64F64[] min = new U64I64F64[3]; // 24 = 3*8 bytes
		public readonly U64I64F64[] max = new U64I64F64[3]; // 24 = 3*8 bytes
		public readonly double[] scale = new double[3]; // 24 = 3*8 bytes
		public readonly double[] offset = new double[3]; // 24 = 3*8 bytes
		public string description;// [32] bytes

		internal LASattribute(LASattribute attribute)
		{
			data_type = attribute.data_type;
			options = attribute.options;
			name = attribute.name;
			attribute.no_data.CopyTo(no_data, 0);
			attribute.min.CopyTo(min, 0);
			attribute.max.CopyTo(max, 0);
			attribute.scale.CopyTo(scale, 0);
			attribute.offset.CopyTo(offset, 0);
			description = attribute.description;
		}

		public LASattribute(byte size)
		{
			if (size == 0) throw new ArgumentOutOfRangeException(nameof(size), "Must be greater zero (0).");
			scale[0] = scale[1] = scale[2] = 1.0;
			options = size;
		}

		public LASattribute(LAS_ATTRIBUTE type, string name, string description = null, int dim = 1)
		{
			if (type > LAS_ATTRIBUTE.F64) throw new ArgumentOutOfRangeException(nameof(type), "Must be one of the enum values.");
			if ((dim < 1) || (dim > 3)) throw new ArgumentOutOfRangeException(nameof(dim), "Must be 1, 2, or 3.");
			if (name == null) throw new ArgumentNullException(nameof(name));

			scale[0] = scale[1] = scale[2] = 1.0;
			data_type = (byte)((dim - 1) * 10 + (int)type + 1);

			if (name.Length > 31) this.name = name.Substring(0, 31);
			else this.name = name;

			if (description != null)
			{
				if (description.Length > 31) this.description = description.Substring(0, 31);
				else this.description = description;
			}
		}

		public bool set_no_data(byte no_data, int dim = 0) { if ((0 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].u64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(sbyte no_data, int dim = 0) { if ((1 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].i64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(ushort no_data, int dim = 0) { if ((2 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].u64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(short no_data, int dim = 0) { if ((3 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].i64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(uint no_data, int dim = 0) { if ((4 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].u64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(int no_data, int dim = 0) { if ((5 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].i64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(ulong no_data, int dim = 0) { if ((6 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].u64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(long no_data, int dim = 0) { if ((7 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].i64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(float no_data, int dim = 0) { if ((8 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.no_data[dim].f64 = no_data; options |= 0x01; return true; } return false; }
		public bool set_no_data(double no_data, int dim = 0)
		{
			if (dim >= 0 && dim < get_dim())
			{
				switch (get_type())
				{
					case 0:
					case 2:
					case 4:
					case 6:
						this.no_data[dim].u64 = (ulong)no_data; options |= 0x01; return true;
					case 1:
					case 3:
					case 5:
					case 7:
						this.no_data[dim].i64 = (long)no_data; options |= 0x01; return true;
					case 8:
					case 9:
						this.no_data[dim].f64 = no_data; options |= 0x01; return true;
				}
			}
			return false;
		}

		public void set_min(byte[] min, int dim = 0) { this.min[dim] = cast(min); options |= 0x02; }
		public void update_min(byte[] min, int dim = 0) { this.min[dim] = smallest(cast(min), this.min[dim]); }
		public bool set_min(byte min, int dim = 0) { if ((0 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].u64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(sbyte min, int dim = 0) { if ((1 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].i64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(ushort min, int dim = 0) { if ((2 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].u64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(short min, int dim = 0) { if ((3 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].i64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(uint min, int dim = 0) { if ((4 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].u64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(int min, int dim = 0) { if ((5 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].i64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(ulong min, int dim = 0) { if ((6 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].u64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(long min, int dim = 0) { if ((7 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].i64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(float min, int dim = 0) { if ((8 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].f64 = min; options |= 0x02; return true; } return false; }
		public bool set_min(double min, int dim = 0) { if ((9 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.min[dim].f64 = min; options |= 0x02; return true; } return false; }

		public void set_max(byte[] max, int dim = 0) { this.max[dim] = cast(max); options |= 0x04; }
		public void update_max(byte[] max, int dim = 0) { this.max[dim] = biggest(cast(max), this.max[dim]); }
		public bool set_max(byte max, int dim = 0) { if ((0 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].u64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(sbyte max, int dim = 0) { if ((1 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].i64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(ushort max, int dim = 0) { if ((2 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].u64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(short max, int dim = 0) { if ((3 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].i64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(uint max, int dim = 0) { if ((4 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].u64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(int max, int dim = 0) { if ((5 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].i64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(ulong max, int dim = 0) { if ((6 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].u64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(long max, int dim = 0) { if ((7 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].i64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(float max, int dim = 0) { if ((8 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].f64 = max; options |= 0x04; return true; } return false; }
		public bool set_max(double max, int dim = 0) { if ((9 == get_type()) && (dim >= 0) && (dim < get_dim())) { this.max[dim].f64 = max; options |= 0x04; return true; } return false; }

		public bool set_scale(float scale, int dim = 0) { if (data_type != 0 && (dim >= 0) && (dim < get_dim())) { this.scale[dim] = scale; options |= 0x08; return true; } return false; }
		public bool set_offset(float offset, int dim = 0) { if (data_type != 0 && (dim >= 0) && (dim < get_dim())) { this.offset[dim] = offset; options |= 0x10; return true; } return false; }

		public bool has_no_data() { return (options & 0x01) != 0; }
		public bool has_min() { return (options & 0x02) != 0; }
		public bool has_max() { return (options & 0x04) != 0; }
		public bool has_scale() { return (options & 0x08) != 0; }
		public bool has_offset() { return (options & 0x10) != 0; }

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
				case 1: casted_value = (int)value[0]; break;
				case 2: casted_value = BitConverter.ToUInt16(value, 0); break;
				case 3: casted_value = BitConverter.ToInt16(value, 0); break;
				case 4: casted_value = BitConverter.ToUInt32(value, 0); break;
				case 5: casted_value = BitConverter.ToInt32(value, 0); break;
				case 6: casted_value = (long)BitConverter.ToUInt64(value, 0); break;
				case 7: casted_value = BitConverter.ToInt64(value, 0); break;
				case 8: casted_value = BitConverter.ToSingle(value, 0); break;
				case 9: default: casted_value = BitConverter.ToDouble(value, 0); break;
			}
			return offset[0] + scale[0] * casted_value;
		}

		int get_type()
		{
			return ((int)data_type - 1) % 10;
		}

		int get_dim()
		{
			return 1 + ((int)data_type - 1) / 10;
		}

		U64I64F64 cast(byte[] value)
		{
			int type = get_type();
			U64I64F64 casted_value = new U64I64F64();

			switch (get_type())
			{
				case 0: casted_value.u64 = value[0]; break;
				case 1: casted_value.i64 = (int)value[0]; break;
				case 2: casted_value.u64 = BitConverter.ToUInt16(value, 0); break;
				case 3: casted_value.i64 = BitConverter.ToInt16(value, 0); break;
				case 4: casted_value.u64 = BitConverter.ToUInt32(value, 0); break;
				case 5: casted_value.i64 = BitConverter.ToInt32(value, 0); break;
				case 6: casted_value.u64 = BitConverter.ToUInt64(value, 0); break;
				case 7: casted_value.i64 = BitConverter.ToInt64(value, 0); break;
				case 8: casted_value.f64 = BitConverter.ToSingle(value, 0); break;
				case 9: default: casted_value.f64 = BitConverter.ToDouble(value, 0); break;
			}

			return casted_value;
		}

		U64I64F64 smallest(U64I64F64 a, U64I64F64 b)
		{
			int type = get_type();
			if (type >= 8) // float compare
			{
				if (a.f64 < b.f64) return a;
				else return b;
			}
			if ((type & 1) != 0) // int compare
			{
				if (a.i64 < b.i64) return a;
				else return b;
			}
			if (a.u64 < b.u64) return a;
			else return b;
		}

		U64I64F64 biggest(U64I64F64 a, U64I64F64 b)
		{
			int type = get_type();
			if (type >= 8) // float compare
			{
				if (a.f64 > b.f64) return a;
				else return b;
			}
			if ((type & 1) != 0) // int compare
			{
				if (a.i64 > b.i64) return a;
				else return b;
			}
			if (a.u64 > b.u64) return a;
			else return b;
		}
	}
}
