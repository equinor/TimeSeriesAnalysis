using System;
using System.Collections.Generic;
using System.Linq;

using System.Text;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary> 
    /// Simulatable "default" process model. 
    /// <remarks>
    /// <para>
    /// This is a model that can be either dynamic or static, have one or multiple inputs
    /// and can be either linear in inputs or have inputs nonlinearity described by a
    /// polynominal. Dynamics can be either 1.order time-constant, time-delay or both.
    /// The model also supports "additive" signals added to its output(intended for modeling disturbances.)
    /// </para>
    /// <para>
    /// The model is designed to lend itesel well to identificaiton from industrial time-series
    /// datasets, and is supported by the accompanying identificaiton method <c>DefaultProcessModelIdentifier</c>.
    /// </para>
    /// <para>
    /// This model is also intended to be co-simulated with <c>PIDModel</c> by <c>ProcessSimulator</c> to study
    /// process control feedback loops.
    /// </para>
    ///  <para>
    /// It is assumed that for most unit processes in industrial process control systems can be described 
    /// sufficiently by this model, and thus that larger plants can be modeled by connecting unit models
    /// based on this model structure.
    /// </para>
    /// <para>
    /// It would be possible to extend this model to also describe second-order dynamics along the same principles by
    /// the intorduction of one additional paramters in future work. 
    /// </para>
    /// </remarks>
    /// <seealso cref="DefaultProcessModelParameters"/>
    /// <seealso cref="DefaultProcessModelIdentifier"/>
    /// <seealso cref="PIDModel"/>
    /// <seealso cref="ProcessSimulator"/>
    /// </summary>
    public class DefaultProcessModel : ModelBaseClass, ISimulatableModel 
    {
        private DefaultProcessModelParameters modelParameters;
        private LowPass lowPass;
        private double timeBase_s;
        private TimeDelay delayObj;

        private bool isFirstIteration;
        private double[] lastGoodValuesOfInputs;

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
                return modelParameters.ProcessGains.Length;
            }
            else
            {
                return Math.Max(modelParameters.ProcessGains.Length, inputIDs.Length);
            }
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
        public DefaultProcessModelParameters GetModelParameters()
        {
            return modelParameters;
        }

        /// <summary>
        /// Calcuate the steady-state input if the output and all-but-one input are known
        /// </summary>
        /// <param name="y0"></param>
        /// <param name="inputIdx"></param>
        /// <param name="givenInputs"></param>
        /// <returns></returns>
        public double? GetSteadyStateInput(double y0, int inputIdx=0, double[] givenInputs=null)
        {
            double u0=0;
            // x = G*(u-u0)+bias ==>
            // y (approx) x
            // u =  (y-bias)/G+ u0 
            if (givenInputs == null)
            {
                u0 = (y0 - modelParameters.Bias) / modelParameters.ProcessGains[inputIdx];
                if (modelParameters.U0 != null)
                {
                    u0 += modelParameters.U0[inputIdx];
                }
                return u0;
            }
            else
            {
                double y_otherInputs = modelParameters.Bias;
                //nb! input may include a disturbance!
                for (int i = 0; i < givenInputs.Length; i++)
                {
                    if (Double.IsNaN(givenInputs[i]))
                        continue;
                    if (i < GetModelInputIDs().Length)//model inputs
                    {
                        y_otherInputs += CalculateProcessGainTerm(i, givenInputs[i]);
                        y_otherInputs += CalcuateCurvatureTerm(i, givenInputs[i]);
                    }
                    else // additive inputs
                    {
                        y_otherInputs += givenInputs[i]; 
                    }
                }

                double y_contributionFromInput = y0 - y_otherInputs;
                if (modelParameters.Curvatures == null)
                {
                    u0 = 0;
                    if (modelParameters.U0 != null)
                    {
                        u0 += modelParameters.U0[inputIdx]; 
                    }
                    u0 += y_contributionFromInput / modelParameters.ProcessGains[inputIdx];

                }
                else
                {
                    double a = modelParameters.Curvatures[inputIdx];
                    if (modelParameters.UNorm != null)
                    {
                        a = a / modelParameters.UNorm[inputIdx];
                    }
                    double b = modelParameters.ProcessGains[inputIdx];
                    double c = -y_contributionFromInput;
                    double[] quadSolution = SolveQuadratic(a, b, c);
                    double chosenU=0;
                    if (quadSolution.Length == 1)
                    {   
                       chosenU = quadSolution[0];
                    }
                    else if (quadSolution.Length == 2)
                    { 
                        chosenU = Math.Min(quadSolution[0], quadSolution[1]);
                    }
                    if (modelParameters.U0 == null)
                    {
                        u0 = chosenU ;
                    }
                    else
                    {
                        u0 = chosenU + modelParameters.U0[inputIdx];
                    }
                }
                return u0;
            }
        }
        /// <summary>
        /// Sovel quadratic equation "a x^2 + b*x +c =0" (second order of polynomial  equation in a single variable x)
        /// x = [ -b +/- sqrt(b^2 - 4ac) ] / 2a
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public static double[] SolveQuadratic(double a, double b, double c)
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
        /// Determine the process-gain(linear) contribution to the outputof a particular index for a particular value
        /// </summary>
        /// <param name="inputIndex">the index of the input</param>
        /// <param name="u">the value of the input</param>
        /// <returns>contribution to the output y, excluding bias and curvature contributions</returns>
        private double CalculateProcessGainTerm(int inputIndex, double u)
        {
            double processGainTerm = 0;
            if (modelParameters.U0 != null)
            {
                processGainTerm += modelParameters.ProcessGains[inputIndex] * (u- modelParameters.U0[inputIndex]);
            }
            else
            {
                processGainTerm += modelParameters.ProcessGains[inputIndex] * u;
            }
            return processGainTerm;
        }

        /// <summary>
        /// Determine the curvature term contribution to the output from a particular input for a particular value
        /// </summary>
        /// <param name="inputIndex">the index of the input</param>
        /// <param name="u">the value of the input</param>
        /// <returns></returns>
        private double CalcuateCurvatureTerm(int inputIndex, double u)
        {
            double curvatureTerm = 0;

            if (modelParameters.Curvatures != null)
            {
                if (modelParameters.Curvatures.Length - 1 < inputIndex)
                {
                    return curvatureTerm;
                }
                double uNorm = 1;
                if (modelParameters.UNorm != null)
                {
                    if (modelParameters.UNorm.Length - 1 >= inputIndex)
                    {
                        uNorm = modelParameters.UNorm[inputIndex];
                    }
                }
                if (modelParameters.U0 != null)
                {
                    curvatureTerm += modelParameters.Curvatures[inputIndex] *
                        Math.Pow((u - modelParameters.U0[inputIndex]) , 2) / uNorm;
                }
                else
                {
                    curvatureTerm += modelParameters.Curvatures[inputIndex] *
                        Math.Pow(u , 2) / uNorm;
                }
            }
            return curvatureTerm;
        }



        /// <summary>
        /// Initalize the process model with a sampling time
        /// </summary>
        /// <param name="timeBase_s">the timebase in seconds, the length of time between calls to Iterate(data sampling time interval)</param>
        public void InitSim(double timeBase_s, DefaultProcessModelParameters modelParameters)
        {
            this.modelParameters = modelParameters;
            this.lastGoodValuesOfInputs =Vec<double>.Fill(Double.NaN,GetLengthOfInputVector());
            this.timeBase_s = timeBase_s;
            this.lowPass = new LowPass(timeBase_s);
            this.delayObj = new TimeDelay(timeBase_s, modelParameters.TimeDelay_s);
            this.isFirstIteration = true;

            this.SetInputIDs(new string[GetLengthOfInputVector()]);

        }

        public void WarmStart(double[] inputs, double output)
        { 

        }

        private double CalculateStaticState(double[] inputs, double badValueIndicator=-9999)
        {
            double x_static = modelParameters.Bias;

            // inputs U may include a disturbance as the last entry
            for (int curInput = 0; curInput < Math.Min(inputs.Length, GetLengthOfInputVector()); curInput++)
            {
                if (curInput + 1 <= modelParameters.ProcessGains.Length)
                {
                    double curUvalue = inputs[curInput];
                    if (Double.IsNaN(inputs[curInput]) || inputs[curInput] == badValueIndicator)
                    {
                        curUvalue = lastGoodValuesOfInputs[curInput];
                    }
                    else
                    {
                        lastGoodValuesOfInputs[curInput] = inputs[curInput];
                    }
                    x_static += CalculateProcessGainTerm(curInput, curUvalue);
                    x_static += CalcuateCurvatureTerm(curInput, curUvalue);
                }
            }
            return x_static;
        }

        public double? GetSteadyStateOutput(double[] u0)
        {
            double? ret = CalculateStaticState(u0);
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

            double x_static = CalculateStaticState(inputs,badValueIndicator);

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
            if (inputs.Length > GetModelInputIDs().Length)
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

        override public SignalType GetOutputSignalType()
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
            if (modelParameters.Curvatures == null)
            {
                sb.AppendLine("ProcessCurvatures : " + "none");
            }
            else
            {
                sb.AppendLine("ProcessCurvatures : " + Vec.ToString(modelParameters.Curvatures, sDigits));
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
