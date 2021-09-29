using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// This is a simplified interface that is inherited by IFittedProcessModelParamters.
    /// It is made to logically separate the interfaces of model paramters that were determined by fitting, and 
    /// those that are simply hard-coded either from a priori process knowledge or as example systems developed during
    /// unit-testing.
    /// 
    /// The <c>ProcessSimulator</c> class should expected models with paramters that derive from this base interface, be they 
    /// fitted or apriori.
    /// </summary>
    public interface IProcessModelParameters
    {
    }
}
