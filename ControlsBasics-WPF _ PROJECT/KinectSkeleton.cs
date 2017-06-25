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

        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        private readonly Pen trackedBonePenGreen = new Pen(Brushes.Green, TrackedBoneThickness);
        private readonly Pen trackedBonePenRed = new Pen(Brushes.Red, TrackedBoneThickness);
        private readonly Pen trackedBonePenOrange = new Pen(Brushes.Orange, TrackedBoneThickness);
        private readonly Pen trackedBonePenYellow = new Pen(Brushes.Yellow, TrackedBoneThickness);

        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, InferredBoneThickness);

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
                    this.DrawBonesAndJoints(this.Skeleton, drawingContext);
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

        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (this.ShowBones)
            {
                // Render Torso
                this.DrawBone(drawingContext,skeleton, JointType.Head, JointType.ShoulderCenter);
                this.DrawBone(drawingContext, skeleton, JointType.ShoulderCenter, JointType.ShoulderLeft);
                this.DrawBone(drawingContext, skeleton, JointType.ShoulderCenter, JointType.ShoulderRight);
                this.DrawBone(drawingContext, skeleton, JointType.ShoulderCenter, JointType.Spine);
                this.DrawBone(drawingContext, skeleton, JointType.Spine, JointType.HipCenter);
                this.DrawBone(drawingContext, skeleton, JointType.HipCenter, JointType.HipLeft);
                this.DrawBone(drawingContext, skeleton, JointType.HipCenter, JointType.HipRight);

                // Left Arm
                this.DrawBone(drawingContext, skeleton, JointType.ShoulderLeft, JointType.ElbowLeft);
                this.DrawBone(drawingContext, skeleton, JointType.ElbowLeft, JointType.WristLeft);
                this.DrawBone(drawingContext, skeleton, JointType.WristLeft, JointType.HandLeft);

                // Right Arm
                this.DrawBone(drawingContext, skeleton, JointType.ShoulderRight, JointType.ElbowRight);
                this.DrawBone(drawingContext, skeleton, JointType.ElbowRight, JointType.WristRight);
                this.DrawBone(drawingContext, skeleton, JointType.WristRight, JointType.HandRight);

                // Left Leg
                this.DrawBone(drawingContext, skeleton, JointType.HipLeft, JointType.KneeLeft);
                this.DrawBone(drawingContext, skeleton, JointType.KneeLeft, JointType.AnkleLeft);
                this.DrawBone(drawingContext, skeleton, JointType.AnkleLeft, JointType.FootLeft);

                // Right Leg
                this.DrawBone(drawingContext, skeleton, JointType.HipRight, JointType.KneeRight);
                this.DrawBone(drawingContext, skeleton, JointType.KneeRight, JointType.AnkleRight);
                this.DrawBone(drawingContext, skeleton, JointType.AnkleRight, JointType.FootRight);
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

        private void DrawBone(DrawingContext drawingContext, Skeleton skeleton, JointType jointType1, JointType jointType2)
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
                drawPen = this.trackedBonePenGreen;
                drawPen.Thickness = TrackedBoneThickness * this.ScaleFactor;

                if (!(JointType.HipCenter <= jointType1 && jointType1 <= JointType.Head)) {
                    JointType reflex;

                    if ((JointType.ShoulderLeft <= jointType1 && jointType1 <= JointType.HandLeft) || (JointType.HipLeft <= jointType1 && jointType1 <= JointType.FootLeft))
                        reflex = jointType1 + 4;
                    else
                        reflex = jointType1 - 4;
                    //JointMapping temp = new JointMapping();
                    
                    Joint mirror = skeleton.Joints[reflex]; // 좌우 대칭관절

                    double leftLenth, rightLength;

                    leftLenth = Math.Abs(joint1.Joint.Position.X - skeleton.Joints[JointType.Spine].Position.X);
                    rightLength = Math.Abs(mirror.Position.X - skeleton.Joints[JointType.Spine].Position.X);


                    //System.Console.WriteLine(skeleton.Position.X);
                    //System.Console.WriteLine(leftLenth);
                    //System.Console.Write( "[" + rightLength + "] ");
                    //System.Threading.Thread.Sleep(1000);

                    //x 축과 y축의 차이가 기준치보다 많으면 빨간색으로 그린다.
                    if ((Math.Abs(leftLenth - rightLength) > 0.06) || Math.Abs(joint1.Joint.Position.Y - mirror.Position.Y) > 0.03)
                        drawPen = new Pen(Brushes.Red, 6);
                    else if ((Math.Abs(leftLenth - rightLength) > 0.04) || Math.Abs(joint1.Joint.Position.Y - mirror.Position.Y) > 0.02)
                        drawPen = new Pen(Brushes.Orange, 6);
                    else if ((Math.Abs(leftLenth - rightLength) > 0.02) || Math.Abs(joint1.Joint.Position.Y - mirror.Position.Y) > 0.01)
                        drawPen = new Pen(Brushes.Yellow, 6);
                }

            }

            

            drawingContext.DrawLine(drawPen, joint1.MappedPoint, joint2.MappedPoint);
        }
    }
}
