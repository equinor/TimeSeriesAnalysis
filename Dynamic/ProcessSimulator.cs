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
            if (type == SignalType.Distubance_D && modelType == ProcessModelType.SubProcess)
            {
                // by convention, the disturbance is always added last to inputs
                List<string> newInputIDs = new List<string>(upstreamModel.GetInputIDs());
                newInputIDs.Add(signalID);
                upstreamModel.SetInputIDs(newInputIDs.ToArray()); ;
                return true;
            }
            else if (type == SignalType.Setpoint_Yset && modelType == ProcessModelType.PID)
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

            outputId = TimeSeriesDataSet.GetSignalName(upstreamModel.GetID(),upstreamModel.GetOutputSignalType());

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

            // initalize the new time-series to be created in simData.

            InitToSteadyState(ref simData);

            int timeIdx = 0;
            for (int modelIdx = 0; modelIdx < orderedSimulatorIDs.Count; modelIdx++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(modelIdx)];
                string[] inputIDs = model.GetInputIDs();
                double[] inputVals = simData.GetData(inputIDs, timeIdx);

                string outputID = TimeSeriesDataSet.GetSignalName(model.GetID(), model.GetOutputSignalType());
                double[] outputVals = simData.GetData(new string[]{ outputID}, timeIdx);

                model.WarmStart(inputVals, outputVals[0]);
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
        /// - If 
        /// </summary>
        /// <param name="simData">simulation dataset containing only the external signals</param>
        bool InitToSteadyState(ref TimeSeriesDataSet simData)
        {
            int? N = externalInputSignals.GetLength();
            var orderedSimulatorIDs = SubSimulatorSimulationOrder();

            // a dictionary that should contain the signalID of each "internal" simulated variable as a .Key,
            // the inital value will be calculated .Value, but is NaN unit calculated.
            var simSignalValueDict = new Dictionary<string, double>();

            // first start by finding all PID-controllers, and setting the "y" equal to "yset"
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
            }
 
            // after the PID-controlled "Y" have been set, go through each "SubProcess" model and back-calculate 
            // the steady-state u for those subsytems that have a defined "Y".
            for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
            {
                var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                if (model.GetProcessModelType() != ProcessModelType.SubProcess)
                    continue;

                string outputID = model.GetOutputID();

                bool isYgiven   = simSignalValueDict.ContainsKey(outputID)|| externalInputSignals.ContainsSignal(outputID);
                // if we know the output of the system, we can (maybe) figure out the input!
                // TODO:generalize to cases when the system has more inputs and all but one of them 
                // is known
                bool canWeFindTheUnknownInput = (model.GetNumberOfInputs() == 1) && isYgiven;
       
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

                string[] inputIDs = model.GetInputIDs();
                double? u0 = model.GetSteadyStateInput(y0);
                //TODO: generalize, but for now the upid is always index zero.
                int uIndex = 0;
                if (!simSignalValueDict.Keys.Contains(inputIDs[uIndex]) &&
                    !externalInputSignals.ContainsSignal(inputIDs[uIndex]))
                {
                    simSignalValueDict.Add(inputIDs[uIndex], u0.Value);
                }
            }

            // last step is to actually write all the values, and create otherwise empty vector to be filled.
            double nonYetSimulatedValue = Double.NaN;
            foreach (string signalID in simSignalValueDict.Keys)
            {
                simData.AddTimeSeries(signalID, Vec<double>.Concat(new double[] { simSignalValueDict[signalID] },
                    Vec<double>.Fill(nonYetSimulatedValue, N.Value-1))) ;
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
