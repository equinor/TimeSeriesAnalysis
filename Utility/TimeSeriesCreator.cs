using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    public class TimeSeriesCreator
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


    }
}
