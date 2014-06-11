using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinTheMove.Boxing
{
    public enum ExerciseVariant { HandLeft, HandRight };
    public enum ExerciseType { Twist, Lift, Push };
    public struct ExerciseSettings {
        bool isTwistActive;
        double twistMinAngle;
        double twistMaxAngle;
    }

    class Game
    {
        private int windowSizeX;
        private int windowSizeY;
        private ExerciseVariant exerciseVariant;
        public static ExerciseType GetCurrentExercise()
        {
            return ExerciseType.Twist;
        }
        public Game(ExerciseVariant ev, int windowSizeX, int windowSizeY)
        {

        }
    }
}
