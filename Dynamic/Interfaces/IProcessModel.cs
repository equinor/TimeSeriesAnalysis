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
        /// <param name="badDataID">is a special reserverd value of inputs U that is to be treated as NaN</param>
        /// <returns>the value of the state x of the process model at the new time step(be aware that if a disturbance is defined, 
        /// they need ot be added to states to get <c>y_sim</c>) </returns>
        double Iterate(double[] inputsU, double badDataID=-9999);

        /// <summary>
        /// Calculates the value u0 of u that at steady-state will give the output value y0.
        /// This method is used when starting a method at steady-state
        /// In its current form,this approach only makes sense for systems of one input. 
        /// </summary>
        /// <param name="y0">value of y for which to find matching u0</param>
        /// <param name="inputIdx">index of input(only applicable if multiple inputs)</param>
        /// <returns></returns>
        double? GetSteadyStateInput(double y0, int inputIdx=0);

        /// <summary>
        /// Get the model parameters
        /// </summary>
        /// <returns>the paramters objet of the model</returns>
        T GetModelParameters();

        /// <summary>
        /// Returns the type of process model
        /// </summary>
        /// <returns></returns>
        ProcessModelType GetProcessModelType();

        /// <summary>
        /// An string that IDs what the model outputs to. 
        /// Typically this can be to a signal tag name, but it can be any unique string.
        /// </summary>
        /// <returns></returns>
        string GetOutputID();

    }
}
