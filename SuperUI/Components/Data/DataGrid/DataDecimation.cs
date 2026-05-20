using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperUI.Components
{
    /// <summary>
    /// Data decimation utilities for handling large datasets efficiently.
    /// Implements the Largest-Triangle-Three-Buckets (LTTB) algorithm.
    /// </summary>
    public static class DataDecimation
    {
        /// <summary>
        /// Represents a point in 2D space for decimation.
        /// </summary>
        public struct DataPoint
        {
            public double X { get; set; }
            public double Y { get; set; }

            public DataPoint(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>
        /// Decimates data using the Largest-Triangle-Three-Buckets (LTTB) algorithm.
        /// This algorithm reduces large datasets while preserving the visual shape of the data.
        /// </summary>
        /// <param name="data">Input data points to decimate</param>
        /// <param name="targetPoints">Target number of points after decimation (default: 1000)</param>
        /// <returns>Decimated data points</returns>
        public static List<DataPoint> DecimateData(List<DataPoint> data, int targetPoints = 1000)
        {
            if (data == null || data.Count <= targetPoints)
                return data ?? new List<DataPoint>();

            var decimated = new List<DataPoint>();
            
            // Always include the first point
            decimated.Add(data[0]);

            // Calculate bucket size
            var bucketSize = (double)(data.Count - 2) / (targetPoints - 2);

            // Process each bucket
            for (int i = 0; i < targetPoints - 2; i++)
            {
                // Calculate the range for this bucket
                var rangeStart = (int)Math.Floor((i + 1) * bucketSize) + 1;
                var rangeEnd = (int)Math.Floor((i + 2) * bucketSize) + 1;

                // Calculate the average point for the next bucket
                var nextBucketStart = (int)Math.Floor((i + 2) * bucketSize) + 1;
                var nextBucketEnd = (int)Math.Floor((i + 3) * bucketSize) + 1;

                double avgX = 0, avgY = 0;
                var nextBucketLength = Math.Min(nextBucketEnd, data.Count) - nextBucketStart;
                
                if (nextBucketLength > 0)
                {
                    for (int j = nextBucketStart; j < Math.Min(nextBucketEnd, data.Count); j++)
                    {
                        avgX += data[j].X;
                        avgY += data[j].Y;
                    }
                    avgX /= nextBucketLength;
                    avgY /= nextBucketLength;
                }

                // Find the point with the largest triangle area
                var maxArea = -1.0;
                var maxAreaIndex = -1;
                var lastPoint = decimated[decimated.Count - 1];

                for (int j = rangeStart; j < Math.Min(rangeEnd, data.Count); j++)
                {
                    // Calculate triangle area using the cross product formula
                    var area = CalculateTriangleArea(lastPoint, data[j], new DataPoint(avgX, avgY));

                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxAreaIndex = j;
                    }
                }

                // Add the point with the largest area
                if (maxAreaIndex >= 0)
                {
                    decimated.Add(data[maxAreaIndex]);
                }
            }

            // Always include the last point
            decimated.Add(data[data.Count - 1]);

            return decimated;
        }

        /// <summary>
        /// Decimates double values using LTTB algorithm.
        /// Assumes X values are sequential indices.
        /// </summary>
        /// <param name="values">Input values to decimate</param>
        /// <param name="targetPoints">Target number of points after decimation</param>
        /// <returns>Decimated values</returns>
        public static List<double> DecimateValues(List<double> values, int targetPoints = 1000)
        {
            if (values == null || values.Count <= targetPoints)
                return values ?? new List<double>();

            // Convert to DataPoints with sequential X values
            var dataPoints = values
                .Select((v, i) => new DataPoint(i, v))
                .ToList();

            // Decimate
            var decimated = DecimateData(dataPoints, targetPoints);

            // Extract Y values
            return decimated.Select(p => p.Y).ToList();
        }

        /// <summary>
        /// Calculates the area of a triangle formed by three points.
        /// Uses the cross product formula for efficiency.
        /// </summary>
        private static double CalculateTriangleArea(DataPoint p1, DataPoint p2, DataPoint p3)
        {
            // Using the cross product formula: Area = 0.5 * |cross product|
            // For 2D: cross product = (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y)
            var area = Math.Abs(
                (p2.X - p1.X) * (p3.Y - p1.Y) - 
                (p3.X - p1.X) * (p2.Y - p1.Y)
            ) * 0.5;

            return area;
        }

        /// <summary>
        /// Determines if data should be decimated based on size.
        /// </summary>
        /// <param name="dataCount">Number of data points</param>
        /// <param name="threshold">Decimation threshold (default: 10000)</param>
        /// <returns>True if data should be decimated</returns>
        public static bool ShouldDecimate(int dataCount, int threshold = 10000)
        {
            return dataCount > threshold;
        }
    }
}
