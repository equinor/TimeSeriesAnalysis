using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{
    class ProcessSimulator
    {
        int timeBase_s;
        Dictionary<string, ISimulatableModel> modelDict;
        TimeSeriesDataSet externalInputSignals;
        private int nConnections=0;
        ConnectionParser connections;

        public ProcessSimulator(int timeBase_s,List<ISimulatableModel> 
            processModelList)
        {
            externalInputSignals = new TimeSeriesDataSet(timeBase_s);

            this.timeBase_s = timeBase_s;
            if (processModelList == null)
            {
                return;
            }

            modelDict = new Dictionary<string, ISimulatableModel>();
            connections = new ConnectionParser();


            // create a unique model ID for each model
            foreach (ISimulatableModel model in processModelList)
            {
                int? number = null;
                string modelNumberSuffix = "";
                bool modelIDisUnique = false;
                string modelID = model.GetID();

               // string modelID="";
                //append a model number in the case that names are not unique
                /*
                while (!modelIDisUnique)
                {
                    if (number != null)
                    {
                        modelNumberSuffix = "_" + number.ToString();
                    }
                    modelID = model.GetID() +
                        model.GetProcessModelType().ToString() + modelNumberSuffix;
                    if (modelDict.ContainsKey(modelID))
                    {
                        number++;
                        modelIDisUnique = false;
                    }
                    else
                    {
                        modelIDisUnique = true;
                    }
                }*/
                if (modelDict.ContainsKey(modelID))
                {
                    Shared.GetParserObj().AddError("ProcessSimulator failed to initalize, modelID" + modelID + "is not unique");
                }
                else
                {
                    modelDict.Add(modelID, model);
                }
            }
            connections.AddAllModelObjects(modelDict);
        }

        /// <summary>
        /// Both connects and adds data to signal that is to be inputted into a specific sub-model, 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        /// <param name="index">the index of the signal, this is only needed if this is an input to a multip-input model</param>

        public bool AddSignal(ISimulatableModel model, SignalType type, double[] values, int index=0)
        {
            ProcessModelType modelType = model.GetProcessModelType();
            string signalID = externalInputSignals.AddTimeSeries(model.GetID(), type, values,index);
            if (signalID == null)
            {
                Shared.GetParserObj().AddError("ProcessSimulator.AddSignal was unable to add signal.");
                return false;
            }
            if (type == SignalType.Distubance_D && modelType == ProcessModelType.SubProcess)
            {
                // by convention, the disturbance is always added last to inputs
                List<string> newInputIDs = new List<string>(model.GetInputIDs());
                newInputIDs.Add(signalID);
                model.SetInputIDs(newInputIDs.ToArray()); ;
                return true;
            }
            else if (type == SignalType.External_U && modelType == ProcessModelType.SubProcess)
            {
                List<string> newInputIDs = new List<string>();
                string[] inputIDs = model.GetInputIDs();
                if (inputIDs != null)
                {
                    newInputIDs = new List<string>(inputIDs);
                }
                if (newInputIDs.Count < index + 1)
                {
                    newInputIDs.Add(signalID);
                }
                else
                {
                    newInputIDs[index] = signalID;
                }
                model.SetInputIDs(newInputIDs.ToArray());
                return true;
            }
            else if (type == SignalType.Setpoint_Yset && modelType == ProcessModelType.PID)
            {
                model.SetInputIDs(new string[] { signalID }, (int)PIDModelInputsIdx.Y_setpoint);
                return true;
            }
            else
            {
                Shared.GetParserObj().AddError("ProcessSimulator.AddSignal was unable to add signal.");
                return false;
            }
        }

        /// <summary>
        /// Connect the output of the upstream model to the input of the downstream model
        /// </summary>
        /// <param name="upstreamModel">the upstream model, meaning the model whose output will be connected</param>
        /// <param name="downstreamModel">the downstream model, meaning the model whose input will be connected</param>
        /// <param name="inputIndex">input index of the downstream model to connect to (default is first input)</param>
        /// <returns></returns>
        public bool ConnectModels(ISimulatableModel upstreamModel, ISimulatableModel downstreamModel, int inputIndex=0)
        {
           
            ProcessModelType upstreamType = upstreamModel.GetProcessModelType();
            ProcessModelType downstreamType = downstreamModel.GetProcessModelType();
            string outputId = upstreamModel.GetID();

            outputId = SignalNamer.GetSignalName(upstreamModel.GetID(),upstreamModel.GetOutputSignalType());

            upstreamModel.SetOutputID(outputId);
            int nInputs = downstreamModel.GetNumberOfInputs();
            if (nInputs == 1)
            {
                downstreamModel.SetInputIDs(new string[] { outputId });
            }
            else
            {// need to decide which input?? 
                // 
                // y->u_pid
                if (upstreamType == ProcessModelType.SubProcess && downstreamType == ProcessModelType.PID)
                {
                  //  upstreamModel.SetOutputID(outputId);
                    downstreamModel.SetInputIDs(new string[] { outputId }, (int)PIDModelInputsIdx.Y_meas);
                }
                //u_pid->u 
                else if (upstreamType == ProcessModelType.PID && downstreamType == ProcessModelType.SubProcess)
                {
                   // upstreamModel.SetOutputID(outputId);
                    downstreamModel.SetInputIDs(new string[] { outputId }, inputIndex);
                }// process output-> connects to process input of another process
                else if (upstreamType == ProcessModelType.SubProcess && downstreamType == ProcessModelType.SubProcess)
                {
                    downstreamModel.SetInputIDs(new string[] { outputId }, inputIndex);
                }
                else
                {
                    Shared.GetParserObj().AddError("ProessSimulator.ConnectModels() tried to connect an unimplemented model type.");
                    return false;
                }
            }
            connections.AddConnection(upstreamModel.GetID(), downstreamModel.GetID());
            nConnections++;
            return true;
        }

        /// <summary>
        /// Simulate over a dataset
        /// </summary>
        /// <param name="externalInputSignals">the external signals
        /// that act on the process while simulating(setpoints, model inputs that are not controlled or disturbances).
        /// The length of these signals determine the length of the simulation</param>
        /// <param name="simData">the simulated data set to be outputted(includes the external signals)</param>
        /// <returns></returns>
        public bool Simulate( out TimeSeriesDataSet simData)
        {
            int? N = externalInputSignals.GetLength();
            if (!N.HasValue)
            {
                simData = null;
                return false;
            }

            var orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();
            simData = new TimeSeriesDataSet(timeBase_s,externalInputSignals);

            // initalize the new time-series to be created in simData.
            var didInit = InitToSteadyState(ref simData);
            if (!didInit)
                return false;

            int timeIdx = 0;
            for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                string[] inputIDs = model.GetInputIDs();
                if (inputIDs == null)
                {
                    Shared.GetParserObj().AddError("ProcessSimulator.Simulate() failed. Model \""+ model.GetID() +
                        "\" has null inputIDs.");
                    return false;
                }
                double[] inputVals = simData.GetData(inputIDs, timeIdx);

                string outputID = model.GetOutputID(); //TimeSeriesDataSet.GetSignalName(model.GetID(), model.GetOutputSignalType());
                if (outputID==null)
                {
                    Shared.GetParserObj().AddError("ProcessSimulator.Simulate() failed. Model \"" + model.GetID() +
                        "\" has null outputID.");
                    return false;
                }
                double[] outputVals = simData.GetData(new string[]{outputID}, timeIdx);
                if (outputVals != null)
                {
                    model.WarmStart(inputVals, outputVals[0]);
                }
            }

            // simulate for all time steps(after first step!)
            for (timeIdx = 1; timeIdx < N; timeIdx++)
            {
                for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
                {
                    var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                    string[] inputIDs = model.GetInputIDs();
                    int inputDataLookBackIdx = 0; 
                    if (model.GetProcessModelType() == ProcessModelType.PID && timeIdx > 0)
                    {
                        inputDataLookBackIdx = 1;
                    }
                    double[] inputVals = simData.GetData(inputIDs, timeIdx- inputDataLookBackIdx);
                    if (inputVals == null)
                    {
                        Shared.GetParserObj().AddError("ProcessSimulator.Simulate() failed. Model \"" + model.GetID() +
                            "\" error retreiving input values.");
                        return false;
                    }
                    double outputVal = model.Iterate(inputVals);
                    simData.AddDataPoint(model.GetOutputID(),timeIdx,outputVal);
                }
            }
            return true;
        }

        /// <summary>
        /// Initalize the empty datasets to their steady-state values.
        /// - All PID-loops must have a setpoint value set and all Y_meas are initalized to setpoint
        /// - Then all subprocesses inputs U are back-calculated to give steady-state Y_meas
        /// </summary>
        /// <param name="simData">simulation dataset containing only the external signals. The new simulated variables are added to this variable with initial values.</param>
        bool InitToSteadyState(ref TimeSeriesDataSet simData)
        {
            int nTotalInputs = 0;
            int nExternalInputs = externalInputSignals.GetSignalNames().Length;

            int? N = externalInputSignals.GetLength();
            var orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();

            // a dictionary that should contain the signalID of each "internal" simulated variable as a .Key,
            // the inital value will be calculated .Value, but is NaN unit calculated.
            var simSignalValueDict = new Dictionary<string, double>();

            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                nTotalInputs += model.GetInputIDs().Length;
            }
            // 
            int nUnaccountedForSignals = nTotalInputs - nExternalInputs - nConnections;
            if (nUnaccountedForSignals > 0)
            {
                return false;
            }

            // forward-calculate the output for those systems where the inputs are given. 
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.SubProcess)
                { 
                    continue;
                }

                string[] inputIDs = model.GetInputIDs();
                bool areAllInputsGiven = true;
                double[] u0 = new double[inputIDs.Length];
                int k = 0;
                foreach (string inputID in inputIDs)
                {
                    if (inputID == null)
                    {
                        Shared.GetParserObj().AddError("ProcessSimulator.InitToSteadyState(): model "
                            +model.GetID()+" has an unexpected null input");
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

            // find all PID-controllers, and setting the "y" equal to "yset"
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.PID)
                    continue;

                string[] inputIDs = model.GetInputIDs();
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
                else
                {//TODO: extend to simulate cascade or other scheme where setpoint is an input??
                    Shared.GetParserObj().AddError("PID-controller has no setpoint given:"+model.GetID());
                    return false;
                }
            }

            // after the PID-controlled "Y" have been set, go through each "SubProcess" model and back-calculate 
            // the steady-state u for those subsytems that have a defined "Y".
            // for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            // assume that subsystems are ordered from left->right, go throught them right->left to propage pid-output backwards!

           // for (int subSystem =  0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            for (int subSystem = orderedSimulatorIDs.Count-1; subSystem >0; subSystem--)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.SubProcess)
                    continue;

                string outputID = model.GetOutputID();

                if (outputID == null)
                {
                    Shared.GetParserObj().AddError("ProcessSimulator could not init because model \"" 
                        +model.GetID()+"\" has no defined output.");
                    return false;
                }
                bool isYgiven   = simSignalValueDict.ContainsKey(outputID)|| externalInputSignals.ContainsSignal(outputID);

                // TODO:generalize for cases where more than one pid may be controlling a single process(does this makes sense?)
                int numberOfPIDInputs = SignalNamer.GetNumberOfSignalsOfType(model.GetInputIDs(), SignalType.PID_U);
                if (numberOfPIDInputs > 1)
                {
                    Shared.GetParserObj().AddError("currently only one pid-input per system is supported(to be address in a future update?)");
                    return false;
                }

                int numberOfExternalInputs = SignalNamer.GetNumberOfSignalsOfType(model.GetInputIDs(), SignalType.External_U);
                bool canWeFindTheUnknownInput = (model.GetNumberOfInputs()- numberOfExternalInputs == 1) && isYgiven;
       
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

                int[] uPIDIndices = SignalNamer.GetIndexOfSignalType(model.GetInputIDs(), SignalType.PID_U);
                int[] uInternalIndices = SignalNamer.GetIndexOfSignalType(model.GetInputIDs(), SignalType.Output_Y_sim);
                int[] uFreeIndices = Vec<int>.Concat(uPIDIndices, uInternalIndices);
  
                if (uFreeIndices.Length > 1)
                {
                    Shared.GetParserObj().AddError("unexpected/unsupported number of free inputs found during init.");
                    return false;
                }
                else if (uFreeIndices.Length == 1)
                {

                    int uPidIndex = uFreeIndices[0];

                    string[] inputIDs = model.GetInputIDs();
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
                    }
                    else
                    {
                        Shared.GetParserObj().AddError("something went wrong when calculating steady-state inputs during init.");
                        return false;
                    }
                }
            }

            // the final step is if there are any final processes "to the left of" the already initalized signals
            // but these signals do not need to be initalized!!


            // last step is to actually write all the values, and create otherwise empty vector to be filled.
            double nonYetSimulatedValue = Double.NaN;
            foreach (string signalID in simSignalValueDict.Keys)
            {
                simData.AddTimeSeries(signalID, Vec<double>.Concat(new double[] { simSignalValueDict[signalID] },
                    Vec<double>.Fill(nonYetSimulatedValue, N.Value-1))) ;
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
