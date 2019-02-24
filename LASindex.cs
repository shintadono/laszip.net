//===============================================================================
//
//  FILE:  lasindex.cs
//
//  CONTENTS:
//
//    This class can create a spatial indexing, store a spatial indexing, write
//    a spatial indexing to file, read a spatial indexing from file, and - most
//    importantly - it can be used together with a lasreader for efficient access
//    to a particular spatial region of a LAS file or a LAZ file.
//
//  PROGRAMMERS:
//
//    martin.isenburg @rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2017, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2014-2018 by Shinta <shintadono@googlemail.com>
//
//    This is free software; you can redistribute and/or modify it under the
//    terms of the GNU Lesser General Licence as published by the Free Software
//    Foundation.See the LICENSE.txt file for more information.
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
	class LASindex : IDisposable
	{
		public uint start = 0, end = 0, full = 0, total = 0, cells = 0;
		private LASquadtree spatial = null;
		private LASinterval interval = null;
		private bool have_interval = false;

		public LASindex() { }

		public void Dispose()
		{
			spatial = null;
			interval = null;
		}

		// create spatial index
		public void prepare(LASquadtree spatial, int threshold = 1000)
		{
			this.spatial = spatial;
			interval = new LASinterval((uint)threshold);
		}

		public bool add(double x, double y, uint p_index)
		{
			int cell = (int)spatial.get_cell_index(x, y);
			return interval.add(p_index, cell);
		}

		public void complete(uint minimum_points = 100000, int maximum_intervals = -1, bool verbose = true)
		{
			if (verbose)
			{
				Console.Error.WriteLine("before complete {0}{1}", minimum_points, maximum_intervals);
				print(false);
			}
			if (minimum_points != 0)
			{
				int hash1 = 0;
				Dictionary<int, uint>[] cell_hash = new Dictionary<int, uint>[2];
				cell_hash[0] = new Dictionary<int, uint>();
				cell_hash[1] = new Dictionary<int, uint>();

				// insert all cells into hash1
				interval.get_cells();
				while (interval.has_cells())
				{
					cell_hash[hash1][interval.index] = interval.full;
				}

				while (cell_hash[hash1].Count != 0)
				{
					int hash2 = (hash1 + 1) % 2;
					cell_hash[hash2].Clear();
					// coarsen if a coarser cell will still have fewer than minimum_points (and points in all subcells)
					bool coarsened = false;

					HashSet<int> zeroed = new HashSet<int>(); // Because we can't change the dict while iterating, we remember the entries set to zero.
					foreach (var hash_element_outer in cell_hash[hash1])
					{
						if (hash_element_outer.Value != 0 && !zeroed.Contains(hash_element_outer.Key))
						{
							int coarser_index;
							uint num_indices;
							int[] indices;
							if (spatial.coarsen(hash_element_outer.Key, out coarser_index, out num_indices, out indices))
							{
								uint full = 0, num_filled = 0;
								for (uint i = 0; i < num_indices; i++)
								{
									if (hash_element_outer.Key == indices[i])
									{
										full += hash_element_outer.Value;
										zeroed.Add(hash_element_outer.Key);
										num_filled++;
										continue;
									}

									if (zeroed.Contains(indices[i])) // Already handled and set to zero before.
									{
										num_filled++;
										continue;
									}

									uint hash_element_inner;
									if (cell_hash[hash1].TryGetValue(indices[i], out hash_element_inner))
									{
										full += hash_element_inner;
										zeroed.Add(indices[i]);
										num_filled++;
									}
								}

								if (full < minimum_points && num_filled == num_indices)
								{
									interval.merge_cells(indices, coarser_index);
									coarsened = true;
									cell_hash[hash2][coarser_index] = full;
								}
							}
						}
					}

					if (!coarsened) break;

					hash1 = (hash1 + 1) % 2;
				}

				// tell spatial about the existing cells
				interval.get_cells();
				while (interval.has_cells())
				{
					spatial.manage_cell((uint)interval.index);
				}

				if (verbose)
				{
					Console.Error.WriteLine("after minimum_points {0}", minimum_points);
					print(false);
				}
			}

			if (maximum_intervals < 0)
			{
				maximum_intervals = -maximum_intervals * (int)interval.get_number_cells();
			}

			if (maximum_intervals != 0)
			{
				interval.merge_intervals((uint)maximum_intervals, verbose);
				if (verbose)
				{
					Console.Error.WriteLine("after maximum_intervals {0}", maximum_intervals);
					print(false);
				}
			}
		}

		// read from file or write to file
		public bool read(string file_name)
		{
			if (string.IsNullOrWhiteSpace(file_name)) return false;

			string name = file_name;
			if (file_name.EndsWith(".las") || file_name.EndsWith(".laz"))
			{
				name = name.Substring(0, name.Length - 1) + 'x';
			}
			else if (file_name.EndsWith(".LAS") || file_name.EndsWith(".LAZ"))
			{
				name = name.Substring(0, name.Length - 1) + 'X';
			}
			else
			{
				// Replace extension when the last dot is after the last folder separator,
				// otherwise, append ".lax".
				int lastFolderSepatator = name.LastIndexOfAny(new char[] { '/', '\\' });
				int lastDot = name.LastIndexOf('.');

				// Remove existing extension.
				if (lastDot > lastFolderSepatator) name = name.Substring(0, lastDot);

				// Add new extension.
				name += ".lax";
			}

			try
			{
				using (var file = File.OpenRead(name))
				{
					if (!read(file))
					{
						Console.Error.WriteLine("ERROR (LASindex): cannot read '{0}'", name);
						return false;
					}
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public bool append(string file_name) { return false; }

		public bool write(string file_name)
		{
			if (string.IsNullOrWhiteSpace(file_name)) return false;

			string name = file_name;
			if (file_name.EndsWith(".las") || file_name.EndsWith(".laz"))
			{
				name = name.Substring(0, name.Length - 1) + 'x';
			}
			else if (file_name.EndsWith(".LAS") || file_name.EndsWith(".LAZ"))
			{
				name = name.Substring(0, name.Length - 1) + 'X';
			}
			else
			{
				// Replace extension when the last dot is after the last folder separator,
				// otherwise, append ".lax".
				int lastFolderSepatator = name.LastIndexOfAny(new char[] { '/', '\\' });
				int lastDot = name.LastIndexOf('.');

				// Remove existing extension.
				if (lastDot > lastFolderSepatator) name = name.Substring(0, lastDot);

				// Add new extension.
				name += ".lax";
			}

			try
			{
				using (var file = File.OpenWrite(name))
				{
					if (!write(file))
					{
						Console.Error.WriteLine("ERROR (LASindex): cannot open '{0} for write'", name);
						return false;
					}
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		public bool read(Stream stream)
		{
			if (interval != null) interval.Dispose();

			byte[] signature = new byte[4];
			if(!stream.getBytes(signature, 4))
			{
				Console.Error.WriteLine("ERROR (LASindex): reading signature");
				return false;
			}
			if (signature[0] != 'L' || signature[1] != 'A' || signature[2] != 'S' || signature[3] != 'S')
			{
				Console.Error.WriteLine("ERROR (LASindex): wrong signature '{0}{1}{3}{4}' instead of 'LASX'", (char)signature[0], (char)signature[1], (char)signature[2], (char)signature[3]);
				return false;
			}

			uint version;
			if (!stream.get32bits(out version))
			{
				Console.Error.WriteLine("ERROR (LASindex): reading version");
				return false;
			}

			// read spatial quadtree
			spatial = new LASquadtree();
			if (!spatial.read(stream))
			{
				Console.Error.WriteLine("ERROR (LASindex): cannot read LASspatial (LASquadtree)");
				return false;
			}

			// read interval
			interval = new LASinterval();
			if (!interval.read(stream))
			{
				Console.Error.WriteLine("ERROR (LASindex): reading LASinterval");
				return false;
			}

			// tell spatial about the existing cells
			interval.get_cells();
			while (interval.has_cells())
			{
				spatial.manage_cell((uint)interval.index);
			}

			return true;
		}

		public bool write(Stream stream)
		{
			byte[] tmp = new byte[4];
			tmp[0] = (byte)'L';
			tmp[1] = (byte)'A';
			tmp[2] = (byte)'S';
			tmp[3] = (byte)'X';
			try
			{
				stream.Write(tmp, 0, 4);
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASindex): writing signature");
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
				Console.Error.WriteLine("ERROR (LASindex): writing version");
				return false;
			}

			// write spatial quadtree
			if (!spatial.write(stream))
			{
				Console.Error.WriteLine("ERROR (LASindex): cannot write LASspatial (LASquadtree)");
				return false;
			}

			// write interval
			if (!interval.write(stream))
			{
				Console.Error.WriteLine("ERROR (LASindex): writing LASinterval");
				return false;
			}

			return true;
		}

		// intersect
		public bool intersect_rectangle(double r_min_x, double r_min_y, double r_max_x, double r_max_y)
		{
			have_interval = false;
			cells = spatial.intersect_rectangle(r_min_x, r_min_y, r_max_x, r_max_y);
			//Console.Error.WriteLine("{0} cells of {1}/{2} {3}/{4} intersect rect {5}/{6} {7}/{8}", num_cells, spatial.get_min_x(), spatial.get_min_y(), spatial.get_max_x(), spatial.get_max_y(), r_min_x, r_min_y, r_max_x, r_max_y);
			if (cells != 0) return merge_intervals();
			return false;
		}

		public bool intersect_tile(float ll_x, float ll_y, float size)
		{
			have_interval = false;
			cells = spatial.intersect_tile(ll_x, ll_y, size);
			//Console.Error.WriteLine("{0} cells of {1}/{2} {3}/{4} intersect tile {5}/{6}/{7}", num_cells, spatial.get_min_x(), spatial.get_min_y(), spatial.get_max_x(), spatial.get_max_y(), ll_x, ll_y, size);
			if (cells != 0) return merge_intervals();
			return false;
		}

		public bool intersect_circle(double center_x, double center_y, double radius)
		{
			have_interval = false;
			cells = spatial.intersect_circle(center_x, center_y, radius);
			//Console.Error.WriteLine("{0} cells of {1}/{2} {3}/{4} intersect circle {5}/{6}/{7}", num_cells, spatial.get_min_x(), spatial.get_min_y(), spatial.get_max_x(), spatial.get_max_y(), center_x, center_y, radius);
			if (cells != 0) return merge_intervals();
			return false;
		}

		// access the intersected intervals
		public bool get_intervals()
		{
			have_interval = false;
			return interval.get_merged_cell();
		}

		public bool has_intervals()
		{
			if (interval.has_intervals())
			{
				start = interval.start;
				end = interval.end;
				full = interval.full;
				have_interval = true;
				return true;
			}
			have_interval = false;
			return false;
		}

		// seek to next interval point
		public bool seek_next(LASreadPoint reader, ref long p_count)
		{
			if (!have_interval)
			{
				if (!has_intervals()) return false;
				reader.seek((uint)p_count, start);
				p_count = start;
			}
			if (p_count == end)
			{
				have_interval = false;
			}
			return true;
		}

		// for debugging
		public void print(bool verbose)
		{
			uint total_cells = 0, total_full = 0, total_total = 0, total_intervals = 0;

			interval.get_cells();
			while (interval.has_cells())
			{
				uint total_check = 0, intervals = 0;
				while (interval.has_intervals())
				{
					total_check += interval.end - interval.start + 1;
					intervals++;
				}

				if (total_check != interval.total) Console.Error.WriteLine("ERROR: total_check {0} != interval->total {1}", total_check, interval.total);
				if (verbose) Console.Error.WriteLine("cell {0} intervals {1} full {2} total {3} ({4:f2})", interval.index, intervals, interval.full, interval.total, (100.0 * interval.full) / interval.total);

				total_cells++;
				total_full += interval.full;
				total_total += interval.total;
				total_intervals += intervals;
			}

			if (verbose) Console.Error.WriteLine("total cells/intervals {0}/{1} full {2} ({3:f2})", total_cells, total_intervals, total_full, 100.0 * total_full / total_total);
		}

		// for visualization
		public LASquadtree get_spatial() { return spatial; }
		public LASinterval get_interval() { return interval; }

		// merge the intervals of non-empty cells
		private bool merge_intervals()
		{
			if (spatial.get_intersected_cells())
			{
				uint used_cells = 0;
				while (spatial.has_more_cells())
				{
					if (interval.get_cell(spatial.current_cell))
					{
						interval.add_current_cell_to_merge_cell_set();
						used_cells++;
					}
				}

				//Console.Error.WriteLine("LASindex: used {0} cells of total {1}", used_cells, interval.get_number_cells());

				if (used_cells != 0)
				{
					bool r = interval.merge();
					full = interval.full;
					total = interval.total;
					interval.clear_merge_cell_set();
					return r;
				}
			}

			return false;
		}
	}
}
