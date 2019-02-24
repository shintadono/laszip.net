//===============================================================================
//
//  FILE:  lasinterval.hpp
//
//  CONTENTS:
//
//    Used by lasindex to manage intervals of consecutive LiDAR points that are
//    read sequentially.
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2007-2015, martin isenburg, rapidlasso - fast tools to catch reality
//    (c) of the C# port 2014-2019 by Shinta <shintadono@googlemail.com>
//
//    This is free software; you can redistribute and/or modify it under the
//    terms of the GNU Lesser General Licence as published by the Free Software
//    Foundation. See the LICENSE.txt file for more information.
//
//    This software is distributed WITHOUT ANY WARRANTY and without even the
//    implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//
//  CHANGE HISTORY: omitted for easier Copy&Paste (pls see the original)
//===============================================================================

using System;
using System.Collections.Generic;
using System.IO;

namespace LASzip.Net
{
	class LASintervalCell
	{
		public uint start = 0, end = 0;
		public LASintervalCell next = null;

		public LASintervalCell() { }

		public LASintervalCell(uint p_index)
		{
			start = end = p_index;
		}

		public LASintervalCell(LASintervalCell cell)
		{
			start = cell.start;
			end = cell.end;
		}
	}

	class LASintervalStartCell : LASintervalCell
	{
		public uint full = 0, total = 0;
		public LASintervalCell last = null;

		public LASintervalStartCell() { }

		public LASintervalStartCell(uint p_index) : base(p_index)
		{
			full = total = 1;
		}

		public bool add(uint p_index, uint threshold = 1000)
		{
			uint current_end = last?.end ?? end;
			//assert(p_index > current_end);

			uint diff = p_index - current_end;
			full++;
			if (diff > threshold)
			{
				if (last != null)
				{
					last.next = new LASintervalCell(p_index);
					last = last.next;
				}
				else
				{
					next = new LASintervalCell(p_index);
					last = next;
				}
				total++;
				return true; // created new interval
			}

			if (last != null) last.end = p_index;
			else end = p_index;
			total += diff;
			return false; // added to interval
		}
	}

	class LASinterval : IDisposable
	{
		public int index;
		public uint start, end, full, total;

		Dictionary<int, LASintervalStartCell> cells = new Dictionary<int, LASintervalStartCell>();
		Dictionary<int, LASintervalStartCell>.Enumerator cellsEnumerator;
		HashSet<LASintervalStartCell> cells_to_merge = null;
		uint threshold, number_intervals = 0;
		int last_index = int.MinValue;
		LASintervalStartCell last_cell = null, merged_cells = null;
		LASintervalCell current_cell = null;
		bool merged_cells_temporary = false;

		public LASinterval(uint threshold = 1000)
		{
			this.threshold = threshold;
		}

		public void Dispose()
		{
			// loop over all cells
			foreach (var hash_element in cells)
			{
				LASintervalCell cell = hash_element.Value;
				while (cell.next != null)
				{
					LASintervalCell next = cell.next;
					cell.next = null;
					cell = next;
				}
			}
			cells.Clear();

			// maybe delete temporary merge cells from the previous merge
			if (merged_cells != null)
			{
				if (merged_cells_temporary)
				{
					LASintervalCell cell = merged_cells;
					while (cell.next != null)
					{
						LASintervalCell next = cell.next;
						cell.next = null;
						cell = next;
					}
				}
				merged_cells = null;
			}

			if (cells_to_merge != null)
			{
				cells_to_merge.Clear();
				cells_to_merge = null;
			}
		}

		// add points and create cells with intervals
		public bool add(uint p_index, int c_index)
		{
			if (last_cell == null || last_index != c_index)
			{
				last_index = c_index;
				if (!cells.TryGetValue(c_index, out last_cell))
				{
					last_cell = new LASintervalStartCell(p_index);
					cells.Add(c_index, last_cell);
					number_intervals++;
					return true;
				}
			}

			if (last_cell.add(p_index, threshold))
			{
				number_intervals++;
				return true;
			}

			return false;
		}

		// get total number of cells
		public uint get_number_cells() { return (uint)cells.Count; }

		// get total number of intervals
		public uint get_number_intervals() { return number_intervals; }

		// merge cells (and their intervals) into one cell
		public bool merge_cells(IReadOnlyList<int> indices, int new_index)
		{
			if (indices == null) throw new ArgumentNullException(nameof(indices));

			if (indices.Count == 1)
			{
				LASintervalStartCell cell;
				if (!cells.TryGetValue(indices[0], out cell)) return false;
				cells.Add(new_index, cell);
				cells.Remove(indices[0]);
			}
			else
			{
				if (cells_to_merge != null) cells_to_merge.Clear();

				for (int i = 0; i < indices.Count; i++)
				{
					add_cell_to_merge_cell_set(indices[i], true);
				}

				if (!merge(true)) return false;

				cells.Add(new_index, merged_cells);
				merged_cells = null;
			}

			return true;
		}

		// merge adjacent intervals with small gaps in cells to reduce total interval number to maximum
		public void merge_intervals(uint maximum_intervals, bool verbose)
		{
			uint diff = 0;

			// each cell has minimum one interval
			if (maximum_intervals < get_number_cells()) maximum_intervals = 0;
			else maximum_intervals -= get_number_cells();

			// order intervals by smallest gap
			SortedDictionary<uint, List<LASintervalCell>> map = new SortedDictionary<uint, List<LASintervalCell>>();
			uint mapSize = 0;
			foreach (var hash_element in cells)
			{
				LASintervalCell cell = hash_element.Value;
				while (cell.next != null)
				{
					diff = cell.next.start - cell.end - 1;
					if (!map.ContainsKey(diff)) map[diff] = new List<LASintervalCell>();
					map[diff].Add(cell);
					cell = cell.next;
					mapSize++;
				}
			}

			// maybe nothing to do
			if (mapSize <= maximum_intervals)
			{
				if (verbose)
				{
					if (mapSize == 0) Console.Error.WriteLine("maximum_intervals: {0} number of interval gaps: 0", maximum_intervals);
					else
					{
						// get the first entry, if there is one, and get the interval gap
						foreach (var map_element in map)
						{
							diff = map_element.Key;
							break;
						}

						Console.Error.WriteLine("maximum_intervals: {0} number of interval gaps: {1} next largest interval gap {2}", maximum_intervals, mapSize, diff);
					}
				}

				return;
			}

			uint size = mapSize;
			while (size > maximum_intervals)
			{
				LASintervalCell cell = null;
				bool deleteMapElement = false;
				foreach (var map_element in map)
				{
					diff = map_element.Key;
					cell = map_element.Value[0];
					map_element.Value.RemoveAt(0);
					deleteMapElement = map_element.Value.Count == 0;
					break;
				}

				if (deleteMapElement) map.Remove(diff);

				if (cell.start == 1 && cell.end == 0) // the (start == 1 && end == 0) signals that the cell is to be deleted
				{
					number_intervals--;
				}
				else
				{
					var delete_cell = cell.next;
					cell.end = delete_cell.end;
					cell.next = delete_cell.next;
					if (cell.next != null)
					{
						uint newDiff = cell.next.start - cell.end - 1;
						if (!map.ContainsKey(newDiff)) map[newDiff] = new List<LASintervalCell>();
						map[newDiff].Add(cell);
						delete_cell.start = 1; delete_cell.end = 0; // the (start == 1 && end == 0) signals that the cell is to be deleted
					}
					else
					{
						number_intervals--;
					}
					size--;
				}
			}

			foreach (var map_element in map)
			{
				foreach (var cell in map_element.Value)
				{
					if (cell.start == 1 && cell.end == 0) // the (start == 1 && end == 0) signals that the cell is to be deleted
					{
						number_intervals--;
					}
				}
			}

			Console.Error.WriteLine("largest interval gap increased to {0}", diff);

			// update totals
			foreach (var hash_element in cells)
			{
				LASintervalStartCell start_cell = hash_element.Value;
				start_cell.total = 0;
				LASintervalCell cell = start_cell;
				while (cell != null)
				{
					start_cell.total += cell.end - cell.start + 1;
					cell = cell.next;
				}
			}
		}

		// get one cell after the other
		// do not mix with add(...), merge_cells(...), add_cell_to_merge_cell_set(...) or read(...)
		// calls to this LASinterval, this will break the iterator
		public void get_cells()
		{
			cellsEnumerator = cells.GetEnumerator();
			current_cell = null;
		}

		// get one cell after the other
		// do not mix with add(...), merge_cells(...), add_cell_to_merge_cell_set(...) or read(...)
		// calls to this LASinterval, this will break the iterator
		public bool has_cells()
		{
			if (!cellsEnumerator.MoveNext())
			{
				current_cell = null;
				return false;
			}

			var hash_element = cellsEnumerator.Current;

			index = hash_element.Key;
			full = hash_element.Value.full;
			total = hash_element.Value.total;
			current_cell = hash_element.Value;

			return true;
		}

		// get a particular cell
		public bool get_cell(int c_index)
		{
			LASintervalStartCell hash_element;
			if (!cells.TryGetValue(c_index, out hash_element))
			{
				current_cell = null;
				return false;
			}

			index = c_index;
			full = hash_element.full;
			total = hash_element.total;
			current_cell = hash_element;

			return true;
		}

		// add cell's intervals to those that will be merged
		public bool add_current_cell_to_merge_cell_set()
		{
			if (current_cell == null) return false;
			if (cells_to_merge == null) cells_to_merge = new HashSet<LASintervalStartCell>();
			cells_to_merge.Add((LASintervalStartCell)current_cell);

			return true;
		}

		// add cell's intervals to those that will be merged
		public bool add_cell_to_merge_cell_set(int c_index, bool erase)
		{
			LASintervalStartCell hash_element;
			if (!cells.TryGetValue(c_index, out hash_element)) return false;

			if (cells_to_merge == null) cells_to_merge = new HashSet<LASintervalStartCell>();
			cells_to_merge.Add(hash_element);

			if (erase) cells.Remove(c_index);

			return true;
		}

		public bool merge(bool erase = false)
		{
			// maybe delete temporary merge cells from the previous merge
			if (merged_cells != null)
			{
				if (merged_cells_temporary)
				{
					LASintervalCell cell = merged_cells;
					while (cell.next != null)
					{
						LASintervalCell next = cell.next;
						cell.next = null;
						cell = next;
					}
				}

				merged_cells = null;
			}

			// are there cells to merge
			if (cells_to_merge == null || cells_to_merge.Count == 0) return false;

			// is there just one cell
			if (cells_to_merge.Count == 1)
			{
				merged_cells_temporary = false;

				// simply use this cell as the merge cell
				foreach (var set_element in cells_to_merge)
				{
					merged_cells = set_element;
					break;
				}
			}
			else
			{
				merged_cells_temporary = true;
				merged_cells = new LASintervalStartCell();

				// iterate over all cells and add their intervals to map
				LASintervalCell cell;
				SortedDictionary<uint, List<LASintervalCell>> map = new SortedDictionary<uint, List<LASintervalCell>>();
				foreach (var set_element in cells_to_merge)
				{
					cell = set_element;
					merged_cells.full += set_element.full;
					while (cell != null)
					{
						if (!map.ContainsKey(cell.start)) map[cell.start] = new List<LASintervalCell>();
						map[cell.start].Add(cell);
						cell = cell.next;
					}
				}

				// initialize merged_cells with first interval
				foreach (var map_element in map)
				{
					cell = map_element.Value[0];
					map_element.Value.RemoveAt(0);
					merged_cells.start = cell.start;
					merged_cells.end = cell.end;
					merged_cells.total = cell.end - cell.start + 1;
				}

				// merge intervals
				LASintervalCell last_cell = merged_cells;
				foreach (var map_element_list in map)
				{
					foreach (var map_element in map_element_list.Value)
					{
						cell = map_element;
						int diff = (int)cell.start - (int)last_cell.end;
						if (diff > (int)threshold)
						{
							last_cell.next = new LASintervalCell(cell);
							last_cell = last_cell.next;
							merged_cells.total += cell.end - cell.start + 1;
						}
						else
						{
							diff = (int)cell.end - (int)last_cell.end;
							if (diff > 0)
							{
								last_cell.end = cell.end;
								merged_cells.total += (uint)diff;
							}
							number_intervals--;
						}
					}

					map_element_list.Value.Clear();
				}
				map.Clear();
			}

			current_cell = merged_cells;
			full = merged_cells.full;
			total = merged_cells.total;

			return false;
		}

		public void clear_merge_cell_set()
		{
			if (cells_to_merge != null) cells_to_merge.Clear();
		}

		public bool get_merged_cell()
		{
			if (merged_cells == null) return false;

			full = merged_cells.full;
			total = merged_cells.total;
			current_cell = merged_cells;
			return true;
		}

		// iterate intervals of current cell (or over merged intervals)
		public bool has_intervals()
		{
			if (current_cell == null) return false;

			start = current_cell.start;
			end = current_cell.end;
			current_cell = current_cell.next;
			return true;
		}

		// read from file
		public bool read(Stream stream)
		{
			byte[] signature = new byte[4];
			if (!stream.getBytes(signature, 4))
			{
				Console.Error.WriteLine("ERROR (LASinterval): reading signature");
				return false;
			}
			if (signature[0] != 'L' && signature[1] != 'A' && signature[2] != 'S' && signature[3] != 'V')
			{
				Console.Error.WriteLine("ERROR (LASinterval): wrong signature '{0}{1}{3}{4}' instead of 'LASV'", (char)signature[0], (char)signature[1], (char)signature[2], (char)signature[3]);
				return false;
			}

			uint version;
			if (!stream.get32bits(out version))
			{
				Console.Error.WriteLine("ERROR (LASinterval): reading version");
				return false;
			}

			// read number of cells
			uint number_cells;
			if (!stream.get32bits(out number_cells))
			{
				Console.Error.WriteLine("ERROR (LASinterval): reading number of cells");
				return false;
			}

			// loop over all cells
			while (number_cells > 0)
			{
				// read index of cell
				int cell_index;
				if (!stream.get32bits(out cell_index))
				{
					Console.Error.WriteLine("ERROR (LASinterval): reading cell index");
					return false;
				}

				// create cell and insert into hash
				LASintervalStartCell start_cell = new LASintervalStartCell();
				cells.Add(cell_index, start_cell);
				LASintervalCell cell = start_cell;

				// read number of intervals in cell
				uint number_intervals;
				if (!stream.get32bits(out number_intervals))
				{
					Console.Error.WriteLine("ERROR (LASinterval): reading number of intervals in cell");
					return false;
				}

				// read number of points in cell
				uint number_points;
				if (!stream.get32bits(out number_points))
				{
					Console.Error.WriteLine("ERROR (LASinterval): reading number of points in cell");
					return false;
				}

				start_cell.full = number_points;
				start_cell.total = 0;
				while (number_intervals > 0)
				{
					// read start of interval
					if (!stream.get32bits(out cell.start))
					{
						Console.Error.WriteLine("ERROR (LASinterval): reading start of interval");
						return false;
					}

					// read end of interval
					if (!stream.get32bits(out cell.end))
					{
						Console.Error.WriteLine("ERROR (LASinterval): reading end of interval");
						return false;
					}

					start_cell.total += cell.end - cell.start + 1;

					number_intervals--;
					if (number_intervals != 0)
					{
						cell.next = new LASintervalCell();
						cell = cell.next;
					}
				}
				number_cells--;
			}

			return true;
		}

		public bool write(Stream stream)
		{
			try
			{
				stream.WriteByte((byte)'L');
				stream.WriteByte((byte)'A');
				stream.WriteByte((byte)'S');
				stream.WriteByte((byte)'V');
			}
			catch
			{
				Console.Error.WriteLine("ERROR (LASinterval): writing signature");
				return false;
			}

			uint version = 0;
			try { stream.Write(BitConverter.GetBytes(version), 0, 4); }
			catch { Console.Error.WriteLine("ERROR (LASinterval): writing version"); return false; }

			// write number of cells
			try { stream.Write(BitConverter.GetBytes((uint)cells.Count), 0, 4); }
			catch { Console.Error.WriteLine("ERROR (LASinterval): writing number of cells {0}", (uint)cells.Count); return false; }

			// loop over all cells
			foreach(var hash_element in cells)
			{
				LASintervalCell cell = hash_element.Value;

				// count number of intervals and points in cell
				uint number_intervals = 0;
				uint number_points = ((LASintervalStartCell)cell).full;
				while (cell != null)
				{
					number_intervals++;
					cell = cell.next;
				}

				// write index of cell
				int cell_index = hash_element.Key;
				try { stream.Write(BitConverter.GetBytes(cell_index), 0, 4); }
				catch { Console.Error.WriteLine("ERROR (LASinterval): writing cell index {0}", cell_index); return false; }

				// write number of intervals in cell
				try { stream.Write(BitConverter.GetBytes(number_intervals), 0, 4); }
				catch { Console.Error.WriteLine("ERROR (LASinterval): writing number of intervals {0} in cell", number_intervals); return false; }

				// write number of points in cell
				try { stream.Write(BitConverter.GetBytes(number_points), 0, 4); }
				catch { Console.Error.WriteLine("ERROR (LASinterval): writing number of point {0} in cell", number_points); return false; }

				// write intervals
				cell = hash_element.Value;
				while (cell != null)
				{
					// write start of interval
					try { stream.Write(BitConverter.GetBytes(cell.start), 0, 4); }
					catch { Console.Error.WriteLine("ERROR (LASinterval): writing start {0} of interval", cell.start); return false; }

					// write end of interval
					try { stream.Write(BitConverter.GetBytes(cell.end), 0, 4); }
					catch { Console.Error.WriteLine("ERROR (LASinterval): writing end {0} of interval", cell.end); return false; }

					cell = cell.next;
				}
			}
			return true;
		}
	}
}
