using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /*  class MultiProcessSimulator<T1, T2> 
          where T1 : IProcessModel<T2> where T2 : IProcessModelParameters
      {
          List<SubProcessSimulator<T1,T2>> subSimulatorList;

          public MultiProcessSimulator(List<SubProcessSimulator<T1, T2>> list = null)
          {
              if (list == null)
              {
                  subSimulatorList = new List<SubProcessSimulator<T1, T2>>();
              }
          }*/
    class MultiProcessSimulator
    {
        List<IProcessModel<IProcessModelParameters>> processModelList;

        public MultiProcessSimulator(List<IProcessModel<IProcessModelParameters>> list=null)
        {
            if (list == null)
            {
                processModelList = new List<IProcessModel<IProcessModelParameters>>();
            }
        }

        public bool SimulateAll()
        {
            foreach (IProcessModel<IProcessModelParameters> model in processModelList)
            {
                var sim = new SubProcessSimulator<IProcessModel<IProcessModelParameters>, IProcessModelParameters>(model);
            }
            return true;
        }


    }
}
