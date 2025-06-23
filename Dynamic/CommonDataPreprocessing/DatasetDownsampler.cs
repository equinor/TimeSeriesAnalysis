using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic.CommonDataPreprocessing
{
    /// <summary>
    /// Class dealing with downsampling oversampled datasets.
    /// </summary>
    public class DatasetDownsampler
    {
        /// <summary>
        /// If the dataset is over-sampled, then this method extracts only those data points where data
        /// is changed compared to previous value. 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns>a tuple consisting of a bool indicating if a copy was made, and secondly the downsampled dataset</returns>
        public static (bool,TimeSeriesDataSet) CreateDownsampledCopyIfPossible(TimeSeriesDataSet rawData)
        {
            (var listFrozenSampleIdx, var avgSamplesBtwGoodIdx, var minSamplesBtwGoodIdx) = 
                FrozenDataDetector.DetectFrozenSamples(rawData);

            // if the above list is "periodic"
            if (avgSamplesBtwGoodIdx >= 1 && minSamplesBtwGoodIdx >= 1)
            {
                // then create a downsampled copy of the original dataset.
                return (true, new TimeSeriesDataSet(rawData, listFrozenSampleIdx));
            }

            return (false,rawData);
        }

    }
}
