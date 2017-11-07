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
        ///<summary>
        /// Author: Antonio Iyda Paganelli
        ///
        /// Calculates the length of a 3D point. 
        /// </summary>
        /// <param name="point"></param>
        /// <returns>A 3D point</returns>
        public static double Length(CameraSpacePoint point)
        {
            return Math.Sqrt(point.X * point.X + point.Y * point.Y + point.Z + point.Z);
        }

    }
}
