using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Examples
{
    [TestFixture]
    class ProcessControl
    {

        [TestCase]
        #region CascadeControl
        void CascadeControl()
        {



        }
        #endregion

        [TestCase]
        #region FeedForward
        void FeedForward()
        {










        }
        #endregion

        [TestCase]
        #region GainScheduling
        void GainScheduling()
        {
            int timeBase_s = 1;
            int N = 500;

            DefaultProcessModelParameters modelParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 0,
                ProcessGains = new double[] { 1 },
                ProcessGainCurvatures = new double[] { 1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 5
            };
            DefaultProcessModel processModel
                = new DefaultProcessModel(modelParameters, timeBase_s, "SubProcess1");

            var pidParameters = new PIDModelParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel = new PIDModel(pidParameters, timeBase_s, "PID1");

            var openLoopSim1 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel });
            openLoopSim1.AddSignal(processModel, SignalType.External_U,
                TimeSeriesCreator.Step(50, 200, 70, 80));
            openLoopSim1.Simulate(out var openLoopData1);

            var openLoopSim2 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel });
            openLoopSim2.AddSignal(processModel, SignalType.External_U,
                TimeSeriesCreator.Step(50, 200, 20, 30));
            openLoopSim2.Simulate(out var openLoopData2);

            Plot.FromList(new List<double[]>
                {openLoopData1.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 openLoopData2.GetValues(processModel.GetID(),SignalType.Output_Y_sim) },
                new List<string> {"y1(high u)","y2(low u)"} ,timeBase_s,"GainSchedulingEx"
                );
        }
        #endregion






        [TestCase]
        #region MinSelect
        void MinSelect()
        {










        }
        #endregion




    }
}
