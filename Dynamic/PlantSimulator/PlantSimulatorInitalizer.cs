using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TimeSeriesAnalysis;

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
        private Dictionary <string,List<string>> computationalLoopDict;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="simulator">simulator object that already includes connections,models and signals to be simulated</param>
        public PlantSimulatorInitalizer(PlantSimulator simulator)
        {
            this.simulator = simulator;
            var connections = simulator.GetConnections();
            (orderedSimulatorIDs,computationalLoopDict) = connections.InitAndDetermineCalculationOrderOfModels(simulator.GetModels());
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
        public bool ToSteadyStateAndEstimateDisturbances(ref TimeSeriesDataSet inputData, ref TimeSeriesDataSet simData,
            Dictionary<string,List<string>> compLoopDict)
        {
            // a dictionary that should contain the signalID of each "internal" simulated variable as a .Key,
            // the inital value will be calculated .Value, but is NaN unit calculated.
            var signalValuesAtT0 = new Dictionary<string, double>();

            EstimateDisturbances(ref inputData,ref simData,ref signalValuesAtT0);

            // estimated disturbances are in "simData", so include them in the "combined" dataset
            var combinedData = inputData.Combine(simData);

            // add external signals to simSignalValueDict
            foreach (var signalId in combinedData.GetSignalNames())
            {
                signalValuesAtT0.Add(signalId, combinedData.GetValues(signalId).First());
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
            isOk = BackwardsCalcFeedbackLoops(ref signalValuesAtT0,ref uninitalizedPID_IDs);
            if (!isOk)
                return false;

            isOk = InitComputationalLoops(compLoopDict, ref signalValuesAtT0, ref uninitalizedPID_IDs);

            // if will still be uninitalized pids if simulator contains a select-block, 
            // try to treat this now
            if (uninitalizedPID_IDs.Count > 0)
            {
                isOk = SelectLoopsCalc(combinedData.GetTimeBase(),ref signalValuesAtT0, ref uninitalizedPID_IDs);
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
                // all model outputs need to be simualted
                List<string> simulatedSignals = new List<string>();
                foreach (var modelName in simulator.modelDict.Keys)
                {
                    simulatedSignals.Add(simulator.modelDict[modelName].GetOutputID());
                }
                // in addtion, any signal addted to signalValuesAtT0, but not in inputData is to be simulated.
                // this includes disturbances
                foreach (string signalID in signalValuesAtT0.Keys)
                {
                    if (!inputData.ContainsSignal(signalID))
                    {
                        simulatedSignals.Add(signalID);
                    }
                }
                int? N = combinedData.GetLength();
                foreach (string signalID in signalValuesAtT0.Keys)
                {
                    // simData will contain copies of the 
                    if (!simulatedSignals.Contains(signalID))
                    {
                        continue;
                    }
                    simData.InitNewSignal(signalID, signalValuesAtT0[signalID], N.Value);
                }
            }
            if (simData.GetSignalNames().Length == 0)
            {
                Shared.GetParserObj().AddError("PlantSimulatorInitalizer initalized zero simulated variables");
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Try to initalize compulatational loops
        /// </summary>
        /// <param name="compLoopDict"> dictionary of computataio</param>
        /// <param name="signalValuesAtT0"></param>
        /// <param name="uninitalizedPID_IDs"></param>
        /// <returns></returns>
        private bool InitComputationalLoops(Dictionary<string, List<string>> compLoopDict, ref Dictionary<string, double> signalValuesAtT0, ref List<string> uninitalizedPID_IDs)
        {
            foreach (var loop in compLoopDict)
            {
                var loopModelList = loop.Value;

                var inputEachModel = new Dictionary<string, double[]>();
                var outputEachModel = new Dictionary<string, double>();
                var ouputNamesEachModel = new Dictionary<string,string>();
                // initialize the input-vector for each model with other signals 
                foreach (var modelId in loopModelList)
                {
                    int nInputs = simulator.modelDict[modelId].GetLengthOfInputVector();
                    double[] input = new double[nInputs];
                    int inputIdx = 0;
                    foreach (string inputID in simulator.modelDict[modelId].GetBothKindsOfInputIDs())
                    {
                        if (signalValuesAtT0.ContainsKey(inputID))
                        {
                            input[inputIdx] = signalValuesAtT0[inputID];
                        }
                        inputIdx++;
                    }
                    inputEachModel[modelId] = input;
                    outputEachModel[modelId] = 0;
                    ouputNamesEachModel.Add(modelId,simulator.modelDict[modelId].GetOutputID());
                }

                // try to iterativly solve the equation set in the stady-state to find a pair of 
                // inital output values that are steady-state for the given other inputs.
                int Niterations = 7;
                int curIt = 0;
                while (curIt < Niterations)
                {
                    int modelIdx = 0;
                    foreach (var modelId in loopModelList)
                    {
                        double[] u = inputEachModel[modelId];
                        var inputIds = simulator.modelDict[modelId].GetBothKindsOfInputIDs();
            
                        // for each outputName in the comp loop, see if this output is in the list of inputs to this model
                        foreach (var keyValuePair in ouputNamesEachModel)
                        {
                            var outputName = keyValuePair.Value;
                            var outputNameBelongsToModel = keyValuePair.Key;
                            int inputIdxOfOtherOutput = Array.IndexOf(inputIds, outputName);
                            if (inputIdxOfOtherOutput == -1)
                            {
                                continue;
                            }
                            u[inputIdxOfOtherOutput] = outputEachModel[outputNameBelongsToModel];
                        }
                        double? outputValue = simulator.modelDict[modelId].GetSteadyStateOutput(u);
                        if (outputValue.HasValue)
                        {
                            outputEachModel[modelId] = outputValue.Value;
                        }
                        else
                        {
                            Shared.GetParserObj().AddError("PlantSimulatorInitializer: computational loop init failed");
                            return false;
                        }
                        modelIdx++;
                    }
               //     Debug.WriteLine(outputEachModel.ElementAt(0).Value + " " + outputEachModel.ElementAt(1).Value);
                    curIt++;
                }

                // write values
                foreach (var modelId in loopModelList)
                {
                    var signalID = simulator.modelDict[modelId].GetOutputID();
                    var value = outputEachModel[modelId];
                    signalValuesAtT0[signalID] = value;
                }
            }
            return true;
        }


        /// <summary>
        /// For a plant, go through and find each plant/pid-controller and attempt to estimate the disturbance.
        /// For the disturbance to be estimateable,the inputs "u_meas" and the outputs "y_meas" for each "process" in
        /// each pid-process loop needs to be given in inputData.
        /// The estimated disturbance signal is addes to simData
        /// </summary>
        /// <param name="inputData">note that for closed loop systems u and y signals are removed(these are used to estimate disturbance, removing them triggers code in PlantSimulator to re-estimate them)</param>
        /// <param name="simData"></param>
        /// <returns>true if everything went ok, otherwise false</returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool EstimateDisturbances(ref TimeSeriesDataSet inputData, ref TimeSeriesDataSet simData,ref Dictionary<string, double> signalValuesAtT0)
        {
            // find all PID-controllers
            List<string> pidIDs = new List<string>();
            foreach (var model in simulator.modelDict)
            {
                if (model.Value.GetProcessModelType() == ModelType.PID)
                {
                    pidIDs.Add(model.Key);
                }
            }
            foreach (var pidID in pidIDs)
            {
                var upstreamModels = simulator.connections.GetAllUpstreamModels(pidID);
                var processId = upstreamModels.First();
                var isOK = simulator.SimulateSingleInternal(inputData, processId,
                    out TimeSeriesDataSet singleSimDataSetWithDisturbance);
                if (isOK)
                {
                    var estDisturbanceId = SignalNamer.EstDisturbance(processId);
                    if (singleSimDataSetWithDisturbance.ContainsSignal(estDisturbanceId))
                    {
                        var estDisturbance = singleSimDataSetWithDisturbance.GetValues(estDisturbanceId);
                        if (estDisturbance == null)
                            continue;
                        if ((new Vec()).IsAllNaN(estDisturbance))
                            continue;
                        // add signal if everything is ok.
                        simData.Add(estDisturbanceId, estDisturbance);
                        // todo: remove pid input pid-u and output y from inputdata(we want to re-estimate it, we have used it to estimate the disturbance)
                        // an alterntive to this would hav been to to alter the code in the plant simulator to add signals in simData output that are duplicates of signal names in inputData
                        // or better: make an internal "stripped" version of "inputData"
                        {
                            string y_meas_signal = simulator.modelDict[processId].GetOutputID();
                            signalValuesAtT0.Add(y_meas_signal,inputData.GetValue(y_meas_signal,0).Value);
                            inputData.Remove(y_meas_signal);

                            string u_pid_signal = simulator.modelDict[pidID].GetOutputID();
                            signalValuesAtT0.Add(u_pid_signal, inputData.GetValue(u_pid_signal, 0).Value);
                            inputData.Remove(u_pid_signal);
                        }
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
        private bool BackwardsCalcFeedbackLoops(ref Dictionary<string, double> simSignalValueDict,
            ref List<string> uninitalizedPID_IDs)
        {
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();
            for (int subSystem = orderedSimulatorIDs.Count - 1; subSystem > 0; subSystem--)
            {
           
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() == ModelType.PID)
                    continue;

                var modelId = model.GetID();

                string outputID = model.GetOutputID();
                if (outputID == null)
                {
                    Shared.GetParserObj().AddError("PlantSimulatorInitalizer could not init because model \""
                        + model.GetID() + "\" has no defined output.");
                    return false;
                }
                int numberOfPIDInputs = connections.GetUpstreamPIDIds(model.GetID(),modelDict).Count();
                if (numberOfPIDInputs > 1)
                {
                    continue; // this will be the case for select-blocks.
                }
                int numberOfExternalInputs = connections.GetModelExternalSignals(model.GetID(),simulator).Count();
                //
                string[] additiveInputs = model.GetAdditiveInputIDs();
                bool isXgiven = true;// y = x + sum(additive signals)
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
                /*  int[] uPIDIndices =
                      SignalNamer.GetIndexOfSignalType(model.GetModelInputIDs(), SignalType.PID_U);
                  int[] uInternalIndices =
                      SignalNamer.GetIndexOfSignalType(model.GetModelInputIDs(), SignalType.Output_Y_sim);
                  int[] uFreeIndices = Vec<int>.Concat(uPIDIndices, uInternalIndices);*/

                int[] uFreeIndices = connections.GetFreeIndices(model.GetID(),simulator);

                if (uFreeIndices.Length > 1)
                {
                    Shared.GetParserObj().AddError("PlantSimulatorInitalizer:unexpected/unsupported number of free inputs found during init.");
                    return false;
                }
                else if (uFreeIndices.Length == 1)
                {
                    int uPidIndex = uFreeIndices[0];//how do we know this is a PID-index?
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
                                Shared.GetParserObj().AddError("A signal upstream of "+ inputID+ " appears to be missing from input data?");
                                return false;
                            }
                        }
                    }
                    
                    double x0 = y0;
                    // todo:this code may be needed  if process is not in closed-loop control
                    /*if (additiveInputs != null)
                    {
                        foreach (string additiveInput in additiveInputs)
                        {
                            if (simSignalValueDict.ContainsKey(additiveInput))
                            {
                                x0 -= simSignalValueDict[additiveInput];
                            }
                        }
                    }*/
                    double? u0 = model.GetSteadyStateInput(x0, uPidIndex, givenInputValues);
                    if (u0.HasValue)
                    {
                        // write value
                        if (!simSignalValueDict.Keys.Contains(inputIDs[uPidIndex]))
                        {
                            simSignalValueDict.Add(inputIDs[uPidIndex], u0.Value);
                        }
                        // if the subprocess is in an inner loop of a cascade control, then 
                        // having determined "y" also now allows us to set the inital value for "yset"
                        bool hasUpstreamPID = connections.HasUpstreamPID(model.GetID(),modelDict);
                        if (hasUpstreamPID)
                        {
                            var pidIDs = connections.GetUpstreamPIDIds(model.GetID(), modelDict);
                            var pidID = pidIDs.First();
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
            (var orderedSimulatorIDs,var compLoodDict) = connections.InitAndDetermineCalculationOrderOfModels(modelDict);
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
                List<string> unsetInputs = new List<string>();
                int k = 0;
                foreach (string inputID in inputIDs)
                {
                    if (inputID == null)
                    {
                        Shared.GetParserObj().AddError("model "
                            + model.GetID() + " has an unexpected null input at index "+k);
                        return false;
                    }
                    if (simSignalValueDict.ContainsKey(inputID))
                    {
                        u0[k] = simSignalValueDict[inputID];
                    }
                    else
                    {
                        unsetInputs.Add(inputID);
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
                else
                {
/*                    Shared.GetParserObj().AddError(unsetInputs.First() + 
                        " not given, unable to initalize "+ model.GetID());
                    return false;*/
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
                var downstream = connections.GetAllDownstreamModelIDs(modelID);
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
                    if (connections.HasUpstreamPID(model.GetID(), modelDict))
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
                var modelIDs = connections.GetAllDownstreamModelIDs(ID);
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
            var processModelIDs = connections.GetAllDownstreamModelIDs(downstreamModelIDs.First());
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
            var selectOutput = selectModel.Iterate(selectInputs.ToArray(), timeBase_s)[0];
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
                double u0 = pidModel.Iterate(pidInputsU.ToArray(),timeBase_s)[0];
                simSignalValueDict[pidModel.GetOutputID()] = u0 + 0.5;
            }
            uninitalizedPID_IDs = new List<string>();//Todo:generalize

            return true;
        }

    }
}
