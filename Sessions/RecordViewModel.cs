using System;

using Microsoft.Kinect;
using Microsoft.Kinect.Tools;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;

namespace Sessions
{
    /// <summary>
    /// Author: Antonio Iyda Paganelli
    /// 
    /// A class to manages the record view where clips are recorded and played.
    /// </summary>
    class RecordViewModel : ObservableObject, IPageViewModel
    {
        private ApplicationViewModel _app = null;
        private bool isPlayButtonEnabled = true;
        private bool isRecordButtonEnabled = true;
        private bool isStopButtonPressed = false;

        private ICommand _playButtonCommand;
        private ICommand _recordButtonCommand;
        private ICommand _stopButtonCommand;

        private string lastFile = string.Empty;

        /// <summary> Recording duration </summary>
        private TimeSpan duration;

        /// <summary> Number of playback iterations </summary>
        private uint loopCount = 0;

        /// <summary> Delegate to use for placing a job with no arguments onto the Dispatcher </summary>
        private delegate void NoArgDelegate();

        /// <summary>
        /// Delegate to use for placing a job with a single string argument onto the Dispatcher
        /// </summary>
        /// <param name="arg">string argument</param>
        private delegate void OneArgDelegate(string arg);

        /// <summary> Active Kinect sensor </summary>
        private KinectSensor kinectSensor = null;

        /// <summary> Current kinect sesnor status text to display </summary>
        private string kinectStatusText = string.Empty;

        /// <summary>
        /// Current record/playback status text to display
        /// </summary>
        private string recordPlayStatusText = string.Empty;

        /// <summary>
        /// Infrared visualizer
        /// </summary>
        private KinectIRView kinectIRView = null;

        /// <summary>
        /// Depth visualizer
        /// </summary>
        private KinectDepthView kinectDepthView = null;

        /// <summary>
        /// BodyIndex visualizer
        /// </summary>
        private KinectBodyIndexView kinectBodyIndexView = null;

        /// <summary>
        /// Body visualizer
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        public RecordViewModel(ApplicationViewModel app)
        {
            _app = app;

            int maxDuration = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MAX_RECORD_DURATION"]);

            duration = TimeSpan.FromSeconds(maxDuration);

            // get the kinectSensor object
            this.kinectSensor = KinectSensor.GetDefault();

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
            // create the IR visualizer
            this.kinectIRView = new KinectIRView(this.kinectSensor);

            // create the Depth visualizer
            this.kinectDepthView = new KinectDepthView(this.kinectSensor);

            // create the BodyIndex visualizer
            this.kinectBodyIndexView = new KinectBodyIndexView(this.kinectSensor);

            // create the Body visualizer
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);
        }

        public string Name
        {
            get { return "Record View"; }
        }

        /// <summary>
        /// Controls record button behaviour and call its action on click
        /// </summary>
        public ICommand RecordButtonCommand
        {
            get
            {
                if (_recordButtonCommand == null)
                {
                    _recordButtonCommand = new RelayCommand(param => RecordCommand(),
                        param => GetIsRecordButtonEnabled);
                }
                return _recordButtonCommand;
            }
        }

        /// <summary>
        /// Controls play button behaivour and call its action on click
        /// </summary>
        public ICommand PlayButtonCommand
        {
            get
            {
                if (_playButtonCommand == null)
                {
                    _playButtonCommand = new RelayCommand(param => PlayCommand(),
                        param => GetIsPlayButtonEnabled);
                }
                return _playButtonCommand;
            }
        }

        /// <summary>
        /// Controls play button behaivour and call its action on click
        /// </summary>
        public ICommand StopButtonCommand
        {
            get
            {
                if (_stopButtonCommand == null)
                {
                    _stopButtonCommand = new RelayCommand(param => StopCommand(),
                        param => !GetIsPlayButtonEnabled || !GetIsRecordButtonEnabled);
                }
                return _stopButtonCommand;
            }
        }

        /// <summary>
        /// Gets Kinect Infrared view.
        /// </summary>
        public KinectIRView GetKinectIRView
        {
            get { return kinectIRView; }
        }

        /// <summary>
        /// Gets Kinect Depth view
        /// </summary>
        public KinectDepthView GetKinectDepthView
        {
            get { return kinectDepthView; }
        }

        /// <summary>
        /// Gets Kinect Body view
        /// </summary>
        public KinectBodyView GetKinectBodyView
        {
            get { return kinectBodyView; }
        }

        /// <summary>
        /// Gets Kinect Body Index view
        /// </summary>
        public KinectBodyIndexView GetKinectBodyIndexView
        {
            get { return kinectBodyIndexView; }
        }

        public bool GetIsPlayButtonEnabled
        {
            get { return  isPlayButtonEnabled; }
        }

        public bool GetIsRecordButtonEnabled
        {
            get { return isRecordButtonEnabled; }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string KinectStatusText
        {
            get { return this.kinectStatusText; }

            set
            {
                if (this.kinectStatusText != value)
                {
                    this.kinectStatusText = value;
                    OnPropertyChanged("KinectStatusText");
                }
            }
        }

        /// <summary>
        /// Handles the event in which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // set the status text
            this.KinectStatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }


        /// <summary>
        /// Gets or sets the current status text to display for the record/playback features
        /// </summary>
        public string RecordPlaybackStatusText
        {
            get { return this.recordPlayStatusText; }

            set
            {
                if (this.recordPlayStatusText != value)
                {
                    this.recordPlayStatusText = value;
                    OnPropertyChanged("RecordPlaybackStatusText");
                }
            }
        }

        /// <summary>
        /// Stops playing or recording the clip.
        /// </summary>
        public void StopCommand()
        {
            isStopButtonPressed = true;
            this.isRecordButtonEnabled = true;
            this.isPlayButtonEnabled = true;
            this.RecordPlaybackStatusText = "Ended";
        }


        public bool GetStopStatus()
        {
            return isStopButtonPressed;
        }

        /// <summary>
        /// Plays a video clip.
        /// </summary>
        public void PlayCommand()
        {
            string filePath = this.OpenFileForPlayback();

            if (!string.IsNullOrEmpty(filePath))
            {
                this.kinectBodyView.ResetTimers();
                this.lastFile = filePath;
                this.RecordPlaybackStatusText = Properties.Resources.PlaybackInProgressText;
                this.isPlayButtonEnabled = false;
                this.isRecordButtonEnabled = false;

                // Start running the playback asynchronously
                OneArgDelegate playback = new OneArgDelegate(this.PlaybackClip);
                playback.BeginInvoke(filePath, null, null);
            }
        }


        /// <summary>
        /// Plays back a .xef file to the Kinect sensor
        /// </summary>
        /// <param name="filePath">Full path to the .xef file that should be played back to the sensor</param>
        private void PlaybackClip(string filePath)
        {            
            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Create the playback object
                using (KStudioPlayback playback = client.CreatePlayback(filePath))
                {
                    playback.LoopCount = this.loopCount;
                    playback.Start();

                    while (playback.State == KStudioPlaybackState.Playing && ! GetStopStatus())
                    {
                        Thread.Sleep(500);                    
                    }
                }

                client.DisconnectFromService();
            }

            this.isStopButtonPressed = false;
        }

        /// <summary>
        /// Launches the OpenFileDialog window to help user find/select an event file for playback
        /// </summary>
        /// <returns>Path to the event file selected by the user</returns>
        private string OpenFileForPlayback()
        {
            string fileName = string.Empty;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = this.lastFile;
            dlg.DefaultExt = Properties.Resources.XefExtension; // Default file extension
            dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter; // Filter files by extension 
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                fileName = dlg.FileName;
            }

            return fileName;
        }

        /// <summary>
        /// Handles the user clicking on the Record button
        /// </summary>
        private void RecordCommand()
        {
            string filePath = this.SaveRecordingAs();

            if (!string.IsNullOrEmpty(filePath))
            {
                this.lastFile = filePath;
                this.RecordPlaybackStatusText = Properties.Resources.RecordingInProgressText;

                // Start running the recording asynchronously
                OneArgDelegate recording = new OneArgDelegate(this.RecordClip);
                recording.BeginInvoke(filePath, null, null);
            }
        }

        /// <summary>
        /// Records a new .xef file
        /// </summary>
        /// <param name="filePath">Full path to where the file should be saved to</param>
        private void RecordClip(string filePath)
        {
            this.kinectBodyView.ElapsedTime = new TimeSpan();

            using (KStudioClient client = KStudio.CreateClient())
            {
                client.ConnectToService();

                // Specify which streams should be recorded
                KStudioEventStreamSelectorCollection streamCollection = new KStudioEventStreamSelectorCollection();
                streamCollection.Add(KStudioEventStreamDataTypeIds.Ir);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Depth);
                streamCollection.Add(KStudioEventStreamDataTypeIds.Body);
                streamCollection.Add(KStudioEventStreamDataTypeIds.BodyIndex);

                // Create the recording object
                //  this.client.CreateRecording(this.TargetFilePath, streamSelectorCollection, this.settings.RecordingBufferSizeMB, flags);

                uint bufferSize = Convert.ToUInt32(System.Configuration.ConfigurationManager.AppSettings["RECORD_BUFFER_SIZE"]);

                using (KStudioRecording recording = client.CreateRecording(filePath, streamCollection, bufferSize, KStudioRecordingFlags.GenerateFileName))
                {
                    recording.StartTimed(this.duration);
                    while (recording.State == KStudioRecordingState.Recording && ! GetStopStatus())
                    {
                        Thread.Sleep(500);
                    }
                }

                client.DisconnectFromService();
            }
        }

        /// <summary>
        /// Launches the SaveFileDialog window to help user create a new recording file
        /// </summary>
        /// <returns>File path to use when recording a new event file</returns>
        private string SaveRecordingAs()
        {
            string fileName = string.Empty;

            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

            // Gets the path and each time selects a new file name, maybe automatically not opening a dialog for it.
            // dlg.FileName = "recordAndPlaybackBasics.xef";
            // dlg.DefaultExt = Properties.Resources.XefExtension;
            // dlg.AddExtension = true;
            // dlg.Filter = Properties.Resources.EventFileDescription + " " + Properties.Resources.EventFileFilter;
            // dlg.CheckPathExists = true;
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                fileName = dlg.SelectedPath;
            }

            return fileName;
        }
    }
}
