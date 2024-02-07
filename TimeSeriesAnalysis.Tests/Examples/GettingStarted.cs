using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using TimeSeriesAnalysis.Dynamic;
using Microsoft.VisualStudio.CodeCoverage;
using System.Diagnostics;
using System.Reflection;

namespace TimeSeriesAnalysis._Examples
{
    /// <summary>
    /// A number of "getting started" examples implemented as explicit unit tests, all of which are described further in the documentation.
    /// <para>
    /// The intention is that new users could start learning about this project by examining these examples.
    /// </para>
    /// </summary>
    [TestFixture]
    class _GettingStarted
    {
        [SetUp]
        public void SetUp()
        {
            Shared.GetParserObj().EnableDebugOutput();
        }

        [Test, Explicit]
    
        public void Ex1_hello_world()
        {
            #region ex_1
            Shared.GetParserObj().EnableConsoleOutput();
            int timeBase_s = 1;
            double filterTc_s = 10;

            double[] input = TimeSeriesCreator.Step(11, 60, 0, 1);

            LowPass lp = new LowPass(timeBase_s);
            var output = lp.Filter(input, filterTc_s);

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> { input, output},
                new List<string> { "y1=V1_input","y1=V2_output"}, timeBase_s, "ex1_hello_world",
                new DateTime(2020, 1, 1, 0, 0, 0));
            Shared.DisablePlots();
            #endregion
        }


        [Test, Explicit]

        public void Ex2_linear_regression()
        {
            #region ex_2
            double[] true_gains = {1,2,3};
            double true_bias = 5;
            double noiseAmplitude = 0.1;

            double[] u1 = TimeSeriesCreator.Step(11, 61, 0, 1);
            double[] u2 = TimeSeriesCreator.Step(31, 61, 1, 2);
            double[] u3 = TimeSeriesCreator.Step(21, 61, 1,-1);

            double[] y = new double[u1.Length];
            double[] noise = (new Vec()).Multiply(Vec.Rand(u1.Length, -1,1,0),noiseAmplitude);
            for (int k = 0; k < u1.Length; k++)
            {
                y[k] = true_gains[0] * u1[k] 
                    + true_gains[1] * u2[k] 
                    + true_gains[2] * u3[k] 
                    + true_bias + noise[k]; 
            }

            Plot.FromList(new List<double[]> { y, u1, u2, u3 }, 
                new List<string> { "y1=y1", "y3=u1", "y3=u2", "y3=u3" }, 1);

            double[][] U = new double[][] { u1, u2, u3 };

            var results = (new Vec()).RegressUnRegularized(y, U);

            TestContext.WriteLine(Vec.ToString(results.Param, 3));
            TestContext.WriteLine(SignificantDigits.Format(results.Rsq, 3));

            Shared.EnablePlots();
            Plot.FromList(new List<double[]>() { y, results.Y_modelled },
                new List<string>() { "y1=y_meas", "y1=y_mod" }, 1);
            Shared.DisablePlots();
            #endregion
        }


        [Test, Explicit]

        public void Ex3_filters()
        {
            #region ex_3
            int timeBase_s = 1;
            int nStepsDuration = 2000;

            var sinus1 = TimeSeriesCreator.Sinus(10, 400, timeBase_s, nStepsDuration);
            var sinus2 = TimeSeriesCreator.Sinus( 1,  25, timeBase_s, nStepsDuration);
            var y_sim = (new Vec()).Add(sinus1,sinus2);
       
            var lpFilter = new LowPass(timeBase_s);
            var lpFiltered = lpFilter.Filter(y_sim, 40,1);

            var hpFilter = new HighPass(timeBase_s);
            var hpFiltered = hpFilter.Filter(y_sim, 3,1);

            Shared.EnablePlots();
            Plot.FromList(new List<double[]> { y_sim, lpFiltered, hpFiltered },
                new List<string> { "y1=y","y3=y_lowpass","y3=y_highpass" }, timeBase_s);
            Shared.DisablePlots();
            #endregion
        }


        [Test, Explicit]

        public void Ex4_system_identification()
        {
            #region ex_4
            Shared.EnablePlots();

            // create unit model and plant simulator           
            var parameters = new UnitParameters
            {
                TimeConstant_s = 5,
                LinearGains = new double[] {1,2},
                TimeDelay_s = 2,
                Bias = 5
            };
            var processModel = new UnitModel(parameters,"processModel");
            var sim = new PlantSimulator(new List<ISimulatableModel> { processModel });
            // create "synthetic" dataset and add the synthetic signals to plant simulator
            int timeBase_s = 1;
            double[] u1 = TimeSeriesCreator.Step(3, 100, 1, 2);
            double[] u2 = TimeSeriesCreator.Step(30, 100, 2, 1);
            //
            var inputData = new TimeSeriesDataSet();
            inputData.Add(sim.AddExternalSignal(processModel, SignalType.External_U, (int)INDEX.FIRST), u1);
            inputData.Add(sim.AddExternalSignal(processModel, SignalType.External_U, (int)INDEX.SECOND), u2);
            inputData.CreateTimestamps(timeBase_s);
            // simulate the plant
            var isOK =  sim.Simulate(inputData, out var simData);
             Assert.IsTrue(isOK);


            // create a "unit data set" and try to estimate the paramters of processModel from the data alone
            var unitDataSet = new UnitDataSet();
            unitDataSet.U = Array2D<double>.CreateFromList(new List<double[]> { u1, u2 });
            unitDataSet.CreateTimeStamps(timeBase_s);
            unitDataSet.Y_meas = simData.GetValues(processModel.GetID(), SignalType.Output_Y);
            // plot results of simulation
         //   Plot.FromList(new List<double[]> { simData.GetValues(processModel.GetID(), SignalType.Output_Y), u1, u2 },
          //     new List<string> { "y1=y_meas", "y3=u1", "y3=u2" }, timeBase_s, "ex4_data");

            var identifiedModel = UnitIdentifier.Identify(ref unitDataSet, new FittingSpecs());
          
            // compare measured y and y from the identifiedModel.
             Plot.FromList(new List<double[]> { identifiedModel.GetFittedDataSet().Y_meas, 
                 identifiedModel.GetFittedDataSet().Y_sim },
                 new List<string> { "y1=y_meas", "y1=y_sim"}, timeBase_s, "ex4_results");

            Console.WriteLine(identifiedModel.ToString());

            // compare dynamic to static identification
            var regResults = (new Vec()).Regress(unitDataSet.Y_meas, unitDataSet.U);
      
            Plot.FromList(new List<double[]> { identifiedModel.GetFittedDataSet().Y_meas,
                identifiedModel.GetFittedDataSet().Y_sim,regResults.Y_modelled },
                new List<string> { "y1=y_meas", "y1=y_dynamic","y1=y_static" }, timeBase_s,
                 "ex4_static_vs_dynamic");
            Shared.DisablePlots();

            Console.WriteLine("static model gains:" + Vec.ToString(regResults.Gains,3));
            #endregion
        }


        [Test, Explicit]

        public void Ex5_feedback_loop_setpoint_change()
        {
            #region ex_5
            UnitParameters modelParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1, 2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            UnitModel processModel
                = new UnitModel(modelParameters, "SubProcess1");
            var pidParameters = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel = new PidModel(pidParameters, "PID1");
            var sim = new PlantSimulator(
                new List<ISimulatableModel> { processModel, pidModel });
            sim.ConnectModels(processModel, pidModel);
            sim.ConnectModels(pidModel, processModel, (int)INDEX.FIRST);
            // create synthetic input data (normally you would get this data from the real-world)
            double timeBase_s = 1;
            int N = 500;
            var inputData = new TimeSeriesDataSet();
            inputData.Add(sim.AddExternalSignal(pidModel, SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(50, N));
            inputData.Add(sim.AddExternalSignal(processModel, SignalType.External_U, (int)INDEX.SECOND),
                TimeSeriesCreator.Step(N / 2, N, 0, 1));
            inputData.CreateTimestamps(timeBase_s);
            // simulate model over the 
            var isOk = sim.Simulate(inputData, out var simData);


            // plot result
            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                simData.GetValues(pidModel.GetID(),SignalType.PID_U)
                },
                new List<string> { "y1=y_sim",  "y2=u_external", "y3=u_pid" },
                timeBase_s, "ex5_results");
            Shared.DisablePlots();
            #endregion
        }


        [Test, Explicit]
        #region ex_6
        public void Ex6_feedback_loop_setpoint_and_disturbance_change()
        {
            // define two unit-model and a "PlantSimulator" where the two are connected
            UnitParameters modelParameters = new UnitParameters
            {
                TimeConstant_s = 10,
                LinearGains = new double[] { 1,2 },
                TimeDelay_s = 0,
                Bias = 5
            };
            UnitModel processModel 
                = new UnitModel(modelParameters, "SubProcess1");
            var pidParameters = new PidParameters()
            {
                Kp = 0.5,
                Ti_s = 20
            };
            var pidModel = new PidModel(pidParameters, "PID1");
            var sim = new PlantSimulator (
                new List<ISimulatableModel> { processModel,pidModel   });
            sim.ConnectModels(processModel,pidModel);
            sim.ConnectModels(pidModel,processModel,(int)INDEX.FIRST);
            // create synthetic input data (normally you would get this data from the real-world)
            double timeBase_s = 1;
            int N = 500;
            var inputData = new TimeSeriesDataSet();
            inputData.Add(sim.AddExternalSignal(processModel,SignalType.Disturbance_D),
                TimeSeriesCreator.Step(N/4,N,0,1));
            inputData.Add(sim.AddExternalSignal(pidModel,SignalType.Setpoint_Yset),
                TimeSeriesCreator.Constant(50,N));
            inputData.Add(sim.AddExternalSignal(processModel,SignalType.External_U, (int)INDEX.SECOND),
                TimeSeriesCreator.Step(N/2,N,0,1));
            inputData.CreateTimestamps(timeBase_s);
            // simulate model over the 
            var isOk = sim.Simulate(inputData,out var simData);


            // plot result
            Shared.EnablePlots();
            Plot.FromList(new List<double[]> {
                simData.GetValues(processModel.GetID(),SignalType.Output_Y),
                inputData.GetValues(processModel.GetID(),SignalType.Disturbance_D),
                inputData.GetValues(processModel.GetID(),SignalType.External_U,(int)INDEX.SECOND),
                simData.GetValues(pidModel.GetID(),SignalType.PID_U)
                },
                new List<string> { "y1=y_sim", "y2=disturbance", "y2=u_external","y3=u_pid"  },
                timeBase_s, "ex6_results");
            Shared.DisablePlots();

            // optional: serialize
            //sim.Serialize("example6");
            //inputData.ToCsv("example6.csv");

        }
        #endregion











    }
}
