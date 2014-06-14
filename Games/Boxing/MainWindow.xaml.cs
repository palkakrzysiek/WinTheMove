
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
using System.Threading.Tasks;
using System.Threading;


namespace WinTheMove.Boxing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    //<Namespace>.Utilities.Extensions




    struct RepetitionCounter
    {
        public int twistsDone { get; internal set; }
        public int liftsDone { get; internal set; }
        public int pushesDone { get; internal set; }
        /*
        public RepetitionCounter()
        {
            twistsDone = 0;
            liftsDone = 0;
            pushesDone = 0;
        }
        */

        public void TwistDone() { twistsDone++; }
        public void LiftDone() { liftsDone++; }
        public void PushDone() { pushesDone++; }
    }



    public partial class MainWindow : Window
    {

        bool closing = false;
        const int skeletonCount = 6; 
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];




        private ExerciseSettings exerciseSettings;

        private const int windowSizeX = 800;
        private const int windowSizeY = 600;


        Hand hand;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeExerciseSettings();
//            textInfo1.Text = "debug\n" + o.ToString();
            hand = new Hand(HandImage, 10, 10, 10); 
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
        }
        public void InitializeExerciseSettings()
        {
            exerciseSettings.exerciseVariant = ExerciseVariant.HandRight ;
            exerciseSettings.isTwistActive = true;
            exerciseSettings.twistMinAngle = 10;
            exerciseSettings.twistMaxAngle = 110;
            exerciseSettings.twistRepetitions = 2;
            exerciseSettings.twistTolerance = 5;
            exerciseSettings.isLiftActive = true;
            exerciseSettings.liftMinHeight = 0;
            exerciseSettings.liftMaxHeight = 15;
            exerciseSettings.liftRepetitions = 3;
            exerciseSettings.liftTolerance = 4;
            exerciseSettings.isPushingActive = true;
            exerciseSettings.pushingMinDistance = 10;
            exerciseSettings.pushingMaxDistance = 35;
            exerciseSettings.pushingTolerance = 3;
            exerciseSettings.pushingRepetitions = 3;
        }

        RepetitionCounter repetitionCounter = new RepetitionCounter();
        int CountRepetitionsLeft()
        {
            int repetitionsLeft = 
                (exerciseSettings.isTwistActive ? exerciseSettings.twistRepetitions - repetitionCounter.twistsDone : 0) +
                (exerciseSettings.isLiftActive ? exerciseSettings.liftRepetitions - repetitionCounter.liftsDone : 0) + 
                (exerciseSettings.isPushingActive ? exerciseSettings.pushingRepetitions - repetitionCounter.pushesDone : 0);
            return repetitionsLeft;
        }

        static ExerciseType exerciseType;
        bool exerciseChanged = true;
        private void ExerciseHandler(Skeleton skeleton)
        {
            if (exerciseChanged)
            {
                exerciseChanged = false;
                SetExerciseType();
            }
            ExerciseVariant exerciseVariant = exerciseSettings.exerciseVariant;
            Rescale(skeleton, exerciseVariant);
            DisplayMessages(skeleton, exerciseVariant);
            UpdateLabels(skeleton, exerciseVariant);
        }
        void SetExerciseType()
        {
            exerciseChanged = true;
            switch (exerciseType)
            {
                case ExerciseType.Lift:
                    if (exerciseSettings.isPushingActive)
                    {
                        exerciseType = ExerciseType.Push;
                        break;
                    }
                    else
                    {
                        goto case ExerciseType.Push;
                    }

                case ExerciseType.Push:
                    if (exerciseSettings.isTwistActive)
                    {
                        exerciseType = ExerciseType.Twist;
                        break;
                    }
                    else
                    {
                        goto case ExerciseType.Twist;
                    }
                case ExerciseType.Twist:
                    if (exerciseSettings.isLiftActive)
                    {
                        exerciseType = ExerciseType.Lift;
                        break;
                    }
                    else
                    {
                        goto case ExerciseType.Lift;
                    }
            }
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

                Correction = 0.5f,
                Prediction = 0.5f,
                JitterRadius = 1.00f,
                MaxDeviationRadius = 0.4f
            };
            
            sensor.SkeletonStream.Enable(parameters);
            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.DepthFrameReady += DepthFrameReady;

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

            ExerciseHandler(first);
            // UpdatePosition(first, exercisevariant);
        }


        /*
        static public Joint GetGloveJoint(Skeleton skeleton, ExerciseVariant exercisevariant)
        {
            Joint scaledJoint;

            switch (exercisevariant)
            {
                case ExerciseVariant.HandLeft:
                    scaledJoint = skeleton.Joints[JointType.WristLeft];
                    break;
                case ExerciseVariant.HandRight:
                default:
                    scaledJoint = skeleton.Joints[JointType.WristRight];
                    break;
            }

            return scaledJoint;
        }
        */

        /*
        private Joint GetShoulderJoint(Skeleton skeleton, ExerciseVariant exercisevariant)
        {
            Joint scaledJoint;

            switch (exercisevariant)
            {
                case ExerciseVariant.HandLeft:
                    scaledJoint = skeleton.Joints[JointType.ShoulderLeft];
                    break;
                case ExerciseVariant.HandRight:
                default:
                    scaledJoint = skeleton.Joints[JointType.ShoulderRight];
                    break;
            }

            return scaledJoint;
        }
        */
        double shiftFromLeft;
        double protruding;

        static public double getPlayerRotationAngle(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            Joint ShoulderLeft = skeleton.Joints[JointType.ShoulderLeft];
            Joint ShoulderRight = skeleton.Joints[JointType.ShoulderRight];
            double hypotenuseLength = Math.Abs(MathsHelper.DistanceBetweenPoints(ShoulderLeft, ShoulderRight, Axis.X, Axis.Z));
            double oppositeSideLength = MathsHelper.DistanceBetweenPoints(ShoulderLeft, ShoulderRight, Axis.Z);
            double playerRotationAngle = Math.Asin(oppositeSideLength / hypotenuseLength);// / Math.PI * 180;
            return playerRotationAngle;
        }

        private bool IsPlayerTurnedSideways(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            double rotationAngle = getPlayerRotationAngle(skeleton, exerciseVariant);
            double tolerance = 30.0f / 180 * Math.PI;
            Joint ShoulderLeft = skeleton.Joints[JointType.ShoulderLeft];
            Joint ShoulderRight = skeleton.Joints[JointType.ShoulderRight];
            bool isTracked = ShoulderLeft.TrackingState == JointTrackingState.Tracked && ShoulderRight.TrackingState == JointTrackingState.Tracked;
            return (rotationAngle > tolerance || rotationAngle < (-1) * tolerance) && !isTracked;

        }

        private void DisplayMessages(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            if (IsPlayerTurnedSideways(skeleton, exerciseVariant))
            {
                warningBox.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                warningBox.Visibility = System.Windows.Visibility.Hidden;
            }

        }


        /*
        private double getProtrudingDistance(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            Joint handJoint = GetGloveJoint(skeleton, exerciseVariant);
            Joint shoulderJoint = GetShoulderJoint(skeleton, exerciseVariant);
            double protrudingLength = Math.Abs(MathsHelper.DistanceBetweenPoints(shoulderJoint, handJoint, Axis.X, Axis.Z));
            return protrudingLength;
        }
        */

        /*
        public double getHandHorizontalPositon(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            double protrudingLength = getProtrudingDistance(skeleton, exerciseVariant);

            double angle;
            if (exerciseVariant == ExerciseVariant.HandRight)
            {
                angle = 180 - getHorizonatalAngle(skeleton, exerciseVariant);
            }
            else
            {
                angle = getHorizonatalAngle(skeleton, exerciseVariant);
            }
            double positionX = - (protrudingLength * Math.Cos(angle/180*Math.PI));
            // textInfo1.Text = "debug\n" + positionX + "\nangle: " + getPlayerRotationAngle(skeleton, exerciseVariant);
            return positionX;
        }
        */

        /*
        static public double getVerticalPosition(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            Joint gloveJoint = GetGloveJoint(skeleton, exerciseVariant);
            const double posYCorrection = -0.2f;
            double diffY = gloveJoint.Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y + posYCorrection;
            return diffY;
        }
        */

        private void Rescale(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            hand.UpdatePosition(skeleton, exerciseVariant);
            /*
            Joint scaledJoint = GetGloveJoint(skeleton, exerciseVariant);

            const double scaleX = 1.0; // range of move
            double scaleY = 1.0 * (Convert.ToDouble(windowSizeY)/Convert.ToDouble(windowSizeX)); // range of move
            
            double diffY = 0;
            int posX = 0;
            int posY = 0;
            double posYCorrection = -0.2f;
            double armLength = 0.6f;
            int leftDisplayMargin = 100;
            int rightDisplayMargin = 100;
            double horizontalPosition = getHandHorizontalPositon(skeleton, exerciseVariant);

            posX = Convert.ToInt32((horizontalPosition + armLength/2)/ armLength * (windowSizeX - leftDisplayMargin - rightDisplayMargin) + leftDisplayMargin);
            diffY = scaledJoint.Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y + posYCorrection;
            posY = Convert.ToInt32(-2.0 * scaleY * diffY * Convert.ToDouble(windowSizeY));

            double handMaxSize = 250; // pixels
            double handMaxPutForward = 0.4f; // meters
            double handMaxPutForwardPercentageSize = 0.5f; // percent / 100, size of image during push
            double handMinSize = handMaxPutForwardPercentageSize * handMaxSize;
            double slope = (handMinSize - handMaxSize) / handMaxPutForward;

            int handSize = Convert.ToInt32(slope * getProtrudingDistance(skeleton, exerciseVariant) + handMaxSize);
            HandImage.Width = handSize;
            HandImage.Height = handSize;

            Canvas.SetLeft(HandImage, posX - HandImage.Width / 2);
            Canvas.SetTop(HandImage, posY - HandImage.Width / 2); 
            */
        }


        /*
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

            return -1 * (MathsHelper.AngleBetweenJoints(leftPoint, centerPoint, rightPoint, Axis.X, Axis.Z) - 180); // 0 when straight angle, increase when twisting
        }
        */
 
        /*
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
        */

        private void UpdateLabels(Skeleton skeleton, ExerciseVariant exercisevariant)
        {
            horizontalDistanceLabel.Content = String.Format("{0:0.0} cm", hand.getProtrudingDistance(skeleton, exercisevariant) * 100);
            verticalDistanceLabel.Content = String.Format("{0:0.0} cm", hand.getLiftDistance(skeleton, exercisevariant) * -100);
            angleLabel.Content = String.Format("{0:0}°", hand.getHorizonatalAngle(skeleton, exercisevariant)); // 0, 2, 4, ...
            RepetitionsCounter.Text = CountRepetitionsLeft().ToString();
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