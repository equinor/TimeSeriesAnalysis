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
        static public DateTime[] CreateTimeSeries(DateTime t0, int dT_s, int N)
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

    }
}
