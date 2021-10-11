using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    class MultiProcessSimulator
    {
        List<IProcessModel<IProcessModelParameters>> processModelList;
        Dictionary<string, SubProcessSimulator<IProcessModel<IProcessModelParameters>, IProcessModelParameters>> simDict;

        public MultiProcessSimulator(List<IProcessModel<IProcessModelParameters>> list=null)
        {
            if (list == null)
            {
                processModelList = new List<IProcessModel<IProcessModelParameters>>();
            }
            simDict = new Dictionary<string, < IProcessModel < IProcessModelParameters >>, IProcessModelParameters >>();
            foreach (IProcessModel<IProcessModelParameters> model in processModelList)
            {
                string modelID = model.GetOutputID() + model.GetProcessModelType().ToString() ;
                simDict.Add(modelID,new SubProcessSimulator<IProcessModel<IProcessModelParameters>, IProcessModelParameters>(model));
            }
        }

        public bool SimulateAll()
        {
            // 
            int N = 100;

            var orderedSimulatorIDs = SubSimulatorSimulationOrder();

            for (int stepIdx = 0; stepIdx < N; stepIdx++)
            {
                for (int subSystem = 0; subSystem < orderedSimulatorIDs.Count; subSystem++)
                {
                    simDict[orderedSimulatorIDs.ElementAt(subSystem)].//TODO: iterate, not simulate?


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
