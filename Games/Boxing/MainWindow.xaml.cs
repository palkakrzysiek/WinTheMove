
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Coding4Fun.Kinect;
using System.IO;



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

        private int windowSizeX = 800;
        private int windowSizeY = 600;

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
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };
            sensor.SkeletonStream.Enable(parameters);

            sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30); 
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

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

            GetCameraPoint(first, e);

            //set scaled position
            ScalePosition(Head, first.Joints[JointType.Head]);
            ScalePosition(LeftHand, first.Joints[JointType.HandLeft]);
            ScalePosition(RightHand, first.Joints[JointType.HandRight]);

            //ProcessGesture(Data.Jonits[JointType.Head], Data.Joints[JointType.HandLeft], Data.Joints[JointType.HandRight]);

            textInfo1.Text= getHorizonatalAngle(first, ExerciseVariant.HandRight).ToString() + "\n" +
                getHigh(first, ExerciseVariant.HandRight);
            
        }

	public enum ExerciseVariant {HandLeft, HandRight};

        private double getHorizonatalAngle(Skeleton skeleton, ExerciseVariant exercisevarinat)
        {
            Joint centerPoint, leftPoint, rightPoint;
            switch (exercisevarinat)
            {
                case ExerciseVariant.HandLeft:
                    leftPoint = skeleton.Joints[JointType.ShoulderCenter];
                    centerPoint = skeleton.Joints[JointType.ShoulderLeft];
                    rightPoint = skeleton.Joints[JointType.WristLeft];
                    break;
                case ExerciseVariant.HandRight:
                default:
                    leftPoint = skeleton.Joints[JointType.ShoulderCenter];
                    centerPoint = skeleton.Joints[JointType.ShoulderRight];
                    rightPoint = skeleton.Joints[JointType.WristRight];
                    break;
            }
            return MathsHelper.AngleBetweenJoints(leftPoint, centerPoint, rightPoint, Axis.X, Axis.Z) - 90;
        }
        private double getHigh(Skeleton skeleton, ExerciseVariant exercisevarinat)
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
            return MathsHelper.DistanceBetweenPoints(firstPoint, secondPoint, Axis.Y);
        }

        private void ProcessGesture(Joint center, Joint leftPoint, Joint rightPoint)
        {
            textInfo1.Text = "center\n";
            textInfo1.Text += "\n" + center.Position.X;
            textInfo1.Text += "\n" + center.Position.Y;
            textInfo1.Text += "\n" + center.Position.Z;
            textInfo2.Text = "left\n";
            textInfo2.Text += "\n" + leftPoint.Position.X;
            textInfo2.Text += "\n" + leftPoint.Position.Y;
            textInfo2.Text += "\n" + leftPoint.Position.Z;
            textInfo3.Text = "right\n";
            textInfo3.Text += "\n" + rightPoint.Position.X;
            textInfo3.Text += "\n" + rightPoint.Position.Y;
            textInfo3.Text += "\n" + rightPoint.Position.Z;
            textInfo3.Text += "\nAngle:\n" + MathsHelper.AngleBetweenJoints(center, leftPoint, rightPoint);
        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                

                //Map a joint location to a point on the depth map
                //head
                DepthImagePoint headDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.Head].Position);
                //left hand
                DepthImagePoint leftDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);
                //right hand
                DepthImagePoint rightDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);


                //Map a depth point to a point on the color image
                //head
                ColorImagePoint headColorPoint =
                    depth.MapToColorImagePoint(headDepthPoint.X, headDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left hand
                ColorImagePoint leftColorPoint =
                    depth.MapToColorImagePoint(leftDepthPoint.X, leftDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right hand
                ColorImagePoint rightColorPoint =
                    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);


                //Set location
                CameraPosition(Head, headColorPoint);
                CameraPosition(LeftHand, leftColorPoint);
                CameraPosition(RightHand, rightColorPoint);

            }        
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


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true; 
            StopKinect(kinectSensorChooser1.Kinect); 
        }

    }
}