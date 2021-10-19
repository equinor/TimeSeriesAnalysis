using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

using NUnit.Framework;

namespace TimeSeriesAnalysis.Test.ProcessSimulatorTests
{
    enum INDEX // this is just here to improve readability
    { 
        FIRST=0,
        SECOND=1,
        THIRD=2,
        FOURTH=3,
        FIFTH=4
    }


    /// <summary>
    /// Test of process simulations where each of or some of the models have multiple inputs
    /// </summary>
    [TestFixture]
    class MISOTests
    {
        int timeBase_s = 1;
        int N = 480;

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
                ProcessGains = new double[] { 0.8,0.7 },
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
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(60, N, 50, 55), (int)INDEX.FIRST);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(180, N, 50, 45), (int)INDEX.SECOND);
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(simData);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);

            Assert.IsTrue(Math.Abs(simY[0] -(1*50 + 0.5*50 +5) ) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() -(1*55 + 0.5*45 +5) ) < 0.01);

            /*Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,0),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,1)
            },
                new List<string> { "y1=y_sim1", "y3=u1","y3=u2" },
                timeBase_s, "UnitTest_SingleMISO");*/
        }


        public void Single_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1 });
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(60, N, 50, 55), (int)INDEX.FIRST);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(180, N, 50, 45), (int)INDEX.SECOND);
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(simData);
            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);

            Assert.IsTrue(Math.Abs(simY[0] - (1 * 50 + 0.5 * 50 + 5)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (1 * 55 + 0.5 * 45 + 5)) < 0.01);

          /*  Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,0),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,1)
            },
                new List<string> { "y1=y_sim1", "y3=u1", "y3=u2" },
                timeBase_s, "UnitTest_SingleMISO");*/
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PIDAndSingle_RunsAndConverges(bool doReverseInputConnections)
        {
            int pidIndex = 0;
            int externalUIndex = 1;
            if (doReverseInputConnections)
            {
                pidIndex = 1;
                externalUIndex = 0;
            }

            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModel1, processModel1 });

            processSim.ConnectModels(processModel1, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1, pidIndex);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(60, N, 50, 45), externalUIndex);
            processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset, TimeSeriesCreator.Constant(60,N)) ;
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);
            Assert.IsTrue(isOk);

           /* Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,externalUIndex)
            },
                new List<string> { "y1=y_sim1", "y3=u1", "y3=u2" },
                timeBase_s, "UnitTest_PIDandSingle");*/

            double[] simY = simData.GetValues(processModel1.GetID(), SignalType.Output_Y_sim);
            SISOTests.CommonAsserts(simData);
            Assert.IsTrue(Math.Abs(simY[0] - (60)) < 0.01);
            Assert.IsTrue(Math.Abs(simY.Last() - (60)) < 0.1);
        }




        [TestCase]
        public void Serial2_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1, processModel2 });

            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(60, N, 50, 55), (int)INDEX.FIRST);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(180, N, 50, 45), (int)INDEX.SECOND);

            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            processSim.AddSignal(processModel2, SignalType.External_U, TimeSeriesCreator.Step(240, N, 50, 40), (int)INDEX.SECOND);
               
            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(simData);

            /*
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.FIRST),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 simData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
             },
            new List<string> { "y1=y_sim1", "y1=y_sim2", "u1", "u2", "u4" },
            timeBase_s, "UnitTest_MISO2Serial");
            */

            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0] - ((1*50+0.5*50+5)*1.1+50*0.6+5)) < 0.01,"unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - ((1*55+0.5*45+5)*1.1+40*0.6+5)) < 0.01, "unexpected ending value");
        }


        [TestCase]
        public void PIDandSerial2_RunsAndConverges()
        {
            int pidIndex = 1;
            int externalUIndex = 0;
            var processSim = new ProcessSimulator(timeBase_s,
              //  new List<ISimulatableModel> { processModel2, processModel1,pidModel1 });//TODO:initalization fails unless model are in this order!
              // (but simulation fails after first run with this order!)
              new List<ISimulatableModel> { pidModel1, processModel1, processModel2  });//TODO:

            processSim.AddSignal(pidModel1, SignalType.Setpoint_Yset, TimeSeriesCreator.Constant(150, N));

            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(60, N, 50, 55), externalUIndex);

            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            processSim.AddSignal(processModel2, SignalType.External_U, TimeSeriesCreator.Step(240, N, 50, 40), (int)INDEX.SECOND);

            processSim.ConnectModels(processModel2, pidModel1);
            processSim.ConnectModels(pidModel1, processModel1, pidIndex);

            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk,"simulation returned false, it failed");
 
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                simData.GetValues(pidModel1.GetID(),SignalType.PID_U),
                simData.GetValues(processModel1.GetID(),SignalType.External_U,externalUIndex),
                simData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND)
            },
                new List<string> { "y1=y_sim1","y1=y_sim2", "y3=u1(pid)", "y3=u2", "y3=u3" },
                timeBase_s, "UnitTest_PIDandSerial2");

            SISOTests.CommonAsserts(simData);
            double[] simY = simData.GetValues(processModel2.GetID(), SignalType.Output_Y_sim);
            Assert.IsTrue(Math.Abs(simY[0] - 150) < 0.01, "unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - 150) < 0.1, "unexpected ending value");
        }




        [TestCase]
        public void Serial3_RunsAndConverges()
        {
            var processSim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel1, processModel2, processModel3 });

            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(60, N, 50, 55), (int)INDEX.FIRST);
            processSim.AddSignal(processModel1, SignalType.External_U, TimeSeriesCreator.Step(180, N, 50, 45), (int)INDEX.SECOND);

            processSim.ConnectModels(processModel1, processModel2, (int)INDEX.FIRST);
            processSim.AddSignal(processModel2, SignalType.External_U, TimeSeriesCreator.Step(240, N, 50, 40), (int)INDEX.SECOND);

            processSim.ConnectModels(processModel2, processModel3, (int)INDEX.FIRST);
            processSim.AddSignal(processModel3, SignalType.External_U, TimeSeriesCreator.Step(300, N, 30, 40), (int)INDEX.SECOND);

            var isOk = processSim.Simulate(out TimeSeriesDataSet simData);

            Assert.IsTrue(isOk);
            SISOTests.CommonAsserts(simData);


            double[] simY = simData.GetValues(processModel3.GetID(), SignalType.Output_Y_sim);
            double expStartVal  = ((1 * 50 + 0.5 * 50 + 5) * 1.1 + 50 * 0.6 + 5)*0.8 + 0.7*30 + 5;
            double expEndVal    = ((1 * 55 + 0.5 * 45 + 5) * 1.1 + 40 * 0.6 + 5)*0.8 + 0.7*40 + 5;

            Assert.IsTrue(Math.Abs(simY[0] - expStartVal) < 0.01, "unexpected starting value");
            Assert.IsTrue(Math.Abs(simY.Last() - expEndVal) < 0.01, "unexpected ending value");

            /*
            Plot.FromList(new List<double[]> {
                 simData.GetValues(processModel1.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel2.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel3.GetID(),SignalType.Output_Y_sim),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.FIRST),
                 simData.GetValues(processModel1.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 simData.GetValues(processModel2.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                 simData.GetValues(processModel3.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                },
                new List<string> { "y1=y_sim1", "y1=y_sim2","y1=y_sim3", "u1", "u2", "u4","u6" },
                timeBase_s, "UnitTest_MISO3Serial");*/

        }








    }
}
