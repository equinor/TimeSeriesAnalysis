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
    //    [JsonInclude]
        public string plantName;

     //   [JsonInclude]
        public int timeBase_s;

     //   [JsonInclude]
        public Dictionary<string, ISimulatableModel> modelDict;

      //  [JsonInclude]
        public TimeSeriesDataSet externalInputSignals;

       // [JsonInclude]
        public ConnectionParser connections;

        private int nConnections = 0;


   

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="timeBase_s"></param>
        /// <param name="processModelList"> A list of process models, each implementing <c>ISimulatableModel</c></param>
        /// <param name="plantName">optional name of plant, used when serializing</param>
        public PlantSimulator(int timeBase_s, List<ISimulatableModel>
            processModelList, string plantName=null)
        {
            externalInputSignals = new TimeSeriesDataSet(timeBase_s);

            this.timeBase_s = timeBase_s;
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
            connections.AddAllModelObjects(modelDict);
        }



        /// <summary>
        /// Both connects and adds data to signal that is to be inputted into a specific sub-model, 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        /// <param name="index">the index of the signal, this is only needed if this is an input to a multip-input model</param>

        public string AddSignal(ISimulatableModel model, SignalType type, double[] values, int index = 0)
        {
            ModelType modelType = model.GetProcessModelType();
            string signalID = externalInputSignals.AddTimeSeries(model.GetID(), type, values, index);
            if (signalID == null)
            {
                Shared.GetParserObj().AddError("PlantSimulator.AddSignal was unable to add signal.");
                return null;
            }
            if (type == SignalType.Disturbance_D && modelType == ModelType.SubProcess)
            {
                // by convention, the disturbance is always added last to inputs
                /* List<string> newInputIDs = new List<string>(model.GetModelInputIDs());
                 newInputIDs.Add(signalID);
                 model.SetInputIDs(newInputIDs.ToArray()); ;*/

                model.AddSignalToOutput(signalID);

                return null;
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
            nConnections++;
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
            nConnections++;
            return outputId;
        }

        /// <summary>
        /// Get a TimeSeriesDataSet of all external signals of model
        /// </summary>
        /// <returns></returns>
        public TimeSeriesDataSet GetExternalSignals()
        {
            return externalInputSignals;
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
        /// <param name="simData">the simulated data set to be outputted(includes the external signals)</param>
        /// <returns></returns>
        public bool Simulate (out TimeSeriesDataSet simData)
        {
            int? N = externalInputSignals.GetLength();
            if (!N.HasValue)
            {
                Shared.GetParserObj().AddError("PlantSimulator could not run, no external signal provided.");
                simData = null;
                return simData.SetSimStatus(false);
            }

            var orderedSimulatorIDs = connections.DetermineCalculationOrderOfModels();
            simData = new TimeSeriesDataSet(timeBase_s,externalInputSignals);

            // initalize the new time-series to be created in simData.
            var init = new PlantSimulatorInitalizer(this);
            var didInit = init.ToSteadyState(ref simData);
            if (!didInit)
            {
                Shared.GetParserObj().AddError("PlantSimulator failed to initalize.");
                return simData.SetSimStatus(false);
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
                    return simData.SetSimStatus(false);
                }
                double[] inputVals = simData.GetData(inputIDs, timeIdx);

                string outputID = model.GetOutputID(); 
                if (outputID==null)
                {
                    Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                        "\" has null outputID.");
                    return simData.SetSimStatus(false);
                }
                double[] outputVals = simData.GetData(new string[]{outputID}, timeIdx);
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
                        inputDataLookBackIdx = 1;
                    }
                    double[] inputVals = simData.GetData(inputIDs, timeIdx- inputDataLookBackIdx);
                    if (inputVals == null)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Model \"" + model.GetID() +
                            "\" error retreiving input values.");
                        return simData.SetSimStatus(false);
                    }
                    double outputVal = model.Iterate(inputVals);
                    bool isOk = simData.AddDataPoint(model.GetOutputID(),timeIdx,outputVal);
                    if (!isOk)
                    {
                        Shared.GetParserObj().AddError("PlantSimulator.Simulate() failed. Unable to add data point for  \"" 
                            + model.GetOutputID() + "\", indicating an error in initalizing. ");
                        return simData.SetSimStatus(false);
                    }
                }
            }
            return simData.SetSimStatus(true);
        }



        public void Serialize()
        {
            string fileName = "PlantSim";
            if (plantName != null)
            {
                fileName += plantName;
            }
            fileName += ".json";

            /// var options = new JsonSerializerOptions { WriteIndented = true };
            //  var serializedTxt =  JsonSerializer.Serialize(this,options);

            var serializedTxt = JsonConvert.SerializeObject(this,Formatting.Indented);

            var fileWriter = new StringToFileWriter(fileName);
            fileWriter.Write(serializedTxt);
            fileWriter.Close();
        }

    }
}
