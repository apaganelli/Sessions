using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sessions
{
    /// <summary>
    /// SessionModel class holds the information of each Session analyzed.
    /// It is identified by SessionId/SessionName/SessionDate and has additionally the following properties:
    /// Modality, SessionType and AmountOfVideo.
    /// </summary>
    class SessionModel : ObservableObject
    {
        #region Fields 
        private int _sessionId;
        private string _sessionName;
        private double _thighLength;
        private double _shankLength;
        private string _sessionDate;
        private string _modality;
        private string _sessionType;
        private ObservableCollection<VideoModel> _videoList;
        private CalibrationModel _calibration;

        #endregion

        #region Properties

        public int SessionId
        {
            get { return _sessionId; }
            set
            {
                if(value != _sessionId)
                {
                    _sessionId = value;
                    OnPropertyChanged("SessionId");
                }
            }
        }

        public string SessionName
        {
            get { return _sessionName; }
            set
            {
                if(value != _sessionName)
                {
                    _sessionName = value;
                    OnPropertyChanged("SessionName");
                }
            }
        }

        public string SessionDate
        {
            get { return _sessionDate; }
            set
            {
                if(value != _sessionDate)
                {
                    _sessionDate = value;
                    OnPropertyChanged("SessionDate");
                }
            }
        }

        public string Modality
        {
            get { return _modality; }
            set
            {
                if(value != _modality)
                {
                    _modality = value;
                    OnPropertyChanged("Modality");
                }
            }
        }

        public string SessionType
        {
            get { return _sessionType; }
            set
            {
                if(value != _sessionType)
                {
                    _sessionType = value;
                    OnPropertyChanged("SessionType");
                }
            }
        }

        public ObservableCollection<VideoModel> VideoList
        {
            get { return _videoList; }
            set
            {
                if(value != _videoList)
                {
                    _videoList = value;
                    OnPropertyChanged("VideoList");
                }
            }
        }

        public CalibrationModel Calibration
        {
            get { return _calibration; }
            set
            {
                if(value != _calibration)
                {
                    _calibration = value;
                    OnPropertyChanged("SessionCalibration");
                }
            }
        }

        public double ThighLength
        {
            get { return _thighLength; }
            set
            {
                if(value != _thighLength)
                {
                    _thighLength = value;
                    OnPropertyChanged("ThighLength");
                }
            }
        }

        public double ShankLength
        {
            get { return _shankLength; }
            set
            {
                if (value != _shankLength)
                {
                    _shankLength = value;
                    OnPropertyChanged("ShankLength");
                }
            }
        }
            #endregion // Properties
        }
    }
