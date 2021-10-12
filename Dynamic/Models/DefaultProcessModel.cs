using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Process model class for the "Default" process model. 
    /// </summary>
    public class DefaultProcessModel : ModelBaseClass, ISimulatableModel 
    {
        private DefaultProcessModelParameters modelParameters;
        private LowPass lowPass;
        private double timeBase_s;
        private TimeDelay delayObj;

        private bool isFirstIteration;
        private double[] lastGoodValuesOfU;

        public SubProcessDataSet FittedDataSet { get; internal set; }
        public List<ProcessTimeDelayIdentWarnings> TimeDelayEstWarnings { get; internal set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modelParameters">model paramter object</param>
        /// <param name="timeBase_s">the timebase in seconds, the time interval between samples 
        /// and between calls to Iterate</param>
        /// <param name="ID">a unique string that identifies this model in larger process models</param>

        public DefaultProcessModel(DefaultProcessModelParameters modelParameters, double timeBase_s,
            string ID="not_named")
        {
            processModelType = ProcessModelType.SubProcess;
            SetID(ID);
            InitSim(timeBase_s,modelParameters);
        }

        override public int GetNumberOfInputs()
        {
            return modelParameters.ProcessGains.Length;
        }

        /// <summary>
        /// Initalizer of model that for the given dataSet also creates the resulting y_sim
        /// </summary>
        /// <param name="modelParameters"></param>
        /// <param name="dataSet"></param>
        public DefaultProcessModel(DefaultProcessModelParameters modelParameters, SubProcessDataSet dataSet)
        {
            InitSim(dataSet.TimeBase_s, modelParameters);
        }

        /// <summary>
        /// Get the objet of model paramters contained in the model
        /// </summary>
        /// <returns>Model paramter object</returns>
        //public IModelParameters GetModelParameters()
        public DefaultProcessModelParameters GetModelParameters()
        {
            return modelParameters;
        }

        public double? GetSteadyStateInput(double y0, int inputIdx=0)
        {
            // x = G*(u-u0)+bias ==>
            // y (approx) x
            // u =  (y-bias)/G+ u0 
            double u0;
            u0 = (y0 - modelParameters.Bias) / modelParameters.ProcessGains[inputIdx];
            if (modelParameters.U0 != null)
            {
                u0 += modelParameters.U0[inputIdx];
            }
            return u0;
        }


        /// <summary>
        /// Initalize the process model with a sampling time
        /// </summary>
        /// <param name="timeBase_s">the timebase in seconds, the length of time between calls to Iterate(data sampling time interval)</param>
        public void InitSim(double timeBase_s, DefaultProcessModelParameters modelParameters)
        {
            this.modelParameters = modelParameters;
            this.lastGoodValuesOfU =Vec<double>.Fill(Double.NaN,modelParameters.ProcessGains.Length);
            this.timeBase_s = timeBase_s;
            this.lowPass = new LowPass(timeBase_s);
            this.delayObj = new TimeDelay(timeBase_s, modelParameters.TimeDelay_s);
            this.isFirstIteration = true;
        }

        public void WarmStart(double[] inputs, double output)
        { 
        
        
        
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
        public double Iterate(double[] inputs, double badValueIndicator=-9999)
        {
            if (!modelParameters.AbleToIdentify())
                return Double.NaN;

            double x_static = modelParameters.Bias;

            // inputs U may include a disturbance as the last entry
            for (int curInput = 0; curInput < Math.Min(inputs.Length, GetNumberOfInputs()); curInput++)
            {
                if (curInput + 1 <= GetNumberOfInputs())
                {
                    double curUvalue = inputs[curInput];
                    if (Double.IsNaN(inputs[curInput]) || inputs[curInput] == badValueIndicator)
                    {
                        curUvalue = lastGoodValuesOfU[curInput];
                    }
                    else
                    {
                        lastGoodValuesOfU[curInput] = inputs[curInput];
                    }
                    if (modelParameters.U0 != null)
                    {
                        x_static += modelParameters.ProcessGains[curInput] *
                            (curUvalue - modelParameters.U0[curInput]);
                    }
                    else
                    {
                        x_static += modelParameters.ProcessGains[curInput] *
                                curUvalue;
                    }

                    if (modelParameters.ProcessGainCurvatures != null)
                    {
                        //TODO
                    }
                }
            }
            // nb! if first iteration, start model at steady-state
            double x_dynamic = lowPass.Filter(x_static, modelParameters.TimeConstant_s, 1, isFirstIteration);
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
            if (inputs.Length > GetNumberOfInputs())
            {
                y += inputs.Last();
            }
            return y; 
         }

        /// <summary>
        /// Is the model static or dynamic?
        /// </summary>
        /// <returns>Returns true if the model is static(no time constant or time delay terms),otherwise false.</returns>
        public bool IsModelStatic()
        {
           return modelParameters.TimeConstant_s == 0 && modelParameters.TimeDelay_s == 0;
        }

        public SignalType GetOutputSignalType()
        {
            return SignalType.Output_Y_sim;
        }

        /// <summary>
        /// Create a nice human-readable summary of all the important data contained in the model object. 
        /// This is especially useful for unit-testing and development.
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            int sDigits = 3;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DefaultProcessModel");
            sb.AppendLine("-------------------------");
            if (modelParameters.AbleToIdentify())
            {
                sb.AppendLine("ABLE to identify");
            }
            else
            {
                sb.AppendLine("---NOT able to identify---");
            }
            sb.AppendLine("TimeConstant : " + 
                SignificantDigits.Format(modelParameters.TimeConstant_s, sDigits) + " sec");
            sb.AppendLine("TimeDelay : " + modelParameters.TimeDelay_s + " sec");
            sb.AppendLine("ProcessGains : " + Vec.ToString(modelParameters.ProcessGains, sDigits));
            if (modelParameters.ProcessGainCurvatures == null)
            {
                sb.AppendLine("ProcessCurvatures : " + "not implemented");
            }
            else
            {
                sb.AppendLine("ProcessCurvatures : " + Vec.ToString(modelParameters.ProcessGainCurvatures, sDigits));
            }
            sb.AppendLine("Bias : " + SignificantDigits.Format(modelParameters.Bias, sDigits));
            sb.AppendLine("u0 : " + Vec.ToString(modelParameters.U0,sDigits));
            sb.AppendLine("-------------------------");
            sb.AppendLine("fitting objective : " + SignificantDigits.Format(modelParameters.GetFittingObjFunVal(),4) );
            sb.AppendLine("fitting R2: " + SignificantDigits.Format(modelParameters.GetFittingR2(), 4) );
            sb.AppendLine("fitting data points: " + modelParameters.NFittingTotalDataPoints + " of which " + modelParameters.NFittingBadDataPoints +" were excluded");
            foreach (var warning in modelParameters.GetWarningList())
                sb.AppendLine("fitting warning :" + warning.ToString());
            if (modelParameters.GetWarningList().Count == 0)
            {
                sb.AppendLine("fitting : no error or warnings");
            }

            foreach (var warning in modelParameters.TimeDelayEstimationWarnings)
                sb.AppendLine("time delay est. warning :" + warning.ToString());
            if (modelParameters.TimeDelayEstimationWarnings.Count == 0)
            {
                sb.AppendLine("time delay est : no error or warnings");
            }

            sb.AppendLine("solver:"+modelParameters.SolverID);

            return sb.ToString();
        }







    }
}
