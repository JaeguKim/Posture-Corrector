﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.ControlsBasics
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.Controls;
    using System.Windows.Media.Imaging;
    using System.Windows.Media;



    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        public static readonly DependencyProperty PageLeftEnabledProperty = DependencyProperty.Register(
            "PageLeftEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty PageRightEnabledProperty = DependencyProperty.Register(
            "PageRightEnabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        private const double ScrollErrorMargin = 0.001;

        private const int PixelScrollByAmount = 20;

        private readonly KinectSensorChooser sensorChooser;

        private KinectExplorer.MainWindow temp;

        public int returnValueFromKinectSensorWindow = -1;
        int postureIndex = 0;


        private void WindowClosedForMachineCheck(object sender, EventArgs e)
        {
            
            this.IsEnabled = true;
        }
        public class WindowClosedArgs : EventArgs
        {
            public int returnValue { get; set; }
        }
        public void WindowClosedForReturnValue(object sender, WindowClosedArgs e)
        {
            Console.WriteLine(e.returnValue);
            Environment.Exit(0);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class. 
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            // initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();

            // Bind the sensor chooser's current sensor to the KinectRegion
            var regionSensorBinding = new Binding("Kinect") { Source = this.sensorChooser };
            BindingOperations.SetBinding(this.kinectRegion, KinectRegion.KinectSensorProperty, regionSensorBinding);

            // Clear out placeholder content
            this.wrapPanel.Children.Clear();

            // Add in display content
            for (var index = 0; index < 30; ++index)
            {
                var button = new KinectTileButton { Label =(index + 1).ToString(CultureInfo.CurrentCulture)};



                if (index + 1 == 1)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri("skeleton.png", UriKind.Relative);
                    bi.EndInit();
                    button.Label = "관절 비추기";
                  //  button.Name = "관절 비추기";
                    button.Background = new ImageBrush(bi);
                }
                else if (index + 1 == 2)
                {
                    BitmapImage bi = new BitmapImage();
                 bi.BeginInit();
                 bi.UriSource = new Uri("좌우대칭.png", UriKind.Relative);
                bi.EndInit();
                    button.Label = "자세교정";
                 button.Background = new ImageBrush(bi);
                }
                else if (index + 1 == 3)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri("yoga1.JPG", UriKind.Relative);
                    bi.EndInit();

                    button.Label = "요가자세1";
                    button.Background = new ImageBrush(bi);
                }
                else if (index + 1 == 4)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri("yoga2.JPG", UriKind.Relative);
                    bi.EndInit();

                    button.Label = "요가자세2";
                    button.Background = new ImageBrush(bi);
                }



                ///////////////////////자세 라벨링

                this.wrapPanel.Children.Add(button);
            }

            // Bind listner to scrollviwer scroll position change, and check scroll viewer position
            this.UpdatePagingButtonState();
            scrollViewer.ScrollChanged += (o, e) => this.UpdatePagingButtonState();
        }

        /// <summary>
        /// CLR Property Wrappers for PageLeftEnabledProperty
        /// </summary>
        public bool PageLeftEnabled
        {
            get
            {
                return (bool)GetValue(PageLeftEnabledProperty);
            }

            set
            {
                this.SetValue(PageLeftEnabledProperty, value);
            }
        }

        /// <summary>
        /// CLR Property Wrappers for PageRightEnabledProperty
        /// </summary>
        public bool PageRightEnabled
        {
            get
            {
                return (bool)GetValue(PageRightEnabledProperty);
            }

            set
            {
                this.SetValue(PageRightEnabledProperty, value);
            }
        }

        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private static void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();

                    try
                    {
                        args.NewSensor.DepthStream.Range = DepthRange.Near;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
        }

        /// <summary>
        /// Handle a button click from the wrap panel.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void KinectTileButtonClick(object sender, RoutedEventArgs e)
        {
            
            var button = (KinectTileButton)e.OriginalSource;
            var selectionDisplay = new SelectionDisplay(button.Label as string);
            this.kinectRegionGrid.Children.Add(selectionDisplay);
            e.Handled = true;
            this.IsEnabled = false;
            string buttonLabel = (string)button.Label;
            
            if (buttonLabel == "1")
            {
                postureIndex = 1;
            }
            else if (buttonLabel == "2")
            {
                postureIndex = 2;
            }


            if (postureIndex < 0)
            {
                return ;
            }

            temp = new KinectExplorer.MainWindow(postureIndex);//인덱스 넘기기
            
            temp.Closed += WindowClosedForMachineCheck;
            temp.OnChildReturnValueEventInMainWindow += new KinectExplorer.MainWindow.OnChildReturnValueHandlerInMainWindow(cw_OnChildCallBack);
            
            
            temp.Show();
        }
        public void cw_OnChildCallBack(int ret)
        {
            returnValueFromKinectSensorWindow = ret;
            Console.WriteLine(returnValueFromKinectSensorWindow + " ControlBasics.MainWindow");//1 true, 2 false

            if (returnValueFromKinectSensorWindow == 1) //성공이면 다음버튼을 눌러야함.
            {
                Console.WriteLine(returnValueFromKinectSensorWindow + "  asdasd");
                returnValueFromKinectSensorWindow = 0;
                postureIndex++;
                temp = new KinectExplorer.MainWindow(postureIndex);//인덱스 넘기기

                temp.Closed += WindowClosedForMachineCheck;
                temp.OnChildReturnValueEventInMainWindow += new KinectExplorer.MainWindow.OnChildReturnValueHandlerInMainWindow(cw_OnChildCallBack);
                this.IsEnabled = false;

                temp.Show();

            }
            
        }
        /// <summary>
        /// Handle paging right (next button).
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void PageRightButtonClick(object sender, RoutedEventArgs e)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + PixelScrollByAmount);
        }

        /// <summary>
        /// Handle paging left (previous button).
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void PageLeftButtonClick(object sender, RoutedEventArgs e)
        {
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - PixelScrollByAmount);
        }

        /// <summary>
        /// Change button state depending on scroll viewer position
        /// </summary>
        private void UpdatePagingButtonState()
        {
            this.PageLeftEnabled = scrollViewer.HorizontalOffset > ScrollErrorMargin;
            this.PageRightEnabled = scrollViewer.HorizontalOffset < scrollViewer.ScrollableWidth - ScrollErrorMargin;
        }

    }
}
