using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Generic interface of process model (interface for ProcessSimulator to connect submodels and iterate simulation)
    /// </summary>
    /// <typeparam name="T">The process model parameters class</typeparam>
    public interface ISimulatableModel
    {
        /// <summary>
        /// Iterate the process model one timestep forward
        /// </summary>
        /// <param name="inputsU">a 2d array of inputs, one row for each time step, or <c>null</c> if model is autonomous</param>
        /// <param name="badDataID">is a special reserverd value of inputs U that is to be treated as NaN</param>
        /// <returns>the value of the state x of the process model at the new time step(be aware that if a disturbance is defined, 
        /// they need ot be added to states to get <c>y_sim</c>) </returns>
        double Iterate(double[] inputsU, double badDataID=-9999);


        void WarmStart(double[] inputs, double output);


        /// <summary>
        /// Calculates the value u0 of u that at steady-state will give the output value y0.
        /// This method is used when starting a method at steady-state
        /// In its current form,this approach only makes sense for systems of one input. 
        /// </summary>
        /// <param name="y0">value of y for which to find matching u0</param>
        /// <param name="inputIdx">index of input(only applicable if multiple inputs)</param>
        /// <param name="givenInputValues">for multip-input systems, all values except one must be given to calculate the steady-state u0</param>
        /// <returns></returns>
        double? GetSteadyStateInput(double y0, int inputIdx=0,double[] givenInputValues=null);


        /// <summary>
        /// Get the steady state value of the model output
        /// </summary>
        /// <param name="u0">vector of inputs for which the steady state is to be calculated</param>
        /// <returns>the steady-state value, if it is not possible to calculate, a <c>null</c> is returned</returns>
        double? GetSteadyStateOutput(double[] u0);

        /// <summary>
        /// Get the model parameters
        /// </summary>
        /// <returns>the paramters object of the model</returns>
       // object GetModelParameters();

        /// <summary>
        /// Returns the type of process model
        /// </summary>
        /// <returns></returns>
        ProcessModelType GetProcessModelType();


        /// <summary>
        /// Return the tag names or unique identifier of each of the variables that 
        /// "Iterate()" expects
        /// </summary>
        /// <returns></returns>
        string[] GetInputIDs();

        string GetOutputID();

        void SetOutputID(string outputID);

        bool SetInputIDs(string[] manipulatedVariablesU_stringIDs, int? index=null);


        int GetNumberOfInputs();

        /// <summary>
        /// An unique name of the process model
        /// </summary>
        /// <returns></returns>
        string GetID();

        SignalType GetOutputSignalType();




    }
}
