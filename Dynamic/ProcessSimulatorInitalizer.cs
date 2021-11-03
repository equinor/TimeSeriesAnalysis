using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    public class ProcessSimulatorInitalizer
    {
        ProcessSimulator simulator;
        List<string> orderedSimulatorIDs;

        public ProcessSimulatorInitalizer(ProcessSimulator simulator)
        {
            this.simulator = simulator;
            var connections = simulator.GetConnections();
            orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();

        }

        private bool ForwardCalculate(ref Dictionary<string, double> simSignalValueDict)
        {
            var externalInputSignals = simulator.GetExternalSignals();
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();

            var orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();

            // forward-calculate the output for those systems where the inputs are given. 
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.SubProcess)
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
                        Shared.GetParserObj().AddError("ProcessSimulator.ToSteadyState(): model "
                            + model.GetID() + " has an unexpected null input");
                        return false;
                    }
                    if (externalInputSignals.ContainsSignal(inputID))
                    {
                        u0[k] = externalInputSignals.GetValues(inputID).First();
                    }
                    else if (simSignalValueDict.ContainsKey(inputID))
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

            return false;
        }


        /// <summary>
        /// Initalize the empty datasets to their steady-state values.
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
        
        public bool ToSteadyState( ref TimeSeriesDataSet simData)
        {
            //   int nTotalInputs = 0;

            var externalInputSignals = simulator.GetExternalSignals();
            var modelDict = simulator.GetModels();
            var connections = simulator.GetConnections();

            //    bool doesSimulationIncludeSelect = DoesSimulationIncludeSelect();

            int? N = externalInputSignals.GetLength();
        

            var uninitalizedPID_IDs = new List<string>();

            // a dictionary that should contain the signalID of each "internal" simulated variable as a .Key,
            // the inital value will be calculated .Value, but is NaN unit calculated.
            var simSignalValueDict = new Dictionary<string, double>();


            // forward-calculate the output for those systems where the inputs are given. 
            var isOk = ForwardCalculate(ref simSignalValueDict);
            if (!isOk)
                return false;

            /*for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.SubProcess)
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
                        Shared.GetParserObj().AddError("ProcessSimulator.ToSteadyState(): model "
                            + model.GetID() + " has an unexpected null input");
                        return false;
                    }
                    if (externalInputSignals.ContainsSignal(inputID))
                    {
                        u0[k] = externalInputSignals.GetValues(inputID).First();
                    }
                    else if (simSignalValueDict.ContainsKey(inputID))
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
            */
            // find all PID-controllers, and setting the "y" equal to "yset"
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.PID)
                    continue;

                string[] inputIDs = model.GetModelInputIDs();
                string ySetpointID = inputIDs[(int)PIDModelInputsIdx.Y_setpoint];
                string yMeasID = inputIDs[(int)PIDModelInputsIdx.Y_meas];
                if (externalInputSignals.ContainsSignal(ySetpointID))
                {
                    double ySetPoint0 = externalInputSignals.GetValues(ySetpointID)[0];
                    if (!simSignalValueDict.Keys.Contains(yMeasID) &&
                        !externalInputSignals.ContainsSignal(yMeasID))
                    {
                        simSignalValueDict.Add(yMeasID, ySetPoint0);
                    }
                }
                else if (simSignalValueDict.Keys.Contains(ySetpointID))
                {
                    //OK!
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
                        Shared.GetParserObj().AddError("ProcessSimulator.InitToSteadyState(): PID-controller has no setpoint given:"
                        + model.GetID());
                        return false;
                    }
                }
            }
            // after the PID-controlled "Y" have been set, go through each "SubProcess" model and back-calculate 
            // the steady-state u for those subsytems that have a defined "Y".
            // assume that subsystems are ordered from left->right, go throught them right->left to propage pid-output backwards!
            for (int subSystem = orderedSimulatorIDs.Count - 1; subSystem > 0; subSystem--)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.SubProcess)
                    continue;

                string outputID = model.GetOutputID();

                if (outputID == null)
                {
                    Shared.GetParserObj().AddError("ProcessSimulator could not init because model \""
                        + model.GetID() + "\" has no defined output.");
                    return false;
                }
                bool isYgiven = simSignalValueDict.ContainsKey(outputID) ||
                    externalInputSignals.ContainsSignal(outputID);

                int numberOfPIDInputs = SignalNamer.GetNumberOfSignalsOfType(model.GetModelInputIDs(), SignalType.PID_U);
                if (numberOfPIDInputs > 1)
                {
                    Shared.GetParserObj().AddError("currently only one pid-input per system is supported(to be address in a future update?)");
                    return false;
                }
                // TODO: what about additive inputs here? This code is likely not general!
                // 
                int numberOfExternalInputs = SignalNamer.GetNumberOfSignalsOfType(model.GetModelInputIDs(), SignalType.External_U);
                // todo: missing step here to calculate "x" from "y" based on additive inputs!
                string[] additiveInputs = model.GetAdditiveInputIDs();
                bool isXgiven = true;
                if (additiveInputs != null)
                {
                    foreach (string additiveInput in additiveInputs)
                    {
                        if (!simSignalValueDict.ContainsKey(outputID) &&
                        !externalInputSignals.ContainsSignal(outputID))
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
                else if (externalInputSignals.ContainsSignal(outputID))
                {
                    y0 = externalInputSignals.GetValues(outputID)[0];
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
                    Shared.GetParserObj().AddError("unexpected/unsupported number of free inputs found during init.");
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
                            if (externalInputSignals.ContainsSignal(inputID))
                            {
                                givenInputValues[i] = externalInputSignals.GetValues(inputID).First();
                            }
                            else if (simSignalValueDict.ContainsKey(inputID))
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
                        if (!simSignalValueDict.Keys.Contains(inputIDs[uPidIndex]) &&
                            !externalInputSignals.ContainsSignal(inputIDs[uPidIndex]))
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
                                string ySetpointID = pidInputIDs[(int)PIDModelInputsIdx.Y_setpoint];
                                simSignalValueDict.Add(ySetpointID, y0);
                                uninitalizedPID_IDs.Remove(pidID);
                            }
                        }
                    }
                    else
                    {
                        Shared.GetParserObj().AddError("something went wrong when calculating steady-state inputs during init.");
                        return false;
                    }
                }
            }

            // the final step is if there are any final processes "to the right of" the already initalized signals
            for (int subSystemIdx = 0; subSystemIdx < orderedSimulatorIDs.Count; subSystemIdx++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystemIdx)];
                if (simSignalValueDict.ContainsKey(model.GetOutputID()))
                {
                    continue;
                }
                bool allInputValuesKnown = true;
                var inputIDs = model.GetBothKindsOfInputIDs();
                double[] u0 = new double[model.GetLengthOfInputVector()];
                int k = 0;
                foreach (string inputId in inputIDs)
                {
                    bool valueFound = false;
                    if (simSignalValueDict.Keys.Contains(inputId))
                    {
                        valueFound = true;
                        u0[k] = simSignalValueDict[inputId];
                    }
                    else if (externalInputSignals.ContainsSignal(inputId))
                    {
                        valueFound = true;
                        u0[k] = externalInputSignals.GetValues(inputId).First();
                    }
                    if (!valueFound)
                    {
                        allInputValuesKnown = false;
                    }
                    k++;
                }
                if (allInputValuesKnown)
                {
                    double? ySteady = model.GetSteadyStateOutput(u0);
                    if (ySteady.HasValue)
                    {
                        simSignalValueDict.Add(model.GetOutputID(), ySteady.Value);
                    }
                }
            }

            // check if any uninitalized pid-controllers remain
            if (uninitalizedPID_IDs.Count > 0)
            {
                Shared.GetParserObj().AddError("ProcessSimulator failed to initalize controller:" + uninitalizedPID_IDs[0]);
                return false;
            }

            // last step is to actually write all the values, and create otherwise empty vector to be filled.
            double nonYetSimulatedValue = Double.NaN;
            foreach (string signalID in simSignalValueDict.Keys)
            {
                simData.AddTimeSeries(signalID, Vec<double>.Concat(new double[] { simSignalValueDict[signalID] },
                    Vec<double>.Fill(nonYetSimulatedValue, N.Value - 1)));
            }
            /*
            // try to assess if initalization has succeed 
            if (simData.GetSignalNames().Length < nTotalInputs)
            {
                Shared.GetParserObj().AddError("ProcessSimulator failed to initalize all signals.");
                return false;
            }
            */
            return true;
        }






    }
}
