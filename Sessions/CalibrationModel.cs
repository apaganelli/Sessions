using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Sessions
{
    class CalibrationModel : ObservableObject
    {
        #region Fields
        private int _calSessionId;
        private JointType _jointType;
        private Vector3 _position;
        private int _numFrames;
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
        #endregion // Properties

    }
}
