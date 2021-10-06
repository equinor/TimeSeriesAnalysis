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
        /// <param name="badValueIndicator">is a special reserverd value of inputs U that is to be treated as NaN</param>
        /// <returns>the value of the state x of the process model at the new time step(be aware that if a disturbance is defined, 
        /// they need ot be added to states to get <c>y_sim</c>) </returns>
        double Iterate(double[] inputsU, double badValueIndicator=-9999);

        /// <summary>
        /// Get the model parameters
        /// </summary>
        /// <returns>the paramters objet of the model</returns>
        T GetModelParameters();

    }
}
