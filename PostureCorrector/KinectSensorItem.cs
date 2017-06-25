//------------------------------------------------------------------------------
// <copyright file="KinectSensorItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.KinectExplorer
{
    using System;
    using System.ComponentModel;
    using Microsoft.Kinect;

    /// <summary>
    /// A KinectSensorItem maintains a bit of state about a KinectSensor and manages showing/closing
    /// a KinectWindow associated with the KinectSensor.
    /// </summary>
    public class KinectSensorItem : INotifyPropertyChanged
    {
        /// <summary>
        /// The last set status.
        /// </summary>
        private KinectStatus status;
        public System.EventHandler tempCallBack;
        
        public int retValueInKinectSensorItem = 0;
        public int returnCodeFromKinectWindow = -1;

        public delegate void CallBack(object sender, EventArgs e);

       


        public KinectSensorItem(KinectSensor sensor, string id,int index)
        {
            this.Sensor = sensor;
            this.Id = id;
            this.postureIndex = index;
        }

        /// <summary>
        /// Part of INotifyPropertyChanged, this event fires whenver a property changes value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public KinectSensor Sensor { get; private set; }
        public int postureIndex { get; set; }
        public string Id { get; private set; }

        public KinectStatus Status
        {
            get
            {
                return this.status;
            }

            set
            {
                if (this.status != value)
                {
                    this.status = value;

                    if (null != this.Window)
                    {
                        this.Window.StatusChanged(value);
                    }

                    this.NotifyPropertyChanged("Status");
                }
            }
        }

        public void OnChildReturnEventInSensorItem(int retValue)
        {
            Console.WriteLine(retValue + " in sensoritem");
        }
        public KinectWindow Window { get; set; }

        /// <summary>
        /// Ensure a KinectWindow is associated with this KinectSensorItem, and Show it and Activate it.
        /// This can be safely called for a fully operation and visible Window.
        /// </summary>
        public void ShowWindow()
        {
            if (null == this.Window)
            {
                var kinectWindow = new KinectWindow(postureIndex);
                kinectWindow.Closed += this.KinectWindowOnClosed;
                kinectWindow.Closed += tempCallBack;
                kinectWindow.OnChildReturnEventInKinectWindow += new KinectWindow.OnChildReturnValueInKinectWindow(cw_OnChildReturnValueEvent);
                this.Window = kinectWindow;
            }
            
            this.Window.KinectSensor = this.Sensor;
            //CallBack callBack = new CallBack(MainWindow.WindowClosed);
            //System.EventHandler tempCallBack = MainWindow.WindowClosed;
            //this.Window.Closed += tempCallBack;
           

            this.Window.Show();
            this.Window.Activate();
        }
        void cw_OnChildReturnValueEvent(int ret)
        {
            returnCodeFromKinectWindow = ret;
        }
        /// <summary>
        /// Activate a Window for this sensor if such a Window already exists.
        /// </summary>
        public void ActivateWindow()
        {
            if (null != this.Window)
            {
                this.Window.Activate();
            }
        }


        /// <summary>
        /// Close the KinectWindow associated with this KinectSensorItem, if present.
        /// </summary>
        public void CloseWindow()
        {
            


            if (null != this.Window)
            {
                this.Window.Close();
                this.Window = null;
            }
        }

        private void KinectWindowOnClosed(object sender, EventArgs e)
        {
            //OnChildReturnEvent(50);
            var sensor = this.Window.KinectSensor;
            this.Window.Closed -= this.KinectWindowOnClosed;
            
            this.Window.Closed -= this.tempCallBack;
            this.Window.KinectSensor = null;
            this.Window = null;

            if ((null != sensor) && sensor.IsRunning)
            {
                sensor.Stop();
            }
            

          
            
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
