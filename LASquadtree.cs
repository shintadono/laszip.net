//===============================================================================
//
//  FILE:  lasquadtree.cs
//
//  CONTENTS:
//
//    An efficient quadtree that can be used for spatial indexing, for tiling,
//    for sorting into space-filling curve order, and for injecting spatial
//    finalization tags to be used in memory-efficienct streaming algorithms.
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

using System;
using System.Collections.Generic;
using System.IO;

namespace LASzip.Net
{
	delegate bool does_cell_exist_proc(int cell_index);

	class LASquadtree
	{
		const int LAS_SPATIAL_QUAD_TREE = 0;

		public LASquadtree()
		{
			levels = 0;
			cell_size = 0;
			min_x = 0;
			max_x = 0;
			min_y = 0;
			max_y = 0;
			cells_x = 0;
			cells_y = 0;
			sub_level = 0;
			sub_level_index = 0;
			level_offset[0] = 0;
			for (int l = 0; l < 23; l++)
			{
				level_offset[l + 1] = level_offset[l] + ((1u << l) * (1u << l));
			}
			current_cells = null;
			adaptive_alloc = 0;
			adaptive = null;
		}

		#region read from file or write to file
		// read from file
		public bool read(Stream stream)
		{
			// read data in the following order
			//     U32  levels          4 bytes
			//     U32  level_index     4 bytes (default 0)
			//     U32  implicit_levels 4 bytes (only used when level_index != 0))
			//     F32  min_x           4 bytes
			//     F32  max_x           4 bytes
			//     F32  min_y           4 bytes
			//     F32  max_y           4 bytes
			// which totals 28 bytes

			byte[] tmp = new byte[4];
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading LASspatial signature");
				return false;
			}
			if (tmp[0] != 'L' || tmp[1] != 'A' || tmp[2] != 'S' || tmp[3] != 'S')
			{
				Console.Error.WriteLine("ERROR (LASquadtree): wrong LASspatial signature {0,4} instead of 'LASS'", tmp);
				return false;
			}
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading LASspatial type");
				return false;
			}
			uint type = BitConverter.ToUInt32(tmp, 0);
			if (type != LAS_SPATIAL_QUAD_TREE)
			{
				Console.Error.WriteLine("ERROR (LASquadtree): unknown LASspatial type {0}", type);
				return false;
			}
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading signature");
				return false;
			}
			if (tmp[0] != 'L' || tmp[1] != 'A' || tmp[2] != 'S' || tmp[3] != 'Q')
			{
				//Console.Error.WriteLine("ERROR (LASquadtree): wrong signature {0,4} instead of 'LASV'", signature);
				//return false;
				levels = tmp[0];
			}
			else
			{
				try { stream.Read(tmp, 0, 4); }
				catch
				{
					Console.Error.WriteLine("ERROR (LASquadtree): reading version");
					return false;
				}
				uint version = BitConverter.ToUInt32(tmp, 0);
				try { stream.Read(tmp, 0, 4); }
				catch
				{
					Console.Error.WriteLine("ERROR (LASquadtree): reading levels");
					return false;
				}
				levels = BitConverter.ToUInt32(tmp, 0);
			}

			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading level_index");
				return false;
			}
			uint level_index = BitConverter.ToUInt32(tmp, 0);
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading implicit_levels");
				return false;
			}
			uint implicit_levels = BitConverter.ToUInt32(tmp, 0);
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading min_x");
				return false;
			}
			min_x = BitConverter.ToSingle(tmp, 0);
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading max_x");
				return false;
			}
			max_x = BitConverter.ToSingle(tmp, 0);
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading min_y");
				return false;
			}
			min_y = BitConverter.ToSingle(tmp, 0);
			try { stream.Read(tmp, 0, 4); }
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): reading max_y");
				return false;
			}
			max_y = BitConverter.ToSingle(tmp, 0);
			return true;
		}

		public bool write(Stream stream)
		{
			// which totals 28 bytes
			//     U32  levels          4 bytes
			//     U32  level_index     4 bytes (default 0)
			//     U32  implicit_levels 4 bytes (only used when level_index != 0))
			//     F32  min_x           4 bytes
			//     F32  max_x           4 bytes
			//     F32  min_y           4 bytes
			//     F32  max_y           4 bytes
			// which totals 28 bytes

			byte[] tmp = new byte[4];
			tmp[0] = (byte)'L';
			tmp[1] = (byte)'A';
			tmp[2] = (byte)'S';
			tmp[3] = (byte)'S';
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing LASspatial signature");
				return false;
			}

			uint type = LAS_SPATIAL_QUAD_TREE;
			tmp = BitConverter.GetBytes(type);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing LASspatial type {0}", type);
				return false;
			}

			tmp[0] = (byte)'L';
			tmp[1] = (byte)'A';
			tmp[2] = (byte)'S';
			tmp[3] = (byte)'Q';
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing signature");
				return false;
			}

			uint version = 0;
			tmp = BitConverter.GetBytes(version);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing version");
				return false;
			}

			tmp = BitConverter.GetBytes(levels);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing levels {0}", levels);
				return false;
			}
			uint level_index = 0;
			tmp = BitConverter.GetBytes(level_index);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing level_index {0}", level_index);
				return false;
			}
			uint implicit_levels = 0;
			tmp = BitConverter.GetBytes(implicit_levels);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing implicit_levels {0}", implicit_levels);
				return false;
			}
			tmp = BitConverter.GetBytes(min_x);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing min_x {0}", min_x);
				return false;
			}
			tmp = BitConverter.GetBytes(max_x);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing max_x {0}", max_x);
				return false;
			}
			tmp = BitConverter.GetBytes(min_y);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing min_y {0}", min_y);
				return false;
			}
			tmp = BitConverter.GetBytes(max_y);
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASquadtree): writing max_y {0}", max_y);
				return false;
			}
			return true;
		}
		#endregion

		// create or finalize the cell (in the spatial hierarchy)
		public bool manage_cell(uint cell_index, bool finalize = false)
		{
			uint adaptive_pos = cell_index / 32;
			uint adaptive_bit = (1u) << (int)(cell_index % 32);
			if (adaptive_pos >= adaptive_alloc)
			{
				if (adaptive != null)
				{
					uint[] old_adaptive = adaptive;
					adaptive = new uint[adaptive_pos * 2];
					Array.Copy(old_adaptive, adaptive, old_adaptive.Length);

					for (uint i = adaptive_alloc; i < adaptive_pos * 2; i++) adaptive[i] = 0;
					adaptive_alloc = adaptive_pos * 2;
				}
				else
				{
					adaptive = new uint[adaptive_pos + 1];

					for (uint i = adaptive_alloc; i <= adaptive_pos; i++) adaptive[i] = 0;
					adaptive_alloc = adaptive_pos + 1;
				}
			}
			adaptive[adaptive_pos] &= ~adaptive_bit;

			uint level = get_level(cell_index);
			uint level_index = get_level_index(cell_index, level);
			while (level != 0)
			{
				level--;
				level_index = level_index >> 2;
				uint index = get_cell_index(level_index, level);
				adaptive_pos = index / 32;
				adaptive_bit = (1u) << (int)(index % 32);
				if ((adaptive[adaptive_pos] & adaptive_bit) != 0) break;
				adaptive[adaptive_pos] |= adaptive_bit;
			}
			return true;
		}

		#region map points to cells
		// check whether the x & y coordinates fall into the tiling
		public bool inside(double x, double y)
		{
			return ((min_x <= x) && (x < max_x) && (min_y <= y) && (y < max_y));
		}

		// returns the index of the cell that x & y fall into
		public uint get_cell_index(double x, double y)
		{
			return get_cell_index(x, y, levels);
		}
		#endregion

		#region map cells to coarser cells
		// returns the indices of parent and siblings for the specified cell index
		public bool coarsen(int cell_index, out int coarser_cell_index, out uint num_cell_indices, out int[/*4*/] cell_indices)
		{
			coarser_cell_index = 0;
			num_cell_indices = 0;
			cell_indices = null;
			if (cell_index < 0) return false;
			uint level = get_level((uint)cell_index);
			if (level == 0) return false;
			uint level_index = get_level_index((uint)cell_index, level);
			level_index = level_index >> 2;
			coarser_cell_index = (int)get_cell_index(level_index, level - 1);
			num_cell_indices = 4;
			cell_indices = new int[4];
			level_index = level_index << 2;
			cell_indices[0] = (int)get_cell_index(level_index + 0, level);
			cell_indices[1] = (int)get_cell_index(level_index + 1, level);
			cell_indices[2] = (int)get_cell_index(level_index + 2, level);
			cell_indices[3] = (int)get_cell_index(level_index + 3, level);

			return true;
		}
		#endregion

		#region describe cells
		// returns the bounding box of the cell with the specified cell_index
		public void get_cell_bounding_box(int cell_index, float[/*2*/] min, float[/*2*/] max)
		{
			uint level = get_level((uint)cell_index);
			uint level_index = get_level_index((uint)cell_index, level);
			get_cell_bounding_box(level_index, level, min, max);
		}

		// returns the bounding box of the cell that x & y fall into
		public void get_cell_bounding_box(double x, double y, float[/*2*/] min, float[/*2*/] max)
		{
			get_cell_bounding_box(x, y, levels, min, max);
		}
		#endregion

		// decribe spatial extend
		public double get_min_x() { return min_x; }
		public double get_min_y() { return min_y; }
		public double get_max_x() { return max_x; }
		public double get_max_y() { return max_y; }

		#region query spatial intersections
		public uint intersect_rectangle(double r_min_x, double r_min_y, double r_max_x, double r_max_y)
		{
			return intersect_rectangle(r_min_x, r_min_y, r_max_x, r_max_y, levels);
		}

		public uint intersect_tile(float ll_x, float ll_y, float size)
		{
			return intersect_tile(ll_x, ll_y, size, levels);
		}

		public uint intersect_circle(double center_x, double center_y, double radius)
		{
			return intersect_circle(center_x, center_y, radius, levels);
		}
		#endregion

		#region iterate over cells
		public bool get_all_cells()
		{
			intersect_rectangle(min_x, min_y, max_x, max_y);
			return get_intersected_cells();
		}

		public bool get_intersected_cells()
		{
			next_cell_index = 0;
			if (current_cells == null)
			{
				return false;
			}
			if (current_cells.Count == 0)
			{
				return false;
			}
			return true;
		}

		public bool has_more_cells()
		{
			if (current_cells == null)
			{
				return false;
			}
			if (next_cell_index >= current_cells.Count)
			{
				return false;
			}
			if (adaptive != null)
			{
				current_cell = current_cells[(int)next_cell_index];
			}
			else
			{
				current_cell = (int)level_offset[levels] + current_cells[(int)next_cell_index];
			}
			next_cell_index++;
			return true;
		}
		#endregion

		#region for LASquadtree
		public bool setup(double bb_min_x, double bb_max_x, double bb_min_y, double bb_max_y, float cell_size = 1000.0f)
		{
			this.cell_size = cell_size;
			this.sub_level = 0;
			this.sub_level_index = 0;

			// enlarge bounding box to units of cells
			if (bb_min_x >= 0) min_x = cell_size * ((int)(bb_min_x / cell_size));
			else min_x = cell_size * ((int)(bb_min_x / cell_size) - 1);
			if (bb_max_x >= 0) max_x = cell_size * ((int)(bb_max_x / cell_size) + 1);
			else max_x = cell_size * ((int)(bb_max_x / cell_size));
			if (bb_min_y >= 0) min_y = cell_size * ((int)(bb_min_y / cell_size));
			else min_y = cell_size * ((int)(bb_min_y / cell_size) - 1);
			if (bb_max_y >= 0) max_y = cell_size * ((int)(bb_max_y / cell_size) + 1);
			else max_y = cell_size * ((int)(bb_max_y / cell_size));

			// how many cells minimally in each direction
			cells_x = MyDefs.U32_QUANTIZE((max_x - min_x) / cell_size);
			cells_y = MyDefs.U32_QUANTIZE((max_y - min_y) / cell_size);

			if (cells_x == 0 || cells_y == 0)
			{
				Console.Error.WriteLine("ERROR: cells_x {0} cells_y {1}", cells_x, cells_y);
				return false;
			}

			// how many quad tree levels to get to that many cells
			uint c = ((cells_x > cells_y) ? cells_x - 1 : cells_y - 1);
			levels = 0;
			while (c != 0)
			{
				c = c >> 1;
				levels++;
			}

			// enlarge bounding box to quad tree size
			uint c1, c2;
			c = (1u << (int)levels) - cells_x;
			c1 = c / 2;
			c2 = c - c1;
			min_x -= (c2 * cell_size);
			max_x += (c1 * cell_size);
			c = (1u << (int)levels) - cells_y;
			c1 = c / 2;
			c2 = c - c1;
			min_y -= (c2 * cell_size);
			max_y += (c1 * cell_size);

			return true;
		}

		public bool setup(double bb_min_x, double bb_max_x, double bb_min_y, double bb_max_y, float cell_size, float offset_x, float offset_y)
		{
			this.cell_size = cell_size;
			this.sub_level = 0;
			this.sub_level_index = 0;

			// enlarge bounding box to units of cells
			if ((bb_min_x - offset_x) >= 0) min_x = cell_size * ((int)((bb_min_x - offset_x) / cell_size)) + offset_x;
			else min_x = cell_size * ((int)((bb_min_x - offset_x) / cell_size) - 1) + offset_x;
			if ((bb_max_x - offset_x) >= 0) max_x = cell_size * ((int)((bb_max_x - offset_x) / cell_size) + 1) + offset_x;
			else max_x = cell_size * ((int)((bb_max_x - offset_x) / cell_size)) + offset_x;
			if ((bb_min_y - offset_y) >= 0) min_y = cell_size * ((int)((bb_min_y - offset_y) / cell_size)) + offset_y;
			else min_y = cell_size * ((int)((bb_min_y - offset_y) / cell_size) - 1) + offset_y;
			if ((bb_max_y - offset_y) >= 0) max_y = cell_size * ((int)((bb_max_y - offset_y) / cell_size) + 1) + offset_y;
			else max_y = cell_size * ((int)((bb_max_y - offset_y) / cell_size)) + offset_y;

			// how many cells minimally in each direction
			cells_x = MyDefs.U32_QUANTIZE((max_x - min_x) / cell_size);
			cells_y = MyDefs.U32_QUANTIZE((max_y - min_y) / cell_size);

			if (cells_x == 0 || cells_y == 0)
			{
				Console.Error.WriteLine("ERROR: cells_x {0} cells_y {1}", cells_x, cells_y);
				return false;
			}

			// how many quad tree levels to get to that many cells
			uint c = ((cells_x > cells_y) ? cells_x - 1 : cells_y - 1);
			levels = 0;
			while (c != 0)
			{
				c = c >> 1;
				levels++;
			}

			// enlarge bounding box to quad tree size
			uint c1, c2;
			c = (1u << (int)levels) - cells_x;
			c1 = c / 2;
			c2 = c - c1;
			min_x -= (c2 * cell_size);
			max_x += (c1 * cell_size);
			c = (1u << (int)levels) - cells_y;
			c1 = c / 2;
			c2 = c - c1;
			min_y -= (c2 * cell_size);
			max_y += (c1 * cell_size);

			return true;
		}

		public bool tiling_setup(float min_x, float max_x, float min_y, float max_y, uint levels)
		{
			this.min_x = min_x;
			this.max_x = max_x;
			this.min_y = min_y;
			this.max_y = max_y;
			this.levels = levels;
			this.sub_level = 0;
			this.sub_level_index = 0;
			return true;
		}

		public bool subtiling_setup(float min_x, float max_x, float min_y, float max_y, uint sub_level, uint sub_level_index, uint levels)
		{
			this.min_x = min_x;
			this.max_x = max_x;
			this.min_y = min_y;
			this.max_y = max_y;
			float[] min = new float[2];
			float[] max = new float[2];
			get_cell_bounding_box(sub_level_index, sub_level, min, max);
			this.min_x = min[0];
			this.max_x = max[0];
			this.min_y = min[1];
			this.max_y = max[1];
			this.sub_level = sub_level;
			this.sub_level_index = sub_level_index;
			this.levels = levels;
			return true;
		}
		#endregion

		#region additional index queries
		// returns the (sub-)level index of the cell that x & y fall into at the specified level
		public uint get_level_index(double x, double y, uint level)
		{
			float cell_min_x = min_x;
			float cell_max_x = max_x;
			float cell_min_y = min_y;
			float cell_max_y = max_y;

			uint level_index = 0;

			while (level != 0)
			{
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (x < cell_mid_x)
				{
					cell_max_x = cell_mid_x;
				}
				else
				{
					cell_min_x = cell_mid_x;
					level_index |= 1;
				}
				if (y < cell_mid_y)
				{
					cell_max_y = cell_mid_y;
				}
				else
				{
					cell_min_y = cell_mid_y;
					level_index |= 2;
				}
				level--;
			}

			return level_index;
		}

		// returns the (sub-)level index of the cell that x & y fall into
		public uint get_level_index(double x, double y)
		{
			return get_level_index(x, y, levels);
		}

		// returns the (sub-)level index and the bounding box of the cell that x & y fall into at the specified level
		public uint get_level_index(double x, double y, uint level, float[/*2*/] min, float[/*2*/] max)
		{
			float cell_min_x = min_x;
			float cell_max_x = max_x;
			float cell_min_y = min_y;
			float cell_max_y = max_y;

			uint level_index = 0;

			while (level != 0)
			{
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (x < cell_mid_x)
				{
					cell_max_x = cell_mid_x;
				}
				else
				{
					cell_min_x = cell_mid_x;
					level_index |= 1;
				}
				if (y < cell_mid_y)
				{
					cell_max_y = cell_mid_y;
				}
				else
				{
					cell_min_y = cell_mid_y;
					level_index |= 2;
				}
				level--;
			}
			if (min != null && min.Length >= 2)
			{
				min[0] = cell_min_x;
				min[1] = cell_min_y;
			}
			if (max != null && max.Length >= 2)
			{
				max[0] = cell_max_x;
				max[1] = cell_max_y;
			}
			return level_index;
		}

		// returns the (sub-)level index and the bounding box of the cell that x & y fall into
		public uint get_level_index(double x, double y, float[/*2*/] min, float[/*2*/] max)
		{
			return get_level_index(x, y, levels, min, max);
		}

		// returns the index of the cell that x & y fall into at the specified level
		public uint get_cell_index(double x, double y, uint level)
		{
			if (sub_level != 0)
			{
				return level_offset[sub_level + level] + (sub_level_index << (int)(level * 2)) + get_level_index(x, y, level);
			}
			else
			{
				return level_offset[level] + get_level_index(x, y, level);
			}
		}
		#endregion

		#region additional bounding box queries
		// returns the bounding box of the cell that x & y fall into at the specified level
		public void get_cell_bounding_box(double x, double y, uint level, float[/*2*/] min, float[/*2*/] max)
		{
			float cell_min_x = min_x;
			float cell_max_x = max_x;
			float cell_min_y = min_y;
			float cell_max_y = max_y;

			while (level != 0)
			{
				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;
				if (x < cell_mid_x)
				{
					cell_max_x = cell_mid_x;
				}
				else
				{
					cell_min_x = cell_mid_x;
				}
				if (y < cell_mid_y)
				{
					cell_max_y = cell_mid_y;
				}
				else
				{
					cell_min_y = cell_mid_y;
				}
				level--;
			}
			if (min != null && min.Length >= 2)
			{
				min[0] = cell_min_x;
				min[1] = cell_min_y;
			}
			if (max != null && max.Length >= 2)
			{
				max[0] = cell_max_x;
				max[1] = cell_max_y;
			}
		}

		// returns the bounding box of the cell with the specified level_index at the specified level
		public void get_cell_bounding_box(uint level_index, uint level, float[/*2*/] min, float[/*2*/] max)
		{
			float cell_min_x = min_x;
			float cell_max_x = max_x;
			float cell_min_y = min_y;
			float cell_max_y = max_y;

			while (level != 0)
			{
				uint index = (level_index >> (int)(2 * (level - 1))) & 3;
				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;
				if ((index & 1) != 0)
				{
					cell_min_x = cell_mid_x;
				}
				else
				{
					cell_max_x = cell_mid_x;
				}
				if ((index & 2) != 0)
				{
					cell_min_y = cell_mid_y;
				}
				else
				{
					cell_max_y = cell_mid_y;
				}
				level--;
			}
			if (min != null && min.Length >= 2)
			{
				min[0] = cell_min_x;
				min[1] = cell_min_y;
			}
			if (max != null && max.Length >= 2)
			{
				max[0] = cell_max_x;
				max[1] = cell_max_y;
			}
		}

		// returns the bounding box of the cell with the specified level_index at the specified level
		public void get_cell_bounding_box(uint level_index, uint level, double[/*2*/] min, double[/*2*/] max)
		{
			double cell_min_x = min_x;
			double cell_max_x = max_x;
			double cell_min_y = min_y;
			double cell_max_y = max_y;

			while (level != 0)
			{
				uint index = (level_index >> (int)(2 * (level - 1))) & 3;
				double cell_mid_x = (cell_min_x + cell_max_x) / 2;
				double cell_mid_y = (cell_min_y + cell_max_y) / 2;
				if ((index & 1) != 0)
				{
					cell_min_x = cell_mid_x;
				}
				else
				{
					cell_max_x = cell_mid_x;
				}
				if ((index & 2) != 0)
				{
					cell_min_y = cell_mid_y;
				}
				else
				{
					cell_max_y = cell_mid_y;
				}
				level--;
			}
			if (min != null && min.Length >= 2)
			{
				min[0] = cell_min_x;
				min[1] = cell_min_y;
			}
			if (max != null && max.Length >= 2)
			{
				max[0] = cell_max_x;
				max[1] = cell_max_y;
			}
		}

		// returns the bounding box of the cell with the specified level_index
		public void get_cell_bounding_box(uint level_index, float[/*2*/] min, float[/*2*/] max)
		{
			get_cell_bounding_box(level_index, levels, min, max);
		}

		// returns the bounding box of the cell with the specified level_index
		public void get_cell_bounding_box(uint level_index, double[/*2*/] min, double[/*2*/] max)
		{
			get_cell_bounding_box(level_index, levels, min, max);
		}
		#endregion

		#region index conversions
		// returns the level the cell index
		public uint get_level(uint cell_index)
		{
			uint level = 0;
			while (cell_index >= level_offset[level + 1]) level++;
			return level;
		}

		// returns the level index of the cell index at the specified level
		public uint get_level_index(uint cell_index, uint level)
		{
			if (sub_level != 0)
			{
				return cell_index - (sub_level_index << (int)(level * 2)) - level_offset[sub_level + level];
			}
			else
			{
				return cell_index - level_offset[level];
			}
		}

		// returns the level index of the cell index
		public uint get_level_index(uint cell_index)
		{
			return get_level_index(cell_index, levels);
		}

		// returns the cell index of the level index at the specified level
		public uint get_cell_index(uint level_index, uint level)
		{
			if (sub_level != 0)
			{
				return level_index + (sub_level_index << (int)(level * 2)) + level_offset[sub_level + level];
			}
			else
			{
				return level_index + level_offset[level];
			}
		}

		// returns the cell index of the level index
		public uint get_cell_index(uint level_index)
		{
			return get_cell_index(level_index, levels);
		}
		#endregion

		#region convenience functions
		// returns the maximal level index at the specified level
		public uint get_max_level_index(uint level)
		{
			return (1u << (int)level) * (1u << (int)level);
		}

		// returns the maximal level index
		public uint get_max_level_index()
		{
			return get_max_level_index(levels);
		}

		// returns the maximal cell index at the specified level
		public uint get_max_cell_index(uint level)
		{
			return level_offset[level + 1] - 1;
		}

		// returns the maximal cell index
		public uint get_max_cell_index()
		{
			return get_max_cell_index(levels);
		}

		// rasters the occupancy to a simple binary raster at depth level
		public uint[] raster_occupancy(does_cell_exist_proc does_cell_exist, uint level)
		{
			uint size_xy = (1u << (int)level);
			uint temp_size = (size_xy * size_xy + 31) / 32;
			uint[] data;
			try
			{
				data = new uint[temp_size];
			}
			catch
			{
				return null;
			}

			raster_occupancy(does_cell_exist, data, 0, 0, 0, 0, level);

			return data;
		}

		// rasters the occupancy to a simple binary raster at depth levels
		public uint[] raster_occupancy(does_cell_exist_proc does_cell_exist)
		{
			return raster_occupancy(does_cell_exist, levels);
		}
		#endregion

		public uint levels;
		public float cell_size;
		public float min_x;
		public float max_x;
		public float min_y;
		public float max_y;
		public uint cells_x;
		public uint cells_y;

		#region spatial queries
		public uint intersect_rectangle(double r_min_x, double r_min_y, double r_max_x, double r_max_y, uint level)
		{
			if (current_cells == null)
			{
				current_cells = new List<int>();
			}
			else
			{
				current_cells.Clear();
			}

			if (r_max_x <= min_x || !(r_min_x <= max_x) || r_max_y <= min_y || !(r_min_y <= max_y))
			{
				return 0;
			}

			if (adaptive != null)
			{
				intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, min_x, max_x, min_y, max_y, 0, 0);
			}
			else
			{
				intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, min_x, max_x, min_y, max_y, level, 0);
			}

			return (uint)current_cells.Count;
		}

		public uint intersect_tile(float ll_x, float ll_y, float size, uint level)
		{
			if (current_cells == null)
			{
				current_cells = new List<int>();
			}
			else
			{
				current_cells.Clear();
			}

			float ur_x = ll_x + size;
			float ur_y = ll_y + size;

			if (ur_x <= min_x || !(ll_x <= max_x) || ur_y <= min_y || !(ll_y <= max_y))
			{
				return 0;
			}

			if (adaptive != null)
			{
				intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, min_x, max_x, min_y, max_y, 0, 0);
			}
			else
			{
				intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, min_x, max_x, min_y, max_y, level, 0);
			}

			return (uint)current_cells.Count;
		}

		public uint intersect_circle(double center_x, double center_y, double radius, uint level)
		{
			if (current_cells == null)
			{
				current_cells = new List<int>();
			}
			else
			{
				current_cells.Clear();
			}

			double r_min_x = center_x - radius;
			double r_min_y = center_y - radius;
			double r_max_x = center_x + radius;
			double r_max_y = center_y + radius;

			if (r_max_x <= min_x || !(r_min_x <= max_x) || r_max_y <= min_y || !(r_min_y <= max_y))
			{
				return 0;
			}

			if (adaptive != null)
			{

				intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, min_x, max_x, min_y, max_y, 0, 0);
			}
			else
			{

				intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, min_x, max_x, min_y, max_y, level, 0);
			}

			return (uint)current_cells.Count;
		}
		#endregion

		public int current_cell;

		uint sub_level;
		uint sub_level_index;
		readonly uint[] level_offset = new uint[24];
		uint adaptive_alloc;
		uint[] adaptive;

		void intersect_rectangle_with_cells(double r_min_x, double r_min_y, double r_max_x, double r_max_y, float cell_min_x, float cell_max_x, float cell_min_y, float cell_max_y, uint level, uint level_index)
		{
			if (level != 0)
			{
				level--;
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (r_max_x <= cell_mid_x)
				{
					// cell_max_x = cell_mid_x;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
					else
					{
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
				}
				else if (!(r_min_x < cell_mid_x))
				{
					// cell_min_x = cell_mid_x;
					// level_index |= 1;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
				else
				{
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_rectangle_with_cells(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
			}
			else
			{
				current_cells.Add((int)level_index);
			}
		}

		void intersect_rectangle_with_cells_adaptive(double r_min_x, double r_min_y, double r_max_x, double r_max_y, float cell_min_x, float cell_max_x, float cell_min_y, float cell_max_y, uint level, uint level_index)
		{
			uint cell_index = get_cell_index(level_index, level);
			uint adaptive_pos = cell_index / 32;
			uint adaptive_bit = (1u) << (int)(cell_index % 32);
			if ((level < levels) && (adaptive[adaptive_pos] & adaptive_bit) != 0)
			{
				level++;
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (r_max_x <= cell_mid_x)
				{
					// cell_max_x = cell_mid_x;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
					else
					{
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
				}
				else if (!(r_min_x < cell_mid_x))
				{
					// cell_min_x = cell_mid_x;
					// level_index |= 1;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
				else
				{
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_rectangle_with_cells_adaptive(r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
			}
			else
			{
				current_cells.Add((int)cell_index);
			}
		}

		void intersect_tile_with_cells(float ll_x, float ll_y, float ur_x, float ur_y, float cell_min_x, float cell_max_x, float cell_min_y, float cell_max_y, uint level, uint level_index)
		{
			if (level != 0)
			{
				level--;
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (ur_x <= cell_mid_x)
				{
					// cell_max_x = cell_mid_x;
					if (ur_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
					}
					else if (!(ll_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
					else
					{
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
				}
				else if (!(ll_x < cell_mid_x))
				{
					// cell_min_x = cell_mid_x;
					// level_index |= 1;
					if (ur_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(ll_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
				else
				{
					if (ur_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(ll_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_tile_with_cells(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
			}
			else
			{
				current_cells.Add((int)level_index);
			}
		}

		void intersect_tile_with_cells_adaptive(float ll_x, float ll_y, float ur_x, float ur_y, float cell_min_x, float cell_max_x, float cell_min_y, float cell_max_y, uint level, uint level_index)
		{
			uint cell_index = get_cell_index(level_index, level);
			uint adaptive_pos = cell_index / 32;
			uint adaptive_bit = (1u) << (int)(cell_index % 32);
			if ((level < levels) && (adaptive[adaptive_pos] & adaptive_bit) != 0)
			{
				level++;
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (ur_x <= cell_mid_x)
				{
					// cell_max_x = cell_mid_x;
					if (ur_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
					}
					else if (!(ll_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
					else
					{
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
				}
				else if (!(ll_x < cell_mid_x))
				{
					// cell_min_x = cell_mid_x;
					// level_index |= 1;
					if (ur_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(ll_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
				else
				{
					if (ur_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(ll_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_tile_with_cells_adaptive(ll_x, ll_y, ur_x, ur_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
			}
			else
			{
				current_cells.Add((int)cell_index);
			}
		}

		void intersect_circle_with_cells(double center_x, double center_y, double radius, double r_min_x, double r_min_y, double r_max_x, double r_max_y, float cell_min_x, float cell_max_x, float cell_min_y, float cell_max_y, uint level, uint level_index)
		{
			if (level != 0)
			{
				level--;
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (r_max_x <= cell_mid_x)
				{
					// cell_max_x = cell_mid_x;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
					else
					{

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
				}
				else if (!(r_min_x < cell_mid_x))
				{
					// cell_min_x = cell_mid_x;
					// level_index |= 1;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
				else
				{
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);

						intersect_circle_with_cells(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
			}
			else
			{
				if (intersect_circle_with_rectangle(center_x, center_y, radius, cell_min_x, cell_max_x, cell_min_y, cell_max_y))
				{
					current_cells.Add((int)level_index);
				}
			}
		}

		void intersect_circle_with_cells_adaptive(double center_x, double center_y, double radius, double r_min_x, double r_min_y, double r_max_x, double r_max_y, float cell_min_x, float cell_max_x, float cell_min_y, float cell_max_y, uint level, uint level_index)
		{
			uint cell_index = get_cell_index(level_index, level);
			uint adaptive_pos = cell_index / 32;
			uint adaptive_bit = (1u) << (int)(cell_index % 32);
			if ((level < levels) && (adaptive[adaptive_pos] & adaptive_bit) != 0)
			{
				level++;
				level_index <<= 2;

				float cell_mid_x = (cell_min_x + cell_max_x) / 2;
				float cell_mid_y = (cell_min_y + cell_max_y) / 2;

				if (r_max_x <= cell_mid_x)
				{
					// cell_max_x = cell_mid_x;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
					else
					{
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
					}
				}
				else if (!(r_min_x < cell_mid_x))
				{
					// cell_min_x = cell_mid_x;
					// level_index |= 1;
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
				else
				{
					if (r_max_y <= cell_mid_y)
					{
						// cell_max_y = cell_mid_y;
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
					}
					else if (!(r_min_y < cell_mid_y))
					{
						// cell_min_y = cell_mid_y;
						// level_index |= 1;
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
					else
					{
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_min_y, cell_mid_y, level, level_index);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_min_y, cell_mid_y, level, level_index | 1);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_min_x, cell_mid_x, cell_mid_y, cell_max_y, level, level_index | 2);
						intersect_circle_with_cells_adaptive(center_x, center_y, radius, r_min_x, r_min_y, r_max_x, r_max_y, cell_mid_x, cell_max_x, cell_mid_y, cell_max_y, level, level_index | 3);
					}
				}
			}
			else
			{
				if (intersect_circle_with_rectangle(center_x, center_y, radius, cell_min_x, cell_max_x, cell_min_y, cell_max_y))
				{
					current_cells.Add((int)cell_index);
				}
			}
		}

		bool intersect_circle_with_rectangle(double center_x, double center_y, double radius, float r_min_x, float r_max_x, float r_min_y, float r_max_y)
		{
			double radius_squared = radius * radius;
			if (r_max_x < center_x) // R to left of circle center
			{
				double r_diff_x = center_x - r_max_x;
				if (r_max_y < center_y) // R in lower left corner
				{
					double r_diff_y = center_y - r_max_y;
					return ((r_diff_x * r_diff_x + r_diff_y * r_diff_y) < radius_squared);
				}
				else if (r_min_y > center_y) // R in upper left corner
				{
					double r_diff_y = -center_y + r_min_y;
					return ((r_diff_x * r_diff_x + r_diff_y * r_diff_y) < radius_squared);
				}
				else // R due West of circle
				{
					return (r_diff_x < radius);
				}
			}
			else if (r_min_x > center_x) // R to right of circle center
			{
				double r_diff_x = -center_x + r_min_x;
				if (r_max_y < center_y) // R in lower right corner
				{
					double r_diff_y = center_y - r_max_y;
					return ((r_diff_x * r_diff_x + r_diff_y * r_diff_y) < radius_squared);
				}
				else if (r_min_y > center_y) // R in upper right corner
				{
					double r_diff_y = -center_y + r_min_y;
					return ((r_diff_x * r_diff_x + r_diff_y * r_diff_y) < radius_squared);
				}
				else // R due East of circle
				{
					return (r_diff_x < radius);
				}
			}
			else // R on circle vertical centerline
			{
				if (r_max_y < center_y) // R due South of circle
				{
					double r_diff_y = center_y - r_max_y;
					return (r_diff_y < radius);
				}
				else if (r_min_y > center_y) // R due North of circle
				{
					double r_diff_y = -center_y + r_min_y;
					return (r_diff_y < radius);
				}
				else // R contains circle centerpoint
				{
					return true;
				}
			}
		}

		// recursively does the actual rastering of the occupancy
		void raster_occupancy(does_cell_exist_proc does_cell_exist, uint[] data, uint min_x, uint min_y, uint level_index, uint level, uint stop_level)
		{
			uint cell_index = get_cell_index(level_index, level);
			uint adaptive_pos = cell_index / 32;
			uint adaptive_bit = 1u << (int)(cell_index % 32);
			// have we reached a leaf
			if ((adaptive[adaptive_pos] & adaptive_bit) != 0) // interior node
			{
				if (level < stop_level) // do we need to continue
				{
					level_index <<= 2;
					level += 1;
					uint size = 1u << (int)(stop_level - level);
					// recurse into the four children
					raster_occupancy(does_cell_exist, data, min_x, min_y, level_index, level, stop_level);
					raster_occupancy(does_cell_exist, data, min_x + size, min_y, level_index + 1, level, stop_level);
					raster_occupancy(does_cell_exist, data, min_x, min_y + size, level_index + 2, level, stop_level);
					raster_occupancy(does_cell_exist, data, min_x + size, min_y + size, level_index + 3, level, stop_level);
					return;
				}
				else // no ... raster remaining subtree
				{
					uint full_size = (1u << (int)stop_level);
					uint size = 1u << (int)(stop_level - level);
					uint max_y = min_y + size;
					uint pos, pos_x, pos_y;
					for (pos_y = min_y; pos_y < max_y; pos_y++)
					{
						pos = pos_y * full_size + min_x;
						for (pos_x = 0; pos_x < size; pos_x++)
						{
							data[pos / 32] |= (1u << (int)(pos % 32));
							pos++;
						}
					}
				}
			}
			else if (does_cell_exist((int)cell_index))
			{
				// raster actual cell
				uint full_size = (1u << (int)stop_level);
				uint size = 1u << (int)(stop_level - level);
				uint max_y = min_y + size;
				uint pos, pos_x, pos_y;
				for (pos_y = min_y; pos_y < max_y; pos_y++)
				{
					pos = pos_y * full_size + min_x;
					for (pos_x = 0; pos_x < size; pos_x++)
					{
						data[pos / 32] |= (1u << (int)(pos % 32));
						pos++;
					}
				}
			}
		}

		List<int> current_cells;

		uint next_cell_index;
	}
}
