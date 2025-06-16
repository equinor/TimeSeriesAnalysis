using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Determines if all data has frozen for any samples
    /// </summary>
    static public class FrozenDataDetector
    {

        /// <summary>
        /// Look at all the time-series n the dataset, and tag thos sample indices where all values seem to be frozen compared to previous
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static List<int> DetectFrozenSamples(TimeSeriesDataSet dataSet)
        {
            var result = new List<int>();
            var signalNames = dataSet.GetSignalNames();
            for (int curIdx=1;curIdx<dataSet.GetNumDataPoints(); curIdx++)
            {
                bool doesAtLeastOneValueChange = false;
                int curSignalIdx = 0;

                while (!doesAtLeastOneValueChange && curSignalIdx < signalNames.Count())
                {
                    if (dataSet.GetValue(signalNames.ElementAt(curSignalIdx), curIdx) != dataSet.GetValue(signalNames.ElementAt(curSignalIdx), curIdx-1))
                    {
                        doesAtLeastOneValueChange = true;
                    }
                    curSignalIdx++;
                }
                if (!doesAtLeastOneValueChange)
                {
                    result.Add(curIdx);
                }
            }
            return result;
        }


        /// <summary>
        /// Look at all the time-series n the dataset, and tag thos sample indices where all values seem to be frozen compared to previous
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static List<int> DetectFrozenSamples(UnitDataSet dataSet)
        {

            var result = new List<int>();
            for (int curIdx = 1; curIdx < dataSet.GetNumDataPoints(); curIdx++)
            {
                bool doesAtLeastOneValueChange = false;
                if (dataSet.Y_meas[curIdx] != dataSet.Y_meas[curIdx-1] )
                {
                    doesAtLeastOneValueChange = true;
                }
                if (dataSet.Y_setpoint[curIdx] != dataSet.Y_setpoint[curIdx - 1])
                {
                    doesAtLeastOneValueChange = true;
                }
                if (dataSet.U.GetColumn(0)[curIdx] != dataSet.U.GetColumn(0)[curIdx-1])
                {
                    doesAtLeastOneValueChange = true;
                }
                if (!doesAtLeastOneValueChange)
                {
                    result.Add(curIdx);
                }
            }
            return result;
        }




    }
}
