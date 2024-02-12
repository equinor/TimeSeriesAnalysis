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
        /// Creates a vector of a constant value
        /// </summary>
        /// <param name="value">constant value of time-series</param>
        /// <param name="N">number of data points of time-series</param>
        /// <returns></returns>
        public static double[] Constant(double value, int N)
        {
            //(wrapper for Vec.Fill, added for code readability)
            return Vec<double>.Fill(value, N);
        }

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
                curTime = curTime.AddSeconds(dT_s);
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
        /// Create a white-noise random time-series
        /// </summary>
        /// <param name="N"></param>
        /// <param name="noiseAmplitude"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        static public double[] Noise(int N, double noiseAmplitude, int? seed=null)
        {
            return Vec.Rand(N, -noiseAmplitude, noiseAmplitude, null);
        }

        /// <summary>
        /// Creates a random-walk time-series
        /// </summary>
        /// <param name="N"></param>
        /// <param name="stepAmplitude"></param>
        /// <param name="startval"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        static public double[] RandomWalk(int N, double stepAmplitude, double startval=0, int? seed = null)
        {
            var steps = Vec.Rand(N, -stepAmplitude, stepAmplitude, seed);

            double[] ret = new double[N];
            ret[0] = startval;
            for (int i= 1;i<N;i++)
            {
                ret[i] = ret[i - 1] + steps[i];
            }
            return ret;
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
        /// Create a time-series with two step changes
        /// </summary>
        /// <param name="step1StartIdx">index of first step change</param>
        /// <param name="step2StartIdx">index of second step change</param>
        /// <param name="N"></param>
        /// <param name="val1">value before steps</param>
        /// <param name="val2">value of first step</param>
        /// <param name="val3">value of second step</param>
        /// <returns></returns>
        static public double[] TwoSteps(int step1StartIdx, int step2StartIdx, int N, double val1, double val2, double val3)
        {
            if (step1StartIdx > N || step2StartIdx>N)
                return null;
            int N1 = step1StartIdx + 1;
            return Vec<double>.Concat(Step(step1StartIdx, step2StartIdx-1,val1,val2),
                Vec<double>.Fill(val3, N-step2StartIdx+1));
        }

        /// <summary>
        /// Create a time-series with three step changes
        /// </summary>
        /// <param name="step1StartIdx"></param>
        /// <param name="step2StartIdx"></param>
        /// <param name="step3StartIdx"></param>
        /// <param name="N"></param>
        /// <param name="val1"></param>
        /// <param name="val2"></param>
        /// <param name="val3"></param>
        /// <param name="val4"></param>
        /// <returns></returns>
        static public double[] ThreeSteps(int step1StartIdx, int step2StartIdx, int step3StartIdx, 
            int N, double val1, double val2, double val3, double val4)
        {
            if (step1StartIdx > N || step2StartIdx > N)
                return null;
            int N1 = step1StartIdx + 1;
            return Vec<double>.Concat(TwoSteps(step1StartIdx, step2StartIdx, step3StartIdx - 1, val1, val2,val3),
                Vec<double>.Fill(val4, N - step3StartIdx + 1));
        }



    }
}
