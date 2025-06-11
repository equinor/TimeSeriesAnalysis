using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Convenience functions for using PlantSimulator
    /// </summary>
    public class PlantSimulatorHelper
    {
        /// <summary>
        /// Create a PlantSimulator and TimeSeriesDataSet from a UnitDataSet, PidModel and UnitModel to do closed-loop simulations
        /// <para>
        /// The feedback loop has NO disturbance signal added, but this can be added to the returned PlantSimulator as needed.
        /// </para>
        /// </summary>
        /// <param name="unitDataSet"></param>
        /// <param name="pidModel"></param>
        /// <param name="unitModel"></param>
        /// <param name="pidInputIdx"></param>
        /// <returns>a simulator object and a dataset object that is ready to be simulated with Simulate() </returns>
        public static (PlantSimulator, TimeSeriesDataSet) CreateFeedbackLoopNoDisturbance(UnitDataSet unitDataSet, PidModel pidModel,
            UnitModel unitModel, int pidInputIdx = 0)
        {
            var plantSim = new PlantSimulator(
                new List<ISimulatableModel> { pidModel, unitModel });
            var signalId1 = plantSim.ConnectModels(unitModel, pidModel);
            var signalId2 = plantSim.ConnectModels(pidModel, unitModel, pidInputIdx);

            var inputData = new TimeSeriesDataSet();
            inputData.Add(signalId1, (double[])unitDataSet.Y_meas.Clone());

            for (int curColIdx = 0; curColIdx < unitDataSet.U.GetNColumns(); curColIdx++)
            {
                if (curColIdx == pidInputIdx)
                {
                    inputData.Add(signalId2, (double[])unitDataSet.U.GetColumn(pidInputIdx).Clone());
                }
                else
                {
                    inputData.Add(plantSim.AddExternalSignal(unitModel, SignalType.External_U, curColIdx),
                        (double[])unitDataSet.U.GetColumn(curColIdx).Clone());
                }
            }
            inputData.Add(plantSim.AddExternalSignal(pidModel, SignalType.Setpoint_Yset), (double[])unitDataSet.Y_setpoint.Clone());
            inputData.CreateTimestamps(unitDataSet.GetTimeBase(),unitDataSet.GetNumDataPoints());
            inputData.SetIndicesToIgnore(unitDataSet.IndicesToIgnore);
            return (plantSim, inputData);
        }

        /// <summary>
        /// Create a feedback loop, where the process model has an additive disturbance that is to be estimated.
        /// </summary>
        /// <param name="unitDataSet"></param>
        /// <param name="pidModel"></param>
        /// <param name="unitModel"></param>
        /// <param name="pidInputIdx"></param>
        /// <returns>a simulator object and a dataset object that is ready to be simulated with Simulate() </returns>
        public static (PlantSimulator, TimeSeriesDataSet) CreateFeedbackLoopWithEstimatedDisturbance(UnitDataSet unitDataSet, PidModel pidModel,
             UnitModel unitModel, int pidInputIdx = 0)
        {
            // vital that signal follows naming convention, otherwise it will not be estimated, but should be provided.
            unitModel.AddSignalToOutput(SignalNamer.EstDisturbance(unitModel));
            (var sim, var data) = CreateFeedbackLoopNoDisturbance(unitDataSet, pidModel, unitModel, pidInputIdx);
            return (sim, data);
        }

        /// <summary>
        /// Returns a unit data set for a given UnitModel.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="pidModel"></param>
        /// <param name="unitModel"></param>
        /// <returns>a tuple with a bool indicating if it was a success as item1, and the dataset as item2</returns>
        public static (bool,UnitDataSet) GetUnitDataSetForLoop(TimeSeriesDataSet inputData, PidModel pidModel, UnitModel unitModel)
        {
            UnitDataSet dataset = new UnitDataSet();
            var inputIDs = unitModel.GetModelInputIDs();
            dataset.U = new double[inputData.GetLength().Value, inputIDs.Length];
            bool success = true;
            dataset.Times = inputData.GetTimeStamps();
        
            var outputID = unitModel.GetOutputID();
            if (inputData.ContainsSignal(outputID))
                dataset.Y_meas = inputData.GetValues(outputID);
            else
                success = false;
            for (int inputIDidx = 0; inputIDidx < inputIDs.Length; inputIDidx++)
            {
                var inputID = inputIDs[inputIDidx];
                if (inputData.ContainsSignal(inputID))
                {
                    var curCol = inputData.GetValues(inputID);
                    dataset.U.WriteColumn(inputIDidx, curCol);
                }
                else
                {
                    success = false;
                }
            }

            var setpointID = pidModel.GetModelInputIDs().ElementAt((int)PidModelInputsIdx.Y_setpoint);
            if (inputData.ContainsSignal(setpointID))
                dataset.Y_setpoint = inputData.GetValues(setpointID);
            else
                success = false;

            return (success,dataset);
        }

        /// <summary>
        /// Simulate a single model to get the output including any additive inputs.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="model"></param>
        /// <param name="simData"></param>
        /// <returns></returns>
        public static bool SimulateSingle(TimeSeriesDataSet inputData, ISimulatableModel model, out TimeSeriesDataSet simData)
        {
            PlantSimulator plant = new PlantSimulator(new List<ISimulatableModel> { model });
            return plant.Simulate(inputData, out simData);
        }


        /// <summary>
        /// Simulates a single model given a unit data set
        /// </summary>
        /// <param name="unitData"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static (bool, double[]) SimulateSingle(UnitDataSet unitData, ISimulatableModel model)
        {
            return SimulateSingleUnitDataWrapper(unitData, model);
        }

        /// <summary>
        /// Simulates a single model for a unit dataset and adds the output to unitData.Y_meas of the unitData, optionally with noise
        /// </summary>
        /// <param name="unitData">the dataset to be simualted over, and where the Y_meas is updated with result</param>
        /// <param name="model">the model to be simulated</param>
        /// <param name="noiseAmplitude">the amplitude of noise to be added to Y_meas</param>
        /// <param name="noiseSeed">a seed value of the randm noise(specify so that tests are repeatable)</param>
        /// <returns></returns>
        public static bool SimulateSingleToYmeas(UnitDataSet unitData, ISimulatableModel model, double noiseAmplitude = 0,
             int noiseSeed = 123)
        {
            (bool isOk, double[] y_proc) = SimulateSingleUnitDataWrapper(unitData, model);

            if (noiseAmplitude > 0)
            {
                // use a specific seed here, to avoid potential issues with "random unit tests" and not-repeatable
                // errors.
                Random rand = new Random(noiseSeed);
                for (int k = 0; k < y_proc.Count(); k++)
                {
                    y_proc[k] += (rand.NextDouble() - 0.5) * 2 * noiseAmplitude;
                }
            }
            unitData.Y_meas = y_proc;
            return isOk;
        }

        /// <summary>
        /// Simulates a single model for a unit dataset and adds the output to unitData.Y_meas of the unitData, optionally with noise
        /// </summary>
        /// <param name="unitData">the dataset to be simualted over, and where the Y_meas is updated with result</param>
        /// <param name="model">the model to be simulated</param>
        /// <returns></returns>
        public static (bool, double[]) SimulateSingleToYsim(UnitDataSet unitData, ISimulatableModel model)
        {
            (bool isOk, double[] y_proc) = SimulateSingleUnitDataWrapper(unitData, model);
            unitData.Y_sim = y_proc;
            return (isOk, y_proc);
        }


        /// <summary>
        /// Simulate single model based on a unit data set
        /// 
        /// This is a convenience function that creates a TimeSeriesDataSet, sets default names in the model and dataset that match based on unitDataset 
        /// The output is returned directly.
        /// 
        /// Optionally, the result can be written to y_meas or y_sim in unitdata.
        /// </summary>
        /// <param name="unitData">contains a unit data set that must have U filled, Y_sim will be written here</param>
        /// <param name="model">model to simulate</param>
        /// <returns>a tuple, first aa true if able to simulate, otherwise false, second is the simulated time-series "y_proc" without any additive </returns>
        static private (bool, double[]) SimulateSingleUnitDataWrapper(UnitDataSet unitData, ISimulatableModel model)
        {
            string defaultOutputName = "output";
            var inputData = new TimeSeriesDataSet();
            var singleModelName = "SimulateSingle";
            var modelCopy = model.Clone(singleModelName);

            if (unitData.Times != null)
                inputData.SetTimeStamps(unitData.Times.ToList());
            else
            {
                inputData.CreateTimestamps(unitData.GetTimeBase(),unitData.GetNumDataPoints());
            }

            if (model.GetProcessModelType() == ModelType.PID)
            {
                // todo: should ideally use (int)PidModelInputsIdx
                inputData.Add("Y", unitData.Y_meas);
                inputData.Add("Y_setpoint", unitData.Y_setpoint);
                modelCopy.SetInputIDs((new List<string> { "Y", "Y_setpoint" }).ToArray());
                inputData.Add(defaultOutputName, unitData.U.GetColumn(0));
                modelCopy.SetOutputID(defaultOutputName);
            }
            else
            {
                var uNames = new List<string>();
                for (int colIdx = 0; colIdx < unitData.U.GetNColumns(); colIdx++)
                {
                    var uName = "U" + colIdx;
                    inputData.Add(uName, unitData.U.GetColumn(colIdx));
                    uNames.Add(uName);
                }
                modelCopy.SetInputIDs(uNames.ToArray());
                // unless pid, then output is Y_meas
                inputData.Add(defaultOutputName, unitData.Y_meas);
                modelCopy.SetOutputID(defaultOutputName);
            }

            inputData.SetIndicesToIgnore(unitData.IndicesToIgnore);

            var isOk = PlantSimulatorHelper.SimulateSingle(inputData, modelCopy, out var simData);

            if (!isOk)
                return (false, null);

            if (model.GetProcessModelType() == ModelType.PID)
            {
                double[] u_sim = null;
                u_sim = simData.GetValues(defaultOutputName);
                return (isOk, u_sim);
            }
            else
            {
                double[] y_proc = null;
                double[] y_sim = null;

                y_sim = simData.GetValues(defaultOutputName);
                if (simData.ContainsSignal(singleModelName))
                {
                    y_proc = simData.GetValues(singleModelName);
                }
                else
                {
                    y_proc = y_sim;
                }
                return (isOk, y_proc);
            }
        }



    }
}
