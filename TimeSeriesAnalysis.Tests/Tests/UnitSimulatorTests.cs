using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Test.UnitSim
{
    [TestFixture]
    class CoSimulations
    {
        int timeBase_s = 1;
        int N = 500;

        int Ysetpoint = 50;

        UnitParameters modelParameters1;
        UnitModel processModel1;
        PidParameters pidParameters1;
        PidModel pidModel1;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();

            modelParameters1 = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                TimeDelay_s = 5,
                Bias = 5
            };


            processModel1 = new UnitModel(modelParameters1, "SubProcess1");

            pidParameters1 = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            pidModel1 = new PidModel(pidParameters1, "PID1");
        }


        [TestCase]
        public void CoSim_NoDisturbance_InitalizesSteady()
        {
            UnitDataSet unitData = new UnitDataSet("test");
            unitData.Y_setpoint = TimeSeriesCreator.Constant(Ysetpoint,N);
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(new DateTime(2000,1,1),timeBase_s,N);

            var sim = new UnitSimulator(processModel1);
            sim.CoSimulate(pidModel1,ref unitData);

         /*   Plot.FromList(new List<double[]> { unitData.Y_sim,
                    unitData.U_sim.GetColumn(0) }, 
             new List<string> { "y1=y_sim", "y3=u_sim", }, unitData.GetTimeBase(), 
                "UnitTestCoSimulate");*/

            Assert.IsTrue(unitData.Y_sim[1] == unitData.Y_sim[0]);
            Assert.IsTrue(unitData.Y_sim[2] == unitData.Y_sim[1]);
            Assert.IsTrue(unitData.U_sim.GetColumn(0)[1] == unitData.U_sim.GetColumn(0)[0]);
            Assert.IsTrue(unitData.U_sim.GetColumn(0)[2] == unitData.U_sim.GetColumn(0)[1]);
        }

        // Work-in-progress
        [TestCase()]
        public void CoSim_WithDisturbance_InitalizesSteady()
        {
            UnitDataSet unitData = new UnitDataSet("test");
            unitData.Y_setpoint = TimeSeriesCreator.Constant(Ysetpoint, N);
            unitData.D = TimeSeriesCreator.Step(N/4,N,1,2);//works if inital value is set to zero
            unitData.Times = TimeSeriesCreator.CreateDateStampArray(
                new DateTime(2000, 1, 1), timeBase_s, N);

            var sim = new UnitSimulator(processModel1);
            sim.CoSimulate(pidModel1, ref unitData);
         /*   
            Plot.FromList(new List<double[]> { unitData.Y_sim,
                    unitData.U_sim.GetColumn(0) },
             new List<string> { "y1=y_sim", "y3=u_sim", }, unitData.GetTimeBase(),
                "UnitTestCoSimulate");
         */
            Assert.IsTrue(unitData.Y_sim[1] == unitData.Y_sim[0]);
            Assert.IsTrue(unitData.U_sim.GetColumn(0)[1] == unitData.U_sim.GetColumn(0)[0]);
        }

    }
}
