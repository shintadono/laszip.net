using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
				// Number of point records needs to be set
				lazWriter.header.number_of_point_records = (uint)points.Count;

				// Header Min/Max needs to be set to extents of points
				lazWriter.header.min_x = points[0].X; // LL Point
				lazWriter.header.min_y = points[0].Y;
				lazWriter.header.min_z = points[0].Z;
				lazWriter.header.max_x = points[1].X; // UR Point
				lazWriter.header.max_y = points[1].Y;
				lazWriter.header.max_z = points[1].Z;

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

						// Write the point to the file
						err = lazWriter.write_point();
						if (err != 0) break;
					}

					// Close the writer to release the file (OS lock)
					err = lazWriter.close_writer();
					lazWriter = null;
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
