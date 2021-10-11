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
        List<IProcessModelSimulate> processModelList;
        Dictionary<string, IProcessModelSimulate> modelDict;

        public ProcessSimulator(int timeBase_s,List<IProcessModelSimulate> 
            processModelList = null)
        {
            this.timeBase_s = timeBase_s;
            if (processModelList == null)
            {
                processModelList = new List<IProcessModelSimulate>();
            }
            modelDict = new Dictionary<string, IProcessModelSimulate>();
            // create a unique model ID for each model
            foreach (IProcessModelSimulate model in processModelList)
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
                    modelID = model.GetID() + model.GetProcessModelType().ToString() + modelNumberSuffix;
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
        // TODO: need to call "SetInputIDs" and "SetOutputID" for every model somehow...
        //
        public bool Simulate(TimeSeriesDataSet externalInputSignals, out TimeSeriesDataSet simData)
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
