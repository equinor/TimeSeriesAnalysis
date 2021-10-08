using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    /// <summary>
    /// Class for static methods create different types of time-series for testing.
    /// </summary>
    static public class TimeSeriesCreator
    {
        /// <summary>
        /// Create an array of DateTimes starting at <c>t0</c> of length N and with sampling interval <c>dT_s</c>
        /// </summary>
        /// <param name="t0">first datetime in the array to be created</param>
        /// <param name="dT_s">sampling internval</param>
        /// <param name="N">number of desired data points</param>
        /// <returns></returns>
        static public DateTime[] CreateDateStampArray(DateTime t0, int dT_s, int N)
        {
            List<DateTime> times = new List<DateTime>();
            DateTime curTime = t0;
            for (int i = 0; i < N; i++)
            {
                times.Add(curTime);
                curTime.AddSeconds(dT_s);
            }
            return times.ToArray();
        }

        /// <summary>
        /// Create an array representing a sinus
        /// </summary>
        /// <param name="amplitude">amplitud of sinus</param>
        /// <param name="sinusPeriod_s">time for a complete 360 degree period of the sinus in seconds</param>
        /// <param name="dT_s">the timebase</param>
        /// <param name="N">number of desired data point in return array</param>
        /// <returns>an array continaing the specified sinus</returns>
        static public double[] Sinus(double amplitude, double sinusPeriod_s, int dT_s, int N)
        {
            List<double> list = new List<double>();

            for (int i = 0; i < N; i++)
            {
                double newVal = amplitude *
                    Math.Sin((i * dT_s) / sinusPeriod_s * Math.PI * 2);
                list.Add(newVal);
            }
            return list.ToArray();


        }






        /// <summary>
        /// Create a step change vector of a given length <c>N</c> starting at value 
        /// <c>val1</c> and ending at <c>val2</c>, step occuring at index <c>stepStartIdx</c>
        /// </summary>
        /// <param name="stepStartIdx">index of step </param>
        /// <param name="N">total time series length</param>
        /// <param name="val1">value before step</param>
        /// <param name="val2">value after step</param>
        /// <returns>created vector, or <c>null</c> if inputs make no sense</returns>
        static public double[] Step(int stepStartIdx, int N, double val1, double val2)
        {
            if (stepStartIdx > N) 
                return null;
            int N1 = stepStartIdx+1;
            int N2 = N-N1;
            return Vec<double>.Concat(Vec<double>.Fill(val1, N1),
                Vec<double>.Fill(val2, N2));
        }

        /// <summary>
        /// Creates a vector of a constant value
        /// </summary>
        /// <param name="value">constant value of timne-series</param>
        /// <param name="N">number of data points of time-series</param>
        /// <returns></returns>
        internal static double[] Constant(int value, int N)
        {
            //(wrapper for Vec.Fill, added for code readability)
            return Vec<double>.Fill(value,N);
        }
    }
}
