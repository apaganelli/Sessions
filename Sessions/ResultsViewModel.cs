using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// Show the results of alignment analysis of some joint positions.
    /// </summary>
    class ResultsViewModel : ObservableObject, IPageViewModel
    {
        /// <summary>
        /// Pointer to main application.
        /// </summary>
        ApplicationViewModel _app;

        /// <summary>
        /// Holds session information.
        /// </summary>
        SessionModel _session;

        /// <summary>
        /// Holds all recorded joints position frame by frame.
        /// </summary>
        List<CameraSpacePoint[]> _positions;

        /// <summary>
        /// Hold all joints position of one frame.
        /// </summary>
        CameraSpacePoint[] _position;

        /// <summary>
        /// Total recorded frames.
        /// </summary>
        float _totalFrames = 0;

        /// <summary>
        /// Number of sets of data for comparisons. i.e., if equals 2, it is possible to compare first and second halves.
        /// </summary>
        int _partitions;

        int _firstSet;
        int _secondSet;
        int _partitionSize;

        ICommand _HKA_Command;

        private double averageHipLeft = 0;
        private double averageKneeLeft = 0;
        private double averageAnkleLeft = 0;
        private double averageHipRight = 0;
        private double averageKneeRight = 0;
        private double averageAnkleRight = 0;

        double sdHipLeft = 0;
        double sdHipRight = 0;
        double sdKneeLeft = 0;
        double sdKneeRight = 0;
        double sdAnkleLeft = 0;
        double sdAnkleRight = 0;

        float minHipLeft = 0;
        float minHipRight = 0;
        float minKneeLeft = 0;
        float minKneeRight = 0;
        float minAnkleLeft = 0;
        float minAnkleRight = 0;

        float maxHipLeft = 0;
        float maxHipRight = 0;
        float maxKneeLeft = 0;
        float maxKneeRight = 0;
        float maxAnkleLeft = 0;
        float maxAnkleRight = 0;

        float ampHipLeft = 0;
        float ampHipRight = 0;
        float ampKneeLeft = 0;
        float ampKneeRight = 0;
        float ampAnkleLeft = 0;
        float ampAnkleRight = 0;

        /// <summary>
        /// ViewModel related to Results tab.
        /// </summary>
        /// <param name="app"></param>
        public ResultsViewModel(ApplicationViewModel app)
        {
            _app = app;
            _positions = new List<CameraSpacePoint[]>();
        }

        public void LoadResultViewModel()
        {
            // There was a Selected Session to Work on.
            if(_app.SessionsViewModel.SelectedSessionId > 0)
            {
                LoadData();
            }

        }

        public float TotalFrames
        {
            get { return _totalFrames; }
            set
            {
                if(_totalFrames != value)
                {
                    _totalFrames = value;
                    OnPropertyChanged("TotalFrames");
                }
            }
        }

        public int Partitions
        {
            get { return _partitions; }
            set
            {
                if (_partitions != value)
                {
                    _partitions = value;
                    OnPropertyChanged("Partitions");
                }
            }
        }

        public int FirstSet
        {
            get { return _firstSet; }
            set
            {
                if(value <= 0 || value > (_partitions - 1))
                {
                    throw new ArgumentException("Value must be > 0 and < " + _partitions);
                }
                if (_firstSet != value)
                {
                    _firstSet = value;
                    OnPropertyChanged("FirstSet");
                }
            }
        }

        public int SecondSet
        {
            get { return _secondSet; }
            set
            {
                if (value <= 0 || value <= _firstSet || value > _partitions)
                    throw new ArgumentException("Value must be > 0 and > " + _firstSet + " and <= " + _partitions);

                if (_secondSet != value)
                {
                    _secondSet = value;
                    OnPropertyChanged("SecondSet");
                }
            }
        }

        public int PartitionSize
        {
            get { return _partitionSize; }
            set
            {
                if (_partitionSize != value)
                {
                    _partitionSize = value;
                    OnPropertyChanged("PartitionSize");
                }
            }
        }

        public ICommand HKA_Command
        {
            get
            {
                if (_HKA_Command == null)
                {
                    _HKA_Command = new RelayCommand(param => HKA_Alignment(),
                        param => _partitionSize > 0);
                }
                return _HKA_Command;
            }
        }

        public double AverageHipLeft
        {
            get { return averageHipLeft; }
            set
            {
                if(averageHipLeft != value)
                {
                    averageHipLeft = value;
                    OnPropertyChanged("AverageHipLeft");
                }
            }
        }

        public double AverageKneeLeft
        {
            get { return averageKneeLeft; }
            set
            {
                if (averageKneeLeft != value)
                {
                    averageKneeLeft = value;
                    OnPropertyChanged("AverageKneeLeft");
                }
            }
        }

        public double AverageAnkleLeft
        {
            get { return averageAnkleLeft; }
            set
            {
                if (averageAnkleLeft != value)
                {
                    averageAnkleLeft = value;
                    OnPropertyChanged("AverageAnkleLeft");
                }
            }
        }

        public double AverageHipRight
        {
            get { return averageHipRight; }
            set
            {
                if (value != averageHipRight)
                {
                    averageHipRight = value;
                    OnPropertyChanged("AverageHipRight");
                }
            }
        }

        public double AverageKneeRight
        {
            get { return averageKneeRight; }
            set
            {
                if (value != averageKneeRight)
                {
                    averageKneeRight = value;
                    OnPropertyChanged("AverageKneeRight");
                }
            }
        }

        public double AverageAnkleRight
        {
            get { return averageAnkleRight; }
            set
            {
                if (value != averageAnkleRight)
                {
                    averageAnkleRight = value;
                    OnPropertyChanged("AverageAnkleRight");
                }
            }
    }

        public double SDHipLeft
        {
            get { return sdHipLeft; }
            set
            {
                if (sdHipLeft != value)
                {
                    sdHipLeft = value;
                    OnPropertyChanged("SDHipLeft");
                }
            }
        }

        public double SDKneeLeft
        {
            get { return sdKneeLeft; }
            set
            {
                if (sdKneeLeft != value)
                {
                    sdKneeLeft = value;
                    OnPropertyChanged("SDKneeLeft");
                }
            }
        }

        public double SDAnkleLeft
        {
            get { return sdAnkleLeft; }
            set
            {
                if (sdAnkleLeft != value)
                {
                    sdAnkleLeft = value;
                    OnPropertyChanged("SDAnkleLeft");
                }
            }
        }

        public double SDHipRight
        {
            get { return sdHipRight; }
            set
            {
                if (sdHipRight != value)
                {
                    sdHipRight = value;
                    OnPropertyChanged("SDHipRight");
                }
            }
        }

        public double SDKneeRight
        {
            get { return sdKneeRight; }
            set
            {
                if (sdKneeRight != value)
                {
                    sdKneeRight = value;
                    OnPropertyChanged("SDKneeRight");
                }
            }
        }

        public double SDAnkleRight
        {
            get { return sdAnkleRight; }
            set
            {
                if (sdAnkleRight != value)
                {
                    sdAnkleRight = value;
                    OnPropertyChanged("SDAnkleRight");
                }
            }
        }

        public float MinHipLeft
        {
            get { return minHipLeft; }
            set
            {
                if (minHipLeft != value)
                {
                    minHipLeft = value;
                    OnPropertyChanged("MinHipLeft");
                }
            }
        }

        public float MinKneeLeft
        {
            get { return minKneeLeft; }
            set
            {
                if (minKneeLeft != value)
                {
                    minKneeLeft = value;
                    OnPropertyChanged("MinKneeLeft");
                }
            }
        }

        public float MinAnkleLeft
        {
            get { return minAnkleLeft; }
            set
            {
                if (minAnkleLeft != value)
                {
                    minAnkleLeft = value;
                    OnPropertyChanged("MinAnkleLeft");
                }
            }
        }

        public float MinHipRight
        {
            get { return minHipRight; }
            set
            {
                if (minHipRight != value)
                {
                    minHipRight = value;
                    OnPropertyChanged("MinHipRight");
                }
            }
        }

        public float MinKneeRight
        {
            get { return minKneeRight; }
            set
            {
                if (minKneeRight != value)
                {
                    minKneeRight = value;
                    OnPropertyChanged("MinKneeRight");
                }
            }
        }

        public float MinAnkleRight
        {
            get { return minAnkleRight; }
            set
            {
                if (minAnkleRight != value)
                {
                    minAnkleRight = value;
                    OnPropertyChanged("MinAnkleRight");
                }
            }
        }

        public float MaxHipLeft
        {
            get { return maxHipLeft; }
            set
            {
                if (maxHipLeft != value)
                {
                    maxHipLeft = value;
                    OnPropertyChanged("MaxHipLeft");
                }
            }
        }

        public float MaxKneeLeft
        {
            get { return maxKneeLeft; }
            set
            {
                if (maxKneeLeft != value)
                {
                    maxKneeLeft = value;
                    OnPropertyChanged("MaxKneeLeft");
                }
            }
        }

        public float MaxAnkleLeft
        {
            get { return maxAnkleLeft; }
            set
            {
                if (maxAnkleLeft != value)
                {
                    maxAnkleLeft = value;
                    OnPropertyChanged("MaxAnkleLeft");
                }
            }
        }

        public float MaxHipRight
        {
            get { return maxHipRight; }
            set
            {
                if (maxHipRight != value)
                {
                    maxHipRight = value;
                    OnPropertyChanged("MaxHipRight");
                }
            }
        }

        public float MaxKneeRight
        {
            get { return maxKneeRight; }
            set
            {
                if (maxKneeRight != value)
                {
                    maxKneeRight = value;
                    OnPropertyChanged("MaxKneeRight");
                }
            }
        }

        public float MaxAnkleRight
        {
            get { return maxAnkleRight; }
            set
            {
                if (maxAnkleRight != value)
                {
                    maxAnkleRight = value;
                    OnPropertyChanged("MaxAnkleRight");
                }
            }
        }

        public float AmpHipLeft
        {
            get { return ampHipLeft; }
            set
            {
                if (ampHipLeft != value)
                {
                    ampHipLeft = value;
                    OnPropertyChanged("AmpHipLeft");
                }
            }
        }

        public float AmpKneeLeft
        {
            get { return ampKneeLeft; }
            set
            {
                if (ampKneeLeft != value)
                {
                    ampKneeLeft = value;
                    OnPropertyChanged("AmpKneeLeft");
                }
            }
        }

        public float AmpAnkleLeft
        {
            get { return ampAnkleLeft; }
            set
            {
                if (ampAnkleLeft != value)
                {
                    ampAnkleLeft = value;
                    OnPropertyChanged("AmpAnkleLeft");
                }
            }
        }

        public float AmpHipRight
        {
            get { return ampHipRight; }
            set
            {
                if (ampHipRight != value)
                {
                    ampHipRight = value;
                    OnPropertyChanged("AmpHipRight");
                }
            }
        }

        public float AmpKneeRight
        {
            get { return ampKneeRight; }
            set
            {
                if (ampKneeRight != value)
                {
                    ampKneeRight = value;
                    OnPropertyChanged("AmpKneeRight");
                }
            }
        }

        public float AmpAnkleRight
        {
            get { return ampAnkleRight; }
            set
            {
                if (ampAnkleRight != value)
                {
                    ampAnkleRight = value;
                    OnPropertyChanged("AmpAnkleRight");
                }
            }
        }

        /// <summary>
        /// Check Hip, Knee, Ankle alignment fills out 
        /// </summary>
        private void HKA_Alignment()
        {
            ExtremeValues(JointType.HipLeft, 'X');
            ExtremeValues(JointType.HipRight, 'X');

            ExtremeValues(JointType.KneeLeft, 'X');
            ExtremeValues(JointType.KneeRight, 'X');

            ExtremeValues(JointType.AnkleLeft, 'X');
            ExtremeValues(JointType.AnkleRight, 'X');

            AmpHipLeft =   Math.Abs(Math.Abs(MaxHipLeft) - Math.Abs(MinHipLeft));
            AmpKneeLeft =  Math.Abs(Math.Abs(MaxKneeLeft) - Math.Abs(MinKneeLeft));
            AmpAnkleLeft = Math.Abs(Math.Abs(MaxAnkleLeft) - Math.Abs(MinAnkleLeft));
            AmpHipRight = Math.Abs(Math.Abs(MaxHipRight) - Math.Abs(MinHipRight));
            AmpKneeRight = Math.Abs(Math.Abs(MaxKneeRight) - Math.Abs(MinKneeRight));
            AmpAnkleRight = Math.Abs(Math.Abs(MaxAnkleRight) - Math.Abs(MinAnkleRight));

            // Check Hip-Knee difference in horizontal axis (X) for both sets.

            // Difference series Set1 = First / Set2 = Last
            double[] HipKneeSet1Right = new double[_partitionSize];
            double[] HipKneeSet2Right = new double[_partitionSize];

            double[] KneeAnkleSet1Right = new double[_partitionSize];
            double[] KneeAnkleSet2Right = new double[_partitionSize];

            double[] HipKneeSet1Left = new double[_partitionSize];
            double[] HipKneeSet2Left = new double[_partitionSize];

            double[] KneeAnkleSet1Left = new double[_partitionSize];
            double[] KneeAnkleSet2Left = new double[_partitionSize];

            CameraSpacePoint[] frame;

            int idx1 = _partitionSize * (_firstSet - 1);
            int idx2 = _partitionSize * (_secondSet - 1);

            for(int i = 0; i < _partitionSize && idx2 < _positions.Count; i++)
            {
                frame = _positions[idx1++];
                HipKneeSet1Left[i]   = frame[(int)JointType.HipLeft].X - frame[(int)JointType.KneeLeft].X;
                KneeAnkleSet1Left[i] = frame[(int)JointType.KneeLeft].X - frame[(int)JointType.AnkleLeft].X;

                HipKneeSet1Right[i]   = frame[(int)JointType.HipRight].X  - frame[(int)JointType.KneeRight].X;
                KneeAnkleSet1Right[i] = frame[(int)JointType.KneeRight].X - frame[(int)JointType.AnkleRight].X;

                frame = _positions[idx2++];
                HipKneeSet2Left[i] = frame[(int)JointType.HipLeft].X - frame[(int)JointType.KneeLeft].X;
                KneeAnkleSet1Left[i] = frame[(int)JointType.KneeLeft].X - frame[(int)JointType.AnkleLeft].X;

                HipKneeSet2Right[i]   = frame[(int)JointType.HipRight].X - frame[(int)JointType.KneeRight].X;
                KneeAnkleSet1Right[i] = frame[(int)JointType.KneeRight].X - frame[(int)JointType.AnkleRight].X;
            }

            // Mean, df, p-value and t of the differences
            double meanDiffHipKnee1Left = 0;
            double meanDiffHipKnee2Left = 0;
            double tDiffHipKneeLeft = 0;
            double dfDiffHipKneeLeft = 0;
            double pValueDiffHipKneeLeft = 0;

            // Hip-Knee differences [Left side]
            Util.TTest(HipKneeSet1Left, HipKneeSet2Left, out meanDiffHipKnee1Left, out meanDiffHipKnee2Left, out tDiffHipKneeLeft, 
                       out dfDiffHipKneeLeft, out pValueDiffHipKneeLeft);

            double meanDiffHipKnee1Right = 0;
            double meanDiffHipKnee2Right = 0;
            double tDiffHipKneeRight = 0;
            double dfDiffHipKneeRight = 0;
            double pValueDiffHipKneeRight = 0;

            // Hip-Knee differences [Right side]
            Util.TTest(HipKneeSet1Right, HipKneeSet2Right, out meanDiffHipKnee1Right, out meanDiffHipKnee2Right, out tDiffHipKneeRight,
                       out dfDiffHipKneeRight, out pValueDiffHipKneeRight);

            double meanDiffKneeAnkle1Left = 0;
            double meanDiffKneeAnkle2Left = 0;
            double tDiffKneeAnkleLeft = 0;
            double dfDiffKneeAnkleLeft = 0;
            double pValueDiffKneeAnkleLeft = 0;

            // Knee-Ankle differences [Left side]
            Util.TTest(KneeAnkleSet1Left, KneeAnkleSet2Left, out meanDiffKneeAnkle1Left, out meanDiffKneeAnkle2Left, out tDiffKneeAnkleLeft,
                       out dfDiffKneeAnkleLeft, out pValueDiffKneeAnkleLeft);

            double meanDiffKneeAnkle1Right = 0;
            double meanDiffKneeAnkle2Right = 0;
            double tDiffKneeAnkleRight = 0;
            double dfDiffKneeAnkleRight = 0;
            double pValueDiffKneeAnkleRight = 0;

            // Knee-Ankle differences [Right side]
            Util.TTest(KneeAnkleSet1Right, KneeAnkleSet2Right, out meanDiffKneeAnkle1Right, out meanDiffKneeAnkle2Right, 
                        out tDiffKneeAnkleRight, out dfDiffKneeAnkleRight, out pValueDiffKneeAnkleRight);
        }

        /// <summary>
        /// Loads recorded joint positions.
        /// </summary>
        private void LoadData()
        {
            SessionViewModel sVM = new SessionViewModel(_app.SessionsViewModel.SelectedSessionId);
            _session = sVM.Session;

            string filename = _session.SessionName + ".txt";

            if(_positions.Count > 0)
            {
                return;
            }

            // Check if there is a data file for the selected session
            if (File.Exists(filename))
            {
                FileInfo file = new FileInfo(filename);

                using (StreamReader sr = file.OpenText())
                {
                    string line;
                    string[] joints;

                    while ((line = sr.ReadLine()) != null)
                    {
                        joints = line.Split(':');
                        _position = new CameraSpacePoint[25];

                        int i = 0;
                        foreach (string cell in joints)
                        {
                            string[] axis = cell.Split(';');
                            if (axis.Length == 3)
                            {
                                _position[i].X = float.Parse(axis[0]);
                                _position[i].Y = float.Parse(axis[1]);
                                _position[i].Z = float.Parse(axis[2]);
                                i++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        _positions.Add(_position);
                    }

                    TotalFrames = _positions.Count;
                }
            }
            else
            {
                MessageBox.Show("Selected session has no data file.", "Error", MessageBoxButton.OK);
            }

        }

        /// <summary>
        /// Calculates average, sd, finds min and max values of selected joint type and axis
        /// </summary>
        /// <param name="pType">Joint type</param>
        /// <param name="pAxis">X Y or Z</param>
        /// <param name="pMax">by reference - max value</param>
        /// <param name="pMin">by reference - min value</param>//
        /// <param name="pAverage">by reference - average value</param>
        /// <param name="pSd">by reference - standard deviation</param>
        private void ExtremeValues(JointType pType, char pAxis)
        {
            int i = 0;
            float value = 0;
            float[] values = new float[_positions.Count];
            
            float pMin = float.MaxValue;
            float pMax = float.MinValue;

            foreach(CameraSpacePoint[] frame in _positions)
            {
                switch(pAxis)
                {
                    case 'X':
                        value = frame[(int)pType].X;
                        break;
                    case 'Y':
                        value = frame[(int)pType].Y;
                        break;
                    case 'Z':
                        value = frame[(int)pType].Z;
                        break;
                }

                if (value < pMin)
                    pMin = value;

                if (value > pMax)
                    pMax = value;

                values[i++] = value;
            }

            double average = values.Average();
            double sumOfSquaresDiffs = values.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresDiffs / values.Length);

            switch(pType)
            {
                case JointType.HipLeft:
                    AverageHipLeft = average;
                    SDHipLeft = sd;
                    MinHipLeft = pMin;
                    MaxHipLeft = pMax;
                    break;
                case JointType.KneeLeft:
                    AverageKneeLeft = average;
                    SDKneeLeft = sd;
                    MinKneeLeft = pMin;
                    MaxKneeLeft = pMax;
                    break;
                case JointType.AnkleLeft:
                    AverageAnkleLeft = average;
                    SDAnkleLeft = sd;
                    MinAnkleLeft = pMin;
                    MaxAnkleLeft = pMax;
                    break;
                case JointType.HipRight:
                    AverageHipRight = average;
                    SDHipRight = sd;
                    MinHipRight = pMin;
                    MaxHipRight = pMax;
                    break;
                case JointType.KneeRight:
                    AverageKneeRight = average;
                    SDKneeRight = sd;
                    MinKneeRight = pMin;
                    MaxKneeRight = pMax;
                    break;
                case JointType.AnkleRight:
                    AverageAnkleRight = average;
                    SDAnkleRight = sd;
                    MinAnkleRight = pMin;
                    MaxAnkleRight = pMax;
                    break;
            }
        }

        public string Name => throw new NotImplementedException();
    }
}
