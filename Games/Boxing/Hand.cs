using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Controls;

namespace WinTheMove.Boxing
{
    class Hand
    {
        Joint handJoint;
        private static Hand instance = new Hand();
        public Hand GetHand()
        {
            return instance;
        }
        private Hand() { }

        /*
        public Hand(Skeleton skeleton,
            ExerciseVariant exerciseVariant,
            Canvas gameField)
        {

        }
        */
    }
}
