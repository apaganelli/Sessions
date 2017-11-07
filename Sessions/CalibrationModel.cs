using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Holds calibration information: id of the related sessions, joint type, its position, an acceptable threshold of variation
    /// a standard deviation, the number of frames to be analysed, the initial time of the clip from which it should start evaluation
    /// the estimated joint distance from the kinect camera and segment lengths of the lower limbs.
    /// </summary>
    class CalibrationModel : ObservableObject
    {
        #region Fields
        private int _calSessionId;
        private JointType _jointType;
        private Vector3 _position;
        private Vector3 _threshold;
        private Vector3 _sd;
        private int _numFrames;
        private Int64 _initialTime;
        private Vector3 _estimated;                 // Estimated joint distance.
        private double _rightThighLength;
        private double _rightShankLength;
        private double _leftThighLength;
        private double _leftShankLength;

        #endregion // Fields

        #region Properties
        public int CalSessionId
        {
            get { return _calSessionId; }
            set
            {
                if (value != _calSessionId)
                {
                    _calSessionId = value;
                    OnPropertyChanged("CalSessionId");
                }
            }
        }

        public JointType JointType
        {
            get { return _jointType; }
            set
            {
                if(value != _jointType)
                {
                    _jointType = value;
                    OnPropertyChanged("JointType");
                }
            }
        }

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                if (value != _position)
                {
                    _position = value;
                    OnPropertyChanged("Position");
                }
            }
        }

        public Vector3 Threshold
        {
            get { return _threshold; }
            set
            {
                if (value != _threshold)
                {
                    _threshold = value;
                    OnPropertyChanged("Threshold");
                }
            }
        }

        /// <summary>
        /// Gets/sets calibration standard deviation
        /// </summary>
        public Vector3 SD
        {
            get { return _sd; }
            set
            {
                if (value != _sd)
                {
                    _sd = value;
                    OnPropertyChanged("SD");
                }
            }
        }

        /// <summary>
        /// Gets/sets calibration estimated joint position
        /// </summary>
        public Vector3 Estimated
        {
            get { return _estimated; }
            set
            {
                if (value != _estimated)
                {
                    _estimated = value;
                    OnPropertyChanged("Estimated");
                }
            }
        }

        public int NumFrames
        {
            get { return _numFrames; }
            set
            {
                if (value != _numFrames)
                {
                    _numFrames = value;
                    OnPropertyChanged("NumFrames");
                }
            }
        }

        public Int64 InitialTime
        {
            get { return _initialTime; }
            set
            {
                if(value != _initialTime)
                {
                    _initialTime = value;
                    OnPropertyChanged("InitialTime");
                }
            }
        }

        public double RightThighLength
        {
            get { return _rightThighLength; }
            set
            {
                if (value != _rightThighLength)
                {
                    _rightThighLength = value;
                    OnPropertyChanged("RightThighLength");
                }
            }
        }

        public double RightShankLength
        {
            get { return _rightShankLength; }
            set
            {
                if (value != _rightShankLength)
                {
                    _rightShankLength = value;
                    OnPropertyChanged("RightShankLength");
                }
            }
        }

        public double LeftThighLength
        {
            get { return _leftThighLength; }
            set
            {
                if (value != _leftThighLength)
                {
                    _leftThighLength = value;
                    OnPropertyChanged("LeftThighLength");
                }
            }
        }

        public double LeftShankLength
        {
            get { return _leftShankLength; }
            set
            {
                if (value != _leftShankLength)
                {
                    _leftShankLength = value;
                    OnPropertyChanged("LeftShankLength");
                }
            }
        }
        #endregion // Properties

    }
}
