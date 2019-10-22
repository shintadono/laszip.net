using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LASzip.Net;

namespace TestLasZipCS
{
	class Program
	{
		struct Point3D
		{
			public double X;
			public double Y;
			public double Z;
		}

		public static void Main()
		{
			WriteLaz(); // Write a file
			ReadLaz();  // Read it back
		}

		static string FileName = Path.GetTempPath() + "Test.laz";

		static void ReadLaz()
		{
			var lazReader = new laszip();
			var compressed = true;
			lazReader.open_reader(FileName, out compressed);
			var numberOfPoints = lazReader.header.number_of_point_records;

			// Check some header values
			Debug.WriteLine(lazReader.header.min_x);
			Debug.WriteLine(lazReader.header.min_y);
			Debug.WriteLine(lazReader.header.min_z);
			Debug.WriteLine(lazReader.header.max_x);
			Debug.WriteLine(lazReader.header.max_y);
			Debug.WriteLine(lazReader.header.max_z);

			int classification = 0;
			var point = new Point3D();
			var coordArray = new double[3];

			// Loop through number of points indicated
			for (int pointIndex = 0; pointIndex < numberOfPoints; pointIndex++)
			{
				// Read the point
				lazReader.read_point();

				// Get precision coordinates
				lazReader.get_coordinates(coordArray);
				point.X = coordArray[0];
				point.Y = coordArray[1];
				point.Z = coordArray[2];

				// Get classification value
				classification = lazReader.point.classification;
			}

			// Close the reader
			lazReader.close_reader();
		}

		private static void WriteLaz()
		{
			// --- Write Example
			var point = new Point3D();
			var points = new List<Point3D>();

			point.X = 1000.0;
			point.Y = 2000.0;
			point.Z = 100.0;
			points.Add(point);

			point.X = 5000.0;
			point.Y = 6000.0;
			point.Z = 200.0;
			points.Add(point);

			var lazWriter = new laszip();
			var err = lazWriter.clean();
			if (err == 0)
			{
				// Set version, point record type, global encoding for WKT, create date and year and other stuff.
				lazWriter.header.version_major = 1;
				lazWriter.header.version_minor = 4;
				lazWriter.header.offset_to_point_data = lazWriter.header.header_size = 375;
				lazWriter.header.global_encoding |= 1 << 4; // Set WKT bit.
				lazWriter.header.file_creation_year = 2019;
				lazWriter.header.file_creation_day = 200;
				byte[] system_identifier = Encoding.ASCII.GetBytes("LASzip.net example");
				Array.Copy(system_identifier, lazWriter.header.system_identifier, Math.Min(system_identifier.Length, 32));
				lazWriter.set_point_type_and_size(6, 30);

				// Number of point records needs to be set. Extended numbers that is, since 1.4.
				lazWriter.header.extended_number_of_point_records = (ulong)points.Count;
				lazWriter.header.extended_number_of_points_by_return[0] = (ulong)points.Count;

				// Header Min/Max needs to be set to extents of points
				lazWriter.header.min_x = points[0].X; // LL Point
				lazWriter.header.min_y = points[0].Y;
				lazWriter.header.min_z = points[0].Z;
				lazWriter.header.max_x = points[1].X; // UR Point
				lazWriter.header.max_y = points[1].Y;
				lazWriter.header.max_z = points[1].Z;

				// Set up some WKT string as spatial reference system definition. (Even if it doesn't make sense with the coordinates in this example.)
				string wkt = "PROJCS[\"WGS 84 / UTM zone 32N\",GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH]]";

				laszip_vlr wktVLR = new laszip_vlr();
				wktVLR.reserved = 0;
				byte[] user_id = Encoding.ASCII.GetBytes("LASF_Projection");
				Array.Copy(user_id, wktVLR.user_id, Math.Min(user_id.Length, 16));
				wktVLR.record_id = 2112;
				wktVLR.description[0] = 0; // add_vlr will fill the description.
				wktVLR.data = Encoding.ASCII.GetBytes(wkt);
				wktVLR.record_length_after_header = (ushort)wktVLR.data.Length;

				err = lazWriter.add_vlr(wktVLR);
				if (err == 0)
				{
					// Open the writer and test for errors
					err = lazWriter.open_writer(FileName, true);
					if (err == 0)
					{
						double[] coordArray = new double[3];
						foreach (var p in points)
						{
							coordArray[0] = p.X;
							coordArray[1] = p.Y;
							coordArray[2] = p.Z;

							// Set the coordinates in the lazWriter object
							lazWriter.set_coordinates(coordArray);

							// Set the classification to ground
							lazWriter.point.classification = 2;

							lazWriter.point.extended_return_number = 1;
							lazWriter.point.extended_number_of_returns = 1;

							// Write the point to the file
							err = lazWriter.write_point();
							if (err != 0) break;
						}

						// Close the writer to release the file (OS lock)
						err = lazWriter.close_writer();
						lazWriter = null;
					}
				}
			}

			if (err != 0)
			{
				// Show last error that occurred
				Debug.WriteLine(lazWriter.get_error());
			}
			// --- Upon completion, file should be 389 bytes
		}
	}
}
