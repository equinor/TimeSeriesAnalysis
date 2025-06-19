using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic.CommonDataPreprocessing
{
    public class DatasetDownsampler
    {
        /// <summary>
        /// If the dataset is over-sampled, then this method extracts only those data points where data
        /// is changed compared to previous value. 
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public static TimeSeriesDataSet CreateDownsampledCopyIfPossible(TimeSeriesDataSet rawData)
        {
            (var listFrozenSampleIdx, var avgSamplesBtwGoodIdx, var minSamplesBtwGoodIdx) = FrozenDataDetector.DetectFrozenSamples(rawData);

            // if the above list is "periodic"
            if (avgSamplesBtwGoodIdx >= 1 && minSamplesBtwGoodIdx >= 1)
            {
                // then create a downsampled copy of the original dataset.
                return new TimeSeriesDataSet(rawData, listFrozenSampleIdx);
            }

            return rawData;
        }

    }
}
