using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Generic interface that any process model needs to implement if it is to be 
    /// simulated by <seealso cref="ProcessSimulator"/>.
    /// </summary>
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

        /// <summary>
        /// If possible, set the internal state of the model so that the given inputs give the given output
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        void WarmStart(double[] inputs, double output);


        /// <summary>
        /// Calculates the value u0 of u that at steady-state will give the output value y0.
        /// This method is used when starting a method at steady-state.
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
        /// Returns the type of process model
        /// </summary>
        /// <returns></returns>
        ProcessModelType GetProcessModelType();

        /// <summary>
        /// Return the inputIDs that are "internal" i.e. related to the model and internal state x, but not "additive"
        /// </summary>
        /// <returns></returns>
        string[] GetModelInputIDs();

        /// <summary>
        /// Get both additive and model input IDs
        /// </summary>
        /// <returns></returns>
        string[] GetBothKindsOfInputIDs();


        /// <summary>
        /// Get additive input IDs
        /// </summary>
        /// <returns></returns>
        string[] GetAdditiveInputIDs();


        /// <summary>
        /// Get the output ID
        /// </summary>
        /// <returns></returns>
        string GetOutputID();

        /// <summary>
        /// Set the output ID
        /// </summary>
        /// <param name="outputID"></param>
        void SetOutputID(string outputID);

        /// <summary>
        /// Set the input IDs
        /// </summary>
        /// <param name="manipulatedVariablesU_stringIDs"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        bool SetInputIDs(string[] manipulatedVariablesU_stringIDs, int? index=null);

        /// <summary>
        /// Add an additive signal to the output
        /// </summary>
        /// <param name="additiveInputID"></param>
        void AddSignalToOutput(string additiveInputID);


        /// <summary>
        /// Get the length of the input vector
        /// </summary>
        /// <returns></returns>
        int GetLengthOfInputVector();

        /// <summary>
        /// An unique name of the process model
        /// </summary>
        /// <returns></returns>
        string GetID();

        /// <summary>
        /// Get the type of the output signal
        /// </summary>
        /// <returns></returns>
        SignalType GetOutputSignalType();




    }
}
