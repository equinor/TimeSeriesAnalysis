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
    public static class CommonDataPreprocessor
    {
        /// <summary>
        /// Looks over UnitDataSet and chooses which indices to ignore
        /// 
        /// - Tags indices where any of the timeseries in dataSet have value badDataID or is NaN 
        /// - if detectFrozenData=true, it will also consider the dataset "frozen" if all tags in dataset retain the exact same value from one data point to the next
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="detectBadData"> if set to true, then any time where any input data equals badDataId or NaN is removd/param>
        /// <param name="detectFrozenData"> if set to true, then any time sample where all inputs are empty will be removed</param>
        /// <returns></returns>
        public static List<int> ChooseIndicesToIgnore(UnitDataSet dataSet, bool detectBadData =true, bool detectFrozenData = false)
        {

            var tsData = CreateTimeSeriesDataSetFromUnitDataSet(dataSet);


            return ChooseIndicesToIgnore(tsData, detectBadData, detectFrozenData);
        }

        /// <summary>
        /// Looks over dataset and chooses indices to ignore. 
        /// For best results, only include those time-series that are needed for simulation, remove unused time-series from this dataset.
        /// </summary>
        /// <param name="dataSet">dataset to be investigated(if this dataset has IndicesToIgnore set, then they are included in the returned lst)</param>
        /// <param name="detectBadData"> if set to true, then any time where any input data equals badDataId or NaN is removd/param>
        /// <param name="detectFrozenData">if set to true, all indices where none of the data changes are considered "frozen"(only use when dataset includes measoured outputs y with noise)</param>
        /// <returns>a sorted list of indicest to ignore</returns>
        public static List<int> ChooseIndicesToIgnore(TimeSeriesDataSet dataSet, bool detectBadData = true, bool detectFrozenData=false)
        {
            var indicesToIgnore = new List<int>();
            if (dataSet.GetIndicesToIgnore().Count() > 0)
            {
                // note that for identification often the trailing indices need also to be removed due to the nature of 
                // reursive models. We can never be certain if this sort of "padded out" indices to ignore is provided or not.
                var indicesMinusOne = Index.Max(Index.Subtract(dataSet.GetIndicesToIgnore().ToArray(), 1), 0).Distinct<int>();
                indicesToIgnore = Index.AppendTrailingIndices(indicesMinusOne.ToList());
            }
            if (detectBadData)
            {
                foreach (var signalID in dataSet.GetSignalNames())
                {
                    var signalValues = dataSet.GetValues(signalID);
                    var signalBadValuesIdx = BadDataFinder.GetAllBadIndicesPlussNext(signalValues, dataSet.BadDataID);
                    indicesToIgnore = indicesToIgnore.Union(signalBadValuesIdx).ToList();
                }
            }
            if (detectFrozenData)
            {
                (var frozenIdx, var avgSampleBtwGoodIdx, var minSampleBtwGoodIdx) =
                    FrozenDataDetector.DetectFrozenSamples(dataSet);
                indicesToIgnore = indicesToIgnore.Union(frozenIdx).ToList();
            }
            
            indicesToIgnore.Sort();
            return indicesToIgnore; 
        }

        /// <summary>
        /// Utility function to create a TimeSeriesDataSet from a UnitDataSet
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static TimeSeriesDataSet CreateTimeSeriesDataSetFromUnitDataSet(UnitDataSet dataSet)
        {
            var tsData = new TimeSeriesDataSet();
            tsData.BadDataID = dataSet.BadDataID;
            tsData.SetIndicesToIgnore(dataSet.IndicesToIgnore);
            tsData.Add("y_meas", dataSet.Y_meas);
            tsData.Add("y_set", dataSet.Y_setpoint);
            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                tsData.Add("U" + i, dataSet.U.GetColumn(i));
            }
            tsData.SetTimeStamps(dataSet.Times.ToList());
            return tsData;
        }

        /// <summary>
        /// To turn a TimeSeriesDataSet created with sister-method <c>CreateTimeSeriesDataSetFromUnitDataSet</c> back into 
        /// a UnitDataSet
        /// </summary>
        /// <param name="tsDataSet">a TimeSeriesDataSet crated with <c>CreateTimeSeriesDataSetFromUnitDataSet</c></param>
        /// <returns></returns>
        public static UnitDataSet CreateUnitDataSetFromTimeSeriesData(TimeSeriesDataSet tsDataSet)
        {
            var unitData = new UnitDataSet();
            unitData.BadDataID = tsDataSet.BadDataID;
            unitData.IndicesToIgnore = tsDataSet.GetIndicesToIgnore();
            unitData.Y_meas = tsDataSet.GetValues("y_meas");
            unitData.Y_setpoint = tsDataSet.GetValues("y_set");
            int curInputIdx = 0;
            var uList = new List<double[]>();
            while (tsDataSet.ContainsSignal("U" + curInputIdx))
            {
                uList.Add(tsDataSet.GetValues("U"+ curInputIdx));
                curInputIdx++;
            }
            unitData.U = Array2D<double>.CreateFromList(uList);
            unitData.Times = tsDataSet.GetTimeStamps();
            return unitData;
        }



    }
}
