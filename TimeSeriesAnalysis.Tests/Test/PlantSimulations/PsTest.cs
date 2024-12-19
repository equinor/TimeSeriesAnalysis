using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis.Test.PlantSimulations
{
    /// <summary>
    /// Common helper class for PlantSimulator tests
    /// </summary>
    internal class PsTest
    {


        public static void CommonAsserts(TimeSeriesDataSet inputData, TimeSeriesDataSet simData, PlantSimulator plant)
        {
            Assert.IsNotNull(simData, "simData should not be null");

            var signalNames = simData.GetSignalNames();

            foreach (string signalName in signalNames)
            {
                var signal = simData.GetValues(signalName);
                // test that all systems start in steady-state
                double firstTwoValuesDiff = Math.Abs(signal.ElementAt(0) - signal.ElementAt(1));
                double lastTwoValuesDiff = Math.Abs(signal.ElementAt(signal.Length - 2) - signal.ElementAt(signal.Length - 1));

                Assert.AreEqual(signal.Count(), simData.GetLength(), "all signals should be same length as N");
                Assert.IsTrue(firstTwoValuesDiff < 0.01, "system should start up in steady-state");
                Assert.IsTrue(lastTwoValuesDiff < 0.01, "system should end up in steady-state");
            }

            Assert.AreEqual(simData.GetLength(), simData.GetTimeStamps().Count(), "number of timestamps should match number of data points in sim");
            Assert.AreEqual(simData.GetTimeStamps().Last(), inputData.GetTimeStamps().Last(), "datasets should end at same timestamp");

            /*    foreach (var modelKeyValuePair in plant.GetModels())
                {
                    Assert.IsNotNull(simData.GetValues(modelKeyValuePair.Value.GetID(), SignalType.Output_Y),"model output was not simulated");
                }*/

        }


    }
}
