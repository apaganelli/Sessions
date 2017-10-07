using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Sessions
{
    class Util
    {
        public static double Length(CameraSpacePoint point)
        {
            return Math.Sqrt(point.X * point.X + point.Y * point.Y + point.Z + point.Z);
        }

    }
}
