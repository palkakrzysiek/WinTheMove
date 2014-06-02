using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace WinTheMove.Boxing
{
    public enum Axis { X, Y, Z };

    class MathsHelper
    {
        public static double DistanceBetweenPoints(Joint j1, Joint j2, params Axis[] axes)
        {
            double shrhX = axes.Contains(Axis.X) ? j1.Position.X - j2.Position.X : 0;
            double shrhY = axes.Contains(Axis.Y) ? j1.Position.Y - j2.Position.Y : 0;
            double shrhZ = axes.Contains(Axis.Z) ? j1.Position.Z - j2.Position.Z : 0;
            return vectorNorm(shrhX, shrhY, shrhZ);
        }
        public static double AngleBetweenJoints(Joint j1, Joint j2, Joint j3, params Axis[] axes)
        {
            double Angulo = 0;
            double shrhX = axes.Contains(Axis.X) ? j1.Position.X - j2.Position.X : 0;
            double shrhY = axes.Contains(Axis.Y) ? j1.Position.Y - j2.Position.Y : 0;
            double shrhZ = axes.Contains(Axis.Z) ? j1.Position.Z - j2.Position.Z : 0;
            double hsl = vectorNorm(shrhX, shrhY, shrhZ);
            double unrhX = axes.Contains(Axis.X) ? j3.Position.X - j2.Position.X : 0;
            double unrhY = axes.Contains(Axis.Y) ? j3.Position.Y - j2.Position.Y : 0;
            double unrhZ = axes.Contains(Axis.Z) ? j3.Position.Z - j2.Position.Z : 0;
            double hul = vectorNorm(unrhX, unrhY, unrhZ);
            double mhshu = shrhX * unrhX + shrhY * unrhY + shrhZ * unrhZ;
            double x = mhshu / (hul * hsl);
            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    Angulo = angleRad * (180.0 / Math.PI);
                }
                else
                    Angulo = 0;


            }
            else
                Angulo = 0;


            return Angulo;

        }
        private static double vectorNorm(double x, double y, double z)
        {

            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));

        }
    }
}
