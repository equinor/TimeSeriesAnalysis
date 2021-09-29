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
        /// <summary>
        /// Iterate the process model one timestep forward
        /// </summary>
        /// <param name="inputsU">a 2d array of inputs, one row for each time step, or <c>null</c> if model is autonomous</param>
        /// <returns>the value of the output y of the process model at the new time step</returns>
        double Iterate(double[] inputsU);

        /// <summary>
        /// Get the model parameters
        /// </summary>
        /// <returns>the paramters objet of the model</returns>
        T GetModelParameters();

    }
}
