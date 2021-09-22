using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.SysId
{
    interface IProcessModelIdentifier<T> where T:IProcessModel
    {
        T Identify(ref ProcessDataSet dataSet);

    }
}
