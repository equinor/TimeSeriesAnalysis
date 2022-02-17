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

        public void CascadeControl_Ex()
        {
            CascadeControl_Ex();
        }

        public TimeSeriesDataSet CascadeControl()
        {
            int N = 600;
            #region CascadeControl

            var processParameters1 = new UnitParameters
            {
                TimeConstant_s = 2,//rapid
                LinearGains = new double[] { 1.1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };

            var processParameters2 = new UnitParameters
            {
                TimeConstant_s = 30,//slow
                LinearGains = new double[] { 1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 5,
                Bias = 50
            };
            var pidParameters1 = new PidParameters()
            {
                Kp = 3,
                Ti_s = 2 //rapid
            };
            var pidParameters2 = new PidParameters()
            {
                Kp = 1,
                Ti_s = 40 //slow
            };
            var process1
                = new UnitModel(processParameters1, timeBase_s, "Process1");
            var process2
                = new UnitModel(processParameters2, timeBase_s, "Process2");
            var pid1 = new PidModel(pidParameters1, timeBase_s, "PID1");
            var pid2 = new PidModel(pidParameters2, timeBase_s, "PID2");

            var sim = new PlantSimulator(new List<ISimulatableModel> { process1, process2, pid1, pid2 });

         //  pid1.SetManualOutput(50);
         //   pid1.SetToManualMode();

         //   pid2.SetManualOutput(50);
         //   pid2.SetToManualMode();

            sim.ConnectModels(process1, process2);
            sim.ConnectModels(process1, pid1);
            sim.ConnectModels(pid1, process1);
            sim.ConnectModels(process2, pid2);
            sim.ConnectModels(pid2, pid1,(int)PidModelInputsIdx.Y_setpoint);

            var inputData = new TimeSeriesDataSet();

            inputData.Add(sim.AddExternalSignal(pid2,SignalType.Setpoint_Yset),TimeSeriesCreator.Constant(50, N));
            inputData.Add(sim.AddExternalSignal(process1,SignalType.Disturbance_D),TimeSeriesCreator.Sinus(5,20,timeBase_s,N));
            inputData.Add(sim.AddExternalSignal(process2,SignalType.Disturbance_D),TimeSeriesCreator.Step(300, N, 0, 1));
            inputData.CreateTimestamps(timeBase_s);

            var isOK = sim.Simulate(inputData,out var simResult);

            Plot.FromList(new List<double[]>
                {
                simResult.GetValues(process1.GetID(),SignalType.Output_Y_sim),
                simResult.GetValues(process2.GetID(),SignalType.Output_Y_sim),
                inputData.GetValues(pid2.GetID(),SignalType.Setpoint_Yset),
                simResult.GetValues(pid1.GetID(),SignalType.PID_U),
                simResult.GetValues(pid2.GetID(),SignalType.PID_U)
                },
                new List<string> { "y1=y1", "y2=y2[right]","y2=y2_set[right]", "y3=u1", "y4=u2[right]" },
                timeBase_s, "CascadeEx");
            #endregion

            Assert.IsTrue(isOK);
            return simResult;
        }


        [TestCase, Explicit]
        public void FeedForward_Part1_Ex()
        {
            FeedForward_Part1();
        }

        public TimeSeriesDataSet FeedForward_Part1()
        {
            #region Feedforward_Part1
            var processParameters = new UnitParameters
            {
                TimeConstant_s = 30,
                LinearGains = new double[] { 1.1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };
            var disturbanceParameters = new UnitParameters
            {
                TimeConstant_s = 30,
                LinearGains = new double[] { 1 },
                U0 = new double[] { 0 },
                TimeDelay_s = 5,
                Bias = 0
            };
            var pidParameters = new PidParameters()
            {
                Kp = 0.3,
                Ti_s = 20
            };
            
            var processModel
                = new UnitModel(processParameters, timeBase_s, "Process1");
            var disturbanceModel
                = new UnitModel(disturbanceParameters, timeBase_s, "Disturbance1");
            var pidModel = new PidModel(pidParameters, timeBase_s, "PID");

            var simNoFeedF = new PlantSimulator(
                new List<ISimulatableModel> { processModel, disturbanceModel, pidModel });

            simNoFeedF.ConnectModels(pidModel, processModel);
            simNoFeedF.ConnectModels(processModel, pidModel);
            simNoFeedF.ConnectModelToOutput(disturbanceModel, processModel);

            var inputData = new TimeSeriesDataSet();

            inputData.Add(simNoFeedF.AddExternalSignal(pidModel, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(60, 600));
            inputData.Add(simNoFeedF.AddExternalSignal(disturbanceModel, SignalType.External_U),
                TimeSeriesCreator.Step(300, 600, 25, 0));
            inputData.CreateTimestamps(timeBase_s);
            var isOk = simNoFeedF.Simulate(inputData,out var dataNoFeedF);

            Plot.FromList(new List<double[]>
                {
                dataNoFeedF.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                inputData.GetValues(pidModel.GetID(),SignalType.Setpoint_Yset),
                dataNoFeedF.GetValues(disturbanceModel.GetID(),SignalType.Output_Y_sim),
                dataNoFeedF.GetValues(pidModel.GetID(),SignalType.PID_U),
                inputData.GetValues(disturbanceModel.GetID(),SignalType.External_U)
                },
                new List<string> { "y1=y_run1", "y1=y_setpoint", "y2=y_dist[right]", "y3=u_pid", "y3=u_dist" }, timeBase_s, "FeedForwardEx1");

            #endregion

            Assert.IsTrue(isOk);
            return dataNoFeedF;
        }



        [TestCase, Explicit]
        public void FeedForward_Part2_Ex()
        {
            FeedForward_Part2();
        }

        public TimeSeriesDataSet FeedForward_Part2()
        { 
        #region Feedforward_Part2

            var processParameters = new UnitParameters
            {
                TimeConstant_s = 30,
                LinearGains = new double[] { 1.1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };
            var disturbanceParameters = new UnitParameters
            {
                TimeConstant_s = 30,
                LinearGains = new double[] { 1 },
                U0 = new double[] { 0 },
                TimeDelay_s = 5,
                Bias = 0
            };
            var pidParameters = new PidParameters()
            {
                Kp = 0.3,
                Ti_s = 20,
                FeedForward = new PidFeedForward()
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
                = new UnitModel(processParameters, timeBase_s, "Process1");
            var disturbanceModel
                = new UnitModel(disturbanceParameters, timeBase_s, "Disturbance1");
            var pidModel = new PidModel(pidParameters, timeBase_s, "PID");

            var simNoFeedF = new PlantSimulator(
                new List<ISimulatableModel> { processModel, disturbanceModel, pidModel });

            simNoFeedF.ConnectModels(pidModel, processModel);
            simNoFeedF.ConnectModels(processModel, pidModel);
            simNoFeedF.ConnectModelToOutput(disturbanceModel, processModel);

            var inputData = new TimeSeriesDataSet();

            inputData.Add(simNoFeedF.AddExternalSignal(pidModel, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(60, 600));
            string dSignalID = simNoFeedF.AddExternalSignal(disturbanceModel, SignalType.External_U);
            inputData.Add(dSignalID, TimeSeriesCreator.Step(300, 600, 25, 0));
            inputData.CreateTimestamps(timeBase_s);

            simNoFeedF.ConnectSignal(dSignalID, pidModel, (int)PidModelInputsIdx.FeedForward);

            var isOk = simNoFeedF.Simulate(inputData,out var dataNoFeedF);

            Plot.FromList(new List<double[]>
                { 
                    dataNoFeedF.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                    inputData.GetValues(pidModel.GetID(),SignalType.Setpoint_Yset),
                    dataNoFeedF.GetValues(disturbanceModel.GetID(),SignalType.Output_Y_sim),
                    dataNoFeedF.GetValues(pidModel.GetID(),SignalType.PID_U),
                    inputData.GetValues(disturbanceModel.GetID(),SignalType.External_U)
                },
                new List<string> { "y1=y_run1", "y1=y_setpoint", "y2=y_dist[right]", "y3=u_pid", "y3=u_dist" }, 
                timeBase_s, "FeedForwardEx2");
            #endregion

            Assert.IsTrue(isOk);
            return dataNoFeedF;
        }



        [TestCase, Explicit]
        public void  GainScheduling_Ex()
        {
            GainScheduling();
        }
        public TimeSeriesDataSet GainScheduling()
        { 
            //step responses on the open-loop system
        #region GainScheduling_Part1

        var modelParameters = new UnitParameters
            {
                TimeConstant_s = 0,
                LinearGains = new double[] { 1.1 },
                Curvatures = new double[] { -0.7 },
                U0 = new double[] { 50 },
                UNorm = new double[] { 50 },
                TimeDelay_s = 0,
                Bias = 50
            };
            var processModel
                = new UnitModel(modelParameters, timeBase_s, "Process1");
            
            var openLoopSim1 = new PlantSimulator(
                new List<ISimulatableModel> { processModel });

            var inputDataSim1 = new TimeSeriesDataSet();
            inputDataSim1.Add(openLoopSim1.AddExternalSignal(processModel, SignalType.External_U),
                TimeSeriesCreator.Step(50, 200, 80, 90));
            inputDataSim1.CreateTimestamps(timeBase_s);
            openLoopSim1.Simulate(inputDataSim1,out var openLoopData1);

            var openLoopSim2 = new PlantSimulator(
                new List<ISimulatableModel> { processModel });
            var inputDataSim2 = new TimeSeriesDataSet();
            inputDataSim2.Add(openLoopSim2.AddExternalSignal(processModel, SignalType.External_U),
                TimeSeriesCreator.Step(50, 200, 20, 30));
            inputDataSim2.CreateTimestamps(timeBase_s);
            var isOk1 = openLoopSim2.Simulate(inputDataSim2,out var openLoopData2);

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
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.3,
                Ti_s = 20
            };
            var pidModel1 = new PidModel(pidParameters1, timeBase_s, "PID1");
            var closedLoopSim1 = new PlantSimulator(
                new List<ISimulatableModel> { pidModel1, processModel });
            closedLoopSim1.ConnectModels(pidModel1,processModel);
            closedLoopSim1.ConnectModels(processModel, pidModel1);

            var inputData1 = new TimeSeriesDataSet();
            inputData1.Add(closedLoopSim1.AddExternalSignal(pidModel1, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(20,400));
            inputData1.Add(closedLoopSim1.AddExternalSignal(processModel,SignalType.Disturbance_D), TimeSeriesCreator.Step(100,400,0,10));
            inputData1.CreateTimestamps(timeBase_s);
            var isOk =closedLoopSim1.Simulate(inputData1,out var closedLoopData1);
            
            //  the system rejecting a disturbance at y=70 with pidModel2
            var pidParameters2 = new PidParameters()
            {
                Kp = 1,//NB! higher Kp
                Ti_s = 20
            };
            var pidModel2 = new PidModel(pidParameters2, timeBase_s, "PID2");
            var closedLoopSim2 = new PlantSimulator(
                new List<ISimulatableModel> { pidModel2, processModel });
            closedLoopSim2.ConnectModels(pidModel2, processModel);
            closedLoopSim2.ConnectModels(processModel, pidModel2);
            var inputData2 = new TimeSeriesDataSet();
            inputData2.Add(closedLoopSim2.AddExternalSignal(pidModel2, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(70, 400));
            inputData2.Add(closedLoopSim2.AddExternalSignal(processModel, SignalType.Disturbance_D),
                TimeSeriesCreator.Step(100, 400, 0, 10));
            inputData2.CreateTimestamps(timeBase_s);
            var isOk2 = closedLoopSim2.Simulate(inputData2,out var closedLoopData2);

            Plot.FromList(new List<double[]>
                {closedLoopData1.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 inputData1.GetValues(pidModel1.GetID(),SignalType.Setpoint_Yset),
                 closedLoopData1.GetValues(pidModel1.GetID(),SignalType.PID_U),
                 closedLoopData2.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 inputData2.GetValues(pidModel2.GetID(),SignalType.Setpoint_Yset),
                 closedLoopData2.GetValues(pidModel2.GetID(),SignalType.PID_U),
                 },
                new List<string> { "y1=y_run1","y1=y_setpoint(run1)", "y2=u_run1(right)","y3=y-run2",
                    "y3=y_setpoint(run2)", "y4=u_run2(right)" }, timeBase_s, "GainSchedulingEx_2");
            #endregion
            
            #region GainScheduling_Part3
            // building a gain-scheduling controller that is able to handle both regimes

            var pidParametersGS = new PidParameters()
            {
                Ti_s = 20,
                GainScheduling = new PidGainScheduling()
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

            var pidModelGS = new PidModel(pidParametersGS, timeBase_s, "PID_GS");

            var closedLoopSimGS_1 = new PlantSimulator(
                new List<ISimulatableModel> { pidModelGS, processModel });
            closedLoopSimGS_1.ConnectModels(pidModelGS, processModel);
            closedLoopSimGS_1.ConnectModels(processModel, pidModelGS);
            var inputDataGS1 = new TimeSeriesDataSet();
            inputDataGS1.Add(closedLoopSimGS_1.AddExternalSignal(pidModelGS, SignalType.Setpoint_Yset),
                  TimeSeriesCreator.Constant(20, 400));
            inputDataGS1.Add(closedLoopSimGS_1.AddExternalSignal(processModel, SignalType.Disturbance_D), 
                TimeSeriesCreator.Step(100, 400, 0, 10));
            inputDataGS1.CreateTimestamps(timeBase_s);
            // Gain-scheduling variable:
            closedLoopSimGS_1.ConnectModels(processModel,pidModelGS,(int)PidModelInputsIdx.GainScheduling);
           var isOk3 = closedLoopSimGS_1.Simulate(inputDataGS1,out var closedLoopDataGS_1);

            var closedLoopSimGS_2 = new PlantSimulator(
                new List<ISimulatableModel> { pidModelGS, processModel });
            closedLoopSimGS_2.ConnectModels(pidModelGS, processModel);
            closedLoopSimGS_2.ConnectModels(processModel, pidModelGS);

            var inputDataGS2 = new TimeSeriesDataSet();
            inputDataGS2.Add(closedLoopSimGS_2.AddExternalSignal(pidModelGS, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(70, 400));
            inputDataGS2.Add(closedLoopSimGS_2.AddExternalSignal(processModel, SignalType.Disturbance_D), 
                TimeSeriesCreator.Step(100, 400, 0, 10));
            inputDataGS2.CreateTimestamps(timeBase_s);
            // Gain-scheduling variable:
            closedLoopSimGS_2.ConnectModels(processModel, pidModelGS, (int)PidModelInputsIdx.GainScheduling);
            var isOk4 = closedLoopSimGS_2.Simulate(inputDataGS2, out var closedLoopDataGS_2);

            Plot.FromList(new List<double[]>
                {closedLoopDataGS_1.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 inputDataGS1.GetValues(pidModelGS.GetID(),SignalType.Setpoint_Yset),
                 closedLoopDataGS_1.GetValues(pidModelGS.GetID(),SignalType.PID_U),
                 closedLoopDataGS_2.GetValues(processModel.GetID(),SignalType.Output_Y_sim),
                 inputDataGS2.GetValues(pidModelGS.GetID(),SignalType.Setpoint_Yset),
                 closedLoopDataGS_2.GetValues(pidModelGS.GetID(),SignalType.PID_U)
                 },
                new List<string> { "y1=y_run1","y1=y_setpoint(run1)", "y2=u_run1(right)","y3=y-run2",
                    "y3=y_setpoint(run2)", "y4=u_run2(right)" }, timeBase_s, "GainSchedulingEx_3");
            #endregion

            Assert.IsTrue(isOk1);
            Assert.IsTrue(isOk2);
            Assert.IsTrue(isOk3);
            Assert.IsTrue(isOk4);
            return closedLoopDataGS_2;
        }

        [TestCase, Explicit]

        public void MinSelect_Ex()
        {
            var ret = MinSelect();
        }

        public TimeSeriesDataSet MinSelect(int N = 600)
        {
            #region MinSelect

            var processParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1 },
                U0 = new double[] { 50 },
                TimeDelay_s = 5,
                Bias = 50,
                Y_min =0,
                Y_max =100
            };
            var pidParameters1 = new PidParameters()
            {
                Kp = 0.5, //low-gain
                Ti_s = 250 // slow control (use buffer capacity, use less valve action)
            };
            // 
           // var pidParameters2 = pidParameters1;//
            var pidParameters2 = new PidParameters()
            {
                Kp = 2,//high-gain
                Ti_s = 15 // faster control(avoid carryover, aggressivley use valve when needed)
            };
            var process
                = new UnitModel(processParameters, timeBase_s, "Process");
            var pid1 = new PidModel(pidParameters1, timeBase_s, "PID1");
            var pid2 = new PidModel(pidParameters2, timeBase_s, "PID2");
            var minSelect = new Select(SelectType.MIN,"minSelect");

            var sim = new PlantSimulator(
                new List<ISimulatableModel> { process, pid1, pid2,minSelect });

            // tracking and min select-related
            sim.ConnectModels(process, pid1);
            sim.ConnectModels(process, pid2);
            sim.ConnectModels(pid1, minSelect,0);
            sim.ConnectModels(pid2, minSelect,1);
            string selectSignalID = sim.ConnectModels(minSelect, process);
            sim.ConnectSignal(selectSignalID,pid1,(int)PidModelInputsIdx.Tracking);
            sim.ConnectSignal(selectSignalID,pid2,(int)PidModelInputsIdx.Tracking);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(sim.AddExternalSignal(pid1, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(50, N));
            inputData.Add(sim.AddExternalSignal(pid2, SignalType.Setpoint_Yset), TimeSeriesCreator.Constant(70, N));
            inputData.Add(sim.AddExternalSignal(process, SignalType.Disturbance_D),
                new Vec().Add(TimeSeriesCreator.Sinus(5,50,timeBase_s,N),
                TimeSeriesCreator.TwoSteps(N*2/8,N*3/8,N,0,80,0))
                );
            inputData.CreateTimestamps(timeBase_s);
            var isOK = sim.Simulate(inputData, out var simResult);
            
            Plot.FromList(new List<double[]>
                {
                simResult.GetValues(process.GetID(),SignalType.Output_Y_sim),
                Vec<double>.Fill(85,N),
                inputData.GetValues(pid1.GetID(),SignalType.Setpoint_Yset),
                inputData.GetValues(pid2.GetID(),SignalType.Setpoint_Yset),
                simResult.GetValues(pid1.GetID(),SignalType.PID_U),
                simResult.GetValues(pid2.GetID(),SignalType.PID_U),
                simResult.GetValues(minSelect.GetID(),SignalType.SelectorOut),
                },
                new List<string> { "y1=y1","y1=yHH", "y1=y1_set", "y1=y2_set",
                "y3=u_pid1", "y3=u_pid2","y3=u_select" }, timeBase_s, "MinSelectEx");
            #endregion

            Assert.IsTrue(isOK);
            return simResult;
        }





    }
}
