using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Common logic that is to be shared among PlantSimulator and different Identification algoirthms, to choose
    /// data points that are to be ignored when either simulating or identifying or both.
    /// </summary>
    public static class DataIndicesToIgnoreChooser
    {
        public static List<int> ChooseIndicesToIgnore(UnitDataSet dataSet)
        {
            var tsData = new TimeSeriesDataSet();

            tsData.Add("y_meas",dataSet.Y_meas);
            tsData.Add("y_set", dataSet.Y_setpoint);
            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                tsData.Add("U"+i, dataSet.U.GetColumn(i));
            }
            tsData.SetTimeStamps(dataSet.Times.ToList());
            return ChooseIndicesToIgnore(tsData);
        }

        public static List<int> ChooseIndicesToIgnore(TimeSeriesDataSet dataSet)
        {
            var frozenIdx = FrozenDataDetector.DetectFrozenSamples(dataSet);

            return frozenIdx;
        }


    }
}
