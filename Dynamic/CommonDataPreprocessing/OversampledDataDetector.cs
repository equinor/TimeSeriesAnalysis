using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic.CommonDataPreprocessing
{
    /// <summary>
    /// (Deprecated, to be removed, do not use.)
    /// This class deals with identifying when a dataset is oversampled. 
    /// Oversampling happens when the data is stored with a given timbase_s, and then retreived at a higher frequency, in which
    /// case many systems will return data at the requested timebese_s even though the data is not stored with sufficient fidelity,
    /// 
    /// When plotted, oversampled datasets look like "step-ladders".
    /// </summary>
    public class OversampledDataDetector
    {

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



    }
}
