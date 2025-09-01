using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Accord.IO;
using Accord.Statistics;

//using System.Text.Json;
//using System.Text.Json.Serialization;

using Newtonsoft.Json;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Dynamic
{


    /// <summary>
    /// Simulates larger "plant-models" that is built up of connected sub-models
    /// that each implement <c>ISimulatableModel</c>.
    /// <para>
    /// To set up a simulation, first connect models, and then add external input signals.
    /// This class handles information about which model is connected to which, and handles calling sub-models in the
    /// correct order with the correct input signals.
    /// </para>
    /// <para>
    /// By default, the model attempts to start in steady-state, intalization handled by <c>ProcessSimulatorInitializer</c>
    /// (this requires no user interaction).
    /// </para>
    /// <para>
    /// The building blocks of plant models are <c>UnitModel</c>, <c>PidModel</c> and <c>Select</c>
    /// </para>
    /// <seealso cref="UnitModel"/>
    /// <seealso cref="PidModel"/>
    /// <seealso cref="PlantSimulatorInitalizer"/>
    /// <seealso cref="Select"/>
    /// </summary>
    public class PlantSimulator
    {

        private const bool doDestBasedONYsimOfLastTimestep = true;

        /// <summary>
        /// How many consequtive bad indices are required before the simulator does a new "warm-up" on the first following good indice.
        /// should be larger than two, because a single bad point can can require skipping two indices in a recursive model, and thus can happen often.
        /// </summary>
        private int restartModelAfterXConsecutiveBadIndices = 3;


        /// <summary>
        /// User-friendly name that may include white spaces.
        /// </summary>
        public String plantName { get; set; }

        /// <summary>
        /// A short user-friendly description of what the plant is and does.
        /// </summary>
        public String plantDescription { get; set; }

        /// <summary>
        /// A list of comments that the user may have added to track changes made over time.
        /// </summary>
        public List<Comment> comments;

        /// <summary>
        /// The date of when the model was last saved.
        /// </summary>
        public DateTime date { get; set; }

        /// <summary>
        /// Dictionary of all unit models in the plant simulator (must implement ISimulatableModel).
        /// </summary>
        public Dictionary<string, ISimulatableModel> modelDict;
        /// <summary>
        /// List of all external signal IDs.
        /// </summary>
        public List<string> externalInputSignalIDs;
        /// <summary>
        /// The connection parser object.
        /// </summary>
        public ConnectionParser connections;

        /// <summary>
        /// The fitScore of the plant the last time it was saved.
        /// </summary>
        public double PlantFitScore;



        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="processModelList"> A list of process models, each implementing <c>ISimulatableModel</c></param>
        /// <param name="plantName">optional name of plant, used when serializing</param>
        /// <param name="plantDescription">optional description of plant</param>
        public PlantSimulator(List<ISimulatableModel> processModelList, string plantName="", string plantDescription="")
        {
            externalInputSignalIDs = new List<string>();
            this.comments = new List<Comment>();
            if (processModelList == null)
            {
                return;
            }
            this.plantName = plantName;
            this.plantDescription = plantDescription;
            modelDict = new Dictionary<string, ISimulatableModel>();
            foreach (ISimulatableModel model in processModelList)
            {
                string modelID = model.GetID();
                if (modelDict.ContainsKey(modelID))
                {
                    Shared.GetParserObj().AddError("PlantSimulator failed to initalize, modelID" + modelID + "is not unique");
                }
                else
                {
                    modelDict.Add(modelID, model);
                }
            }
            connections = new ConnectionParser();
        }

        /// <summary>
        /// Add an external signal. Preferred implementation, as the signal can have any ID without naming convention.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="signalID"></param>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <returns>signalID or <c>null</c> if something went wrong</returns>
        public string AddAndConnectExternalSignal(ISimulatableModel model,string signalID, SignalType type, int index = 0)
        {
            ModelType modelType = model.GetProcessModelType();
            externalInputSignalIDs.Add(signalID);
            if (signalID == null)
            {
                Shared.GetParserObj().AddError("PlantSimulator.AddSignal was unable to add 'null' signal name.");
                return null;
            }
            if (type == SignalType.Disturbance_D && modelType == ModelType.SubProcess)
            {
                // by convention, the disturbance is always added last to inputs
                model.AddSignalToOutput(signalID);
                return signalID;
            }
            else if (type == SignalType.Output_Y && modelType == ModelType.SubProcess)
            {
                model.AddSignalToOutput(signalID);
                return signalID;
            }
            else if (type == SignalType.External_U && modelType == ModelType.SubProcess)
            {
                List<string> newInputIDs = new List<string>();
                string[] inputIDs = model.GetModelInputIDs();
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
                return signalID;
            }
            else if (type == SignalType.Setpoint_Yset && modelType == ModelType.PID)
            {
                model.SetInputIDs(new string[] { signalID }, (int)PidModelInputsIdx.Y_setpoint);
                return signalID;
            }
            else if (type == SignalType.Output_Y && modelType == ModelType.PID)
            {
                model.SetInputIDs(new string[] { signalID }, (int)PidModelInputsIdx.Y_meas);
                return signalID;
            }
            else
            {
                Shared.GetParserObj().AddError("PlantSimulator.AddSignal was unable to add signal '" + signalID + "'");
                return null;
            }

        }


        /// <summary>
        /// Informs the PlantSimulator that a specific sub-model has a specific signal at its input
        /// (use for unit testing only, using a naming convention to name signal).
        /// </summary>
        /// <param name="model"></param>
        /// <param name="type"></param>
        /// <param name="index">the index of the signal, this is only needed if this is an input to a multi-input model</param>

        public string AddExternalSignal(ISimulatableModel model, SignalType type, int index = 0)
        {
            string signalID = SignalNamer.GetSignalName(model.GetID(), type, index);
            return AddAndConnectExternalSignal(model,signalID,type,index);
        }

        /// <summary>
        /// Connect an existing signal with a given signalID to a new model.
        /// </summary>
        /// <param name="signalID"></param>
        /// <param name="model"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool ConnectSignalToInput(string signalID, ISimulatableModel model, int idx)
        {
            model.SetInputIDs(new string[] { signalID }, idx);
            return true;
        }

        /// <summary>
        /// Add a disturbance model to the output a given <c>model</c>.
        /// </summary>
        /// <param name="disturbanceModel"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool ConnectModelToOutput(ISimulatableModel disturbanceModel, ISimulatableModel model )
        {
            model.AddSignalToOutput(disturbanceModel.GetOutputID());
            return true;
        }

        /// <summary>
        /// Connect the output of the upstream model to the input of the downstream model.
        /// </summary>
        /// <param name="upstreamModel">the upstream model, meaning the model whose output will be connected</param>
        /// <param name="downstreamModel">the downstream model, meaning the model whose input will be connected</param>
        /// <param name="inputIndex">input index of the downstream model to connect to (default is first input)</param>
        /// <returns>returns the signal id if all is ok, otherwise <c>null</c>.</returns>
        public string ConnectModels(ISimulatableModel upstreamModel, ISimulatableModel downstreamModel, int? inputIndex=null)
        {
            ModelType upstreamType = upstreamModel.GetProcessModelType();
            ModelType downstreamType = downstreamModel.GetProcessModelType();
            string outputId = upstreamModel.GetOutputID();

            int nInputs = downstreamModel.GetLengthOfInputVector();
            if (nInputs == 1 && inputIndex ==0)
            {
                downstreamModel.SetInputIDs(new string[] { outputId });
            }
            else
            {// need to decide which input?? 
                // 
                // y->u_pid
                if (upstreamType == ModelType.SubProcess && downstreamType == ModelType.PID)
                {
                    if (inputIndex.HasValue)
                    {
                        downstreamModel.SetInputIDs(new string[] { outputId }, inputIndex.Value);
                    }
                    else
                    {
                        downstreamModel.SetInputIDs(new string[] { outputId }, (int)PidModelInputsIdx.Y_meas);
                    }
                }
                //u_pid->u 
                else if (upstreamType == ModelType.PID && downstreamType == ModelType.SubProcess)
                {
                    downstreamModel.SetInputIDs(new string[] { outputId }, inputIndex);
                }// process output-> connects to process input of another process
                /*else if (upstreamType == ProcessModelType.SubProcess && downstreamType == ProcessModelType.SubProcess)
                {
                    var isOk = downstreamModel.SetInputIDs(new string[] { outputId }, inputIndex);
                    if (!isOk)
                    {
                        Shared.GetParserObj().AddError("ProcessSimulator.ConnectModels() error connecting:" + outputId);
                        return false;
                    }
                }*/
                else 
                {
                    var isOk = downstreamModel.SetInputIDs(new string[] { outputId }, inputIndex);
                    if (!isOk)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.ConnectModels() error connecting:" + outputId);
                        return null;
                    }
                }
            }
            return outputId;
        }

        /// <summary>
        /// Get a list of all the outputs that are pid-controlled and thus need different treatment in the simualor. 
        /// 
        /// NOte that in the case that a PIDmodel is simulated in isolation using for example SimulateSingle, the 
        /// </summary>
        /// <returns>a dictionary of the output signal ids and the modelIDs used to generate them. ModelIDs can be null</returns>
        private Dictionary<string,string> DeterminePidControlledOutputs()
        {
            var ret = new Dictionary<string,string>();

            foreach (var model in modelDict)
            {
                if (model.Value.GetProcessModelType() == ModelType.PID)
                {
                    var pidControlledOutputSignalID = model.Value.GetModelInputIDs().ElementAt((int)PidModelInputsIdx.Y_meas);
                    string modelThatCreatesOutputID = null;
                    foreach (var otherModel in modelDict)
                    {
                        if (otherModel.Value.GetOutputID() == pidControlledOutputSignalID)
                        {
                            modelThatCreatesOutputID = otherModel.Value.GetID();
                        }
                    }
                    var key = model.Value.GetModelInputIDs().ElementAt((int)PidModelInputsIdx.Y_meas);
                    // note that in the case of a select, the key may already be present in the dictionary
                    if (!ret.ContainsKey(key))
                        ret.Add(key, modelThatCreatesOutputID);
                }
            }
            return ret;
        }

        /// <summary>
        /// Returns a "unitDataSet" for the given pidModel in the plant.
        /// This function only works when the unit model connected to the pidModel only has a single input. 
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="pidModel"></param>
        /// <returns></returns>
        public UnitDataSet GetUnitDataSetForPID(TimeSeriesDataSet inputData, PidModel pidModel)
        {
            bool allOk = true;
            var unitModID = connections.GetUnitModelControlledByPID(pidModel.GetID(), modelDict);
            string[] modelInputIDs = null;
            if (unitModID != null)
            {
                modelInputIDs = modelDict[unitModID].GetModelInputIDs();
            }
            UnitDataSet dataset = new UnitDataSet();

            if (modelInputIDs != null)
            {
                dataset.U = new double[inputData.GetLength().Value, modelInputIDs.Length];
                for (int modelInputIdx = 0; modelInputIdx < modelInputIDs.Length; modelInputIdx++)
                {
                    var inputID = modelInputIDs[modelInputIdx];
                    var values = inputData.GetValues(inputID);
                    if (values != null)
                        dataset.U.WriteColumn(modelInputIdx, inputData.GetValues(inputID));
                    else
                    {
                        allOk = false;
                        dataset.U = new double[inputData.GetLength().Value, 1];
                        dataset.U.WriteColumn(0, inputData.GetValues(pidModel.GetOutputID()));
                    }
                     
                }
            }
            else
            {
                dataset.U = new double[inputData.GetLength().Value, 1];
                dataset.U.WriteColumn(0, inputData.GetValues(pidModel.GetOutputID()));
            }

            dataset.Times = inputData.GetTimeStamps();
            var inputIDs = pidModel.GetModelInputIDs();

            for (int inputIDidx = 0; inputIDidx < inputIDs.Length; inputIDidx++)
            {
                var inputID = inputIDs[inputIDidx];

                if (inputIDidx == (int)PidModelInputsIdx.Y_setpoint)
                {
                    var values = inputData.GetValues(inputID);
                    if (values != null)
                    {
                        dataset.Y_setpoint = inputData.GetValues(inputID);
                    }
                    else
                    {
                        allOk = false;
                    }
                }
                else if (inputIDidx == (int)PidModelInputsIdx.Y_meas)
                {
                    var values = inputData.GetValues(inputID);
                    if (values != null)
                    {
                        dataset.Y_meas = inputData.GetValues(inputID);
                    }
                    else
                    {
                        allOk = false;
                    }
                }
                //todo: feedforward?
                /*else if (type == SignalType.Output_Y_sim)
                {
                    dataset.U.WriteColumn(1, inputData.GetValues(inputID));
                }
                else
                {
                    throw new Exception("unexepcted signal type");
                }*/
            }
            return dataset;
        }

        /// <summary>
        /// Get a TimeSeriesDataSet of all external signals of model.
        /// </summary>
        /// <returns></returns>
        public string[] GetExternalSignalIDs()
        {
            return externalInputSignalIDs.ToArray();
        }

        /// <summary>
        /// Get ConnenectionParser object.
        /// </summary>
        /// <returns></returns>
        public ConnectionParser GetConnections()
        {
            return connections;
        }

        /// <summary>
        /// Get a dictionary of all models.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,ISimulatableModel> GetModels()
        {
            return modelDict;
        }




        /// <summary>
        /// Gets data for a given model from either of two datasets (usally the inputdata and possibly simulated data. 
        /// This method also has a special treatment of PID-inputs.
        /// This method is called to retreive input data during simulation.
        /// </summary>
        /// <param name="model">the model that the data is for()</param>
        /// <param name="inputIDs"></param>
        /// <param name="timeIndex"></param>
        /// <param name="simDataSet">the dataset of the simulation that is being written to</param>
        /// <param name="inputDataSet">measured or otherwise external data given at time of simulation</param>
        /// <returns></returns>
        private double[] GetValuesFromEitherDataset(ISimulatableModel model, string[] inputIDs, int timeIndex,
            TimeSeriesDataSet simDataSet, TimeSeriesDataSet inputDataSet)
        {
            // internal helper, set "NaN" if a value is not found
            double[] GetValuesFromEitherDatasetInternal( int timeIndexInternal)
            {
                double[] retVals = new double[inputIDs.Length];

                int index = 0;
                foreach (var inputId in inputIDs)
                {
                    double? retVal = null;

                    // if the signal exists in the simulated dataset, prefer to use that one.
                    if (simDataSet.ContainsSignal(inputId))
                    {
                        retVal = simDataSet.GetValue(inputId, timeIndexInternal);
                        if (retVal != null)
                            if (Double.IsNaN(retVal.Value))
                            {
                                if (inputDataSet.ContainsSignal(inputId))
                                {
                                    retVal = inputDataSet.GetValue(inputId, timeIndexInternal);
                                }
                            }
                    }
                    else if  ( inputDataSet.ContainsSignal(inputId))
                    {
                        retVal = inputDataSet.GetValue(inputId, timeIndexInternal);
                    }
                    if (!retVal.HasValue)
                    {
                        retVals[index] = Double.NaN;
                    }
                    else
                    {
                        retVals[index] = retVal.Value;
                    }

                    index++;
                }
                return retVals;
            }

            if (model.GetProcessModelType() == ModelType.PID && timeIndex > 0)
            {
                double[] currentValues = GetValuesFromEitherDatasetInternal( timeIndex);
                // "use values from current data point when available, but fall back on using values from the previous sample if need be"
                // for instance, always use the most current setpoint value, but if no disturbance vector is given, then use the y_proc simulated from the last iteration.
                double[] retValues = new double[currentValues.Length];
                retValues = currentValues;

                // adding in the below code  seems to remove the issue with there being a one sample wait time before the effect of a setpoint 
                // is seen on the output, but causes there to be small deviation between what the PlantSimulator.SimulateSingle and PlantSimulator.Simulate
                // seem to return for a PID-loop in the test BasicPID_CompareSimulateAndSimulateSingle_MustGiveSameResultForDisturbanceEstToWork
                
                  /*for (int i = 0; i < currentValues.Length; i++)
                  {
                      if (Double.IsNaN(currentValues[i]))
                      {
                          retValues[i] = lookBackValues[i];
                      }
                      else
                      {
                          retValues[i] = currentValues[i];
                      }
                 }*/
                return retValues;
            }
            else
            {
                return GetValuesFromEitherDatasetInternal(timeIndex);
            }
        }

        /// <summary>
        /// Simulate plant. This version of simualte does not internally determine any indices to ignore, but it will consider
        /// indicesToIgnore of <c>inputData</c>
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="simData"></param>
        /// <returns>true if able to simulate, otherwise false</returns>
        public bool Simulate(TimeSeriesDataSet inputData, out TimeSeriesDataSet simData)
        {
            return Simulate(inputData, false, out simData);
        }

        /// <summary>
        /// Perform a "plant-wide" full dynamic simulation of the entire plant,i.e. all models in the plant, given the specified connections and external signals. 
        /// <para>
        /// The dynamic simulation will also return estimated disturbances in simData, if the plant contains feedback loops where there is an additive 
        /// disturbance with a signal named according to SignalNamer.EstDisturbance() convention
        /// </para>
        /// <para>
        ///  The simulation will also set the <c>PlantFitScore</c> which can be used to evalute the fit of the simulation to the plant data.
        ///  For this score to be calculated, the measured time-series corresponding to <c>simData</c> need to be provided in <c>inputData</c>
        ///  </para>
        ///  <para>
        ///  If <c>doDetermineIndicesToIgnore</c> is set to <c>false</c>, then the 
        ///  the simulation will consider the <c>.IndicesToIgnore</c> member of the inputData, and ignore these data indices, re-starting dynamic
        ///  models once periods of bad data pass.(this indcies will be padded internally do not need to be padded when supplied)
        ///  If instead set to <c>true</c>, the simulator will parse the input data to determine 
        ///  bad indices, but it will not be able to determine periods of "frozen" datasets (as outputs y are generally not required to be given in the input data.)
        /// </para>
        /// 
        /// </summary>
        /// <param name="inputData">the external signals for the simulation(also, determines the simulation time span and timebase)</param>
        /// <param name="doDetermineIndicesToIgnore"> is set to true, the simulator tries to determine indices to ignore internally</param>
        /// <param name="simData">the simulated data set to be outputted(excluding the external signals)</param>
        /// <returns></returns>
        public bool Simulate (TimeSeriesDataSet inputData, bool doDetermineIndicesToIgnore, out TimeSeriesDataSet simData)
        {
            var timeBase_s = inputData.GetTimeBase(); ;

            int? N = inputData.GetLength();
            if (!N.HasValue)
            {
                Shared.GetParserObj().AddError("PlantSimulator could not run, no external signal provided.");
                simData = null;
                return false;
            }
            for (int i = 0; i < modelDict.Count; i++)
            {
                if (!modelDict.ElementAt(i).Value.IsModelSimulatable(out string explStr))
                {
                    Shared.GetParserObj().AddError("PlantSimulator could not run, model "+
                        modelDict.ElementAt(i).Key + " lacks all required inputs to be simulatable:"+ 
                        explStr);
                    simData = null;
                    return false;
                }
            }

            (var orderedSimulatorIDs,var compLoopDict) = 
                connections.InitAndDetermineCalculationOrderOfModels(modelDict);
            simData = new TimeSeriesDataSet();

            // initalize the new time-series to be created in simData.
            var init = new PlantSimulatorInitalizer(this);

            var inputDataMinimal = SelectMinimalInputData(inputData);
            // parse dataset and determine indices that are to be ignored when simulating.
            if (doDetermineIndicesToIgnore)
            {
                inputDataMinimal.SetIndicesToIgnore(
                    CommonDataPreprocessor.ChooseIndicesToIgnore(inputDataMinimal, detectFrozenData: true));
            }
            else
            {
                inputDataMinimal.SetIndicesToIgnore(Index.AppendTrailingIndices(inputDataMinimal.GetIndicesToIgnore()));
            }

            // todo: disturbances could also instead be estimated in closed-loop? 
            var didInit = init.ToSteadyStateAndEstimateDisturbances(ref inputDataMinimal, ref simData, compLoopDict);

            // need to keep special track of pid-controlled outputs.
            var pidControlledOutputsDict = DeterminePidControlledOutputs();
            // create internal "process outputs" for each such model
            foreach (var pidOutput in pidControlledOutputsDict)
            {
                var signalID = pidOutput.Value;
                simData.InitNewSignal(signalID, Double.NaN, N.Value);
            }

            if (!didInit)
            {
                Shared.GetParserObj().AddError("PlantSimulator failed to initalize.");
                return false;
            }

            var idxToIgnore = inputDataMinimal.GetIndicesToIgnore();
            int lastGoodTimeIndex = 0;
            // if start of dataset is bad, then parse forward until first good time index..
            while (idxToIgnore.Contains(lastGoodTimeIndex) && lastGoodTimeIndex < inputDataMinimal.GetLength())
            {
                lastGoodTimeIndex++;
            }

            // simulate for all time steps(after first step!)
       
            int nConsecutiveBadIndicesCounter = 0;
            int numRestartCounter = -1;
            for (int timeIdx = 0; timeIdx < N; timeIdx++)
            {
                bool doRestartModels = false;
                if (timeIdx == 0)
                    doRestartModels = true;
                if (!idxToIgnore.Contains(timeIdx))
                {
                    lastGoodTimeIndex = timeIdx;
                    if (nConsecutiveBadIndicesCounter > restartModelAfterXConsecutiveBadIndices)
                        doRestartModels = true;
                    nConsecutiveBadIndicesCounter = 0;
                }
                else
                {
                    nConsecutiveBadIndicesCounter++;
                }
             
                // warm start every model on first data point, and after any long period of bad data
                if (doRestartModels)
                {
                    bool useMinimalDataSet = true;
                    if (timeIdx > 0)
                        useMinimalDataSet = false;

                    numRestartCounter++;
                    for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
                    {
                        var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                        string[] inputIDs = model.GetBothKindsOfInputIDs();
                        if (inputIDs == null)
                        {
                            Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                                "\" has null inputIDs.");
                            return false;
                        }
                        double[] inputVals;
                        if (useMinimalDataSet)
                            inputVals = GetValuesFromEitherDataset(model, inputIDs, timeIdx, simData, inputDataMinimal);
                        else
                            inputVals = GetValuesFromEitherDataset(model, inputIDs, timeIdx, simData, inputData);
                        string outputID = model.GetOutputID();
                        if (outputID == null)
                        {
                            Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                                "\" has null outputID.");
                            return false;
                        }
                        double[] outputVals;
                        if (useMinimalDataSet)
                            outputVals = GetValuesFromEitherDataset(model, new string[] { outputID }, timeIdx, simData, inputDataMinimal);
                        else
                            outputVals = GetValuesFromEitherDataset(model, new string[] { outputID }, timeIdx, simData, inputData);
                        if (outputVals != null)
                        {
                            model.WarmStart(inputVals, outputVals[0]);
                        }
                    }
                }

                for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
                {
                    var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                    string[] inputIDs = model.GetBothKindsOfInputIDs();

                    ////////////////////////
                    // before calculating a PID-model, treat the "junction" before it to get the right y_meas[k] = y_proc[k_1] + D[k] 
                    if (model.GetProcessModelType() == ModelType.PID && timeIdx > 0)
                    {
                        var junctionSignalID = inputIDs.ElementAt((int)PidModelInputsIdx.Y_meas);
                        if (!pidControlledOutputsDict.ContainsKey(junctionSignalID))
                        {
                            Shared.GetParserObj().AddError("PlantSimulator.Simulate() junction signal error \"" + junctionSignalID + "\"");
                        }
                        else
                        {
                            var modelID = pidControlledOutputsDict[junctionSignalID];
                            if (modelID != null) // Simulataing a single PID-model not the whole loop
                            {
                                var value = simData.GetValue(modelID, timeIdx - 1); //y_proc[k-1]
                                if (modelID != null)
                                {
                                    if (modelDict[modelID].GetAdditiveInputIDs() != null)       // + D[k] 
                                    {
                                        var additiveSignalsValues = GetValuesFromEitherDataset(modelDict[modelID],
                                            modelDict[modelID].GetAdditiveInputIDs(), timeIdx, simData, inputDataMinimal);
                                        foreach (var signalValue in additiveSignalsValues)
                                        {
                                            value += signalValue;
                                        }
                                    }
                                }
                                if (value.HasValue)
                                {
                                    bool isOk = simData.AddDataPoint(junctionSignalID, timeIdx, value.Value);
                                }
                                else
                                {
                                    Shared.GetParserObj().AddError("PlantSimulator.Simulate() error. Error calculating junction signal \"" + junctionSignalID +
                                  "\"");
                                }
                            }
                        }
                    }
                    ///////////////////////

                    double[] inputVals = null;
                    inputVals = GetValuesFromEitherDataset(model, inputIDs, lastGoodTimeIndex, simData, inputDataMinimal);
                    if (inputVals == null)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                            "\" error retreiving input values.");
                        return false;
                    }

                    double[] outputVal = model.Iterate(inputVals, timeBase_s);

                    if (pidControlledOutputsDict.Keys.Contains(model.GetOutputID()))
                    {
                        // for pid-controlled outputs, save the result with the name of the process ID, to be used in the 
                        // next iteration, possibly in combination with a disturbance signal
                        bool isOk = simData.AddDataPoint(model.GetID(), timeIdx, outputVal[0]);
                        if (!isOk)
                        {
                            Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Unable to add data point for  \""
                                + model.GetOutputID() + "\", indicating an error in initalization. ");
                            return false;
                        }
                    }
                    else
                    {
                        bool isOk = simData.AddDataPoint(model.GetOutputID(), timeIdx, outputVal[0]);
                        if (!isOk)
                        {
                            Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Unable to add data point for  \""
                                + model.GetOutputID() + "\", indicating an error in initalization. ");
                            return false;
                        }
                    }
                    if (outputVal.Length > 1)
                    {
                        if (timeIdx ==0)
                        {
                            simData.InitNewSignal(model.GetID(), outputVal[1],N.Value);
                        }
                        bool isOk2 = simData.AddDataPoint(model.GetID(), timeIdx, outputVal[1]);
                        if (!isOk2)
                            return false;
                    }
                }
            }
            if (inputDataMinimal != null)
                if (inputDataMinimal.GetTimeStamps() != null)
            simData.SetTimeStamps(inputDataMinimal.GetTimeStamps().ToList());
            simData.SetIndicesToIgnore(inputDataMinimal.GetIndicesToIgnore());
            simData.SetNumSimulatorRestarts(numRestartCounter);
            PlantFitScore = FitScoreCalculator.GetPlantWideSimulated(this, inputData, simData, inputDataMinimal.GetIndicesToIgnore() );

            return true;
        }

        /// <summary>
        /// Choose only the input time series that are actually used by a given plant
        /// </summary>
        /// <param name="rawInputData"></param>
        /// <returns></returns>
        public TimeSeriesDataSet SelectMinimalInputData(TimeSeriesDataSet rawInputData)
        {
            var retTimeSeries = new TimeSeriesDataSet();

            foreach (var model in modelDict)
            {
                foreach (var inputID in model.Value.GetBothKindsOfInputIDs())
                {
                    var values = rawInputData.GetValues(inputID);
                    if (values != null)
                    {
                        retTimeSeries.Add(inputID, values);
                    }
                }
                var outputID = model.Value.GetOutputID();
                if (rawInputData.ContainsSignal(outputID))
                {
                    var values = rawInputData.GetValues(outputID);
                    retTimeSeries.Add(outputID, values);
                }

                var outputIdentID = model.Value.GetOutputIdentID();
                if (rawInputData.ContainsSignal(outputIdentID))
                {
                    var values = rawInputData.GetValues(outputIdentID);
                    retTimeSeries.Add(outputIdentID, values);
                }
            }

            retTimeSeries.SetTimeStamps(rawInputData.GetTimeStamps().ToList());
            retTimeSeries.SetIndicesToIgnore(rawInputData.GetIndicesToIgnore());

            return retTimeSeries;
        }



        /// <summary>
        /// Creates a JSON text string serialization of this object.
        /// </summary>
        /// <returns></returns>
        public string SerializeTxt()
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto; 
            settings.Formatting = Formatting.Indented;

            // models outputs that are not connected to anyting are "null"

            foreach (var keyvalue in this.modelDict)
            {
                var outputID = this.modelDict[keyvalue.Key].GetOutputID();
                this.modelDict[keyvalue.Key].SetOutputID(outputID);
            }
            // https://khalidabuhakmeh.com/serialize-interface-instances-system-text-json
            return JsonConvert.SerializeObject(this, settings);

        }

        /// <summary>
        /// Creates a JSON file representation of this object.
        /// </summary>
        /// <param name="newPlantName">the desired file name and plant name (can be null, in which case the filename should be given in the path argument)</param>
        /// <param name="path">create file in the given path</param>
        public bool Serialize(string newPlantName = null, string path= null)
        {
            string fileName = "";
            if (path != null)
            {
                fileName = path;
                if (!fileName.EndsWith(@"\"))
                        fileName +=  @"\";
            }
            if (newPlantName!=null)
            {
                fileName += newPlantName;
            }
            else if (plantName != null)
            {
                fileName += plantName;
            }
            if (!fileName.EndsWith(".json")|| !fileName.EndsWith(".ajson"))
                fileName += ".json";

            var serializedTxt = SerializeTxt();

            var fileWriter = new StringToFileWriter(fileName);
            fileWriter.Write(serializedTxt);
            return fileWriter.Close();
        }

        /// <summary>
        /// If the number of consecutive bad samples exceeds this threshold, then the simulator will restart on the 
        /// subsequent first good sample.
        /// </summary>
        /// <param name="samples"></param>
        public void SetNumberOfConsecutiveBadIndicesBeforeSimRestarts(int samples)
        { 
            this.restartModelAfterXConsecutiveBadIndices = samples;
        }

    }
    
}
