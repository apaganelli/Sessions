using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Kinect;

namespace Sessions
{
    class Util
    {
        ///<summary>
        /// Author: Antonio Iyda Paganelli
        ///
        /// Calculates the length of a 3D point. 
        /// </summary>
        /// <param name="point"></param>
        /// <returns>A 3D point</returns>
        public static double Length(CameraSpacePoint point)
        {
            return Math.Sqrt(point.X * point.X + point.Y * point.Y + point.Z + point.Z);
        }

        /// <summary>
        /// Input: z-value (-inf to +inf)
        /// Output:  p under Standard Normal curve from -inf to z
        /// e.g., if z = 0.0, function returns 0.5000
        /// ACM Algorithm #209
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static double Gauss(double z)
        {
            double y; // 209 scratch variable
            double p; // result. called 'z' in 209
            double w; // 209 scratch variable
            if (z == 0.0)
                p = 0.0;
            else
            {
                y = Math.Abs(z) / 2;
                if (y >= 3.0)
                {
                    p = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    p = ((((((((0.000124818987 * w
                      - 0.001075204047) * w + 0.005198775019) * w
                      - 0.019198292004) * w + 0.059054035642) * w
                      - 0.151968751364) * w + 0.319152932694) * w
                      - 0.531923007300) * w + 0.797884560593) * y * 2.0;
                }
                else
                {
                    y = y - 2.0;
                    p = (((((((((((((-0.000045255659 * y
                      + 0.000152529290) * y - 0.000019538132) * y
                      - 0.000676904986) * y + 0.001390604284) * y
                      - 0.000794620820) * y - 0.002034254874) * y
                      + 0.006549791214) * y - 0.010557625006) * y
                      + 0.011630447319) * y - 0.009279453341) * y
                      + 0.005353579108) * y - 0.002141268741) * y
                      + 0.000535310849) * y + 0.999936657524;
                }
            }
            if (z > 0.0)
                return (p + 1.0) / 2;
            else
                return (1.0 - p) / 2;
        }


        /// <summary>
        /// For large integer df or double df adapted from ACM algorithm #395
        /// Returns 2-tail p-value.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="df">number of elements</param>
        /// <returns></returns>
        public static double Student(double t, double df)
        {
            double n = df; // to sync with ACM parameter name
            double a, b, y;
            t = t * t;
            y = t / n;
            b = y + 1.0;
            if (y > 1.0E-6) y = Math.Log(b);
            a = n - 0.5;
            b = 48.0 * a * a;
            y = a * y;
            y = (((((-0.4 * y - 3.3) * y - 24.0) * y - 85.5) /
              (0.8 * y * y + 100.0 + b) + y + 3.0) / b + 1.0) *
              Math.Sqrt(y);
            return 2.0 * Gauss(-y); // ACM algorithm 209
        }

        /// <summary>
        /// T-test student (Welch)
        /// </summary>
        /// <param name="x">First set of doubles to be compared</param>
        /// <param name="y">Second set of doubles to be compared</param>
        public static void TTest(double[] x, double[] y, out double meanX, out double meanY, out double t, out double df, out double p)
        {
            double sumX = 0.0;
            double sumY = 0.0;

            // Sum up all elements values of each group (x and y)
            for (int i = 0; i < x.Length; ++i)
                sumX += x[i];

            for (int i = 0; i < y.Length; ++i)
                sumY += y[i];

            // Calculate the mean of each group.
            int n1 = x.Length;
            int n2 = y.Length;

            meanX = sumX / n1;
            meanY = sumY / n2;

            // Calculate variances of the means.
            double sumXminusMeanSquared = 0.0;
            double sumYminusMeanSquared = 0.0;

            for (int i = 0; i < n1; ++i)
                sumXminusMeanSquared += (x[i] - meanX) * (x[i] - meanX);

            for (int i = 0; i < n2; ++i)
                sumYminusMeanSquared += (y[i] - meanY) * (y[i] - meanY);

            double varX = sumXminusMeanSquared / (n1 - 1);
            double varY = sumYminusMeanSquared / (n2 - 1);

            // Calculates t value.
            // t is the difference of the means divided by the square root of the sum of the variations
            // divided by samples sizes.
            double top = (meanX - meanY);
            double bot = Math.Sqrt((varX / n1) + (varY / n2));
            t = top / bot;

            // Calculates the degrees of freedom.
            double num = ((varX / n1) + (varY / n2)) * ((varX / n1) + (varY / n2));
            double denomLeft = ((varX / n1) * (varX / n1)) / (n1 - 1);
            double denomRight = ((varY / n2) * (varY / n2)) / (n2 - 1);
            double denom = denomLeft + denomRight;
            df = num / denom;

            // Calculates the p value.
            p = Student(t, df); // Cumulative two-tail density

            // Console.WriteLine("mean of x = " + meanX.ToString("F2"));
            // Console.WriteLine("mean of y = " + meanY.ToString("F2"));
            // Console.WriteLine("t = " + t.ToString("F4"));
            // Console.WriteLine("df = " + df.ToString("F3"));
            // Console.WriteLine("p-value = " + p.ToString("F5"));
        }
    }
}
