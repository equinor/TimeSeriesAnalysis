using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using System.Text.Json;
//using System.Text.Json.Serialization;

using Newtonsoft.Json;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Simulates larger "plant-models" that is built up connected sub-models, 
    /// that each implement <c>ISimulatableModel</c>
    /// <para>
    /// To set up a simulation, first connect models, and then add external input signals.
    /// This class handles information about which model is connected to which, and handles callig sub-models in the
    /// correct order with the correct input signals.
    /// </para>
    /// <para>
    /// By default, the model attempts to start in steady-state, intalization handled by <c>ProcessSimulatorInitalizer</c>
    /// (this requires no user interaction)
    /// </para>
    /// <para>
    /// The building blocks of plant models are <c>PIDModel</c>, <c>DefaultProcessModel</c> and <c>Select</c>
    /// </para>
    /// <seealso cref="UnitModel"/>
    /// <seealso cref="PidModel"/>
    /// <seealso cref="PlantSimulatorInitalizer"/>
    /// <seealso cref="Select"/>
    /// </summary>
    public class PlantSimulator
    {
        public string plantName;

        public Dictionary<string, ISimulatableModel> modelDict;

        public List<string> externalInputSignalIDs;

        public ConnectionParser connections;

        public UnitDataSet GetUnitDataSetForPID(TimeSeriesDataSet inputData,PidModel pidModel)
        {
            UnitDataSet dataset = new UnitDataSet(); 
            dataset.U = new double[inputData.GetLength().Value,1];
            dataset.U.WriteColumn(0, inputData.GetValues(pidModel.outputID));
            dataset.Times = inputData.GetTimeStamps();
            var inputIDs = pidModel.GetModelInputIDs();


//            PidModelInputsIdx

            for (int inputIDidx=0; inputIDidx<inputIDs.Length; inputIDidx++)
            {
                var inputID = inputIDs[inputIDidx];
                //  var type = SignalNamer.GetSignalType(inputID);

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
        public PlantSimulator(List<ISimulatableModel>
            processModelList, string plantName=null)
        {
            externalInputSignalIDs = new List<string>();

            if (processModelList == null)
            {
                return;
            }

            this.plantName = plantName;

            modelDict = new Dictionary<string, ISimulatableModel>();
            connections = new ConnectionParser();

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
          //  connections.AddAllModelObjects(modelDict);
        }

        /// <summary>
        /// Add an external signal. Preferred implementation, as signal can have any ID without naming convention.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="signalID"></param>
        /// <param name="type"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public string AddAndConnectExternalSignal(ISimulatableModel model,string signalID, SignalType type, int index = 0)
        {
            ModelType modelType = model.GetProcessModelType();
            externalInputSignalIDs.Add(signalID);
            if (signalID == null)
            {
                Shared.GetParserObj().AddError("PlantSimulator.AddSignal was unable to add signal.");
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
            else
            {
                Shared.GetParserObj().AddError("PlantSimulator.AddSignal was unable to add signal.");
                return null;
            }

        }


        /// <summary>
        /// Informs the PlantSimulator that a specific sub-model has a specifc signal at its input, 
        /// (use for unit testing only, using a naming conventino to name signal)
        /// </summary>
        /// <param name="model"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        /// <param name="index">the index of the signal, this is only needed if this is an input to a multi-input model</param>

        public string AddExternalSignal(ISimulatableModel model, SignalType type, int index = 0)
        {
            string signalID = SignalNamer.GetSignalName(model.GetID(), type, index);
            return AddAndConnectExternalSignal(model,signalID,type,index);
        }

        /// <summary>
        /// Connect an existing signal with a given signalID to a new model
        /// </summary>
        /// <param name="signalID"></param>
        /// <param name="model"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool ConnectSignal(string signalID, ISimulatableModel model, int idx)
        {
            model.SetInputIDs(new string[] { signalID }, idx);
            return true;
        }

        /// <summary>
        /// Add a disturbance model to the output a given <c>model</c>
        /// </summary>
        /// <param name="disturbanceModel"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool ConnectModelToOutput(ISimulatableModel disturbanceModel, ISimulatableModel model )
        {
            model.AddSignalToOutput(disturbanceModel.GetOutputID());
            connections.AddConnection(disturbanceModel.GetID(), model.GetID());
            return true;
        }

        /// <summary>
        /// Connect the output of the upstream model to the input of the downstream model
        /// </summary>
        /// <param name="upstreamModel">the upstream model, meaning the model whose output will be connected</param>
        /// <param name="downstreamModel">the downstream model, meaning the model whose input will be connected</param>
        /// <param name="inputIndex">input index of the downstream model to connect to (default is first input)</param>
        /// <returns>returns the signal id if all is ok, otherwise null.</returns>
        public string ConnectModels(ISimulatableModel upstreamModel, ISimulatableModel downstreamModel, int? inputIndex=null)
        {
            ModelType upstreamType = upstreamModel.GetProcessModelType();
            ModelType downstreamType = downstreamModel.GetProcessModelType();
            string outputId = upstreamModel.GetID();

            outputId = SignalNamer.GetSignalName(upstreamModel.GetID(),upstreamModel.GetOutputSignalType());

            upstreamModel.SetOutputID(outputId);
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
            connections.AddConnection(upstreamModel.GetID(), downstreamModel.GetID());
            return outputId;
        }

        /// <summary>
        /// Get a TimeSeriesDataSet of all external signals of model
        /// </summary>
        /// <returns></returns>
        public string[] GetExternalSignalIDs()
        {
            return externalInputSignalIDs.ToArray();
        }

        /// <summary>
        /// Get ConnenectionParser object
        /// </summary>
        /// <returns></returns>
        public ConnectionParser GetConnections()
        {
            return connections;
        }

        /// <summary>
        /// Get dictionary of all models 
        /// </summary>
        /// <returns></returns>
        public Dictionary<string,ISimulatableModel> GetModels()
        {
            return modelDict;
        }



        /// <summary>
        /// Perform a dynamic simulation of the model provided, given the specified connections and external signals 
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

            var orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels(modelDict);
            simData = new TimeSeriesDataSet();

            // initalize the new time-series to be created in simData.
            var init = new PlantSimulatorInitalizer(this);
            var didInit = init.ToSteadyState(inputData, ref simData) ;
            if (!didInit)
            {
                Shared.GetParserObj().AddError("PlantSimulator failed to initalize.");
                return false;
            }
            if (simData.GetSignalNames().Length < orderedSimulatorIDs.Count())
            {
                Shared.GetParserObj().AddError("PlantSimulator initalized only "+ simData.GetSignalNames().Length+
                    ". Expected "+ orderedSimulatorIDs.Count());
                return false;
            }


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
                // simData.GetData(new string[]{outputID}, timeIdx);
                if (outputVals != null)
                {
                    model.WarmStart(inputVals, outputVals[0]);
                }
            }

            // simulate for all time steps(after first step!)
            for (timeIdx = 0; timeIdx < N; timeIdx++)
            {
                for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
                {
                    var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                    string[] inputIDs = model.GetBothKindsOfInputIDs();
                    int inputDataLookBackIdx = 0; 
                    if (model.GetProcessModelType() == ModelType.PID && timeIdx > 0)
                    {
                        inputDataLookBackIdx = 1;//if set to zero, model fails(requires changing model order).
                    }
                    double[] inputVals = GetValuesFromEitherDataset(inputIDs, timeIdx - inputDataLookBackIdx, simData,inputData);
                    if (inputVals == null)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                            "\" error retreiving input values.");
                        return false;
                    }
                    double outputVal = model.Iterate(inputVals, timeBase_s);
                    bool isOk = simData.AddDataPoint(model.GetOutputID(),timeIdx,outputVal);
                    if (!isOk)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Unable to add data point for  \"" 
                            + model.GetOutputID() + "\", indicating an error in initalization. ");
                        return false;
                    }
                }
            }
            simData.SetTimeStamps(inputData.GetTimeStamps().ToList());
            return true;
        }

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
        /// Creates a JSON text string serialization of this object
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
        /// Creates a file JSON representation of this object
        /// </summary>
        /// <param name="newPlantName"></param>
        public void Serialize(string newPlantName = null)
        {
            string fileName = "";
            if (newPlantName!=null)
            {
                fileName += newPlantName;
            }
            else if (plantName != null)
            {
                fileName += plantName;
            }
            fileName += ".json";

            /// var options = new JsonSerializerOptions { WriteIndented = true };
            //  var serializedTxt =  JsonSerializer.Serialize(this,options);

            var serializedTxt = SerializeTxt();

            var fileWriter = new StringToFileWriter(fileName);
            fileWriter.Write(serializedTxt);
            fileWriter.Close();
        }

    }

    //https://stackoverflow.com/questions/15880574/deserialize-collection-of-interface-instances

    /*
    public class ModelInterfaceDictionaryConverter<T> : JsonConverter
    {
        JsonSerializerSettings settings;

        public ModelInterfaceDictionaryConverter()
         {
           var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.Formatting = Formatting.Indented;
        }


        public override bool CanConvert(Type objectType) => true;

        public override object ReadJson(JsonReader reader,
         Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<T>(reader);
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value,);
        }
    }*/
    
}
