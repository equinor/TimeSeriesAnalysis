using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// This determines the position in the U-vector given to Iterate for the class <c>PIDModel</c>
    /// </summary>
    enum PIDModelInputsIdx
    { 
        Y_meas =0,
        Y_setpoint=1,
        Tracking=2,
        GainScheduling=3
    }

    /// <summary>
    /// Model of PID-controller
    /// This class should acta as a wrapper for PIDcontroller class.
    /// </summary>
    public class PIDModel : ModelBaseClass,ISimulatableModel
    {
        int timeBase_s;
        PIDModelParameters pidParameters;
        PIDcontroller pid;

        public PIDModel(PIDModelParameters pidParameters, int timeBase_s, string ID="not_named")
        {
            processModelType = ProcessModelType.PID;
            SetID(ID);
            this.timeBase_s     = timeBase_s;
            this.pidParameters  = pidParameters;
            pid                 = new PIDcontroller(timeBase_s,pidParameters.Kp, 
                pidParameters.Ti_s, pidParameters.Td_s, pidParameters.NanValue);
            if (pidParameters.Scaling != null)
            {
                pid.SetScaling(pidParameters.Scaling);
            }
            else
            {// write back default scaling
                this.pidParameters.Scaling = pid.GetScaling();
            }
            //pid.SetGainScehduling(pidParameters.GainScheduling);
            pid.SetAntiSurgeParams(pidParameters.AntiSugeParams);
        }

        /// <summary>
        /// [Method not currently implemented]
        /// </summary>
        /// <param name="y0"></param>
        /// <param name="inputIdx"></param>
        /// <returns></returns>
        public double? GetSteadyStateInput(double y0, int inputIdx=0)
        {
            return null;
        }

        /// <summary>
        /// Initalizes the controller internal state(integral term) to be steady at the given process value and output value, 
        /// useful to avoid bumps when staring controller
        /// </summary>
        public void WarmStart(double y_process_abs, double y_set, double u)
        {
            pid.WarmStart(y_process_abs, y_set, u);
        }

        public void WarmStart(double[] inputs, double output)
        {
            WarmStart(inputs[0], inputs[1],output); 
        }

        /// <summary>
        /// Iterate the PID controller one step
        /// </summary>
        /// <param name="inputs">is a vector of length 2, 3 or 4. 
        /// First value is<c>y_process_abs</c>,  second value is <c>y_set_abs</c>, optional third value is
        /// <c>uTrackSignal</c>, optional fourth value is <c>gainSchedulingVariable</c>
        /// </param>
        /// <param name="badDataID">value of inputs that is to be treated as <c>NaN</c></param>
        /// <returns>the output <c>u</c> of the pid-controller. If not enough inputs, it returns <c>NaN</c></returns>
        public double Iterate(double[] inputs, double badDataID = -9999)
        {
            if (inputs.Length < 2)
            {
                return Double.NaN;
            }
            double y_process_abs = inputs[(int)PIDModelInputsIdx.Y_meas];
            double y_set_abs = inputs[(int)PIDModelInputsIdx.Y_setpoint];
            double? uTrackSignal = null;
            double? gainSchedulingVariable = null;
            if (inputs.Length >= 3)
            {
                uTrackSignal = inputs[(int)PIDModelInputsIdx.Tracking];
            }
            if (inputs.Length >= 4)
            {
                gainSchedulingVariable = inputs[(int)PIDModelInputsIdx.GainScheduling];
            }
            return pid.Iterate(y_process_abs,y_set_abs, uTrackSignal, gainSchedulingVariable);
        }

        /// <summary>
        /// Get the model parameters
        /// </summary>
        /// <returns>the parameters object of the model</returns>
        public PIDModelParameters GetModelParameters()
        {
            return pidParameters;
        }

        /// <summary>
        /// Get number of inputs (between 2 and 4)
        /// first input is always ymeas, second input is y_setpoint, optionally, 
        /// input 3 is track signal and input 4 is gain scheduling variable
        /// </summary>
        /// <returns></returns>
        override public int GetNumberOfInputs()
        {
            int nInputs = 2;
            return nInputs;// TODO:generalize
        }

        public SignalType GetOutputSignalType()
        {
            return SignalType.PID_U;
        }


    }
}
