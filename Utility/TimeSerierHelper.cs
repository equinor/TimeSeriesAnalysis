using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    public class TimeSerierHelper
    {
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
