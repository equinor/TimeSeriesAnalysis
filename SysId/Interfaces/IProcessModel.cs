using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.SysId
{
    public interface IProcessModel
    {
        double Iterate(double[] inputsU);

        // TODO: possibly add that a model should have a method that returns its step response

        IModelParameters GetModelParameters();

    }
}
