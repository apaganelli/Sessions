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
    /// Video class exposes properties of videos that are related to a Trial.
    /// </summary>
    class VideoModel : ObservableObject
    {
        #region Fields
        private int _sequence;
        private int _power;
        private string _filename;
        private bool _isCalibration = false;
        #endregion

        #region Properties
        public int Sequence
        {
            get { return _sequence; }
            set
            {
                if(value != _sequence)
                {
                    _sequence = value;
                    OnPropertyChanged("Sequence");
                }
            }
        }

        public int Power
        {
            get { return _power; }
            set
            {
                if(value != _power)
                {
                    _power = value;
                    OnPropertyChanged("Power");
                }
            }
        }

        public string Filename
        {
            get { return _filename; }
            set
            {
                if(value != _filename)
                {
                    _filename = value;
                    OnPropertyChanged("Filename");
                }
            }
        }

        public bool IsCalibration
        {
            get { return _isCalibration; }
            set
            {
                if(value != _isCalibration)
                {
                    _isCalibration = value;
                    OnPropertyChanged("IsCalibration");
                }
            }
        }

        #endregion // Properties
    }
}
