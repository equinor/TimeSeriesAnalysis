using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    interface IProcessModelIdentifier<T1,T2> where T1: IProcessModel<T2> where T2: IFittedProcessModelParameters
    {
        T1 Identify(ref SubProcessDataSet dataSet, double[] u0);

    }
}
