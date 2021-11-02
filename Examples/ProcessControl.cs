using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Dynamic;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis._Examples
{
    [TestFixture]
    class ProcessControl
    {
        int timeBase_s = 1;

        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        [TestCase, Explicit]

        public void CascadeControl()
        {
            int N = 600;
            #region CascadeControl

            var processParameters1 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 2,//rapid
                ProcessGains = new double[] { 1.1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };

            var processParameters2 = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 30,//slow
                ProcessGains = new double[] { 1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 5,
                Bias = 50
            };
            var pidParameters1 = new PIDModelParameters()
            {
                Kp = 3,
                Ti_s = 2 //rapid
            };
            var pidParameters2 = new PIDModelParameters()
            {
                Kp = 1,
                Ti_s = 40 //slow
            };
            var process1
                = new DefaultProcessModel(processParameters1, timeBase_s, "Process1");
            var process2
                = new DefaultProcessModel(processParameters2, timeBase_s, "Process2");
            var pid1 = new PIDModel(pidParameters1, timeBase_s, "PID1");
            var pid2 = new PIDModel(pidParameters2, timeBase_s, "PID2");

            var sim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { process1, process2, pid1, pid2 });

         //  pid1.SetManualOutput(50);
         //   pid1.SetToManualMode();

         //   pid2.SetManualOutput(50);
         //   pid2.SetToManualMode();

            sim.ConnectModels(process1, process2);
            sim.ConnectModels(process1, pid1);
            sim.ConnectModels(pid1, process1);
            sim.ConnectModels(process2, pid2);
            sim.ConnectModels(pid2, pid1,(int)PIDModelInputsIdx.Y_setpoint);

            sim.AddSignal(pid2,SignalType.Setpoint_Yset,TimeSeriesCreator.Constant(50, N));
            sim.AddSignal(process1,SignalType.Distubance_D,TimeSeriesCreator.Sinus(5,20,timeBase_s,N));
            sim.AddSignal(process2,SignalType.Distubance_D,TimeSeriesCreator.Step(300, N, 0, 1));

            var isOK = sim.Simulate(out var simResult);

            Plot.FromList(new List<double[]>
                {
                simResult.GetValues(process1.GetID(),SignalType.Output_Y_sim),
                simResult.GetValues(process2.GetID(),SignalType.Output_Y_sim),
                simResult.GetValues(pid2.GetID(),SignalType.Setpoint_Yset),
                simResult.GetValues(pid1.GetID(),SignalType.PID_U),
                simResult.GetValues(pid2.GetID(),SignalType.PID_U)
                },
            new List<string> { "y1=y1", "y2=y2[right]","y2=y2_set[right]", "y3=u1", "y4=u2[right]" }, timeBase_s, "CascadeEx");
            #endregion

            Assert.IsTrue(isOK);
        }


        [TestCase, Explicit]
        public void FeedForward_Part1()
        {
            #region Feedforward_Part1
            var processParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 30,
                ProcessGains = new double[] { 1.1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };
            var disturbanceParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 30,
                ProcessGains = new double[] { 1 },
                U0 = new double[] { 0 },
                TimeDelay_s = 5,
                Bias = 0
            };
            var pidParameters = new PIDModelParameters()
            {
                Kp = 0.3,
                Ti_s = 20
            };
            
            var processModel
                = new DefaultProcessModel(processParameters, timeBase_s, "Process1");
            var disturbanceModel
                = new DefaultProcessModel(disturbanceParameters, timeBase_s, "Disturbance1");
            var pidModel = new PIDModel(pidParameters, timeBase_s, "PID");

            var simNoFeedF = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel, disturbanceModel, pidModel });

            simNoFeedF.ConnectModels(pidModel, processModel);
            simNoFeedF.ConnectModels(processModel, pidModel);
            simNoFeedF.ConnectModelToOutput(disturbanceModel, processModel);

            simNoFeedF.AddSignal(pidModel, SignalType.Setpoint_Yset,
                TimeSeriesCreator.Constant(60, 600));
            simNoFeedF.AddSignal(disturbanceModel, SignalType.External_U,
                TimeSeriesCreator.Step(300, 600, 25, 0));

            var isOk = simNoFeedF.Simulate(out var dataNoFeedF);

            Plot.FromList(new List<double[]>
                {dataNoFeedF.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                    dataNoFeedF.GetValues(pidModel.GetID(),SignalType.Setpoint_Yset),
                    dataNoFeedF.GetValues(disturbanceModel.GetID(),SignalType.Output_Y_sim),
                    dataNoFeedF.GetValues(pidModel.GetID(),SignalType.PID_U),
                    dataNoFeedF.GetValues(disturbanceModel.GetID(),SignalType.External_U)
                    },
                new List<string> { "y1=y_run1", "y1=y_setpoint", "y2=y_dist[right]", "y3=u_pid", "y3=u_dist" }, timeBase_s, "FeedForwardEx1");

            #endregion

            Assert.IsTrue(isOk);
        }



        [TestCase, Explicit]
        public void FeedForward_Part2()
        {
            #region Feedforward_Part2

            var processParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 30,
                ProcessGains = new double[] { 1.1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };
            var disturbanceParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 30,
                ProcessGains = new double[] { 1 },
                U0 = new double[] { 0 },
                TimeDelay_s = 5,
                Bias = 0
            };
            var pidParameters = new PIDModelParameters()
            {
                Kp = 0.3,
                Ti_s = 20,
                FeedForward = new PIDfeedForward()
                {
                    isFFActive = true,
                    FF_Gain = -0.7,
                    FFHP_filter_order = 1,
                    FFLP_filter_order = 1,
                    FF_HP_Tc_s = 60,
                    FF_LP_Tc_s = 0//120
                }
            };

            var processModel
                = new DefaultProcessModel(processParameters, timeBase_s, "Process1");
            var disturbanceModel
                = new DefaultProcessModel(disturbanceParameters, timeBase_s, "Disturbance1");
            var pidModel = new PIDModel(pidParameters, timeBase_s, "PID");

            var simNoFeedF = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel, disturbanceModel, pidModel });

            simNoFeedF.ConnectModels(pidModel, processModel);
            simNoFeedF.ConnectModels(processModel, pidModel);
            simNoFeedF.ConnectModelToOutput(disturbanceModel, processModel);

            simNoFeedF.AddSignal(pidModel, SignalType.Setpoint_Yset,
                TimeSeriesCreator.Constant(60, 600));
            string dSignalID = simNoFeedF.AddSignal(disturbanceModel, SignalType.External_U,
                TimeSeriesCreator.Step(300, 600, 25, 0));
            simNoFeedF.ConnectSignal(dSignalID, pidModel, (int)PIDModelInputsIdx.FeedForward);

            var isOk = simNoFeedF.Simulate(out var dataNoFeedF);

            Plot.FromList(new List<double[]>
                {dataNoFeedF.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                    dataNoFeedF.GetValues(pidModel.GetID(),SignalType.Setpoint_Yset),
                    dataNoFeedF.GetValues(disturbanceModel.GetID(),SignalType.Output_Y_sim),
                    dataNoFeedF.GetValues(pidModel.GetID(),SignalType.PID_U),
                    dataNoFeedF.GetValues(disturbanceModel.GetID(),SignalType.External_U)
                    },
                new List<string> { "y1=y_run1", "y1=y_setpoint", "y2=y_dist[right]", "y3=u_pid", "y3=u_dist" }, 
                timeBase_s, "FeedForwardEx2");
            #endregion

            Assert.IsTrue(isOk);
        }



        [TestCase, Explicit]
        public void GainScheduling()
        {
                                  
            //step responses on the open-loop system
            #region GainScheduling_Part1
         
            var modelParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 0,
                ProcessGains = new double[] { 1.1 },
                Curvatures = new double[] { -0.7 },
                U0 = new double[] { 50 },
                UNorm = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };
            var processModel
                = new DefaultProcessModel(modelParameters, timeBase_s, "Process1");
            
            var openLoopSim1 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel });
            openLoopSim1.AddSignal(processModel, SignalType.External_U,
                TimeSeriesCreator.Step(50, 200, 80, 90));
            openLoopSim1.Simulate(out var openLoopData1);

            var openLoopSim2 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { processModel });
            openLoopSim2.AddSignal(processModel, SignalType.External_U,
                TimeSeriesCreator.Step(50, 200, 20, 30));
            var isOk1 = openLoopSim2.Simulate(out var openLoopData2);

            Plot.FromList(new List<double[]>
                {openLoopData1.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 openLoopData2.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 openLoopData1.GetValues(processModel.GetID(),SignalType.External_U),
                 openLoopData2.GetValues(processModel.GetID(),SignalType.External_U)
                 },
                new List<string> {"y1=y1(run1)","y1=y2(run2)","y3=u(run1)","y3=u(run2)"} 
                ,timeBase_s,"GainSchedulingEx");
            #endregion

            #region GainScheduling_Part2
            // the system rejecting a disturbance at y=20 with pidModel1
            var pidParameters1 = new PIDModelParameters()
            {
                Kp = 0.3,
                Ti_s = 20
            };
            var pidModel1 = new PIDModel(pidParameters1, timeBase_s, "PID1");
            var closedLoopSim1 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModel1, processModel });
            closedLoopSim1.ConnectModels(pidModel1,processModel);
            closedLoopSim1.ConnectModels(processModel, pidModel1);
            closedLoopSim1.AddSignal(pidModel1, SignalType.Setpoint_Yset,
                TimeSeriesCreator.Constant(20,400));
            closedLoopSim1.AddSignal(processModel,SignalType.Distubance_D, TimeSeriesCreator.Step(100,400,0,10));
            var isOk =closedLoopSim1.Simulate(out var closedLoopData1);

            //  the system rejecting a disturbance at y=70 with pidModel2
            var pidParameters2 = new PIDModelParameters()
            {
                Kp = 1,//NB! higher Kp
                Ti_s = 20
            };
            var pidModel2 = new PIDModel(pidParameters2, timeBase_s, "PID2");
            var closedLoopSim2 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModel2, processModel });
            closedLoopSim2.ConnectModels(pidModel2, processModel);
            closedLoopSim2.ConnectModels(processModel, pidModel2);
            closedLoopSim2.AddSignal(pidModel2, SignalType.Setpoint_Yset,
                TimeSeriesCreator.Constant(70, 400));
            closedLoopSim2.AddSignal(processModel, SignalType.Distubance_D, TimeSeriesCreator.Step(100, 400, 0, 10));
            var isOk2 = closedLoopSim2.Simulate(out var closedLoopData2);

            Plot.FromList(new List<double[]>
                {closedLoopData1.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 closedLoopData1.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset),
                 closedLoopData1.GetValues(pidModel1.GetID(),SignalType.PID_U),
                 closedLoopData2.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 closedLoopData2.GetValues(pidModel2.GetID(),SignalType.Setpoint_Yset),
                 closedLoopData2.GetValues(pidModel2.GetID(),SignalType.PID_U),
                 },
                new List<string> { "y1=y_run1","y1=y_setpoint(run1)", "y2=u_run1(right)","y3=y-run2",
                    "y3=y_setpoint(run2)", "y4=u_run2(right)" }, timeBase_s, "GainSchedulingEx_2");
            #endregion
            
            #region GainScheduling_Part3
            // building a gain-scheduling controller that is able to handle both regimes

            var pidParametersGS = new PIDModelParameters()
            {
                Ti_s = 20,
                GainScheduling = new PIDgainScheduling()
                {
                    GSActive_b =true,// turn on gain-scheduling 
                    GS_x_Min =0,    //Gain-scheduling: x minimum x=GsVariable
                    GS_x_1= 20,     //Gain-scheduling: x1,x=GsVariable
                    GS_x_2=70,      //Gain-scheduling: x2,x=GsVariable
                    GS_x_Max=100,   //Gain-scheduling: x maxiumum, x = GsVariable
                    GS_Kp_Min=0.1,  //Gain-scheduling: KP @ GsVariable=GS_x_Min
                    GS_Kp_1=0.2,    //Gain-scheduling: KP @ GsVariable=GS_x_1
                    GS_Kp_2=1,      //Gain-scheduling: KP @ GsVariable=GS_x_2
                    GS_Kp_Max=1.2   //Gain-scheduling: KP @ GsVariable=GS_x_Max
                }
            };

            var pidModelGS = new PIDModel(pidParametersGS, timeBase_s, "PID_GS");

            var closedLoopSimGS_1 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModelGS, processModel });
            closedLoopSimGS_1.ConnectModels(pidModelGS, processModel);
            closedLoopSimGS_1.ConnectModels(processModel, pidModelGS);
            closedLoopSimGS_1.AddSignal(pidModelGS, SignalType.Setpoint_Yset,
                  TimeSeriesCreator.Constant(20, 400));
            closedLoopSimGS_1.AddSignal(processModel, SignalType.Distubance_D, 
                TimeSeriesCreator.Step(100, 400, 0, 10));
            // Gain-scheduling variable:
            closedLoopSimGS_1.ConnectModels(processModel,pidModelGS,(int)PIDModelInputsIdx.GainScheduling);
           var isOk3 = closedLoopSimGS_1.Simulate(out var closedLoopDataGS_1);

            var closedLoopSimGS_2 = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { pidModelGS, processModel });
            closedLoopSimGS_2.ConnectModels(pidModelGS, processModel);
            closedLoopSimGS_2.ConnectModels(processModel, pidModelGS);
            closedLoopSimGS_2.AddSignal(pidModelGS, SignalType.Setpoint_Yset,
                TimeSeriesCreator.Constant(70, 400));
            closedLoopSimGS_2.AddSignal(processModel, SignalType.Distubance_D, 
                TimeSeriesCreator.Step(100, 400, 0, 10));
            // Gain-scheduling variable:
            closedLoopSimGS_2.ConnectModels(processModel, pidModelGS, (int)PIDModelInputsIdx.GainScheduling);
            var isOk4 = closedLoopSimGS_2.Simulate(out var closedLoopDataGS_2);

            Plot.FromList(new List<double[]>
                {closedLoopDataGS_1.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 closedLoopDataGS_1.GetValues(pidModelGS.GetID(),SignalType.Setpoint_Yset),
                 closedLoopDataGS_1.GetValues(pidModelGS.GetID(),SignalType.PID_U),
                 closedLoopDataGS_2.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 closedLoopDataGS_2.GetValues(pidModelGS.GetID(),SignalType.Setpoint_Yset),
                 closedLoopDataGS_2.GetValues(pidModelGS.GetID(),SignalType.PID_U)
                 },
                new List<string> { "y1=y_run1","y1=y_setpoint(run1)", "y2=u_run1(right)","y3=y-run2",
                    "y3=y_setpoint(run2)", "y4=u_run2(right)" }, timeBase_s, "GainSchedulingEx_3");
            #endregion

            Assert.IsTrue(isOk1);
            Assert.IsTrue(isOk2);
            Assert.IsTrue(isOk3);
            Assert.IsTrue(isOk4);



        }

    
        [TestCase,Explicit]
     
        public void MinSelect()
        {
            int N = 600;

            #region MinSelect

            var processParameters = new DefaultProcessModelParameters
            {
                WasAbleToIdentify = true,
                TimeConstant_s = 30,
                ProcessGains = new double[] { 1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 5,
                Bias = 50
            };
            var pidParameters1 = new PIDModelParameters()
            {
                Kp = 3, //high-gain
                Ti_s = 20
            };
            var pidParameters2 = new PIDModelParameters()
            {
                Kp = 0.5,//low-gain
                Ti_s = 20 
            };
            var process
                = new DefaultProcessModel(processParameters, timeBase_s, "Process");
            var pid1 = new PIDModel(pidParameters1, timeBase_s, "PID1");
            var pid2 = new PIDModel(pidParameters2, timeBase_s, "PID2");
            var minSelect = new Select(SelectType.MIN,"minSelect");

            var sim = new ProcessSimulator(timeBase_s,
                new List<ISimulatableModel> { process, pid1, pid2,minSelect });

            // tracking and min select-related
            sim.ConnectModels(process, pid1);
            sim.ConnectModels(process, pid2);
            sim.ConnectModels(pid1, minSelect,0);
            sim.ConnectModels(pid2, minSelect,1);
            string selectSignalID = sim.ConnectModels(minSelect, process);
            sim.ConnectSignal(selectSignalID,pid1, (int)PIDModelInputsIdx.Tracking);
            sim.ConnectSignal(selectSignalID,pid2, (int)PIDModelInputsIdx.Tracking);

             sim.AddSignal(pid1, SignalType.Setpoint_Yset, TimeSeriesCreator.Constant(50, N));
            sim.AddSignal(pid2, SignalType.Setpoint_Yset, TimeSeriesCreator.Constant(50, N));
            sim.AddSignal(process, SignalType.Distubance_D, TimeSeriesCreator.Step(300, N, 0, 1));

            var isOK = sim.Simulate(out var simResult);
            
            Plot.FromList(new List<double[]>
                {
                simResult.GetValues(process.GetID(),SignalType.Output_Y_sim),
                simResult.GetValues(pid2.GetID(),SignalType.Setpoint_Yset),
                simResult.GetValues(pid1.GetID(),SignalType.PID_U),
                simResult.GetValues(pid2.GetID(),SignalType.PID_U),
                simResult.GetValues(minSelect.GetID(),SignalType.SelectorOut),
                },
            new List<string> { "y1=y1", "y1=y_set", "y3=u_pid1", "y3=u_pid2","y3=u_select" }, timeBase_s, "MinSelectEx");
            #endregion

            Assert.IsTrue(isOK);

        }





    }
}
