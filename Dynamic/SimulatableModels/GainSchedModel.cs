using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

using System.Text;

using Newtonsoft.Json;

using TimeSeriesAnalysis.Utility;
using System.Reflection;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary> 
    /// Simulatable gain-scheduled model.
    /// <remarks>
    /// <para>
    /// A model for systems that cannot be adequately modelled by UnitModel,because they either have time constants or gains or both that vary 
    /// signficantly depending on the value of one of the inputs. 
    /// </para>
    /// <para>
    /// One input is selected as the "scheduling varible" and one ore more thresholds are given for this scheduling variable. 
    /// The thresholds can be set indepently for time-contant and linear gain.
    /// </para>
    /// <para>
    /// Remember that with more thresholds defined, the higher the requirement for information content in data will be if the model is to be identified from it. 
    /// </para>
    /// <para>
    /// This should not be confuesed with "gain-scheduled" PID-control, which is a similar concept but applied to PID-control parameters.
    /// </para>
    /// 
    /// </remarks>
    /// See also: <seealso cref="GainSchedParameters"/>
    /// </summary>
    public class GainSchedModel : ModelBaseClass, ISimulatableModel 
    {
        public  GainSchedParameters modelParameters;

        private LowPass lowPass;
        private TimeDelay delayObj;

        private bool isFirstIteration;
        private double[] lastGoodValuesOfInputs;

        private GainSchedDataSet FittedDataSet=null;

        private List<ProcessTimeDelayIdentWarnings> TimeDelayEstWarnings { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modelParameters">model parameter object</param>
        /// <param name="ID">a unique string that identifies this model in larger process models</param>
        [JsonConstructor]
        public GainSchedModel(GainSchedParameters modelParameters, string ID="not_named")
        {
            processModelType = ModelType.SubProcess;
            this.ID = ID;
            InitSim(modelParameters);
        }
        /// <summary>
        /// Initalizer of model that for the given dataSet also creates the resulting y_sim
        /// </summary>
        /// <param name="modelParameters"></param>
        /// <param name="dataSet"></param>
        /// <param name="ID">a unique string that identifies this model in larger process models</param>
        public GainSchedModel(GainSchedParameters modelParameters, GainSchedDataSet dataSet,string ID = "not_named")
        {
            processModelType = ModelType.SubProcess;
            this.ID = ID;
            InitSim(modelParameters);
        }
        public bool IsModelSimulatable(out string explainStr)
        {
            explainStr = "";
            if (modelParameters == null)
            {
                explainStr = "modelParamters is null";
                return false;
            }
            if (modelParameters.LinearGains == null)
            {
                explainStr = "LinearGains is null"; 
                return false;
            }
            if (modelParameters.LinearGains.Count == 0)
            {
                explainStr = "LinearGains is empty";
                return false;
            }

            if (modelParameters.GainSchedParameterIndex >= modelParameters.LinearGains.Count)
            {
                explainStr = "GainSchedParamterIndex must be smaller than number of linear gains";
                return false;
            }

            if (ModelInputIDs == null)
            {
                explainStr = "ModelInputIDs is null";
                return false;
            }
           if (modelParameters.LinearGains == null)
            {
                explainStr = "LinearGains is null";
                return false;
            }
            else
            {
                if (modelParameters.LinearGains.Count == 0)
                {
                    explainStr = "LinearGains is empty";
                    return false;
                }

                foreach (var gain in modelParameters.LinearGains)
                {
                    if (gain == null)
                    {
                        explainStr = "null gains in LinearGainss";
                        return false;
                    }
                    if (gain.Length < ModelInputIDs.Length)
                    {
                        explainStr = "fewer LinearGains than ModelInputIDs";
                        return false;
                    }
                }

                if (modelParameters.LinearGains.Count -1  != modelParameters.LinearGainThresholds.Count())
                {
                    explainStr = "LinearGainThresholds size:"+ modelParameters.LinearGainThresholds.Count() 
                        + " should be one element smaller than LinearGains size:" + modelParameters.LinearGains.Count;
                    return false;
                }

            }
            return true;
        }


        /// <summary>
        /// Initalize the process model with a sampling time
        /// </summary>
        /// <param name="timeBase_s">the timebase in seconds>0, the length of time between calls to Iterate(data sampling time interval)</param>
        /// <param name="modelParameters">model paramters object</param>
        private void InitSim(GainSchedParameters modelParameters)
        {
            this.isFirstIteration = true;
            this.modelParameters = modelParameters;

            bool doSim = false;
            if (modelParameters.Fitting != null)
            {
                if (modelParameters.Fitting.WasAbleToIdentify)
                {
                    doSim = true;
                }
            }
            else
            {
                if (modelParameters.LinearGains != null)
                    doSim = true;
            }
            if (doSim)
            {
                this.lastGoodValuesOfInputs = Vec<double>.Fill(Double.NaN, 
                    GetLengthOfInputVector());
                 this.SetInputIDs(new string[GetLengthOfInputVector()]);
            }
        }

        public void SetFittedDataSet(GainSchedDataSet dataset)
        {
            FittedDataSet = dataset;
        }

        public GainSchedDataSet GetFittedDataSet()
        {
            return FittedDataSet;
        }

        /// <summary>
        /// Returns the number of external inputs U of the model. Note that this model may have an disturbance signal
        /// added to the output in addition to the other signals.
        /// </summary>
        /// <returns></returns>
        override public int GetLengthOfInputVector()
        {
            var inputIDs = GetBothKindsOfInputIDs();

            if (inputIDs == null)
            {
                if (modelParameters.LinearGains != null)
                    return modelParameters.LinearGains.First().Length;
                else
                    return 0;
            }
            else
            {
                if (modelParameters.LinearGains != null)
                    return Math.Max(modelParameters.LinearGains.First().Length, inputIDs.Length);
                else
                    return inputIDs.Length;
            }
        }
 
        /// <summary>
        /// Get the objet of model paramters contained in the model
        /// </summary>
        /// <returns>Model paramter object</returns>
        public GainSchedParameters GetModelParameters()
        {
            return modelParameters;
        }

        /// <summary>
        /// Update the paramter object of the model
        /// </summary>
        /// <param name="parameters"></param>
        public void SetModelParameters(GainSchedParameters parameters)
        {
            modelParameters = parameters;
        }

        /// <summary>
        /// Calcuate the steady-state input if the output and all-but-one input are known
        /// </summary>
        /// <para>
        /// This method has no concept of disturbances, so a nonzero disturbane at time zero may throw it off.
        /// </para>
        /// <param name="x0">If no additive inputs y=x, otherwise subtract additive inputs from y to get x</param>
        /// <param name="inputIdx"></param>
        /// <param name="givenInputs"></param>
        /// <returns></returns>
        public double? GetSteadyStateInput(double x0, int inputIdx=0, double[] givenInputs=null)
        {
            double u0=0;

            if (modelParameters.LinearGains == null)
            {
                return u0;
            }
            // x = G*(u-u0)+bias ==>
            // y (approx) x
            // u =  (y-bias)/G+ u0 
            /*    if (givenInputs == null)
                {
                    u0 = (y0 - modelParameters.Bias) / modelParameters.LinearGains[inputIdx];
                    if (modelParameters.U0 != null)
                    {
                        u0 += modelParameters.U0[inputIdx];
                    }
                    return u0;
                }
                else*/
            double x_otherInputs = modelParameters.Bias;
            double gainSched = givenInputs[modelParameters.GainSchedParameterIndex]; 
            //nb! input may include a disturbance!
            if (givenInputs != null)
            {
                for (int i = 0; i < givenInputs.Length; i++)
                {
                    if (Double.IsNaN(givenInputs[i]))
                        continue;
                    if (i < GetModelInputIDs().Length)//model inputs
                    {
                        x_otherInputs += CalculateLinearProcessGainTerm(i, givenInputs[i], gainSched);
                    }
                    else // additive inputs
                    {
                        x_otherInputs += givenInputs[i];
                    }
                }
            }
            double y_contributionFromInput = x0 - x_otherInputs;
            u0 = 0;
            if (modelParameters.U0 != null)
            {
                u0 += modelParameters.U0[inputIdx]; 
            }
            //TODO
            //u0 += y_contributionFromInput / modelParameters.LinearGains[inputIdx];
            return u0;
        }

        /// <summary>
        /// Determine the process-gain(linear) contribution to the output of a particular index for a particular value
        /// </summary>
        /// <param name="inputIndex">the index of the input</param>
        /// <param name="u">the value of the input</param>
        /// <param name="u_GainSched">the value of scheduling input</param>
        /// <returns>contribution to the output y, excluding bias and curvature contributions</returns>
        private double CalculateLinearProcessGainTerm(int inputIndex, double u, double u_GainSched)
        {
            double processGainTerm = 0;
            int gainSchedModelIdx = 0;
            for (int idx = 0; idx < modelParameters.LinearGainThresholds.Length; idx++)
            {
                if (u_GainSched < modelParameters.LinearGainThresholds[idx])
                {
                    gainSchedModelIdx = idx;
                    break;
                }
                else if (idx == modelParameters.LinearGainThresholds.Length - 1)
                {
                    gainSchedModelIdx = idx + 1;
                }
            }
 
            if (modelParameters.U0 != null) // For curvature only
            {
                processGainTerm = modelParameters.LinearGains.ElementAt(gainSchedModelIdx)[inputIndex] * (u- modelParameters.U0[inputIndex]);
            }
            else
            {
                processGainTerm = modelParameters.LinearGains.ElementAt(gainSchedModelIdx)[inputIndex] * u;
            }
            return processGainTerm;
        }

        /// <summary>
        /// Determine the time constant for a particular sceduling input
        /// </summary>
        /// <param name="u_GainSched">the value of scheduling input</param>
        /// <returns>time constant at particular scheduling input</returns>
        private double GetScheduledTimeConstant(double u_GainSched)
        {
            if (modelParameters.TimeConstantThresholds == null)
            {
                if (modelParameters.TimeConstant_s != null)
                    if (modelParameters.TimeConstant_s.Count() > 0)
                        return modelParameters.TimeConstant_s.First();
                 return 0;
            }
            if (modelParameters.TimeConstantThresholds.Count() == 0)
            {
                if (modelParameters.TimeConstant_s != null)
                    if (modelParameters.TimeConstant_s.Count() > 0)
                        return modelParameters.TimeConstant_s.First();
                return 0;
            }
            if (modelParameters.TimeConstant_s == null)
                return 0;
            if (modelParameters.TimeConstant_s.Count() == 0)
                return 0;

            int timeConstantIdx = 0;
            for (int idx = 0; idx < modelParameters.TimeConstantThresholds.Length; idx++)
            {
                if (u_GainSched < modelParameters.TimeConstantThresholds[idx])
                {
                    timeConstantIdx = idx;
                    break;
                }
                else if (idx == modelParameters.TimeConstantThresholds.Length - 1)
                {
                    timeConstantIdx = idx + 1;
                }
            }
            return modelParameters.TimeConstant_s[timeConstantIdx];
        }

        /// <summary>
        /// Determine the curvature term contribution(c*(u-u0)^2/unorm) to the output from a particular input for a particular value
        /// </summary>
        /// <param name="inputIndex">the index of the input</param>
        /// <param name="u">the value of the input</param>
        /// <returns></returns>
        private double CalculateStaticStateWithoutAdditive(double[] inputs, double badValueIndicator=-9999)
        {
            double x_static = modelParameters.Bias;

            // inputs U may include a disturbance as the last entry
            double gainSched = inputs[modelParameters.GainSchedParameterIndex]; // TODO: make sure GainSchedParIndx is updated

            for (int curInput = 0; curInput < Math.Min(inputs.Length, GetLengthOfInputVector()); curInput++)
            {
                if (curInput + 1 <= modelParameters.GetNumInputs())
                {
                    double curUvalue = inputs[curInput];
                    if (Double.IsNaN(inputs[curInput]) || inputs[curInput] == badValueIndicator)
                    {
                        curUvalue = lastGoodValuesOfInputs[curInput];
                    }
                    else
                    {
                        if (lastGoodValuesOfInputs == null)
                        {
                            lastGoodValuesOfInputs = new double[inputs.Length];
                        }
                        lastGoodValuesOfInputs[curInput] = inputs[curInput];
                    }
                    x_static += CalculateLinearProcessGainTerm(curInput, curUvalue, gainSched);
                }
            }
            return x_static;
        }

        /// <summary>
        /// Get the steady state output y for a given input(including additive terms)
        /// </summary>
        /// <param name="u0"></param>
        /// <returns></returns>
        public double? GetSteadyStateOutput(double[] u0)
        {
            if (modelParameters.LinearGains == null)
                return 0;

            double? ret = CalculateStaticStateWithoutAdditive(u0);
            if (ret.HasValue)
            {
                // additve output values
                for (int i = GetModelInputIDs().Length; i < u0.Length; i++)
                {
                    ret += u0[i];
                }
            }
            return ret;
        }

        /// <summary>
        /// Get the type of output signal
        /// </summary>
        /// <returns></returns>
        override public SignalType GetOutputSignalType()
        {
            return SignalType.Output_Y;
        }




        /// <summary>
        /// Iterates the process model state one time step, based on the inputs given
        /// </summary>
        /// <param name="inputs">vector of inputs U. Optionally the output disturbance D can be added as the last value.</param>
        /// <param name="badValueIndicator">value in U that is to be treated as NaN</param>
        /// <returns>the updated process model state(x) - the output without any output noise or disturbance.
        ///  NaN is returned if model was not able to be identfied, or if no good values U values yet have been given.
        ///  If some data points in U inputsU are NaN or equal to <c>badValueIndicator</c>, the last good value is returned 
        /// </returns>
        public double[] Iterate(double[] inputs, double timeBase_s,double badValueIndicator=-9999)
        {
            if (modelParameters.Fitting!= null)
            {
                if (!modelParameters.Fitting.WasAbleToIdentify)
                {
                    Shared.GetParserObj().AddError(GetID() +
                        ":Iterate() returned NaN because trying to simulate a model that was not able to be identified.");
                    return new double[] { Double.NaN };
                }
            }
            if (this.lowPass == null)
            {
                this.lowPass = new LowPass(timeBase_s);
            }
            if (this.delayObj == null)
            {
                this.delayObj = new TimeDelay(timeBase_s, modelParameters.TimeDelay_s);
            }
            // this is the case for newly created models that have not yet been fitted
            if (modelParameters.LinearGains == null)
            {
                return new double[] { 0 }; 
            }

            // notice! the model does not use the paramters [a,b,c,unorm,td.u0] to simulate the process model
            // instead it calculates the steady-state and then filters the steady-state with LowPass to get the appropriate time constant
            //  - so it uses the parmaters [linearGain, curvatureGain, Timeconstant,td]

            double x_static = CalculateStaticStateWithoutAdditive(inputs,badValueIndicator);

            // nb! if first iteration, start model at steady-state

            // modelParameters.GainSchedParameterIndex = updateGainSchedParIndx();
            double gainSched = inputs[modelParameters.GainSchedParameterIndex]; // TODO: make sure GainSchedParIndx is updated
            double TimeConstant_s = GetScheduledTimeConstant(gainSched);

            double x_dynamic = lowPass.Filter(x_static, TimeConstant_s, 1, isFirstIteration);
            isFirstIteration = false;
            double y = 0;
            if (modelParameters.TimeDelay_s <= 0)
            {
                y =  x_dynamic;
            }
            else
            {
                y = delayObj.Delay(x_dynamic);
            }
            // if a disturbance D has been given along with inputs U, then add it to output
            double? y_internal = null;
            if (inputs.Length > GetModelInputIDs().Length)
            {
                y_internal = y;
                y += inputs.Last();
            }
            if (!Double.IsNaN(modelParameters.Y_max))
            {
                if (y > modelParameters.Y_max)
                {
                    y = modelParameters.Y_max;
                }
            }
            if (!Double.IsNaN(modelParameters.Y_min))
            {
                if (y < modelParameters.Y_min)
                {
                    y = modelParameters.Y_min;
                }
            }
            if (y_internal.HasValue)
                return new double[] { y, y_internal.Value};
            else
                return new double[] { y };
         }



        /// <summary>
        /// Sovel quadratic equation "a x^2 + b*x +c =0" (second order of polynomial  equation in a single variable x)
        /// x = [ -b +/- sqrt(b^2 - 4ac) ] / 2a
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        private static double[] SolveQuadratic(double a, double b, double c)
        {
            double sqrtpart = b * b - 4 * a * c;
            double x, x1, x2, img;
            if (sqrtpart > 0)// two real solutions
            {
                x1 = (-b + System.Math.Sqrt(sqrtpart)) / (2 * a);
                x2 = (-b - System.Math.Sqrt(sqrtpart)) / (2 * a);
                return new double[] { x1, x2 };
            }
            else if (sqrtpart < 0)// two imaginary solutions
            {
                sqrtpart = -sqrtpart;
                x = -b / (2 * a);
                img = System.Math.Sqrt(sqrtpart) / (2 * a);
                return new double[] { x };// in this case, the answer is of course slightly wrong
            }
            else// one real solution
            {
                x = (-b + System.Math.Sqrt(sqrtpart)) / (2 * a);
                return new double[] { x };
            }
        }


        /// <summary>
        /// Create a nice human-readable summary of all the important data contained in the model object. 
        /// This is especially useful for unit-testing and development.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
           return "";
        }


        /// <summary>
        /// Warm-starting
        /// </summary>
        /// <param name="inputs">not used, leave as null</param>
        /// <param name="output">not used, leave as null</param>
        public void WarmStart(double[] inputs=null, double output=0)
        {
            // re-setting this variable, will cause "iterate" to start in steady-state.
            isFirstIteration = true;
            this.lowPass = null;
            this.delayObj = null;
        }

    }
}
