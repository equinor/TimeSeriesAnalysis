using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic.CommonDataPreprocessing
{
    /// <summary>
    /// This class deals with identifying when a dataset is oversampled. 
    /// Oversampling happens when the data is stored with a given timbase_s, and then retreived at a higher frequency, in which
    /// case many systems will return data at the requested timebese_s even though the data is not stored with sufficient fidelity,
    /// 
    /// When plotted, oversampled datasets look like "step-ladders".
    /// </summary>
    public class OversampledDataDetector
    {
        /// <summary>
        /// Get the oversampling factor of a UnitDataSet
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        static public (double, int) GetOversampledFactor(UnitDataSet dataSet)
        {
            var tsData = new TimeSeriesDataSet();

            tsData.Add("y_meas", dataSet.Y_meas);
            tsData.Add("y_set", dataSet.Y_setpoint);
            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                tsData.Add("U" + i, dataSet.U.GetColumn(i));
            }
            tsData.SetTimeStamps(dataSet.Times.ToList());
            return GetOversampledFactor(tsData);
        }

        /// <summary>
        /// Identify the oversampled factor of a TimeSeriesDataSet. The oversamples need to be evenly spread in the dataset.
        /// Subsets of oversamples in an otherwise non-oversampled dataset will not be taken into account here.
        /// </summary>
        /// <returns>the average distance between two unique values in the dataset arrays and a key index where the first new value occurs. 
        /// If the dataset is not oversampled, the method returns (1,0) </returns>
        static public (double,int) GetOversampledFactor(TimeSeriesDataSet dataSet)
        {
            int keyIndex=0;
            // Find the indices where all dataset arrays have the same value as the previous index
            var vec = new Vec();

            var signalNames = dataSet.GetSignalNames();

            List<int> indOversampled = vec.FindValues(dataSet.GetValues(signalNames.First()), -9999, VectorFindValueType.SameAsPrevious);
            for (int idx = 0; idx < signalNames.Length; idx++)
            {
                var curIndOversampled = vec.FindValues(dataSet.GetValues(signalNames.ElementAt(idx)), -9999, VectorFindValueType.SameAsPrevious);
                indOversampled = indOversampled.Intersect(curIndOversampled).ToList();
            }

            if (indOversampled.Count() < 2)
            {
                return (1,0);
            }

            // Find the initial oversampled factor
            int N = dataSet.GetNumDataPoints();
            double oversampledFactor = (double)N / (double)(N - indOversampled.Count());
            if (oversampledFactor <= 1)
            {
                return (1,0);
            }

            // Check if the floor of the oversampled factor equals the size of smallest group of consecutive oversampled signals.
            // Also find the best key index to use for downsampling.
            // Sort the oversampled index vector.
            indOversampled.Sort();

            // Initialize counters
            int consecutiveOversamples = 1;
            var consecutiveOversamplesList = new List<int>();
            int mostConsecutiveOversamples = 1;
            int largestIndexDiff = indOversampled[1] - indOversampled[0];
            int secondLargestIndexDiff = indOversampled[1] - indOversampled[0];
            int consecutiveLargeIndexDiffs = 1;
            int mostConsecutiveLargeIndexDiffs = 0;
            int consecutiveLargeIndexDiffsStartIndex = indOversampled[0] + 1;
            var consecutiveLargeIndexDiffsStartIndexList = new List<int>();
            int mostConsecutiveLargeIndexDiffsStartIndex = indOversampled[0] + 1;

            // Loop over the oversampled indices
            for (int idx = 1; idx < indOversampled.Count(); idx++)
            {
                int indexDiff = indOversampled[idx] - indOversampled[idx - 1];
                if (indexDiff == 1)
                {
                    consecutiveOversamples++;
                    if (consecutiveOversamples > mostConsecutiveOversamples)
                    {
                        mostConsecutiveOversamples = consecutiveOversamples;
                    }
                }
                else
                {
                    if (indexDiff > largestIndexDiff)
                    {
                        secondLargestIndexDiff = largestIndexDiff;
                        largestIndexDiff = indexDiff;
                        consecutiveLargeIndexDiffsStartIndex = indOversampled[idx - 1] + 1;
                        mostConsecutiveLargeIndexDiffsStartIndex = indOversampled[idx - 1] + 1;
                        consecutiveLargeIndexDiffsStartIndexList.Add(consecutiveLargeIndexDiffsStartIndex);
                        consecutiveLargeIndexDiffs = 1;
                        mostConsecutiveLargeIndexDiffs = 1;
                    }
                    else if (indexDiff == largestIndexDiff)
                    {
                        consecutiveLargeIndexDiffs++;
                        consecutiveLargeIndexDiffsStartIndexList.Add(consecutiveLargeIndexDiffsStartIndex);
                        if (consecutiveLargeIndexDiffs > mostConsecutiveLargeIndexDiffs)
                        {
                            mostConsecutiveLargeIndexDiffs = consecutiveLargeIndexDiffs;
                            mostConsecutiveLargeIndexDiffsStartIndex = consecutiveLargeIndexDiffsStartIndex;
                        }
                    }
                    else
                    {
                        if (indexDiff > secondLargestIndexDiff)
                        {
                            secondLargestIndexDiff = indexDiff;
                        }
                        consecutiveLargeIndexDiffsStartIndexList.Add(consecutiveLargeIndexDiffsStartIndex);
                        consecutiveLargeIndexDiffs = 0;
                        consecutiveLargeIndexDiffsStartIndex = indOversampled[idx] + 1;
                    }
                    consecutiveOversamplesList.Add(consecutiveOversamples);
                    consecutiveOversamples = 1;
                }
            }

            // If both flatlines and oversamples are present, disregard the flatline data, keeping in mind that flatlines can 'amputate' part of an oversampled group of points.
            /*int flatlinepoints = 0;
            int flatlines = 0;
            for (int i = 2; i < consecutiveOversamplesList.Count() - flatlines; i++)
            {
                if ((consecutiveOversamplesList[i] > consecutiveOversamplesList[i - 1] + 2) & (consecutiveOversamplesList[i] > consecutiveOversamplesList[i - 2] + 2))
                {
                    flatlinepoints += consecutiveOversamplesList[i];
                    flatlines++;
                    consecutiveOversamplesList.RemoveAt(i);
                    consecutiveLargeIndexDiffsStartIndexList.RemoveAt(i);
                }
            }
            if (consecutiveOversamplesList.Count() > 3)
            {
                if ((consecutiveOversamplesList[0] > consecutiveOversamplesList[1] + 2) & (consecutiveOversamplesList[0] > consecutiveOversamplesList[2] + 2))
                {
                    flatlinepoints += consecutiveOversamplesList[0];
                    flatlines++;
                    consecutiveOversamplesList.RemoveAt(0);
                    consecutiveLargeIndexDiffsStartIndexList.RemoveAt(0);
                    if (consecutiveOversamplesList[0] > consecutiveOversamplesList[1] + 2)
                    {
                        flatlinepoints += consecutiveOversamplesList[0];
                        flatlines++;
                        consecutiveOversamplesList.RemoveAt(0);
                        consecutiveLargeIndexDiffsStartIndexList.RemoveAt(0);
                    }
                }
                else if ((consecutiveOversamplesList[1] > consecutiveOversamplesList[0] + 2) & (consecutiveOversamplesList[1] > consecutiveOversamplesList[2] + 2))
                {
                    flatlinepoints += consecutiveOversamplesList[1];
                    flatlines++;
                    consecutiveOversamplesList.RemoveAt(1);
                    consecutiveLargeIndexDiffsStartIndexList.RemoveAt(1);
                }
            }

            // Revise the oversampled factor calculation if necessary
            if (flatlines > 0)
            {
                // Flatlines will cause some oversampled points to be mislabeled as flatlinepoints on each end.
                // These should be taken into account for the calculation. Half an oversample on each end of the flatline is an average value.
                oversampledFactor = (double)(N - flatlinepoints) / (double)(N - indOversampled.Count());
                oversampledFactor = (double)(N - (flatlinepoints - (flatlines * oversampledFactor))) / (double)(N - indOversampled.Count());
                if (oversampledFactor <= 1)
                {

                    return (1,0);
                }
                else if ((consecutiveOversamplesList.Max() <= (int)Math.Ceiling(oversampledFactor) + 1) & (consecutiveOversamplesList.Max() >= (int)Math.Floor(oversampledFactor) - 1))
                {
                    int numConsecutiveSmallOversamples = 0;
                    int mostConsecutiveSmallOversamples = 0;
                    int smallestOversample = (int)Math.Floor(oversampledFactor);
                    for (int i = 0; i < consecutiveOversamplesList.Count(); i++)
                    {
                        if (consecutiveOversamplesList[i] == smallestOversample)
                        {
                            numConsecutiveSmallOversamples++;
                            if (numConsecutiveSmallOversamples > mostConsecutiveSmallOversamples)
                            {
                                mostConsecutiveSmallOversamples = numConsecutiveSmallOversamples;
                                keyIndex = Math.Max(consecutiveLargeIndexDiffsStartIndexList[i - mostConsecutiveSmallOversamples + 1] - 1, 0);
                            }
                        }
                    }
                    return (oversampledFactor,keyIndex);
                }
                else
                {
                    return (1,0);
                }
            }*/

            // Return the oversampled factor, or 1 if no evenly spread oversampling is found.
            var oversampleEstimateV1 = mostConsecutiveOversamples + 1;
            var oversampleEstimateV2 = (int)Math.Ceiling(oversampledFactor);

            if ((oversampleEstimateV1 == oversampleEstimateV2) &
                (largestIndexDiff - secondLargestIndexDiff < 3))
            {
                keyIndex = mostConsecutiveLargeIndexDiffsStartIndex;
                return (oversampledFactor, keyIndex);
            }
            else
            {
                return (1,0);
            }
        }


        /// <summary>
        /// Returns a copy of the dataset that is downsampled by the given factor.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="downsampleFactor">value greater than 1 indicating that every Floor(n*factor) value of the orignal data will be transferred.</param>
        /// <param name="keyIndex">optional index around which to perform the downsampling.</param>
        /// <returns></returns>
        static public TimeSeriesDataSet CreateDownsampledCopy(TimeSeriesDataSet dataSet, double downsampleFactor, int keyIndex = 0)
        {
            var ret = new TimeSeriesDataSet();
            ret.SetTimeStamps( Vec<DateTime>.Downsample(dataSet.GetTimeStamps().ToArray(), downsampleFactor, keyIndex).ToList());
            foreach (var signalName in dataSet.GetSignalNames())
            {
                ret.Add(signalName, Vec<double>.Downsample(dataSet.GetValues(signalName), downsampleFactor, keyIndex));
            }
            return ret;
        }

        /// <summary>
        /// Returns a copy of the dataset that is oversampled by the given factor.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="oversampleFactor">value greater than 1 indicating that every value will be sampled until (i / factor > 1).</param>
        /// <param name="keyIndex">optional index around which to perform the oversampling.</param>
        /// <returns> a downsampled copy, or null if operation failed. (Method will fail if no timestamps are given.)</returns>
        static public TimeSeriesDataSet CreateOversampledCopy(TimeSeriesDataSet dataSet, double oversampleFactor, int keyIndex = 0)
        {
            var ret = new TimeSeriesDataSet();

            if (dataSet.GetTimeStamps().Length == 0)
            {
                return null;
            }
            ret.CreateTimestamps(timeBase_s: dataSet.GetTimeBase() / oversampleFactor, 
                N: (int)Math.Ceiling(dataSet.GetNumDataPoints() * oversampleFactor), 
                t0: dataSet.GetTimeStamps()[0]);
            foreach (var signalName in dataSet.GetSignalNames())
            {
                ret.Add(signalName, Vec<double>.Oversample(dataSet.GetValues(signalName), oversampleFactor, keyIndex)) ;
            }
            return ret;
        }


    }
}
