using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Just a new type of 3D data with X, Y, Z axis information.
    /// </summary>
    class Vector3
    {
        private float _x;
        private float _y;
        private float _z;
        
        public Vector3(float x, float y, float z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public float X
        {
            get { return _x; }
            set
            {
                if(value != _x)
                {
                    _x = value;
                }
            }
        }

        public float Y
        {
            get { return _y; }
            set
            {
                if (value != _y)
                {
                    _y = value;
                }
            }
        }

        public float Z
        {
            get { return _z; }
            set
            {
                if (value != _z)
                {
                    _z = value;
                }
            }
        }
    }
}
