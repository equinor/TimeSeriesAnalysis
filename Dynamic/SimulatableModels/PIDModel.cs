using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Simulatable industrial PID-controller
    /// <remark>
    /// <para>
    /// This class is as a wrapper for <seealso cref="PidController"/> class, 
    /// that implements <seealso cref="ISimulatableModel"/>.
    /// </para>
    /// <para>
    /// To simulate minimum or maximum select controllers, combine this class
    /// with <seealso cref="Select"/> blocks.
    /// </para>
    /// <para>
    /// The controller paramters belong to different aspects of the controller like, tuning, scaling,
    /// gain-scheduling, feedforward and anti-surge are adjusted have been collected into a number of 
    /// data-classes, linked below:
    /// </para>
    /// </remark>
    /// <seealso cref="PidParameters"/>
    /// <seealso cref="PidAntiSurgeParams"/>
    /// <seealso cref="PidFeedForward"/>
    /// <seealso cref="PidGainScheduling"/>
    /// <seealso cref="PidStatus"/>
    /// <seealso cref="PidScaling"/>
    /// <seealso cref="PidTuning"/>
    /// </summary>
    public class PidModel : ModelBaseClass, ISimulatableModel
    {
        /// <summary>
        /// The paramters which define the behavior of the PidModel
        /// </summary>
        public PidParameters pidParameters;
        private PidController pid;

        private double? WarmStart_y_process_abs ;
        private double? WarmStart_y_set;
        private double? WarmStart_u;

        private TimeDelaySamples delayObj;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pidParameters">object containing the paramters of the controller</param>
        /// <param name="ID">Each controller shoudl be given a unique ID</param>
        public PidModel(PidParameters pidParameters, string ID = "not_named")
        {
            processModelType = ModelType.PID;
            this.ID = ID;
            this.pidParameters = pidParameters;
            if (pidParameters.DelayOutputOneSample)
            {
                delayObj = new TimeDelaySamples(1);
            }
        }


        /// <summary>
        /// Answers if the model with the curret paramters makes sense to simulate
        /// </summary>
        /// <param name="explainStr"></param>
        /// <returns></returns>
        public bool IsModelSimulatable(out string explainStr)
        {
            explainStr = "";
            if (pidParameters == null)
            {
                explainStr = "pidParameters is null";
                return false;
            }
            if (Double.IsNaN(this.pidParameters.Kp) || pidParameters.Kp == pidParameters.NanValue)
            {
                explainStr = "illegal or missing Kp";
                return false;
            }
            if (Double.IsNaN(this.pidParameters.Ti_s) || pidParameters.Ti_s == pidParameters.NanValue)
            {
                explainStr = "illegal or missing Ti_s";
                return false;
            }
            if (Double.IsNaN(this.pidParameters.Td_s) || pidParameters.Td_s == pidParameters.NanValue)
            {
                explainStr = "illegal or missing Td_s";
                return false;
            }
            if (this.ModelInputIDs == null)
            {
                explainStr = "modelinputs is null";
                return false;
            }
            if (this.ModelInputIDs.Length < 2)
            {
                explainStr = "less than two modelinputs";
                return false;
            }
            if (this.ModelInputIDs[(int)PidModelInputsIdx.Y_meas] == null)
            {
                explainStr = "modelinputs signal name for Ymeas is null";
                return false;
            }
            if (this.ModelInputIDs[(int)PidModelInputsIdx.Y_setpoint] == null)
            {
                explainStr = "modelinputs signal name for Y_setpoint is null";
                return false;
            }
            return true;
        }

        /// <summary>
        /// [Method not currently implemented]
        /// </summary>
        /// <param name="y0"></param>
        /// <param name="inputIdx"></param>
        /// <param name="givenInputValues"></param>
        /// <returns></returns>
        public double? GetSteadyStateInput(double y0, int inputIdx = 0, double[] givenInputValues = null)
        {
            return null;
        }

        /// <summary>
        /// Initalizes the controller internal state(integral term) to be steady at the given process value and output value, 
        /// useful to avoid bumps when staring controller
        /// </summary>
        public void WarmStart(double y_process_abs, double y_set, double u)
        {
            WarmStart_y_process_abs = y_process_abs;
            WarmStart_y_set = y_set;
            WarmStart_u = u;

            pid = null;// create new object in next run of iterate
         //    pid.WarmStart(y_process_abs, y_set, u);
        }

        /// <summary>
        ///  Initalizes the controller internal state(integral term) to be steady at the given process value and output value, 
        /// useful to avoid bumps when staring controller
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        public void WarmStart(double[] inputs, double output)
        {
            WarmStart(inputs[0], inputs[1], output);
        }

        /// <summary>
        /// Iterate the PID controller one step
        /// </summary>
        /// <param name="inputs">is a vector of length 2, 3 or 4. 
        /// First value is<c>y_process_abs</c>,  second value is <c>y_set_abs</c>, optional third value is
        /// <c>uTrackSignal</c>, optional fourth value is <c>gainSchedulingVariable</c>
        /// </param>
        /// <param name="timeBase_s">time base in seconds</param>
        /// <param name="badDataID">value of inputs that is to be treated as <c>NaN</c></param>
        /// <returns>the output <c>u</c> of the pid-controller. If not enough inputs, it returns <c>NaN</c></returns>
        public double[] Iterate(double[] inputs, double timeBase_s,double badDataID = -9999)
        {
            if (inputs.Length < 2)
            {
                return new double[] { Double.NaN };
            }
            if (pid == null)//init PidController object on first run
            {
                pid = new PidController(timeBase_s, pidParameters.Kp,
                    pidParameters.Ti_s, pidParameters.Td_s, pidParameters.NanValue);
                if (pidParameters.Scaling != null)
                {
                    pid.SetScaling(pidParameters.Scaling);
                }
                else
                {// write back default scaling
                    this.pidParameters.Scaling = pid.GetScaling();
                }
                pid.SetGainScheduling(pidParameters.GainScheduling);
                pid.SetAntiSurgeParams(pidParameters.AntiSurgeParams);
                pid.SetFeedForward(pidParameters.FeedForward);
                pid.SetPidFiltering(pidParameters.Filtering);
                pid.SetU0ForPcontrol(pidParameters.u0);

                if (WarmStart_y_process_abs.HasValue)
                {
                    pid.WarmStart(WarmStart_y_process_abs.Value, 
                        WarmStart_y_set.Value, WarmStart_u.Value);
                }
            }

            double y_process_abs = inputs[(int)PidModelInputsIdx.Y_meas];
            double y_set_abs = inputs[(int)PidModelInputsIdx.Y_setpoint];
            double? uTrackSignal = null;
            double? gainSchedulingVariable = null;
            double? feedForwardVariable = null;
            if (inputs.Length >= 3)
            {
                double trackingSignal = inputs[(int)PidModelInputsIdx.Tracking];
                if (!Double.IsNaN(trackingSignal))
                {
                    uTrackSignal = trackingSignal;
                }
            }
            if (inputs.Length >= 4)
            {
                gainSchedulingVariable = inputs[(int)PidModelInputsIdx.GainScheduling];
            }
            if (inputs.Length >= 5)
            {
                feedForwardVariable = inputs[(int)PidModelInputsIdx.FeedForward];
            }

            var rawOut = pid.Iterate(y_process_abs, y_set_abs, uTrackSignal, gainSchedulingVariable, feedForwardVariable);

            double u = rawOut;
            if (pidParameters.DelayOutputOneSample)
            {
                u = delayObj.Delay(rawOut);
            }
            return new double[] { u };
        }

        /// <summary>
        /// Get the model parameters
        /// </summary>
        /// <returns>the parameters object of the model</returns>
        public PidParameters GetModelParameters()
        {
            return pidParameters;
        }

        /// <summary>
        /// Get number of inputs (between 2 and 4)
        /// first input is always ymeas, second input is y_setpoint, optionally, 
        /// input 3 is track signal and input 4 is gain scheduling variable
        /// </summary>
        /// <returns></returns>
        override public int GetLengthOfInputVector()
        {
            int nInputs = 2;//default and minimum is two inputs

            if (pidParameters.GainScheduling != null)
            {
                if (pidParameters.GainScheduling.GSActive_b || pidParameters.GainScheduling.GSActiveTi_b)
                {
                    nInputs = 4;
                }
            }

            // TODO: set to three if pid-controller has tracking!
            // if (pidParameters.)

            return nInputs;// TODO:generalize
        }

        /// <summary>
        /// Return the type of the output signal
        /// </summary>
        /// <returns></returns>
        override public SignalType GetOutputSignalType()
        {
            return SignalType.PID_U;
        }


        /// <summary>
        /// NOT IMPLEMENTED/NOT APPLICABLE
        /// </summary>
        /// <param name="u0"></param>
        /// <param name="badDataID">optional special value that indicates "Nan"</param>
        /// <returns></returns>
        public double? GetSteadyStateOutput(double[] u0, double badDataID)
        {
            return null;
        }

        /// <summary>
        /// Set controller in auto
        /// </summary>
        public void SetToAutoMode()
        {
            pid.SetAutoMode();
        }

        /// <summary>
        /// Set controller in auto
        /// </summary>
        public void SetToManualMode()
        {
            pid.SetManualMode();
        }

        /// <summary>
        /// Set the desired manual output fo controller. 
        /// This output will only be applied if controller is in MANUAL mode.
        /// </summary>
        /// <param name="manualOutput_prc"></param>
        public void SetManualOutput(double manualOutput_prc)
        {
            pid.SetManualOutput(manualOutput_prc);
        }


        /// <summary>
        /// Create a nice human-readable summary of all the important data contained in the model object. 
        /// This is especially useful for unit-testing and development.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return pidParameters.ToString();
        }

        /// <summary>
        /// Create deep copy/clone
        /// </summary>
        /// <returns></returns>
        public ISimulatableModel Clone(string ID = null)
        {
            string IDinternal = ID + "clone";
            if (ID != null)
                IDinternal = ID;

            var newPidParameters = new PidParameters(pidParameters); 

            var clone = new PidModel(newPidParameters, IDinternal);

            clone.ModelInputIDs = GetModelInputIDs();
            clone.outputID = GetOutputID(); 

            return clone;
        }

        /// <summary>
        /// Create deep copy/clone that replaces the existing paramters with a new paramters object, keeping the id the same
        /// </summary>
        /// <returns></returns>
        public ISimulatableModel Clone( PidParameters newPidParameters)
        {
            var clone = new PidModel(newPidParameters, ID);

            clone.ModelInputIDs = GetModelInputIDs();
            clone.outputID = GetOutputID();

            return clone;
        }

    }

}
