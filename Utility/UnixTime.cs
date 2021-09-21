using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    ///<summary>
    /// Utility class to work with unix time stamps
    ///</summary>

    public class UnixTime
    {
        public static double GetNowUnixTime()//does not work properly, off by one hour
        {
            return ConvertToUnixTimestamp(DateTime.UtcNow);
        }
        /// <summary>
        /// Converts a unix timestamp into a DateTime
        /// </summary>
        /// <param name="timestamp">the double time stamp to be converted</param>
        /// <returns>a converted DateTime object</returns>
        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }
        /// <summary>
        /// Converts a DateTime into a unix timestamp
        /// </summary>
        /// <param name="date"></param>
        /// <returns>A unix time stamp double</returns>
        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return diff.TotalSeconds;
        }
    }
}
