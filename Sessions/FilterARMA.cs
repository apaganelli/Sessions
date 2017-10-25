using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Sessions
{
    /// <summary>
    /// Implements an ARMA filter of n size.
    /// Predicts the next position based on the weighted average of the last n positions.
    /// </summary>
    class FilterARMA
    {
        private int idx;
        private int N;
        private CameraSpacePoint[] history;

        /// <summary>
        /// Constructor of the filter ARMA
        /// </summary>
        /// <param name="n">Size of the average moved window of historical position data</param>
        public FilterARMA(int n)
        {
            idx = 0;
            N = n;
            history = new CameraSpacePoint[N];
        }

        /// <summary>
        /// Stores a new position and get rid of the oldest one.
        /// </summary>
        /// <param name="point"></param>
        public void UpdateSerie(CameraSpacePoint point)
        {
            // Update history for smoothing not tracked points
            if (idx < N)
            {
                history[idx] = point;
                idx++;
            }
            else
            {
                // history is full, then shift last N-1 elements to left
                // last position receives new point.
                Array.Copy(history, 1, history, 0, N - 1);
                history[N - 1] = point;
            }
        }

        /// <summary>
        /// Predict the next point based on the historical data. Most recent information receives a larger weight 
        /// Exponentially calculated, base 2.
        /// </summary>
        /// <returns>The position of the next camera space point based on the historical data</returns>
        public CameraSpacePoint PredictNextPoint()
        {
            CameraSpacePoint point = new CameraSpacePoint() { X = 0, Y = 0, Z = 0 };
            int j = 0;
            int weightTotal = 0;
            int weight = 0;

            // Sum up the historical data. Most recent data receives a larger weight (power of 2 of its position in a serie).
            // Most recent data is at the end (highest position) of the array.
            for (int i = history.Length; i > 0; i--)
            {
                // Since index goes to 0, using the length avoids getting a weight of 0 and missing the oldest information
                weight = (int) Math.Pow((history.Length - j), 2);

                point.X += history[i - 1].X * weight;
                point.Y += history[i - 1].Y * weight;
                point.Z += history[i - 1].Z * weight;
                weightTotal += weight;
                j++;
            }

            if (weightTotal > 0)
            {
                // Gets the predicted position
                point.X = point.X / weightTotal;
                point.Y = point.Y / weightTotal;
                point.Z = point.Z / weightTotal;

                // Update the time series with this new position.
                UpdateSerie(point);
            }

            return point;
        }
    }
}
