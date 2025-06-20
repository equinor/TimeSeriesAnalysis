using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis;

using TimeSeriesAnalysis.Dynamic;
using NUnit.Framework;
using TimeSeriesAnalysis.Dynamic.CommonDataPreprocessing;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.Fundamentals
{
    [TestFixture]
    internal class DataPreprocessingTests
    {
       /* [TestCase(1,10)]
        [TestCase(5,10)]

        public void OversampledDataDetector_NotOversampledData_NoOversamplingDetected(int nSignals,int N)
        {
            var dataSet = new TimeSeriesDataSet();
            for (int i = 0; i < nSignals; i++)
            {
                dataSet.Add("TEST"+i, TimeSeriesCreator.Noise(N, 1000));
            }
            (var distance, var key) = OversampledDataDetector.GetOversampledFactor(dataSet);

            Assert.AreEqual(distance, 1);
            Assert.AreEqual(key, 0);
        }*/

        [TestCase(3,0)]
        [TestCase(3,1)]

        public void OversampledDataDetector_TwoSignalsOversampled_IsDetected(double oversampleFactor, int keyIndex)
        {
      /*      int N = 10;
            double timeBase_s = 5;
            var dataSetOrig = new TimeSeriesDataSet();

            for (int i = 0; i < 2; i++)
            {
                dataSetOrig.Add("TEST" + i, TimeSeriesCreator.Noise(N, 1000));
            }
            dataSetOrig.CreateTimestamps(timeBase_s,N);
            var dataSet = OversampledDataDetector.CreateOversampledCopy(dataSetOrig, oversampleFactor, keyIndex);

            var frozenIdx =  FrozenDataDetector.DetectFrozenSamples(dataSet);
      */
     //       (var estFactor,var estKeyIndex) = OversampledDataDetector.GetOversampledFactor(dataSet);

//            Assert.AreEqual(oversampleFactor,estFactor,"oversample factor estimated incorrectly");
 //           Assert.AreEqual(keyIndex, estKeyIndex, "key index estimated incorrectly");
        }





    }
}
