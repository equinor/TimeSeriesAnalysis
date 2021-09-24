using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.SysId
{
    /// <summary>
    /// The data for a porition of a process, containg only one output and one or multiple inputs that influence it
    /// </summary>
    public class ProcessDataSet
    {
        public  string ProcessName { get;}
        public DateTime[] Times { get; }
        public double[] Y_meas { get; set; }
        public double[] Y_sim { get; set; }//TODO: add support for multiple y_sim

        public double[,] U { get;}

        public int TimeBase_s { get; set; }

        public DateTime t0;


        public ProcessDataSet(double[] y_meas, double[,] U, int timeBase_s, string name=null)
        {
            this.Y_meas = y_meas;
            this.U = U;
            this.TimeBase_s = timeBase_s;
            this.ProcessName = name;
        }

        /// <summary>
        /// Get the  number of data points in the dataset
        /// </summary>
        /// <returns>the number of data points</returns>
        public int GetNumDataPoints()
        {
            return U.GetNRows();
        }

        /// <summary>
        /// Get the time spanned by the dataset
        /// </summary>
        /// <returns>The time spanned by the dataset</returns>
        public TimeSpan GetTimeSpan()
        {
            if (Times == null)
            {
                return new TimeSpan(0, 0, (int)Math.Ceiling((double)GetNumDataPoints() * TimeBase_s));
            }
            else
            {
                return Times.Last() - Times.First();
            }
        }
        /// <summary>
        /// Get the average value of each input in the dataset. This is useful when defining model local around a working point.
        /// </summary>
        /// <returns>an array of averages, each corrsponding to one column of U. 
        /// Returns null if it was not possible to calculate averages</returns>
        public double[] GetAverageU()
        {
            List<double> averages = new List<double>();

            for (int i = 0; i < U.GetNColumns(); i++)
            {
                double? avg = Vec.Mean(U.GetColumn(i));
                if (!avg.HasValue)
                    return null;
                averages.Add(avg.Value);
            }
            return averages.ToArray();
        }


    }
}
