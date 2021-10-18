using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

using NUnit.Framework;

namespace TimeSeriesAnalysis.Test
{
    enum INDEX // just for improved readability
    { 
        FIRST=0,
        SECOND=1,
        THIRD=2
    }


    /// <summary>
    /// Test of process simulations where each of or some of the models have multiple inputs
    /// </summary>
    [TestFixture]
    class ProcessSimulatorMISOTests
    {
        int timeBase_s = 1;
        int N = 500;

        int Ysetpoint = 50;

        DefaultProcessModelParameters modelParameters1;
        DefaultProcessModelParameters modelParameters2;
        DefaultProcessModelParameters modelParameters3;
        DefaultProcessModel processModel1;
        DefaultProcessModel processModel2;
        DefaultProcessModel processModel3;
        PIDModelParameters pidParameters1;
        PIDModel pidModel1;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 10,
                ProcessGains = new double[] { 1,0.5 },
                TimeDelay_s = 5,
                Bias = 5
            };
            modelParameters2 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 20,
                ProcessGains = new double[] { 1.1,0.6 },
                TimeDelay_s = 10,
                Bias = 5
            };
            modelParameters3 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 20,
                ProcessGains = new double[] { 1.1,0.7 },
                TimeDelay_s = 10,
                Bias = 5
            };

            processModel1 = new DefaultProcessModel(modelParameters1, timeBase_s, "SubProcess1");
            processModel2 = new DefaultProcessModel(modelParameters2, timeBase_s, "SubProcess2");
            processModel3 = new DefaultProcessModel(modelParameters3, timeBase_s, "SubProcess3");

            pidParameters1 = new PIDModelParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PIDModel(pidParameters1, timeBase_s, "PID1");
        }


        [TestCase]
        public void MISO_Single_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1 });
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(N / 4, N, 50, 55), (int)INDEX.FIRST);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(N / 2, N, 50, 45), (int)INDEX.SECOND);
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            ProcessSimulatorSISOTests.CommonAsserts(simData);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);
            //   Assert.IsTrue(Math.Abs(simY[0] - 55) < 0.01);
            //   Assert.IsTrue(Math.Abs(simY.Last() - 60) < 0.01);

           /* Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim)
            },
               new List<string> { "y1=y_sim1", },
               timeBase_s, "UnitTest_SingleMISO"); */
            
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,0),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,1)
            },
                new List<string> { "y1=y_sim1", "y3=u1","y3=u2" },
                timeBase_s, "UnitTest_SingleMISO");
        }

        [TestCase]
        public void MISOs_2Serial_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1, processModel2 });

            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(N / 4, N, 50, 55), (int)INDEX.FIRST);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(N / 2, N, 50, 45), (int)INDEX.SECOND);

            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            processSim.AddSignal(processModel2, SignalType.External_U, TimeSeriesCreator.Step(N *3 / 4, N, 50, 40), (int)INDEX.SECOND);
               
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            ProcessSimulatorSISOTests.CommonAsserts(simData);

          //  double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y_sim);
          //  Assert.IsTrue(Math.Abs(simY[0] - (55 * 1.1 + 5)) < 0.01);
          //  Assert.IsTrue(Math.Abs(simY.Last() - (60 * 1.1 + 5)) < 0.01);

             Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim)},
             new List<string> { "y1=y_sim1", "y1=y_sim2" },
             timeBase_s, "UnitTest_MISO2Serial");
        }










    }
}
