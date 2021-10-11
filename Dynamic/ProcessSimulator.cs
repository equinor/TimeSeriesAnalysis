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
        List<IProcessModel<IProcessModelParameters>> processModelList;
        Dictionary<string, IProcessModel<IProcessModelParameters>> modelDict;

        public ProcessSimulator(int timeBase_s,List<IProcessModel<IProcessModelParameters>> list=null)
        {
            this.timeBase_s = timeBase_s;
            if (list == null)
            {
                processModelList = new List<IProcessModel<IProcessModelParameters>>();
            }
            modelDict = new Dictionary<string, IProcessModel<IProcessModelParameters>>();
            // create a unique model ID for each model
            foreach (IProcessModel<IProcessModelParameters> model in processModelList)
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

            for (int stepIdx = 0; stepIdx < N; stepIdx++)
            {
                for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
                {
                    var model = modelDict[orderedSimulatorIDs.ElementAt(subSystem)];
                    string[] inputIDs = model.GetModelInputIDs();
                    double[] inputsU = simData.GetData(inputIDs, stepIdx);
                    model.Iterate(inputsU);
                }
            }
            return true;
        }


        private List<string> SubSimulatorSimulationOrder()
        {
            return null;//TODO
        }




    }
}
