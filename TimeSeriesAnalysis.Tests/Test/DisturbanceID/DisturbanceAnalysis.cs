using NUnit.Framework;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.DisturbanceAnalysis
{
    [TestFixture]
    class DisturbanceAnalysisTests
    {
        const double tolerance = 0.15;

        [Test]
        public void FFT_singleSinusoid_Period()
        {
            int nStepsDuration = 32;
            int timeBase_s = 1;
            double truePeriod = 4;

            double[] signal = TimeSeriesCreator.Sinus(amplitude: 5.0, truePeriod, timeBase_s, nStepsDuration);

            double? period = SignalPeriodEstimator.EstimatePeriod
            (
                signal, timeBase_s
            );

            if (period != null)
            {
                Assert.AreEqual((float)period, truePeriod, delta: tolerance * truePeriod);
            }
        }

        [TestCase(5, 10, 7, 84)]
        [TestCase(61, 13, 13, 10)]
        public void FFT_dualSinusoid_Period(
            double amplitude_1, double amplitude_2, double period_1, double period_2
            )
        {
            int nStepsDuration = 2087;
            int timeBase_s = 1;
            double truePeriod = Math.Max(period_1, period_2);

            double[] signal_1 = TimeSeriesCreator.Sinus(amplitude_1, period_1, timeBase_s, nStepsDuration);
            double[] signal_2 = TimeSeriesCreator.Sinus(amplitude_2, period_2, timeBase_s, nStepsDuration);
            double[] signal = new Vec().Add(signal_1, signal_2);

            double? period = SignalPeriodEstimator.EstimatePeriod
            (
                signal, timeBase_s
            );

            if (period != null)
            {
                Assert.AreEqual((float)period, truePeriod, delta: tolerance * truePeriod);
            }
        }
    }
}
