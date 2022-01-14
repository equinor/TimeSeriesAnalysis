using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{

    /// <summary>
    /// Treating time series as tuples of corrsponding dates/values
    /// </summary>
    public class TimeSeries
    {


        /// <summary>
        /// Creates a new timeseries that is var1 and var2 concatenated together
        /// </summary>
        /// <param name="var1"></param>
        /// <param name="var2"></param>
        /// <returns></returns>
        public static (double[], DateTime[]) Concat((double[], DateTime[]) var1, (double[], DateTime[]) var2)
        {
            List<double> values1 = new List<double>(var1.Item1);
            List<DateTime> dates1 = new List<DateTime>(var1.Item2);

            List<double> values2 = new List<double>(var2.Item1);
            List<DateTime> dates2 = new List<DateTime>(var2.Item2);

            values1.AddRange(values2);
            dates1.AddRange(dates2);

            return (values1.ToArray(), dates1.ToArray());
        }

        /// <summary>
        /// Gt the index of the time-series that is closest to a given date
        /// </summary>
        /// <param name="input">time-series tuple</param>
        /// <param name="date">the </param>
        /// <returns></returns>
        public static int GetClosestIndexToDate((double[], DateTime[]) input, DateTime date)
        {
            if (date < input.Item2.First())
            {
                return 0;
            }

            // go-backwards from end
            bool done = false;
            int index = input.Item2.Length - 2;
            while (!done && index > 0)
            {
                if (/*input.Item2[index + 1] > time &&*/ input.Item2[index] <= date)
                {
                    done = true;
                }
                index--;
            }
            return index;
        }


        /// <summary>
        /// Get the gradient of a time-series
        /// </summary>
        /// <param name="valueDateTuple">a value/datetime array tuple</param>
        /// <param name="sampleTime_sec">sample time in which to present the result</param>
        /// <param name="indicesToIgnore">indices to ignore</param>
        /// <returns>The gradient is the gain of the returned object</returns>
        public static RegressionResults GetGradient((double[], DateTime[]) valueDateTuple, int sampleTime_sec = 1, int[] indicesToIgnore = null)
        {
            return Vec.GetGradient(valueDateTuple.Item1, valueDateTuple.Item2, sampleTime_sec, indicesToIgnore);
        }

        /// <summary>
        /// Clip out a subset of the given time-series with a given number of days. 
        /// <para>
        /// By default it gives the last N days, but optionally 
        /// </para>
        /// </summary>
        /// <param name="input">the original time-series to be clipped</param>
        /// <param name="nSpanDays">the desired length of the returend dataset</param>
        /// <param name="nDaysBack">the number of days to push the end of the returned dataset back(default is zero, in which case method gets last nSpanDays)</param>
        /// <returns></returns>
        public static (double[], DateTime[]) GetSubsetOfDays((double[], DateTime[]) input, int nSpanDays, int nDaysBack = 0)
        {
            var curTime = DateTime.Now;

            var endTime = curTime.Subtract(new TimeSpan(nDaysBack, 0, 0, 0));
            var startTime = endTime.Subtract(new TimeSpan(nSpanDays, 0, 0, 0));

            int startInd = GetClosestIndexToDate(input, startTime);
            int endInd = GetClosestIndexToDate(input, endTime);

            return SubSet(input, startInd, endInd);
        }



        /// <summary>
        /// Reverses the order of the time-series
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static (double[], DateTime[]) Reverse((double[], DateTime[]) input)
        {
            return (input.Item1.Reverse<double>().ToArray(), input.Item2.Reverse<DateTime>().ToArray());
        }






        /// <summary>
        /// Get a subset of a given value/datetime tuple, given start and end indices
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startInd"></param>
        /// <param name="endInd"></param>
        /// <returns></returns>
        public static (double[], DateTime[]) SubSet((double[], DateTime[]) input, int startInd, int endInd)
        {
            var values = Vec<double>.SubArray(input.Item1, startInd, endInd);
            var dates = Vec<DateTime>.SubArray(input.Item2, startInd, endInd);

            return (values, dates);
        }


        /// <summary>
        /// Get a subset starting at a specific date
        /// </summary>
        /// <param name="input"></param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public static (double[], DateTime[]) SubSet((double[], DateTime[]) input, DateTime startDate)
        {
            int endInd = input.Item1.Length-1;

            int startInd = GetClosestIndexToDate(input, startDate);

            return SubSet(input,startInd,endInd);
        }



    }
}
