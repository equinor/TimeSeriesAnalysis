using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    class ProcessSimulator
    {
        int timeBase_s;
        Dictionary<string, ISimulatableModel> modelDict;

        TimeSeriesDataSet externalInputSignals;

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
            // create a unique model ID for each model
            foreach (ISimulatableModel model in processModelList)
            {
                int? number = null;
                string modelNumberSuffix = "";
                bool modelIDisUnique = false;
                string modelID="";
                //append a model number in the case that names are not unique
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
                }
                modelDict.Add(modelID,model);
            }
        }

       /// <summary>
       /// Both connects and adds data to signal that is to be inputted into a specific sub-model, 
       /// </summary>
       /// <param name="upstreamModel"></param>
       /// <param name="type"></param>
       /// <param name="values"></param>
       public bool AddSignal(ISimulatableModel upstreamModel, SignalType type, double[] values)
        {
            ProcessModelType modelType = upstreamModel.GetProcessModelType();
            string signalID = externalInputSignals.AddTimeSeries(upstreamModel.GetID(), type, values);
            if (signalID == null)
            {
                return false;
            }
            if (type == SignalType.Process_Distubance_D && modelType == ProcessModelType.SubProcess)
            {
                // by convention, the disturbance is always added last to inputs
                upstreamModel.SetInputIDs(new string[] { signalID }, (int)upstreamModel.GetNumberOfInputs());
                return true;
            }
            else if (type == SignalType.PID_Setpoint_Yset && modelType == ProcessModelType.PID)
            {
                upstreamModel.SetInputIDs(new string[] { signalID }, (int)PIDModelInputsIdx.Y_setpoint);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Connect two submodels
        /// </summary>
        /// <param name="upstreamModel">the upstream model, meaning the model whose output will be connected</param>
        /// <param name="downstreamModel">the downstream model, meaning the model whose input will be connected</param>
        /// <returns></returns>
        public bool ConnectModels(ISimulatableModel upstreamModel, ISimulatableModel downstreamModel)
        {
           
            ProcessModelType upstreamType = upstreamModel.GetProcessModelType();
            ProcessModelType downstreamType = downstreamModel.GetProcessModelType();
            string outputId = upstreamModel.GetID();
            if (upstreamType == ProcessModelType.PID)
            {
                outputId += "_upid";
            }
            else if (upstreamType == ProcessModelType.SubProcess)
            {
                outputId += "_y";
            } 

            upstreamModel.SetOutputID(outputId);
            int nInputs = downstreamModel.GetNumberOfInputs();
            if (nInputs == 1)
            {
                downstreamModel.SetInputIDs(new string[] { outputId });
            }
            else
            {// need to decide which input?? 
                // 
                int pidInputIdx = 0;// always connect pid to first intput u of subprocess for now

                // y->u_pid
                if (upstreamType == ProcessModelType.SubProcess && downstreamType == ProcessModelType.PID  )
                {
                    upstreamModel.SetOutputID(outputId);
                    downstreamModel.SetInputIDs(new string[] { outputId}, (int)PIDModelInputsIdx.Y_meas);
                }
                //u_pid->u 
                if (upstreamType == ProcessModelType.PID && downstreamType == ProcessModelType.SubProcess )
                {
                    upstreamModel.SetOutputID(outputId);
                    downstreamModel.SetInputIDs(new string[] { outputId }, pidInputIdx);
                }
            }
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

            var orderedSimulatorIDs = SubSimulatorSimulationOrder();
            simData = new TimeSeriesDataSet(timeBase_s,externalInputSignals);

            // TODO: initialize? 
          /*  for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                string[] inputIDs = model.GetModelInputIDs();
                double[] inputsU = simData.GetData(inputIDs,0);
                model.Iterate(inputsU);
            }*/

            for (int timeIdx = 0; timeIdx < N; timeIdx++)
            {
                for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
                {
                    var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                    string[] inputIDs = model.GetInputIDs();
                    double[] inputsU = simData.GetData(inputIDs, timeIdx);
                    double output = model.Iterate(inputsU);
                    simData.AddDataPoint(model.GetOutputID(),timeIdx,output);
                }
            }
            return true;
        }


        private List<string> SubSimulatorSimulationOrder()
        {
            //TODO: as of now there is no ordering of models by causality, this method is a stub
            return modelDict.Keys.ToList();
        }




    }
}
