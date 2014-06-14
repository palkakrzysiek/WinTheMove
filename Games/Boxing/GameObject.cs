using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinTheMove.Boxing
{
    class Hand : GameObject
    {
        double posYCorrection = -0.2f;
        public Hand(Shape hand, double angle, double length, double protruding)
            : base(angle, length, protruding)
        {
            image = hand;
        }
        static Joint GetShoulderJoint(Skeleton skeleton, ExerciseVariant exercisevariant)
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

        public Joint GetGloveJoint(Skeleton skeleton, ExerciseVariant exercisevariant)
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
        public double getLiftDistance(Skeleton skeleton, ExerciseVariant exercisevarinat)
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

            return MathsHelper.DistanceBetweenPoints(firstPoint, secondPoint, Axis.Y) ;
        }

        public double getProtrudingDistance(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            Joint handJoint = GetGloveJoint(skeleton, exerciseVariant);
            Joint shoulderJoint = GetShoulderJoint(skeleton, exerciseVariant);
            double protrudingLength = Math.Abs(MathsHelper.DistanceBetweenPoints(shoulderJoint, handJoint, Axis.X, Axis.Z));
            return protrudingLength;
        }

        public double getHorizonatalAngle(Skeleton skeleton, ExerciseVariant exercisevarinat)
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

        public double getHandHorizontalAngle(Skeleton skeleton, ExerciseVariant exerciseVariant)
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
            // double positionX = - (protrudingLength * Math.Cos(angle/180*Math.PI));
            // textInfo1.Text = "debug\n" + positionX + "\nangle: " + getPlayerRotationAngle(skeleton, exerciseVariant);
            return angle;
        }
        public double getVerticalPosition(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            Joint gloveJoint = GetGloveJoint(skeleton, exerciseVariant);
            const double posYCorrection = -0.2f;
            double diffY = gloveJoint.Position.Y - skeleton.Joints[JointType.ShoulderCenter].Position.Y + posYCorrection;
            return diffY;
        }

        public void UpdatePosition(Skeleton skeleton, ExerciseVariant exerciseVariant)
        {
            double lift, angle, protruding;
            lift = getVerticalPosition(skeleton, exerciseVariant);
            angle = getHandHorizontalAngle(skeleton, exerciseVariant);
            protruding = getProtrudingDistance(skeleton, exerciseVariant);
            SetPosition(angle, lift, protruding);

        }

    }

    abstract class GameObject
    {

        private const int windowSizeX = 800;
        private const int windowSizeY = 600;

        const double scaleX = 1.0; // range of move
        double scaleY = 1.0 * (Convert.ToDouble(windowSizeY) / Convert.ToDouble(windowSizeX)); // range of move

        double diffY = 0;
        int posX = 0;
        int posY = 0;
        int leftDisplayMargin = 100;
        int rightDisplayMargin = 100;
        double horizontalPosition;

        const double armLength = 0.6f;
        const double handMaxSize = 250; // pixels
        const double handMaxPutForward = 0.5f; // meters
        const double handMaxPutForwardPercentageSize = 0.5f; // percent / 100, size of image during push
        const double handMinSize = handMaxPutForwardPercentageSize * handMaxSize;
        const double slope = (handMinSize - handMaxSize) / handMaxPutForward;


        double angle, lift, push;
        public GameObject(double a, double l, double p)
        {
            angle = a;
            lift = l;
            push = p;
        }

        public int GetObjectSize(double distance)
        {
            double value = (slope * distance) + handMaxSize;
            Console.WriteLine(distance);
            if (value < handMinSize)
            {
                value = handMinSize;
            }
            else if (value > handMaxSize)
            {
                value = handMaxSize;
            }
            return Convert.ToInt32(value);
        }
        protected System.Windows.Shapes.Shape image;
        //public void SetPosition(int x, int y, double z)
        public void SetPosition(double angle, double lift, double protrudingLength)
        {
            double horizontalPosition = - (protrudingLength * Math.Cos(angle/180*Math.PI));
            posX = Convert.ToInt32((horizontalPosition + armLength/2)/ armLength * (windowSizeX - leftDisplayMargin - rightDisplayMargin) + leftDisplayMargin);
            posY = Convert.ToInt32(-2.0 * scaleY * lift * Convert.ToDouble(windowSizeY));
            image.Width = GetObjectSize(protrudingLength);
            image.Height = GetObjectSize(protrudingLength);
            Canvas.SetLeft(image, posX - image.Width / 2);
            Canvas.SetTop(image, posY - image.Width / 2);
        }
    }

    abstract class ObjectToDestroy : GameObject
    {
        const int MAX_OBJECT_SIZE = 250;
        protected Canvas gameArea;
        public ObjectToDestroy(Canvas gameArea, double angle, double protruding, double lift)
        : base(angle, lift, protruding)
        {
            this.gameArea = gameArea;
        }

        public abstract void Destroy();

    }
    class Orange : ObjectToDestroy
    {
        private const string ORANGE_IMAGE = "Resources/orange.png";
        private const string SMASHED_ORANGE_IMAGE = "Resources/smashed.png";
        ImageBrush orangeBrush = new ImageBrush();
        ImageBrush smashedOrangeBrush = new ImageBrush();
        public Orange(Canvas gameArea, int x, int y, float z) : base(gameArea, x, y, z)
        {
            // orangeBrush.ImageSource = new BitmapImage(new Uri(@"./orange.png", UriKind.Relative));
            orangeBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Boxing;component/Resources/orange.png", UriKind.Absolute));
            smashedOrangeBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Boxing;component/Resources/smashed.png", UriKind.Absolute));
            image = new System.Windows.Shapes.Rectangle();

            image.Fill = orangeBrush;
            SolidColorBrush unimplementedExerciseBackground = new SolidColorBrush(Colors.Aqua);
            image.Fill = orangeBrush;//unimplementedExerciseBackground;
            SetPosition(x, y, z);
            gameArea.Children.Add(image);
        }
        System.Threading.Timer timer;
        void RemoveObject()
        {
            gameArea.Children.Remove(image);
        }
        override public void Destroy()
        {
            image.Fill = smashedOrangeBrush;
            // timer = new System.Threading.Timer(obj => { RemoveObject(); }, null, 9000, System.Threading.Timeout.Infinite);
        }
    }
}
