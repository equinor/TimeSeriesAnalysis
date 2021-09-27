using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Generic interface of process model 
    /// </summary>
    /// <typeparam name="T">The process model parameters class</typeparam>
    public interface IProcessModel<T> where T : IProcessModelParameters
    {
        double Iterate(double[] inputsU);

        // TODO: possibly add that a model should have a method that returns its step response

        T GetModelParameters();

    }
}
