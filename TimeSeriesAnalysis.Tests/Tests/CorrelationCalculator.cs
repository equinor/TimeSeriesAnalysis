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
            double timeBase_s = 1;

            int N = 10;

            var dataset = new TimeSeriesDataSet();

            var amplitude = 10;

            var mainSignal = TimeSeriesCreator.Step(N/2,N,0, amplitude);
            var oppositeSignal = TimeSeriesCreator.Step(N/2,N, amplitude, 0);

            dataset.Add("mainSignal",mainSignal);
            dataset.Add("oppsiteSignal", oppositeSignal);

            var results = CorrelationCalculator.Calculate("mainSignal",dataset);

            Assert.AreEqual(-1, results["oppsiteSignal"]);
        }

        [TestCase]
        public void CorrelateToSelf()
        {
            double timeBase_s = 1;

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
            double timeBase_s = 1;

            int N = 10;

            var dataset = new TimeSeriesDataSet();

            var amplitude = 10;

            var mainSignal = TimeSeriesCreator.Step(N / 2, N, 0, amplitude);
            var otherSignal = TimeSeriesCreator.Constant(0,N); 

            dataset.Add("mainSignal", mainSignal);
            dataset.Add("oppsiteSignal", otherSignal);

            var results = CorrelationCalculator.Calculate("mainSignal", dataset);

            Assert.AreEqual(0, results["oppsiteSignal"]);
        }




    }
}
