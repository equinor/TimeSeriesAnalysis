using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Accord.Statistics;

//using System.Text.Json;
//using System.Text.Json.Serialization;

using Newtonsoft.Json;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that holds comments added to models.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Author of comment.
        /// </summary>
        public string author;
        /// <summary>
        /// Date of comment.
        /// </summary>
        public DateTime date;
        /// <summary>
        /// Comment string
        /// </summary>
        public string comment;
        /// <summary>
        /// Plant score, intended to hold manully set values indicating specific statuses of the model.
        /// </summary>
        public double plantScore;

        /// <summary>
        /// Comment constructor.
        /// </summary>
        /// <param name="author"></param>
        /// <param name="date"></param>
        /// <param name="comment"></param>
        /// <param name="plantScore"></param>
        public Comment(string author, DateTime date, string comment, double plantScore =0)
        {
            this.author = author;
            this.date = date;
            this.comment = comment;
            this.plantScore= plantScore;
        }
     }

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
        /// Returns a unit data set for a given UnitModel.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="unitModel"></param>
        /// <returns></returns>
        public UnitDataSet GetUnitDataSetForProcess(TimeSeriesDataSet inputData, UnitModel unitModel)
        {
            UnitDataSet dataset = new UnitDataSet();
            dataset.U = new double[inputData.GetLength().Value, 1];
        
            dataset.Times = inputData.GetTimeStamps();
            var inputIDs = unitModel.GetModelInputIDs();
            var outputID = unitModel.GetOutputID();
            dataset.Y_meas = inputData.GetValues(outputID);
            for (int inputIDidx = 0; inputIDidx < inputIDs.Length; inputIDidx++)
            {
                var inputID = inputIDs[inputIDidx];
                var curCol = inputData.GetValues(inputID);
                dataset.U.WriteColumn(inputIDidx, curCol);
            }
            return dataset;
        }


        /// <summary>
        /// Returns a "unitDataSet" for the given pidModel in the plant.
        /// This function only works when the unit model connected to the pidModel only has a single input. 
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="pidModel"></param>
        /// <returns></returns>
        public UnitDataSet GetUnitDataSetForPID(TimeSeriesDataSet inputData,PidModel pidModel)
        {
            var unitModID = connections.GetUnitModelControlledByPID(pidModel.GetID(),modelDict);
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
                    dataset.U.WriteColumn(modelInputIdx, inputData.GetValues(inputID));
                }
            }
            else
            {
                dataset.U = new double[inputData.GetLength().Value, 1];
                dataset.U.WriteColumn(0, inputData.GetValues(pidModel.GetOutputID()));
            }

            dataset.Times = inputData.GetTimeStamps();
            var inputIDs = pidModel.GetModelInputIDs();

            for (int inputIDidx=0; inputIDidx<inputIDs.Length; inputIDidx++)
            {
                var inputID = inputIDs[inputIDidx];

                if (inputIDidx == (int)PidModelInputsIdx.Y_setpoint)
                {
                    dataset.Y_setpoint = inputData.GetValues(inputID);
                }
                else if (inputIDidx == (int)PidModelInputsIdx.Y_meas)
                {
                    dataset.Y_meas = inputData.GetValues(inputID);
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
                Shared.GetParserObj().AddError("PlantSimulator.AddSignal was unable to add signal '"+ signalID+"'" );
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
        /// Simulate a single model to get the internal "x" unmeasured output that excludes any additive outputs (like disturbances).
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="singleModelName"></param>
        /// <param name="simData"></param>
        /// <returns></returns>
        public bool SimulateSingleInternal(TimeSeriesDataSet inputData, string singleModelName, out TimeSeriesDataSet simData)
        {
            return SimulateSingle(inputData,singleModelName,true, out simData);
        }

        /// <summary>
        /// Simualate a single model to get the output including any additive inputs.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="singleModelName"></param>
        /// <param name="simData"></param>
        /// <returns></returns>
        public bool SimulateSingle(TimeSeriesDataSet inputData, string singleModelName, out TimeSeriesDataSet simData)
        {
            return SimulateSingle(inputData, singleModelName, false, out simData);
        }

        /// <summary>
        /// Simulate a single model (any ISimulatableModel), using inputData as inputs, 
        ///
        ///  If the model is a unitModel and the inputData inludes both the measured y and measured u, the
        ///  simData will include an estimate of the additive disturbance.
        /// </summary>
        /// <param name="inputData"></param>
        /// <param name="singleModelName"></param>
        /// <param name="doCalcYwithoutAdditiveTerms"></param>
        /// <param name="simData"></param>
        /// <returns></returns>
        public bool SimulateSingle(TimeSeriesDataSet inputData, string singleModelName, 
            bool doCalcYwithoutAdditiveTerms, out TimeSeriesDataSet simData)
        {
            if (!modelDict.ContainsKey(singleModelName))
            {
                simData = null;
                return false;
            }
            if (!modelDict[singleModelName].IsModelSimulatable(out string explStr))
            {
                Shared.GetParserObj().AddError(explStr);
                simData = null;
                return false;
            }

            simData = new TimeSeriesDataSet();
            int? N = inputData.GetLength();
            int timeIdx = 0;
            var model = modelDict[singleModelName];
            string[] additiveInputIDs = model.GetAdditiveInputIDs();
            string outputID = model.GetOutputID();

            string[] inputIDs = model.GetModelInputIDs();
            if (doCalcYwithoutAdditiveTerms == false)
            {
                inputIDs = model.GetBothKindsOfInputIDs();
            }
            bool doEstimateDisturbance = false;
            if (additiveInputIDs != null)
            {
                if (!inputData.ContainsSignal(additiveInputIDs[0]))
                {
                    doEstimateDisturbance = true;
                    inputIDs = model.GetModelInputIDs();
                }
            }

            var vec = new Vec();
            var nameOfSimulatedSignal = model.GetOutputID();
            if (doEstimateDisturbance)
            {
                nameOfSimulatedSignal = model.GetID();
            }

            // initalize
            {
                double[] inputVals = GetValuesFromEitherDataset(inputIDs, timeIdx, simData, inputData);
                double[] outputVals =
                    GetValuesFromEitherDataset(new string[] { outputID }, timeIdx, simData, inputData);
                simData.InitNewSignal(nameOfSimulatedSignal, outputVals[0], N.Value);
                model.WarmStart(inputVals, outputVals[0]);
            }
            // main loop

            var timeBase_s = inputData.GetTimeBase(); ;

            for (timeIdx = 0; timeIdx < N; timeIdx++)
            {
                double[] inputVals = inputData.GetValuesAtTime(inputIDs, timeIdx);
                double[] outputVal = model.Iterate(inputVals, timeBase_s);

                // if a second output is given, this is by definition the internal output upstream the additive signals.
                if (outputVal.Count() == 2)
                {
                    if (timeIdx == 0)
                    {
                        simData.InitNewSignal(model.GetID(), outputVal[1], N.Value);
                    }
                    var isOk_internal = simData.AddDataPoint(model.GetID(), timeIdx, outputVal[1]);
                    if (!isOk_internal)
                    {
                        return false;
                    }
                }
                var  isOk = simData.AddDataPoint(nameOfSimulatedSignal, timeIdx, outputVal[0]);
                if (!isOk)
                {
                    return false;
                }
            }
            simData.SetTimeStamps(inputData.GetTimeStamps().ToList());
            // disturbance estimation
            if (modelDict[singleModelName].GetProcessModelType() == ModelType.SubProcess && doEstimateDisturbance)
            {
                // y_meas = y_internal+d as defined here
                var y_meas = inputData.GetValues(outputID);
                if (!(new Vec()).IsAllNaN(y_meas) && y_meas != null)
                {
                    var y_sim = simData.GetValues(nameOfSimulatedSignal);
                    if ((new Vec()).IsAllNaN(y_sim))
                    {
                        return false;
                    }
                    var est_disturbance = (new Vec()).Subtract(y_meas, y_sim);
                    simData.Add(SignalNamer.EstDisturbance(model), est_disturbance);
                    simData.Add(model.GetOutputID(), y_meas);
                }
            }
            return true;
        }
        /// <summary>
        /// Perform a dynamic simulation of the model provided, given the specified connections and external signals. 
        /// </summary>
        /// <param name="inputData">the external signals for the simulation(also, determines the simulation time span and timebase)</param>
        /// <param name="simData">the simulated data set to be outputted(excluding the external signals)</param>
        /// <returns></returns>
        public bool Simulate (TimeSeriesDataSet inputData, out TimeSeriesDataSet simData)
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

            (var orderedSimulatorIDs,var compLoopDict) = connections.InitAndDetermineCalculationOrderOfModels(modelDict);
            simData = new TimeSeriesDataSet();

            // initalize the new time-series to be created in simData.
            var init = new PlantSimulatorInitalizer(this);

            var didInit = init.ToSteadyStateAndEstimateDisturbances(ref inputData, ref simData, compLoopDict) ;
            if (!didInit)
            {
                Shared.GetParserObj().AddError("PlantSimulator failed to initalize.");
                return false;
            }

            // warm start every model
            int timeIdx = 0;
            for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                string[] inputIDs = model.GetBothKindsOfInputIDs();
                if (inputIDs == null)
                {
                    Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \""+ model.GetID() +
                        "\" has null inputIDs.");
                    return false;
                }
                double[] inputVals = GetValuesFromEitherDataset(inputIDs, timeIdx,simData,inputData);

                string outputID = model.GetOutputID(); 
                if (outputID==null)
                {
                    Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                        "\" has null outputID.");
                    return false;
                }
                double[] outputVals =
                    GetValuesFromEitherDataset(new string[] { outputID }, timeIdx, simData, inputData);
                if (outputVals != null)
                {
                    model.WarmStart(inputVals, outputVals[0]);
                }
            }

            var idxToIgnore = inputData.GetIndicesToIgnore();
            int lastGoodTimeIndex = 0;
            // if start of dataset is bad, then parse forward until first good time index..
            while (idxToIgnore.Contains(lastGoodTimeIndex) && lastGoodTimeIndex < inputData.GetLength())
            {
                lastGoodTimeIndex++;
            }

            // simulate for all time steps(after first step!)
            for (timeIdx = 0; timeIdx < N; timeIdx++)
            {
                if (!idxToIgnore.Contains(timeIdx))
                    lastGoodTimeIndex = timeIdx;

                for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
                {
                    var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                    string[] inputIDs = model.GetBothKindsOfInputIDs();
                    int inputDataLookBackIdx = 0; 
                    if (model.GetProcessModelType() == ModelType.PID && timeIdx > 0)
                    {
                        inputDataLookBackIdx = 1;//if set to zero, model fails(requires changing model order).
                    }
                    double[] inputVals = GetValuesFromEitherDataset(inputIDs, lastGoodTimeIndex - inputDataLookBackIdx, simData,inputData);
                    if (inputVals == null)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                            "\" error retreiving input values.");
                        return false;
                    }
                    double[] outputVal = model.Iterate(inputVals, timeBase_s);
                    bool isOk = simData.AddDataPoint(model.GetOutputID(),timeIdx,outputVal[0]);
                    if (!isOk)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Unable to add data point for  \"" 
                            + model.GetOutputID() + "\", indicating an error in initalization. ");
                        return false;
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
            simData.SetTimeStamps(inputData.GetTimeStamps().ToList());
            return true;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputIDs"></param>
        /// <param name="timeIndex"></param>
        /// <param name="dataSet1"></param>
        /// <param name="dataSet2"></param>
        /// <returns></returns>
        private double[] GetValuesFromEitherDataset(string[] inputIDs, int timeIndex, 
            TimeSeriesDataSet dataSet1, TimeSeriesDataSet dataSet2)
        {
            double[] retVals = new double[inputIDs.Length];

            int index = 0;
            foreach (var inputId in inputIDs)
            {
                double? retVal=null;
                if (dataSet1.ContainsSignal(inputId))
                {
                    retVal = dataSet1.GetValue(inputId, timeIndex);
                }
                else if (dataSet2.ContainsSignal(inputId))
                {
                    retVal= dataSet2.GetValue(inputId, timeIndex);
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
            if (!fileName.EndsWith(".json"))
                fileName += ".json";

            // var options = new JsonSerializerOptions { WriteIndented = true };
            //  var serializedTxt =  JsonSerializer.Serialize(this,options);

            var serializedTxt = SerializeTxt();

            var fileWriter = new StringToFileWriter(fileName);
            fileWriter.Write(serializedTxt);
            return fileWriter.Close();
        }

    }
    
}
