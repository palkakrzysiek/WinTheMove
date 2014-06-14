using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinTheMove.Boxing
{
    public enum ExerciseVariant { HandLeft, HandRight };
    public enum ExerciseType { Twist, Lift, Push };
    public struct ExerciseSettings
    {

        public ExerciseVariant exerciseVariant;
        public bool isTwistActive;
        public double twistMinAngle;
        public double twistMaxAngle;
        public double twistTolerance;
        public int twistRepetitions;
        public bool isLiftActive;
        public double liftMinHeight;
        public double liftMaxHeight;
        public double liftTolerance;
        public int liftRepetitions;
        public bool isPushingActive;
        public double pushingMinDistance;
        public double pushingMaxDistance;
        public int pushingTolerance;
        public int pushingRepetitions;
    }

    class Game
    {
        private int windowSizeX;
        private int windowSizeY;
        private ExerciseVariant exerciseVariant;
        public Game(ExerciseVariant ev, int windowSizeX, int windowSizeY)
        {

        }
    }
}
