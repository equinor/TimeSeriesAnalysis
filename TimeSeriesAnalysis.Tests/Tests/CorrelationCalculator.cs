using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using NUnit.Framework;

namespace TimeSeriesAnalysis.Test
{
    [TestFixture]
    class CorrelationCalculatorTests
    {

        [TestCase]
        public void CorrelateToOppsite()
        {
            int N = 10;
            var dataset = new TimeSeriesDataSet();
            var amplitude = 10;
            var mainSignal = TimeSeriesCreator.Step(N / 2, N, 0, amplitude);
            var oppositeSignal = TimeSeriesCreator.Step(N / 2, N, amplitude, 0);
            dataset.Add("mainSignal", mainSignal);
            dataset.Add("oppsiteSignal", oppositeSignal);
            var results = CorrelationCalculator.Calculate("mainSignal", dataset);
            Assert.AreEqual(-1, results["oppsiteSignal"]);
        }

        [TestCase]
        public void CorrelateToSelf()
        {
            int N = 10;
            var dataset = new TimeSeriesDataSet();
            var amplitude = 10;
            var mainSignal = TimeSeriesCreator.Step(N / 2, N, 0, amplitude);
            var otherSignal = mainSignal;
            dataset.Add("mainSignal", mainSignal);
            dataset.Add("oppsiteSignal", otherSignal);
            var results = CorrelationCalculator.Calculate("mainSignal", dataset);
            Assert.AreEqual(1, results["oppsiteSignal"]);
        }

        [TestCase]
        public void CorrelateToZero()
        {
            int N = 10;
            var dataset = new TimeSeriesDataSet();
            var amplitude = 10;
            var mainSignal = TimeSeriesCreator.Step(N / 2, N, 0, amplitude);
            var otherSignal = TimeSeriesCreator.Constant(0, N);
            dataset.Add("mainSignal", mainSignal);
            dataset.Add("oppsiteSignal", otherSignal);
            var results = CorrelationCalculator.Calculate("mainSignal", dataset);
            Assert.AreEqual(0, results["oppsiteSignal"]);
        }

        [TestCase()]
        public void EstimateTimeConstants_GivesCorrectTimeConstants()
        {
            int N = 100;
            double timeBase_s = 1;
            int delaySamples = 10;
            var dataset = new TimeSeriesDataSet();
            var amplitude = 10;
            var mainSignal  = TimeSeriesCreator.Step(N/2, N, 0, amplitude);
            var lateSignal  = TimeSeriesCreator.Step(N/2+delaySamples, N, 0, amplitude*2);
            var earlySignal = TimeSeriesCreator.Step(N/2-delaySamples, N, 0, amplitude/2);
            dataset.Add("mainSignal", mainSignal);
            dataset.Add("lateSignal", lateSignal);
            dataset.Add("earlySignal", earlySignal);
            dataset.CreateTimestamps(timeBase_s);
            var results = CorrelationCalculator.CalculateAndOrder("mainSignal", dataset);

            Assert.AreEqual(delaySamples,results[1].timeDelay_s);
            Assert.AreEqual(0, results[1].timeConstant_s);
            Assert.AreEqual(null,results[2].timeDelay_s,"dont bother calculating negative time-delays or time constants" );


        }


        [TestCase(true )]
        [TestCase(false)]
        public void CorrelateAndOrder(bool orderedInput)
        {
            int N = 100;
            var dataset = new TimeSeriesDataSet();
            var amplitude = 10;
            var mainSignal = TimeSeriesCreator.Step(N / 2, N, 0, amplitude);
            var zeroSignal = TimeSeriesCreator.Constant(0, N);
            var oppsiteSignal = TimeSeriesCreator.Step(N *7/15, N, amplitude, 0);  // "almost" correlated
            if (orderedInput)
            {
                dataset.Add("mainSignal", mainSignal);
                dataset.Add("zeroSignal", zeroSignal);
                dataset.Add("oppsiteSignal", oppsiteSignal);
            }
            else
            {
                dataset.Add("oppsiteSignal", oppsiteSignal);
                dataset.Add("mainSignal", mainSignal);
                dataset.Add("zeroSignal", zeroSignal);
            }
            var results = CorrelationCalculator.CalculateAndOrder("mainSignal", dataset);
            Assert.AreEqual("mainSignal", results[0].signalName);
            Assert.AreEqual(1, results[0].correlationFactor);
            Assert.AreEqual("oppsiteSignal", results[1].signalName);
            Assert.IsTrue(results[1].correlationFactor<-0.9);
            Assert.AreEqual("zeroSignal", results[2].signalName);
            Assert.AreEqual(0, results[2].correlationFactor);
        }

    }
}
