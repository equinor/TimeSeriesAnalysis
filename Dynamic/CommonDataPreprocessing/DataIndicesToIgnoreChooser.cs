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
        /// <summary>
        /// Looks over UnitDataSet and chooses which indices to ignore
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="detectFrozenData"> if set to true, then any time sample where all inputs are empty will be removed</param>
        /// <returns></returns>
        public static List<int> ChooseIndicesToIgnore(UnitDataSet dataSet, bool detectFrozenData = false)
        {
            var tsData = new TimeSeriesDataSet();

            tsData.Add("y_meas",dataSet.Y_meas);
            tsData.Add("y_set", dataSet.Y_setpoint);
            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                tsData.Add("U"+i, dataSet.U.GetColumn(i));
            }
            tsData.SetTimeStamps(dataSet.Times.ToList());
            return ChooseIndicesToIgnore(tsData, detectFrozenData);
        }

        /// <summary>
        /// Looks over dataset and chooses indices to ignore. 
        /// For best results, only include those time-series that are needed for simulation, remove unused time-series from this dataset.
        /// </summary>
        /// <param name="dataSet">dataset to be investigated</param>
        ///  <param name="detectFrozenData">if set to true, all indices where none of the data changes are considered "frozen"(only use when dataset includes measoured outputs y with noise)</param>
        /// <returns></returns>
        public static List<int> ChooseIndicesToIgnore(TimeSeriesDataSet dataSet, bool detectFrozenData=false)
        {
            var badDataIdx = new List<int>();
            foreach (var signalID in dataSet.GetSignalNames())
            {
                var signalValues = dataSet.GetValues(signalID);
                badDataIdx.Union(BadDataFinder.GetAllBadIndicesPlussNext(signalValues, dataSet.BadDataID));
            }

            if (detectFrozenData)
            {
                var frozenIdx = FrozenDataDetector.DetectFrozenSamples(dataSet);
                return badDataIdx.Union(frozenIdx).ToList();
            }
            else
            {
                return badDataIdx;
            }
        }


    }
}
