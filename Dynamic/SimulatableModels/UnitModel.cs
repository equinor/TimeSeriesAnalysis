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
    /// Simulatable "default" process model. 
    /// <remarks>
    /// <para>
    /// This is a model that can be either dynamic or static, have one or multiple inputs
    /// and can be either linear in inputs or have inputs nonlinearity described by a
    /// second-order polynominal. Dynamics can be either 1. order time-constant, time-delay or both.
    /// The model also supports "additive" signals added to its output (intended for modeling disturbances).
    /// </para>
    /// <para>
    /// The model is designed to lend itself well to identification from industrial time-series
    /// datasets, and is supported by the accompanying identification method <seealso cref="UnitIdentifier"/>.
    /// </para>
    /// <para>
    /// This model is also intended to be co-simulated with <seealso cref="PidModel"/> by <seealso cref="PlantSimulator"/> to study
    /// process control feedback loops.
    /// </para>
    ///  <para>
    /// It is assumed that most unit processes in industrial process control systems can be described 
    /// sufficiently by this model, and thus that larger plants can be modeled by connecting unit models
    /// based on this model structure.
    /// </para>
    /// <para>
    /// It would be possible to extend this model to also describe second-order dynamics along the same principles by
    /// the introduction of one additional parameter in future work. 
    /// </para>
    /// </remarks>
    /// See also: <seealso cref="UnitParameters"/>
    /// </summary>
    public class UnitModel : ModelBaseClass, ISimulatableModel 
    {
        /// <summary>
        /// The parameters of the UnitModel.
        /// </summary>
        public  UnitParameters modelParameters;

        private LowPass lowPass;
        private TimeDelay delayObj;

        private bool isFirstIteration;
        private double[] lastGoodValuesOfInputs;

        private UnitDataSet FittedDataSet=null;

        private List<ProcessTimeDelayIdentWarnings> TimeDelayEstWarnings { get; }

        public UnitModel(){}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modelParameters">model parameter object</param>
        /// <param name="ID">a unique string that identifies this model in larger process models</param>
        [JsonConstructor]
        public UnitModel(UnitParameters modelParameters, string ID="not_named")
        {
            processModelType = ModelType.SubProcess;
            this.ID = ID;
            InitSim(modelParameters);
        }
        /// <summary>
        /// Initalizer of model that for the given dataSet also creates the resulting y_sim.
        /// </summary>
        /// <param name="modelParameters"></param>
        /// <param name="dataSet"></param>
        /// <param name="ID">a unique string that identifies this model in larger process models</param>
        public UnitModel(UnitParameters modelParameters, UnitDataSet dataSet,string ID = "not_named")
        {
            processModelType = ModelType.SubProcess;
            this.ID = ID;
            InitSim(modelParameters);
        }
        public string test()
        {
            return "test";
        }
        /// <summary>
        /// Answers if the model can be simulated with the inputs provided.
        /// </summary>
        /// <param name="explainStr">a string that explains why the model cannot be simulated if that is the case</param>
        /// <returns></returns>
        public bool IsModelSimulatable(out string explainStr)
        {
            explainStr = "";
            if (modelParameters == null)
            {
                explainStr = "modelParameters is null";
                return false;
            }
           /* if (modelParameters.LinearGains == null)
            {
                explainStr = "LinearGains is null"; 
                return false;
            }
            if (modelParameters.LinearGains.Length == 0)
            {
                explainStr = "LinearGains is empty";
                return false;
            }*/
            if (ModelInputIDs == null)
            {
                explainStr = "ModelInputIDs is null";
                return false;
            }

            if (modelParameters.LinearGains != null)
            {
                if (modelParameters.LinearGains.Length < ModelInputIDs.Length)
                {
                    explainStr = "fewer LinearGains than ModelInputIDs";
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Initalize the process model with a sampling time.
        /// </summary>
        /// <param name="modelParameters">model parameters object</param>
        public void InitSim(UnitParameters modelParameters)
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


        /// <summary>
        /// Store the fitted dataset.
        /// </summary>
        /// <param name="dataset"></param>
        public void SetFittedDataSet(UnitDataSet dataset)
        {
            FittedDataSet = dataset;
        }

        /// <summary>
        /// Returns a copy of the dataset against which the model was fitted.
        /// </summary>
        /// <returns></returns>
        public UnitDataSet GetFittedDataSet()
        {
            return FittedDataSet;
        }


        /// <summary>
        /// Returns the number of external inputs U of the model. Note that this model may have a disturbance signal
        /// added to the output in addition to the other signals.
        /// </summary>
        /// <returns></returns>
        override public int GetLengthOfInputVector()
        {
            var inputIDs = GetBothKindsOfInputIDs();

            if (inputIDs == null)
            {
                if (modelParameters.LinearGains != null)
                    return modelParameters.LinearGains.Length;
                else
                    return 0;
            }
            else
            {
                if (modelParameters.LinearGains != null)
                    return Math.Max(modelParameters.LinearGains.Length, inputIDs.Length);
                else
                    return inputIDs.Length;
            }
        }
 
        /// <summary>
        /// Get the object of model parameters contained in the model.
        /// </summary>
        /// <returns>Model parameter object</returns>
        public UnitParameters GetModelParameters()
        {
            return modelParameters;
        }

        /// <summary>
        /// Update the parameter object of the model.
        /// </summary>
        /// <param name="parameters"></param>
        public void SetModelParameters(UnitParameters parameters)
        {
            modelParameters = parameters;
        }

        /// <summary>
        /// Calcuate the steady-state input if the output and all-but-one input are known.
        /// </summary>
        /// <para>
        /// This method has no concept of disturbances, so a nonzero disturbance at time zero may throw it off.
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
            {
                double x_otherInputs = modelParameters.Bias;
                //nb! input may include a disturbance!
                if (givenInputs != null)
                {
                    for (int i = 0; i < givenInputs.Length; i++)
                    {
                        if (Double.IsNaN(givenInputs[i]))
                            continue;
                        if (i < GetModelInputIDs().Length)//model inputs
                        {
                            x_otherInputs += CalculateLinearProcessGainTerm(i, givenInputs[i]);
                            x_otherInputs += CalculateCurvatureProcessGainTerm(i, givenInputs[i]);
                        }
                        else // additive inputs
                        {
                            x_otherInputs += givenInputs[i];
                        }
                    }
                }

                double y_contributionFromInput = x0 - x_otherInputs;

                bool hasCurvature = false;
                if (modelParameters.Curvatures != null)
                {
                    Vec vec = new Vec();
                    if (!vec.ContainsBadData(modelParameters.Curvatures))
                    {
                        hasCurvature = true;
                    }
                }

                if (!hasCurvature)
                {
                    u0 = 0;
                    if (modelParameters.U0 != null)
                    {
                        u0 += modelParameters.U0[inputIdx]; 
                    }
                    u0 += y_contributionFromInput / modelParameters.LinearGains[inputIdx];

                }
                else
                {
                    double a = modelParameters.Curvatures[inputIdx];
                    if (modelParameters.UNorm != null)
                    {
                        a = a / modelParameters.UNorm[inputIdx];
                    }
                    double b = modelParameters.LinearGains[inputIdx];
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
        /// Determine the process-gain (linear) contribution to the output of a particular index for a particular value.
        /// </summary>
        /// <param name="inputIndex">the index of the input</param>
        /// <param name="u">the value of the input</param>
        /// <returns>contribution to the output y, excluding bias and curvature contributions.</returns>
        private double CalculateLinearProcessGainTerm(int inputIndex, double u)
        {
            double processGainTerm = 0;
            if (modelParameters.U0 != null)
            {
                processGainTerm += modelParameters.LinearGains[inputIndex] * (u- modelParameters.U0[inputIndex]);
            }
            else
            {
                processGainTerm += modelParameters.LinearGains[inputIndex] * u;
            }
            return processGainTerm;
        }

        /// <summary>
        /// Determine the curvature term contribution (c*(u-u0)^2/unorm) to the output from a particular input for a particular value.
        /// </summary>
        /// <param name="inputIndex">the index of the input</param>
        /// <param name="u">the value of the input</param>
        /// <returns></returns>
        private double CalculateCurvatureProcessGainTerm(int inputIndex, double u)
        {
            double curvatureTerm = 0;

            if (modelParameters.Curvatures != null)
            {
                if (Double.IsNaN(modelParameters.Curvatures[inputIndex]))
                {
                    return curvatureTerm;
                }
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
        /// Calculates the state x_ss excluding transients (y_ss = x_ss+bias).
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="badValueIndicator"></param>
        /// <returns></returns>
        private double CalculateSteadyStateWithoutAdditive(double[] inputs, double badValueIndicator=-9999)
        {
            double x_ss = modelParameters.Bias;
            // inputs U may include a disturbance as the last entry
            for (int curInput = 0; curInput < Math.Min(inputs.Length, GetLengthOfInputVector()); curInput++)
            {
                if (curInput + 1 <= modelParameters.LinearGains.Length)
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
                    x_ss += CalculateLinearProcessGainTerm(curInput, curUvalue);
                    x_ss += CalculateCurvatureProcessGainTerm(curInput, curUvalue);
                }
            }
            return x_ss;
        }

        /// <summary>
        /// Get the steady state output y for a given input (including additive terms).
        /// </summary>
        /// <param name="u">vector of input values</param>
        /// <param name="badDataID"></param>
        /// <returns></returns>
        public double? GetSteadyStateOutput(double[] u, double badDataID=-9999)
        {
            if (modelParameters.LinearGains == null)
                return 0;

            double? ret = CalculateSteadyStateWithoutAdditive(u, badDataID);
            if (ret.HasValue)
            {
                // additive output values
                for (int i = GetModelInputIDs().Length; i < u.Length; i++)
                {
                    ret += u[i];
                }
            }
            return ret;
        }

        /// <summary>
        /// Get the type of output signal.
        /// </summary>
        /// <returns></returns>
        override public SignalType GetOutputSignalType()
        {
            return SignalType.Output_Y;
        }




        /// <summary>
        /// Iterates the process model state one time step, based on the inputs given.
        /// </summary>
        /// <param name="inputs">vector of inputs U. Optionally the output disturbance D can be added as the last value.</param>
        /// <param name="timeBase_s">the time in seconds between samples</param>
        /// <param name="badValueIndicator">value in U that is to be treated as NaN</param>
        /// <returns>the updated process model state (x) - the output without any output noise or disturbance.
        ///  NaN is returned if model was not able to be identfied, or if no good U values have been given yet.
        ///  If some data points in U inputs are NaN or equal to <c>badValueIndicator</c>, the last good value is returned 
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

            // notice! the model does not use the parameters [a,b,c,unorm,td.u0] to simulate the process model
            // instead it calculates the steady-state and then filters the steady-state with LowPass to get the appropriate time constant
            //  - so it uses the parameters [linearGain, curvatureGain, Timeconstant,td]

            double x_ss = CalculateSteadyStateWithoutAdditive(inputs,badValueIndicator);

            // nb! if first iteration, start model at steady-state
            double x_dynamic = x_ss;
            if (modelParameters.TimeConstant_s >= 0)
            {
                x_dynamic = lowPass.Filter(x_ss, modelParameters.TimeConstant_s, 1, isFirstIteration);
            }
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
        /// Is the model static or dynamic?
        /// </summary>
        /// <returns>Returns true if the model is static(no time constant or time delay terms),otherwise false.</returns>
        public bool IsModelStatic()
        {
           return modelParameters.TimeConstant_s == 0 && modelParameters.TimeDelay_s == 0;
        }




        /// <summary>
        /// Solve quadratic equation "a x^2 + b*x +c =0" (second order polynomial equation in a single variable x)
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
            var writeCulture = new CultureInfo("en-US");// System.Globalization.CultureInfo.InstalledUICulture;
            var numberFormat = (System.Globalization.NumberFormatInfo)writeCulture.NumberFormat.Clone();
            numberFormat.NumberDecimalSeparator = ".";

            int sDigits = 3;
            int sDigitsUnc = 2;

            int cutOffForUsingDays_s = 86400;
            int cutOffForUsingHours_s = 3600;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine("-------------------------");
            if (modelParameters.Fitting == null)
            {
                sb.AppendLine("a priori model");
            }
            else
            {
                if (modelParameters.Fitting.WasAbleToIdentify)
                {
                    sb.AppendLine("ABLE to identify");
                }
                else
                {
                    sb.AppendLine("---NOT able to identify---");
                }
            }

            string timeConstantString = "TimeConstant : ";

            // time constant
            if (modelParameters.TimeConstant_s < cutOffForUsingHours_s)
            {
                timeConstantString +=
                    SignificantDigits.Format(modelParameters.TimeConstant_s, sDigits).ToString(writeCulture) + " sec";
            }
            else if (modelParameters.TimeConstant_s < cutOffForUsingDays_s)
            {
                timeConstantString +=
                    SignificantDigits.Format(modelParameters.TimeConstant_s/3600, sDigits).ToString(writeCulture) + " hours";
            }
            else // use days
            {
                timeConstantString +=
                    SignificantDigits.Format(modelParameters.TimeConstant_s/86400, sDigits).ToString(writeCulture) + " days";
            }
            if (modelParameters.TimeConstantUnc_s.HasValue)
            {
                timeConstantString += " ± " + SignificantDigits.Format(modelParameters.TimeConstantUnc_s.Value, sDigitsUnc).ToString(writeCulture);
            }
            sb.AppendLine(timeConstantString);

            // time delay
            if (modelParameters.TimeDelay_s < cutOffForUsingHours_s)
            {
                sb.AppendLine("TimeDelay : " + modelParameters.TimeDelay_s.ToString(writeCulture) + " sec");
            }
            else if (modelParameters.TimeDelay_s < cutOffForUsingDays_s)
            {
                sb.AppendLine("TimeDelay : " + (modelParameters.TimeDelay_s/3600).ToString(writeCulture) + " sec");
            }
            else
            {
                sb.AppendLine("TimeDelay : " + (modelParameters.TimeDelay_s/86400).ToString(writeCulture) + " days");
            }
           
            if (modelParameters.Curvatures == null)
            {

                //    sb.AppendLine("ProcessGains(lin) : " + Vec.ToString(modelParameters.GetProcessGains(), sDigits));
                //    sb.AppendLine("ProcessGainsUncertainty : " + Vec.ToString(modelParameters.GetProcessGainUncertainties(), sDigits));

                sb.AppendLine("ProcessGains(lin) : ");
                for (int idx = 0; idx < modelParameters.GetNumInputs(); idx++)
                {
                    sb.AppendLine(
                        "\t" + SignificantDigits.Format(modelParameters.GetTotalCombinedProcessGain(idx), sDigits).ToString(writeCulture) + " ± "
                            + SignificantDigits.Format(modelParameters.GetTotalCombinedProcessGainUncertainty(idx), sDigitsUnc).ToString(writeCulture)
                        );
                }
                sb.AppendLine("ProcessCurvatures : " + "none");
            }
            else
            {
                sb.AppendLine("ProcessGains(at u0) : ");
                for (int idx = 0; idx < modelParameters.GetNumInputs(); idx++)
                {
                    sb.AppendLine(
                        "\t" + SignificantDigits.Format(modelParameters.GetTotalCombinedProcessGain(idx), sDigits).ToString(writeCulture) + " ± "
                            + SignificantDigits.Format(modelParameters.GetTotalCombinedProcessGainUncertainty(idx), sDigitsUnc).ToString(writeCulture)
                        );
                }
                sb.AppendLine(" -> Linear Gain : "); //+ Vec.ToString(modelParameters.LinearGains, sDigits));
                for (int idx = 0; idx < modelParameters.GetNumInputs(); idx++)
                {
                    if (modelParameters.LinearGainUnc != null)
                    {
                        sb.AppendLine(
                            "\t" + SignificantDigits.Format(modelParameters.LinearGains[idx], sDigits).ToString(writeCulture) + " ± "
                                + SignificantDigits.Format(modelParameters.LinearGainUnc[idx], sDigitsUnc).ToString(writeCulture)
                            );
                    }
                    else
                    {
                        sb.AppendLine(
                            "\t" + SignificantDigits.Format(modelParameters.LinearGains[idx], sDigits).ToString(writeCulture)   );
                    }
                }

                sb.AppendLine(" -> Curvature Gain : ");//  + Vec.ToString(modelParameters.Curvatures, sDigits));
                for (int idx = 0; idx < modelParameters.GetNumInputs(); idx++)
                {
                    if (modelParameters.CurvatureUnc != null)
                    {
                        sb.AppendLine(
                            "\t" + SignificantDigits.Format(modelParameters.Curvatures[idx], sDigits).ToString(writeCulture) + " ± "
                                + SignificantDigits.Format(modelParameters.CurvatureUnc[idx], sDigitsUnc).ToString(writeCulture)
                            );
                    }
                    else
                    {
                        sb.AppendLine(
                            "\t" + SignificantDigits.Format(modelParameters.Curvatures[idx], sDigits).ToString(writeCulture));
                    }
                }
            }


            sb.AppendLine(" -> u0 : " + Vec.ToString(modelParameters.U0, sDigits).ToString(writeCulture));
            if (modelParameters.UNorm == null)
            {
                sb.AppendLine(" -> uNorm : " + "none");
            }
            else
            {
                sb.AppendLine(" -> uNorm : " + Vec.ToString(modelParameters.UNorm, sDigits).ToString(writeCulture));
            }

            if (modelParameters.BiasUnc == null)
            {
                sb.AppendLine("Bias : " + SignificantDigits.Format(modelParameters.Bias, sDigits).ToString(writeCulture));
            }
            else
            {
                sb.AppendLine("Bias : " + SignificantDigits.Format(modelParameters.Bias, sDigits).ToString(writeCulture) + 
                    " ± " + SignificantDigits.Format(modelParameters.BiasUnc.Value, sDigitsUnc).ToString(writeCulture));
            }

            sb.AppendLine("-------------------------");
            if (modelParameters.Fitting != null)
            {
                sb.AppendLine("Fit score(%): " + modelParameters.Fitting.FitScorePrc.ToString(writeCulture));

                sb.AppendLine("objective(diffs): " + SignificantDigits.Format(modelParameters.Fitting.ObjFunValDiff, 4).ToString(writeCulture));
                sb.AppendLine("R2(diffs): " + SignificantDigits.Format(modelParameters.Fitting.RsqDiff, 4).ToString(writeCulture));
                sb.AppendLine("R2(abs): " + SignificantDigits.Format(modelParameters.Fitting.RsqAbs, 4).ToString(writeCulture));

                sb.AppendLine("model fit data points: " + modelParameters.Fitting.NFittingTotalDataPoints + " of which " + modelParameters.Fitting.NFittingBadDataPoints + " were excluded");
                foreach (var warning in modelParameters.GetWarningList())
                    sb.AppendLine("model fit warning :" + warning.ToString());
                if (modelParameters.GetWarningList().Count == 0)
                {
                    sb.AppendLine("model fit : no error or warnings");
                }

                if (modelParameters.TimeDelayEstimationWarnings != null)
                {
                    foreach (var warning in modelParameters.TimeDelayEstimationWarnings)
                        sb.AppendLine("time delay est. warning :" + warning.ToString());
                    if (modelParameters.TimeDelayEstimationWarnings.Count == 0)
                    {
                        sb.AppendLine("time delay est : no error or warnings");
                    }
                }
                sb.AppendLine("solver: " + modelParameters.Fitting.SolverID);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Warm-starting.
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
