using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Intializes a plant simulator in the first data point 
    /// <para>
    /// Currently, only initalizing to steady-state is supported.
    /// </para>
    /// <para>
    /// By design choice, this class traverses the models by logic to initialize the plant model rather than
    /// using mathematical programming/matrix solvers.
    /// </para>
    /// </summary>
    public class PlantSimulatorInitalizer
    {
        private PlantSimulator simulator;
        private List<string> orderedSimulatorIDs;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simulator">simulator object that already includes connections,models and signals to be simulated</param>
        public PlantSimulatorInitalizer(PlantSimulator simulator)
        {
            this.simulator = simulator;
            var connections = simulator.GetConnections();
            orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();
        }
        /// <summary>
        /// Initalize the empty datasets to their steady-state values 
        /// <para>
        /// <list>
        /// <item><description> Forward-calculate all processes in series (not in any feedback-loops), where inputs to leftmost process is given</description></item>
        /// <item><description> All PID-loops must have a setpoint value set and all Y_meas are initalized to setpoint</description></item>
        /// <item><description> Then all subprocesses inputs inside feedback-loops U are back-calculated(right-to-left) to give steady-state Y_meas equal to Y_set</description></item>
        /// <item><description> Finally determine if there are ny serial processes downstream of any loops that can be calucated left-to-right</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="simData">simulation dataset containing only the external signals. The new simulated variables are added to this variable with initial values.</param>
        public bool ToSteadyState(TimeSeriesDataSet inputData, ref TimeSeriesDataSet simData)
        {
            // a dictionary that should contain the signalID of each "internal" simulated variable as a .Key,
            // the inital value will be calculated .Value, but is NaN unit calculated.
            var signalValuesAtT0 = new Dictionary<string, double>();
            // add external signals to simSiganlValueDIct
            foreach (var signalId in inputData.GetSignalNames())
            {
                signalValuesAtT0.Add(signalId, inputData.GetValues(signalId).First());
            }
            // forward-calculate the output for those systems where the inputs are given. 
            var isOk = ForwardCalcNonPID(ref signalValuesAtT0);
            if (!isOk)
                return false;
            // find all PID-controllers, and setting the "y" equal to "yset"
            var uninitalizedPID_IDs = SetPidControlledVariablesToSetpoints(ref signalValuesAtT0);
            if (uninitalizedPID_IDs == null)
            {
                return false;
            }
            // after the PID-controlled "Y" have been set, go through each "SubProcess" model and back-calculate 
            // the steady-state u for those subsytems that have a defined "Y".
            // assume that subsystems are ordered from left->right, go throught them right->left to propage pid-output backwards!
            isOk = BackwardsCalcNonPID(ref signalValuesAtT0,ref uninitalizedPID_IDs);
            if (!isOk)
                return false;
            // if will still be uninitalized pids if simulator contains a select-block, 
            // try to treat this now
            if (uninitalizedPID_IDs.Count > 0)
            {
                isOk = SelectLoopsCalc(inputData.GetTimeBase(),ref signalValuesAtT0, ref uninitalizedPID_IDs);
                if (!isOk)
                    return false;
            }
            // the final step is if there are any final processes "to the right of" the already initalized signals
            isOk = ForwardCalcNonPID(ref signalValuesAtT0);
            if (!isOk)
                return false;
            // check if any uninitalized pid-controllers remain
            if (uninitalizedPID_IDs.Count > 0)
            {
                Shared.GetParserObj().AddError("PlantSimulatorInitalizer failed to initalize controller:" + uninitalizedPID_IDs[0]);
                return false;
            }
            // last step is to actually write all the values, and create otherwise empty vector to be filled.
            {
                double nonYetSimulatedValue = Double.NaN;
                var externalInputSignals = simulator.GetExternalSignalIDs();
                int? N = inputData.GetLength();
                foreach (string signalID in signalValuesAtT0.Keys)
                {
                    // external signals are already rpesent in simData, do not add twice
                    if (!simData.ContainsSignal(signalID) && !inputData.ContainsSignal(signalID))
                    {
                        simData.Add(signalID, Vec<double>.Concat(new double[] { signalValuesAtT0[signalID] },
                        Vec<double>.Fill(nonYetSimulatedValue, N.Value - 1)));
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Initalizes sub-processes inside PID-feedback loops "from right-to-left"(backwards,finds  for a given y what is u)
        /// This method only supports a single PID-input per model.
        /// </summary>
        /// <param name="simSignalValueDict"></param>
        /// <param name="uninitalizedPID_IDs"></param>
        /// <returns></returns>
        private bool BackwardsCalcNonPID(ref Dictionary<string, double> simSignalValueDict,
            ref List<string> uninitalizedPID_IDs)
        {
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();
            for (int subSystem = orderedSimulatorIDs.Count - 1; subSystem > 0; subSystem--)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() == ModelType.PID)
                    continue;
                string outputID = model.GetOutputID();
                if (outputID == null)
                {
                    Shared.GetParserObj().AddError("PlantSimulatorInitalizer could not init because model \""
                        + model.GetID() + "\" has no defined output.");
                    return false;
                }
                int numberOfPIDInputs = SignalNamer.GetNumberOfSignalsOfType(model.GetModelInputIDs(), SignalType.PID_U);
                if (numberOfPIDInputs > 1)
                {
                    continue; // this will be the case for select-blocks.
                }
                int numberOfExternalInputs = SignalNamer.GetNumberOfSignalsOfType(model.GetModelInputIDs(), 
                    SignalType.External_U);
                string[] additiveInputs = model.GetAdditiveInputIDs();
                bool isXgiven = true;
                if (additiveInputs != null)
                {
                    foreach (string additiveInput in additiveInputs)
                    {
                        if (!simSignalValueDict.ContainsKey(outputID))
                            isXgiven = false;
                    }
                }
                bool canWeFindTheUnknownInput =
                    (model.GetModelInputIDs().Length - numberOfExternalInputs == 1) && isXgiven;
                if (!canWeFindTheUnknownInput)
                    continue;
                double y0 = Double.NaN;
                if (simSignalValueDict.ContainsKey(outputID))
                {
                    y0 = simSignalValueDict[outputID];
                }
                else
                    continue;//should not happen
                int[] uPIDIndices =
                    SignalNamer.GetIndexOfSignalType(model.GetModelInputIDs(), SignalType.PID_U);
                int[] uInternalIndices =
                    SignalNamer.GetIndexOfSignalType(model.GetModelInputIDs(), SignalType.Output_Y_sim);
                int[] uFreeIndices = Vec<int>.Concat(uPIDIndices, uInternalIndices);
                if (uFreeIndices.Length > 1)
                {
                    Shared.GetParserObj().AddError("PlantSimulatorInitalizer:unexpected/unsupported number of free inputs found during init.");
                    return false;
                }
                else if (uFreeIndices.Length == 1)
                {
                    int uPidIndex = uFreeIndices[0];
                    string[] inputIDs = model.GetBothKindsOfInputIDs();
                    double[] givenInputValues = new double[inputIDs.Length];
                    for (int i = 0; i < inputIDs.Length; i++)
                    {
                        if (i == uPidIndex)
                        {
                            givenInputValues[i] = Double.NaN;
                        }
                        else
                        {
                            string inputID = inputIDs[i];
                            if (simSignalValueDict.ContainsKey(inputID))
                            {
                                givenInputValues[i] = simSignalValueDict[inputID];
                            }
                            else
                            {
                                Shared.GetParserObj().AddError("error during system initalization.");
                                return false;
                            }
                        }
                    }
                    double? u0 = model.GetSteadyStateInput(y0, uPidIndex, givenInputValues);
                    if (u0.HasValue)
                    {
                        // write value
                        if (!simSignalValueDict.Keys.Contains(inputIDs[uPidIndex]))
                        {
                            simSignalValueDict.Add(inputIDs[uPidIndex], u0.Value);
                        }
                        // if the subprocess is in an inner loop of a cascade control, then 
                        // having determined "y" also now allows us to set the inital value for "yset"
                        bool hasUpstreamPID = connections.HasUpstreamPID(model.GetID());
                        if (hasUpstreamPID)
                        {
                            var pidID = connections.GetUpstreamPIDId(model.GetID());
                            if (uninitalizedPID_IDs.Contains(pidID))
                            {
                                string[] pidInputIDs = modelDict[pidID].GetBothKindsOfInputIDs();
                                string ySetpointID = pidInputIDs[(int)PidModelInputsIdx.Y_setpoint];
                                simSignalValueDict.Add(ySetpointID, y0);
                                uninitalizedPID_IDs.Remove(pidID);
                            }
                        }
                    }
                    else
                    {
                        Shared.GetParserObj().AddError("PlantSimulatorInitalizer:something went wrong when calculating steady-state inputs during init.");
                        return false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Calculates "from left-to-right"(forward, so from input to output) interconnected models where these are
        /// connected in series, where the first model in the chain only has external inputs.
        /// <para>
        /// This method does not initalize PID-controllers or inside control loops. 
        /// </para>
        /// </summary>
        /// <param name="simSignalValueDict"></param>
        /// <returns></returns>
        private bool ForwardCalcNonPID(ref Dictionary<string, double> simSignalValueDict)
        {
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();
            var orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();
            // forward-calculate the output for those systems where the inputs are given. 
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (simSignalValueDict.ContainsKey(model.GetOutputID()))
                {
                    continue;
                }
                if (model.GetProcessModelType() == ModelType.PID)
                {
                    continue;
                }

                string[] inputIDs = model.GetBothKindsOfInputIDs();
                bool areAllInputsGiven = true;
                double[] u0 = new double[inputIDs.Length];
                int k = 0;
                foreach (string inputID in inputIDs)
                {
                    if (inputID == null)
                    {
                        Shared.GetParserObj().AddError("PlantSimulatorInitalizer: model "
                            + model.GetID() + " has an unexpected null input");
                        return false;
                    }
                    if (simSignalValueDict.ContainsKey(inputID))
                    {
                        u0[k] = simSignalValueDict[inputID];
                    }
                    else
                    {
                        areAllInputsGiven = false;
                    }
                    k++;
                }
                if (areAllInputsGiven)
                {
                    double? outputValue = model.GetSteadyStateOutput(u0);
                    if (outputValue.HasValue)
                    {
                        string outputID = model.GetOutputID();
                        simSignalValueDict.Add(outputID, outputValue.Value);
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// Method that initalizes PID-controllers
        /// <para>
        /// It does not atttempt to initalize min-select controllers or cascade controllers, this is deferred to other methods
        /// </para>
        /// </summary>
        /// <param name="simSignalValueDict"></param>
        /// <returns>the IDs of the PID-controllers the method did not initalize</returns>
        private List<string> SetPidControlledVariablesToSetpoints(ref Dictionary<string, double> simSignalValueDict)
        {
            List<string> uninitalizedPID_IDs = new List<string>();
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var modelID = orderedSimulatorIDs.ElementAt(subSystem);
                var model = modelDict[modelID];
                if (model.GetProcessModelType() != ModelType.PID)
                    continue;
                var downstream = connections.GetDownstreamModelIDs(modelID);
                if (downstream.Count > 0)
                {
                    // this method does not handle Select PID-controllers
                    if (modelDict[downstream.First()].GetProcessModelType() == ModelType.Select)
                    {
                        uninitalizedPID_IDs.Add(modelID);
                        continue;
                    }
                }
                string[] inputIDs = model.GetModelInputIDs();
                string ySetpointID = inputIDs[(int)PidModelInputsIdx.Y_setpoint];
                string yMeasID = inputIDs[(int)PidModelInputsIdx.Y_meas];
                if (simSignalValueDict.ContainsKey(ySetpointID))
                {
                    double ySetPoint0 = simSignalValueDict[ySetpointID];
                    if (!simSignalValueDict.Keys.Contains(yMeasID))
                    {
                        simSignalValueDict.Add(yMeasID, ySetPoint0);
                    }
                }
                else if (simSignalValueDict.Keys.Contains(ySetpointID))
                {
                    //OK!, do nothing
                }
                else
                {
                    // if the pid is in a cascade, it cannot be initalized before all the outer-loop models have run.
                    if (connections.HasUpstreamPID(model.GetID()))
                    {
                        uninitalizedPID_IDs.Add(model.GetID());
                    }
                    else
                    {
                        Shared.GetParserObj().AddError("PlantSimulatorInitalizer: PID-controller has no setpoint given:"
                        + model.GetID());
                        return null;
                    }
                }
            }
            return uninitalizedPID_IDs;
        }
        /// <summary>
        /// Initalizes PID controllers inside min/max select loops
        /// </summary>
        /// <para>
        /// Which controller is active inside select loops depends on the value of the disturbance on the output of the modelled process
        /// </para>
        /// <param name="simSignalValueDict"></param>
        /// <param name="uninitalizedPID_IDs"></param>
        /// <returns></returns>
        private bool SelectLoopsCalc(double timeBase_s,ref Dictionary<string, double> simSignalValueDict, 
            ref List<string> uninitalizedPID_IDs)
        {
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();

            var downstreamModelIDs = new HashSet<string>();
            foreach (string ID in uninitalizedPID_IDs)
            {
                var modelIDs = connections.GetDownstreamModelIDs(ID);
                foreach (var modelID in modelIDs)
                {
                    downstreamModelIDs.Add(modelID);
                }
            }
            // TODO: generalize, a simulator could have more than one select loop
            // then we need code to sort PIDs by which block they connect to.
            if (downstreamModelIDs.Count() == 0 || downstreamModelIDs.Count() > 1)
            {
                Shared.GetParserObj().AddError("PlantSimulatorInitalizer: PID-configuration not yet supported");
                return false;
            }
            if (modelDict[downstreamModelIDs.First()].GetProcessModelType() != ModelType.Select)
            {
                Shared.GetParserObj().AddError("PlantSimulatorInitalizer: PID-configuration unrecognized(expected select block?):" +
                    downstreamModelIDs.First());
                return false;
            }
            // if you get this far, then all controllers are connected to the same select block. 
            var processModelIDs = connections.GetDownstreamModelIDs(downstreamModelIDs.First());
            if (processModelIDs.Count > 1)
            {
                Shared.GetParserObj().AddError("PlantSimulatorInitalizer: Multiple process models inside select loop not supported");
                return false;
            }
            var processModelID = processModelIDs.First();
            var processModel = modelDict[processModelID];
            var freeInputIdxs = new List<int>();
            int idx = 0;
            var processInputsList = new List<double>(); 
            foreach (var inputID in processModel.GetBothKindsOfInputIDs())
            {
                if (simSignalValueDict.ContainsKey(inputID))
                {
                    processInputsList.Add(simSignalValueDict[inputID]);
                    continue;
                }
                else
                {
                    processInputsList.Add(Double.NaN);
                }
                // input not found, it must be "free" 
                freeInputIdxs.Add(idx);
                idx++;
            }
            if (freeInputIdxs.Count > 1)
            {
                Shared.GetParserObj().AddError("PlantSimulatorInitalizer: Process has too many free inputs to initalize min-selet:"+ 
                    processModelID);
                return false;
            }
            var givenProcessModelInputs = processInputsList.ToArray();
            var distubanceIDs = modelDict[processModelID].GetAdditiveInputIDs();
            double disturbance0 = simSignalValueDict[distubanceIDs.First()];
            // back-calculate the pid input to the controlled process for the given setpoint and disturbance
            List<double> selectInputs = new List<double>();
            List<double> pidSetpoints = new List<double>();
            foreach (string pidID in uninitalizedPID_IDs)
            {
                var pidModel = modelDict[pidID];
                string[] pidInputIDs = pidModel.GetModelInputIDs();
                string ySetpointID = pidInputIDs[(int)PidModelInputsIdx.Y_setpoint];
                if (!simSignalValueDict.ContainsKey(ySetpointID))
                {
                    Shared.GetParserObj().AddError("PlantSimulatorInitalizer:missing setpoint signal found while initalizing select loop:");
                    return false;
                }
                double ySetpointForCurrentPID0 = simSignalValueDict[ySetpointID];
                pidSetpoints.Add(ySetpointForCurrentPID0);
                double? u0 = processModel.GetSteadyStateInput(ySetpointForCurrentPID0, 
                    freeInputIdxs.First(), givenProcessModelInputs);
                if (u0.HasValue)
                {
                    selectInputs.Add(u0.Value);
                }
            }
            // run inputs through select block
            var selectModel = modelDict[downstreamModelIDs.First()];
            var selectOutput = selectModel.Iterate(selectInputs.ToArray(), timeBase_s);
            var indOfActivePID = (new Vec()).FindValues(selectInputs.ToArray(),
                selectOutput, VectorFindValueType.Equal);
            var y0 = pidSetpoints.ElementAt(indOfActivePID.First());
            // now we know what the select output will give.
            simSignalValueDict[selectModel.GetOutputID()] = selectOutput;
            string activePID_ID = uninitalizedPID_IDs[indOfActivePID.First()];
            simSignalValueDict[processModel.GetOutputID()] = y0;
            // add active PID output signal to steady-state values and remove from uninitalized list
            simSignalValueDict[modelDict[activePID_ID].GetOutputID()] = 
                selectInputs.ElementAt(indOfActivePID.First());
            uninitalizedPID_IDs.Remove(activePID_ID);
            // set initial u0 for inactive PIDs
            foreach (string pidID in uninitalizedPID_IDs)
            {
                var pidModel = modelDict[pidID];
                string[] pidInputIDs = pidModel.GetModelInputIDs();
                string ySetpointID = pidInputIDs[(int)PidModelInputsIdx.Y_setpoint];
                double[] pidInputsU = new double[pidInputIDs.Length];
                pidInputsU[(int)PidModelInputsIdx.Y_meas] = y0;
                pidInputsU[(int)PidModelInputsIdx.Y_setpoint] = simSignalValueDict[ySetpointID];
                if (pidInputIDs.Length-1>= (int)PidModelInputsIdx.Tracking)
                {
                    pidInputsU[(int)PidModelInputsIdx.Tracking] = selectOutput;//todo:generalize
                }
                else
                {
                    Shared.GetParserObj().AddError("PlantSimulatorInitalizer:PID input vector too short, no tracking?");
                    return false;
                }
                pidModel.WarmStart(pidInputsU, y0);
                double u0 = pidModel.Iterate(pidInputsU.ToArray(),timeBase_s);
                simSignalValueDict[pidModel.GetOutputID()] = u0 + 0.5;
            }
            uninitalizedPID_IDs = new List<string>();//Todo:generalize

            return true;
        }
    }
}
