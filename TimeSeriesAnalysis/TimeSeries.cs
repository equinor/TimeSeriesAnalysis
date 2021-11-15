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
        /// <returns></returns>
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


    }
}
