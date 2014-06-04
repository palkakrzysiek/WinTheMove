﻿
// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Coding4Fun.Kinect;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using System.Windows.Media;



namespace WinTheMove.Boxing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        bool closing = false;
        const int skeletonCount = 6; 
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];


        private const int windowSizeX = 800;
        private const int windowSizeY = 600;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // textInfo3.Text = ChooserHelper.returnPath();
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
            {
                return;
            }

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.04f
            };
            sensor.SkeletonStream.Enable(parameters);

            // sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            // sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.DepthFrameReady += DepthFrameReady;
            // sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            Skeleton first =  GetFirstSkeleton(e);

            if (first == null)
            {
                return; 
            }

            ExerciseVariant exercisevariant = ExerciseVariant.HandRight;

            Rescale(first, exercisevariant);

            UpdateLabels(first, exercisevariant);
        }


        private Joint GetGloveJoint(Skeleton skeleton, ExerciseVariant exercisevariant)
        {
            Joint scaledJoint;

            switch (exercisevariant)
            {
                case ExerciseVariant.HandLeft:
                    scaledJoint = skeleton.Joints[JointType.HandLeft];
                    break;
                case ExerciseVariant.HandRight:
                default:
                    scaledJoint = skeleton.Joints[JointType.HandRight];
                    break;
            }

            return scaledJoint;

        }

        private void Rescale(Skeleton skeleton, ExerciseVariant exercisevariant)
        {


            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            //Canvas.SetLeft(element, point.X - element.Width / 2);
            //Canvas.SetTop(element, point.Y - element.Height / 2);

            // Joint scaledJoint = joint.ScaleTo(windowSizeX, windowSizeY); 
            Joint scaledJoint = GetGloveJoint(skeleton, exercisevariant);

            const double scaleX = 1.0; // range of move
            double scaleY = 1.0 * (Convert.ToDouble(windowSizeY)/Convert.ToDouble(windowSizeX)); // range of move
            
            //convert & scale (.3 = means 1/3 of joint distance)
            //Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);
            double diffX = 0;
            double diffY = 0;
            int posX = 0;
            int posY = 0;
            double posYCorrection = -0.2f;
            if (exercisevariant == ExerciseVariant.HandRight)
            {
                diffX = scaledJoint.Position.X - skeleton.Joints[JointType.ShoulderCenter].Position.X;
                posX = 2 * Convert.ToInt32(scaleX * diffX * Convert.ToDouble(windowSizeX));
            }
            else
            {
                diffX = scaledJoint.Position.X - skeleton.Joints[JointType.ShoulderCenter].Position.X;
                posX = 2 * Convert.ToInt32(scaleX * diffX * Convert.ToDouble(windowSizeX)) + windowSizeX / 2;
            }
            diffY = scaledJoint.Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y + posYCorrection;
            posY = Convert.ToInt32(-2.0 * scaleY * diffY * Convert.ToDouble(windowSizeY));

            double handMaxSize = 250; // pixels
            double handMaxPutForward = 0.5f; // meters
            double handMaxPutForwardPercentageSize = 0.5f; // percent
            double handMinSize = handMaxPutForwardPercentageSize * handMaxSize;
            double slope = (handMinSize - handMaxSize) / handMaxPutForward;

            int handSize = Convert.ToInt32(slope * getWirstShoulderDistance(skeleton, exercisevariant, Axis.Z) + handMaxSize);
            Hand.Width = handSize;
            Hand.Height = handSize;

            Canvas.SetLeft(Hand, posX - Hand.Width / 2);
            Canvas.SetTop(Hand, posY - Hand.Width / 2); 
        }


        private double getHorizonatalAngle(Skeleton skeleton, ExerciseVariant exercisevarinat)
        {
            Joint centerPoint, leftPoint, rightPoint;
            switch (exercisevarinat)
            {
                case ExerciseVariant.HandLeft:
                    leftPoint = skeleton.Joints[JointType.ShoulderRight];
                    centerPoint = skeleton.Joints[JointType.ShoulderLeft];
                    rightPoint = skeleton.Joints[JointType.HandLeft];
                    break;
                case ExerciseVariant.HandRight:
                default:
                    leftPoint = skeleton.Joints[JointType.ShoulderLeft];
                    centerPoint = skeleton.Joints[JointType.ShoulderRight];
                    rightPoint = skeleton.Joints[JointType.HandRight];
                    break;
            }
            textInfo1.Text = "left\n";
            textInfo1.Text += leftPoint.Position.X + "\n";
            textInfo1.Text += leftPoint.Position.Y + "\n";
            textInfo1.Text += leftPoint.Position.Z + "\n";
            textInfo2.Text = "center\n";
            textInfo2.Text += centerPoint.Position.X + "\n";
            textInfo2.Text += centerPoint.Position.Y + "\n";
            textInfo2.Text += centerPoint.Position.Z + "\n";
            textInfo3.Text = "right\n";
            textInfo3.Text += rightPoint.Position.X + "\n";
            textInfo3.Text += rightPoint.Position.Y + "\n";
            textInfo3.Text += rightPoint.Position.Z + "\n";
            return -1 * (MathsHelper.AngleBetweenJoints(leftPoint, centerPoint, rightPoint, Axis.X, Axis.Z) - 180); // 0 when straight angle, increase when twisting
        }
        private double getWirstShoulderDistance(Skeleton skeleton, ExerciseVariant exercisevarinat, Axis axis)
        {
            Joint firstPoint, secondPoint;
            switch (exercisevarinat)
            {
                case ExerciseVariant.HandLeft:
                    firstPoint = skeleton.Joints[JointType.ShoulderLeft];
                    secondPoint = skeleton.Joints[JointType.WristLeft];
                    break;
                case ExerciseVariant.HandRight:
                default:
                    firstPoint = skeleton.Joints[JointType.ShoulderRight];
                    secondPoint = skeleton.Joints[JointType.WristRight];
                    break;
            }

            return MathsHelper.DistanceBetweenPoints(firstPoint, secondPoint, axis) ;
        }

        private void UpdateLabels(Skeleton skeleton, ExerciseVariant exercisevariant)
        {
            // textInfo1.Text= getHorizonatalAngle(first, ExerciseVariant.HandRight).ToString() + "\n" +
              //  getHigh(first, ExerciseVariant.HandRight);
            horizontalDistanceLabel.Content = String.Format("{0:0.0} cm", getWirstShoulderDistance(skeleton, exercisevariant, Axis.Z) * 100);
            verticalDistanceLabel.Content = String.Format("{0:0.0} cm", getWirstShoulderDistance(skeleton, exercisevariant, Axis.Y) * -100);
            angleLabel.Content = String.Format("{0:0}°", getHorizonatalAngle(skeleton, exercisevariant)); // 0, 2, 4, ...
        }


        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null; 
                }

                
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                         where s.TrackingState == SkeletonTrackingState.Tracked
                                         select s).FirstOrDefault();

                return first;

            }
        }



        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }
                }
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);

        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            Joint scaledJoint = joint.ScaleTo(windowSizeX, windowSizeY); 
            
            //convert & scale (.3 = means 1/3 of joint distance)
            //Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y); 
            
        }

        Bitmap DepthToBitmap(DepthImageFrame imageFrame)
        {
            short[] pixelData = new short[imageFrame.PixelDataLength];
            imageFrame.CopyPixelDataTo(pixelData);

            Bitmap bmap = new Bitmap(
            imageFrame.Width,
            imageFrame.Height,
            System.Drawing.Imaging.PixelFormat.Format16bppRgb555);

            BitmapData bmapdata = bmap.LockBits(
             new System.Drawing.Rectangle(0, 0, imageFrame.Width,
                                    imageFrame.Height),
             ImageLockMode.WriteOnly,
             bmap.PixelFormat);
            IntPtr ptr = bmapdata.Scan0;
            Marshal.Copy(pixelData,
             0,
             ptr,
             imageFrame.Width *
               imageFrame.Height);
            bmap.UnlockBits(bmapdata);
            return bmap;
        }
        BitmapSource DepthToBitmapSource(
                DepthImageFrame imageFrame)
        {
            short[] pixelData = new short[imageFrame.PixelDataLength];
            imageFrame.CopyPixelDataTo(pixelData);

            BitmapSource bmap = BitmapSource.Create(
             imageFrame.Width,
             imageFrame.Height,
             96, 96,
             PixelFormats.Gray16,
             null,
             pixelData,
             imageFrame.Width * imageFrame.BytesPerPixel);
            return bmap;
        }
        void DepthFrameReady(object sender,
           DepthImageFrameReadyEventArgs e)
        {
            DepthImageFrame imageFrame =
                                e.OpenDepthImageFrame();
            if (imageFrame != null)
            {
                dephPreview.Source = DepthToBitmapSource(
                                               imageFrame);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true; 
            StopKinect(kinectSensorChooser1.Kinect); 
        }

    }
}