//------------------------------------------------------------------------------
// <copyright file="KinectSkeleton.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.WpfViewers
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;
    using System.Threading;

    /// <summary>
    /// This control is used to render a player's skeleton.
    /// If the ClipToBounds is set to "false", it will be allowed to overdraw
    /// it's bounds.
    /// </summary>
    public class KinectSkeleton : Control
    {
        public static readonly DependencyProperty ShowClippedEdgesProperty =
            DependencyProperty.Register("ShowClippedEdges", typeof(bool), typeof(KinectSkeleton), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowJointsProperty =
            DependencyProperty.Register("ShowJoints", typeof(bool), typeof(KinectSkeleton), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowBonesProperty =
            DependencyProperty.Register("ShowBones", typeof(bool), typeof(KinectSkeleton), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowCenterProperty =
            DependencyProperty.Register("ShowCenter", typeof(bool), typeof(KinectSkeleton), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SkeletonProperty =
            DependencyProperty.Register(
                "Skeleton",
                typeof(Skeleton),
                typeof(KinectSkeleton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty JointMappingsProperty =
            DependencyProperty.Register(
                "JointMappings",
                typeof(Dictionary<JointType, JointMapping>),
                typeof(KinectSkeleton),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register(
                "Center",
                typeof(Point),
                typeof(KinectSkeleton),
                new FrameworkPropertyMetadata(new Point(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(
                "ScaleFactor",
                typeof(double),
                typeof(KinectSkeleton),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double TrackedBoneThickness = 6;
        private const double InferredBoneThickness = 1;
        private const double ClipBoundsThickness = 10;

        private int numOfGreen = 0;
        private bool isSuccess = false;
        private bool isThreadRunning = false;
        bool isSuccessTemp = false;
        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, TrackedBoneThickness);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, InferredBoneThickness);
        Thread timerThread;
        /// <summary>
        /// /////////////////////////////
        /// </summary>




        private readonly Pen trackedBonePenGreen = new Pen(Brushes.Green, TrackedBoneThickness);
        private readonly Pen trackedBonePenRed = new Pen(Brushes.Red, TrackedBoneThickness);
        private readonly Pen trackedBonePenOrange = new Pen(Brushes.Orange, TrackedBoneThickness);
        private readonly Pen trackedBonePenYellow = new Pen(Brushes.Yellow, TrackedBoneThickness);



        /// <summary>
        /// /////////////////////////////////
        /// </summary>


        private readonly Brush bottomClipBrush = new LinearGradientBrush(
            Color.FromArgb(0, 255, 0, 0), Color.FromArgb(255, 255, 0, 0), new Point(0, 0), new Point(0, 1));

        private readonly Brush topClipBrush = new LinearGradientBrush(
            Color.FromArgb(0, 255, 0, 0), Color.FromArgb(255, 255, 0, 0), new Point(0, 1), new Point(0, 0));

        private readonly Brush leftClipBrush = new LinearGradientBrush(
            Color.FromArgb(0, 255, 0, 0), Color.FromArgb(255, 255, 0, 0), new Point(1, 0), new Point(0, 0));

        private readonly Brush rightClipBrush = new LinearGradientBrush(
            Color.FromArgb(0, 255, 0, 0), Color.FromArgb(255, 255, 0, 0), new Point(0, 0), new Point(1, 0));

        public bool ShowClippedEdges
        {
            get { return (bool)GetValue(ShowClippedEdgesProperty); }
            set { SetValue(ShowClippedEdgesProperty, value); }
        }

        public bool ShowJoints
        {
            get { return (bool)GetValue(ShowJointsProperty); }
            set { SetValue(ShowJointsProperty, value); }
        }

        public bool ShowBones
        {
            get { return (bool)GetValue(ShowBonesProperty); }
            set { SetValue(ShowBonesProperty, value); }
        }

        public bool ShowCenter
        {
            get { return (bool)GetValue(ShowCenterProperty); }
            set { SetValue(ShowCenterProperty, value); }
        }

        public Skeleton Skeleton
        {
            get { return (Skeleton)GetValue(SkeletonProperty); }
            set { SetValue(SkeletonProperty, value); }
        }

        public Dictionary<JointType, JointMapping> JointMappings
        {
            get { return (Dictionary<JointType, JointMapping>)GetValue(JointMappingsProperty); }
            set { SetValue(JointMappingsProperty, value); }
        }
        
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        public double ScaleFactor
        {
            get { return (double)GetValue(ScaleFactorProperty); }
            set { SetValue(ScaleFactorProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return new Size();                   
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return arrangeBounds;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            
            var currentSkeleton = this.Skeleton;

            // Don't render if we don't have a skeleton, or it isn't tracked
            if (drawingContext == null || currentSkeleton == null || currentSkeleton.TrackingState == SkeletonTrackingState.NotTracked)
            {
                return;
            }

            // Displays a gradient near the edge of the display where the skeleton is leaving the screen
            this.RenderClippedEdges(drawingContext);

            switch (currentSkeleton.TrackingState)
            {
                case SkeletonTrackingState.PositionOnly:
                    if (this.ShowCenter)
                    {
                        drawingContext.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.Center,
                            BodyCenterThickness * this.ScaleFactor,
                            BodyCenterThickness * this.ScaleFactor);
                    }

                    break;
                case SkeletonTrackingState.Tracked:
                    this.DrawBonesAndJoints(drawingContext,KinectSensorManager.postureIndex1);
                break;
            }
        }

        private void RenderClippedEdges(DrawingContext drawingContext)
        {
            var currentSkeleton = this.Skeleton;

            if (!this.ShowClippedEdges || 
                currentSkeleton.ClippedEdges.Equals(FrameEdges.None))
            {
                return;
            }

            double scaledThickness = ClipBoundsThickness * this.ScaleFactor;
            if (currentSkeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    this.bottomClipBrush,
                    null,
                    new Rect(0, this.RenderSize.Height - scaledThickness, this.RenderSize.Width, scaledThickness));
            }

            if (currentSkeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    this.topClipBrush,
                    null,
                    new Rect(0, 0, this.RenderSize.Width, scaledThickness));
            }

            if (currentSkeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    this.leftClipBrush,
                    null,
                    new Rect(0, 0, scaledThickness, this.RenderSize.Height));
            }

            if (currentSkeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    this.rightClipBrush,
                    null,
                    new Rect(this.RenderSize.Width - scaledThickness, 0, scaledThickness, this.RenderSize.Height));
            }
        }

        private void endSkeletonTracking(bool success)
        {
            if (success)
            {
                KinectSensorManager.returnWithSuccessValue(1);
            }
            else
            {
                KinectSensorManager.returnWithSuccessValue(2);
            }
        }

        private void DrawBonesAndJoints(DrawingContext drawingContext,int postureIndex)
        {

            if (isSuccess == true)
            {
                KinectSensorManager.returnWithSuccessValue(1);
                return;
            }
            
           
            //넘어온 버튼 인덱스 출력
            if (this.ShowBones)
            {
                switch (postureIndex) {
                    case 1:
                        DrawBoneFirstPosture(drawingContext);

                        break;
                    case 2:
                        DrawBoneSecondPosture(drawingContext);
                        break;
                    case 3:
                        DrawBoneSecondPosture(drawingContext);
                        break;

                }
            }

            if (this.ShowJoints)
            {
                // Render Joints
                foreach (JointMapping joint in this.JointMappings.Values)
                {
                    Brush drawBrush = null;
                    switch (joint.Joint.TrackingState)
                    {
                        case JointTrackingState.Tracked:
                            drawBrush = this.trackedJointBrush;
                            break;
                        case JointTrackingState.Inferred:
                            drawBrush = this.inferredJointBrush;
                            break;
                    }

                    if (drawBrush != null)
                    {
                        drawingContext.DrawEllipse(drawBrush, null, joint.MappedPoint, JointThickness * this.ScaleFactor, JointThickness * this.ScaleFactor);
                    }
                }
            }
        }

        private void DrawBoneFirstPosture(DrawingContext drawingContext)
        {
            numOfGreen = 0;
            // Render Torso
            this.DrawBone1(drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone1(drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone1(drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone1(drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone1(drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone1(drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone1(drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone1(drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone1(drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone1(drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone1(drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone1(drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone1(drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone1(drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone1(drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone1(drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone1(drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone1(drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone1(drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Console.WriteLine(numOfGreen);
            timerThreadFunc(numOfGreen);


        }

        public void timerThreadFunc(int numOfGreen)
        {
            if (isThreadRunning == false && (numOfGreen == 11 && isSuccessTemp == false))
            {
                isSuccessTemp = true;
                //타이머 시작
                //Console.WriteLine(isSuccessTemp);
                isThreadRunning = true;
                timerThread = new Thread(timerFunc);
                timerThread.Start();
            }
        }

        public void timerFunc() {
            for (int i = 0; i < 30; i++)
            {
                
                if (isSuccessTemp == false)
                {
                    isThreadRunning = false;
                    return;
                }
                Thread.Sleep(100);

            }
            isSuccess = true;
        }
        
        private void DrawBoneSecondPosture(DrawingContext drawingContext)
        {
            numOfGreen = 0;
            // Render Torso
            this.DrawBone2(drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone2(drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone2(drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone2(drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone2(drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone2(drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone2(drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone2(drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone2(drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone2(drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone2(drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone2(drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone2(drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone2(drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone2(drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone2(drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone2(drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone2(drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone2(drawingContext, JointType.AnkleRight, JointType.FootRight);
        }

        private void DrawBone2(DrawingContext drawingContext, JointType jointType1, JointType jointType2)
        {
            JointMapping joint1;
            JointMapping joint2;

          
            // If we can't find either of these joints, exit
            if (!this.JointMappings.TryGetValue(jointType1, out joint1) ||
               joint1.Joint.TrackingState == JointTrackingState.NotTracked ||
               !this.JointMappings.TryGetValue(jointType2, out joint2) ||
               joint2.Joint.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint1.Joint.TrackingState == JointTrackingState.Inferred &&
               joint2.Joint.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            Pen drawPen = this.trackedBonePenGreen;

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            // 몸통
            if ((JointType.HipCenter <= jointType2 && jointType2 <= JointType.Head))
            {
                double aveSpineX = CalcSpineX(Skeleton);
                double coreX = Skeleton.Joints[jointType2].Position.X;

                if (Math.Abs(aveSpineX - coreX) > 0.1)
                {
                    isSuccessTemp = false;
                    drawPen = this.trackedBonePenRed;
                }
                else if (Math.Abs(aveSpineX - coreX) > 0.04)
                {
                    drawPen = this.trackedBonePenOrange;
                }
                else if (Math.Abs(aveSpineX - coreX) > 0.04)
                {
                    drawPen = this.trackedBonePenYellow;
                }
                else
                {
                    numOfGreen++;
                }

                //System.Console.WriteLine(aveSpineX + " " + coreX);


            }
            else if (JointType.AnkleLeft == joint1.Joint.JointType || JointType.AnkleRight == joint1.Joint.JointType
                          || JointType.WristLeft == joint1.Joint.JointType || JointType.WristRight == joint1.Joint.JointType)
            {
                ;
            }
            //팔다리
            else if (joint1.Joint.TrackingState == JointTrackingState.Tracked && joint2.Joint.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePenGreen;
                drawPen.Thickness = TrackedBoneThickness * this.ScaleFactor;

                if (!(JointType.HipCenter <= jointType1 && jointType1 <= JointType.Head))
                {
                    JointType reflex1;
                    JointType reflex2;

                    if ((JointType.ShoulderLeft <= jointType1 && jointType1 <= JointType.HandLeft) ||
                       (JointType.HipLeft <= jointType1 && jointType1 <= JointType.FootLeft) ||
                       (jointType1 == JointType.ShoulderCenter))
                    {
                        reflex1 = jointType1 + 4;
                        reflex2 = jointType2 + 4;
                    }
                    else
                    {
                        reflex1 = jointType1 - 4;
                        reflex2 = jointType2 - 4;
                    }//JointMapping temp = new JointMapping();

                    Joint mirror1 = Skeleton.Joints[reflex1]; // 좌우 대칭관절
                    Joint mirror2 = Skeleton.Joints[reflex2];

                    double left1Length, right1Length;
                    double left2Length, right2Length;
                    double leftAngle, rightAngle;

                    // joint1과 중심과의 거리. (x1-x2)^2+(y1-y2)^2;
                    left1Length = Math.Abs(Math.Pow(joint1.Joint.Position.X - Skeleton.Joints[JointType.Spine].Position.X, 2) + Math.Pow(joint1.Joint.Position.Y - Skeleton.Joints[JointType.Spine].Position.Y, 2));
                    right1Length = Math.Abs(Math.Pow(mirror1.Position.X - Skeleton.Joints[JointType.Spine].Position.X, 2) + Math.Pow(mirror1.Position.Y - Skeleton.Joints[JointType.Spine].Position.Y, 2));

                    //joint2과 중심과의 거리
                    left2Length = Math.Abs(Math.Pow(joint2.Joint.Position.X - Skeleton.Joints[JointType.Spine].Position.X, 2) + Math.Pow(joint2.Joint.Position.Y - Skeleton.Joints[JointType.Spine].Position.Y, 2));
                    right2Length = Math.Abs(Math.Pow(mirror2.Position.X - Skeleton.Joints[JointType.Spine].Position.X, 2) + Math.Pow(mirror2.Position.Y - Skeleton.Joints[JointType.Spine].Position.Y, 2));


                    leftAngle = Math.Abs((joint1.Joint.Position.X - joint2.Joint.Position.X) / joint1.Joint.Position.Y - joint2.Joint.Position.Y);
                    rightAngle = Math.Abs((mirror1.Position.X - mirror2.Position.X) / mirror1.Position.Y - mirror2.Position.Y);

                    //System.Console.WriteLine(skeleton.Position.X);
                    //System.Console.WriteLine(joint1.Joint.JointType + " "+ mirror1.JointType + " : "  + leftAngle + ", " + rightAngle);
                    //System.Console.WriteLine(joint2.Joint.JointType + " " + mirror2.JointType + " : " + leftAngle + ", " + rightAngle);
                    //System.Console.WriteLine(joint1.Joint.JointType + " - " + joint2.Joint.JointType);
                    //System.Console.WriteLine(joint1.Joint.Position.X + " " + joint2.Joint.Position.X + "=" + (joint1.Joint.Position.X - joint2.Joint.Position.X) + " | " + joint1.Joint.Position.Y + " " + joint2.Joint.Position.Y + "=" + (joint1.Joint.Position.Y - joint2.Joint.Position.Y));
                    //System.Console.WriteLine(mirror1.Position.X + " " + mirror2.Position.X + "="+ (mirror1.Position.X - mirror2.Position.X) +" | " + mirror1.Position.Y + " " + mirror2.Position.Y + "=" + (mirror1.Position.Y - mirror2.Position.Y ));
                    //System.Console.WriteLine();

                    //System.Threading.Thread.Sleep(1000);


                    //x 축과 y축의 차이가 기준치보다 많으면 빨간색으로 그린다.

                    if ((Math.Abs(left1Length - right1Length) > 0.06) || Math.Abs(left2Length - right2Length) > 0.06)
                    {
                        isSuccessTemp = false;
                        drawPen = new Pen(Brushes.Red, 6);
                    }
                    else if ((Math.Abs(left1Length - right1Length) > 0.04) || Math.Abs(left2Length - right2Length) > 0.02)
                    {
                        drawPen = new Pen(Brushes.Orange, 6);
                    }
                    else if ((Math.Abs(left1Length - right1Length) > 0.02) || Math.Abs(left2Length - right2Length) > 0.01)
                    {
                        drawPen = new Pen(Brushes.Yellow, 6);
                    }
                    else
                    {
                        numOfGreen++;
                    }

                }
            }





            drawingContext.DrawLine(drawPen, joint1.MappedPoint, joint2.MappedPoint);
        }
        private void DrawBone1(DrawingContext drawingContext, JointType jointType1, JointType jointType2)
        {
            JointMapping joint1;
            JointMapping joint2;


            // If we can't find either of these joints, exit
            if (!this.JointMappings.TryGetValue(jointType1, out joint1) ||
                joint1.Joint.TrackingState == JointTrackingState.NotTracked ||
                !this.JointMappings.TryGetValue(jointType2, out joint2) ||
                joint2.Joint.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint1.Joint.TrackingState == JointTrackingState.Inferred &&
                joint2.Joint.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            drawPen.Thickness = InferredBoneThickness * this.ScaleFactor;
            if (joint1.Joint.TrackingState == JointTrackingState.Tracked && joint2.Joint.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
                drawPen.Thickness = TrackedBoneThickness * this.ScaleFactor;
            }

            drawingContext.DrawLine(drawPen, joint1.MappedPoint, joint2.MappedPoint);
        }
        public double CalcSpineX(Skeleton skeleton)
        {
            double aveX = 0;

            for (JointType i = JointType.HipCenter; i <= JointType.Head; i++)
            {
                Joint temp = skeleton.Joints[i];
                aveX += temp.Position.X;
            }

            aveX /= 4;

            return aveX;
        }
    }
}
