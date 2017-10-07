using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Sessions
{
    class FilterARMA
    {
        private int idx;
        private int N;
        private CameraSpacePoint[] history;

        public FilterARMA(int n)
        {
            idx = 0;
            N = n;
            history = new CameraSpacePoint[N];
        }

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

        public CameraSpacePoint PredictNextPoint()
        {
            CameraSpacePoint point = new CameraSpacePoint() { X = 0, Y = 0, Z = 0 };
            int j = 0;
            int weightTotal = 0;
            int weight;

            for (int i = history.Length; i > 0; i--)
            {
                weight = 0;
                weight = (int) Math.Pow((history.Length - j), 2);

                point.X += history[i - 1].X * weight;
                point.Y += history[i - 1].Y * weight;
                point.Z += history[i - 1].Z * weight;
                weightTotal += weight;
                j++;
            }

            if (weightTotal > 0)
            {
                point.X = point.X / weightTotal;
                point.Y = point.Y / weightTotal;
                point.Z = point.Z / weightTotal;
                UpdateSerie(point);
            }

            return point;
        }
    }
}
