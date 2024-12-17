using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;
using System.Net.Http.Headers;
using System.Data;
using System.Reflection;
using System.ComponentModel.Design;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Identifier of the "Default" process model - a dynamic process model with time-constant, time-delay, 
    /// linear process gain and optional (nonlinear) curvature process gains.
    /// <para>
    /// This model class is sufficent for real-world linear or weakly nonlinear dynamic systems, yet also introduces the fewest possible 
    /// parameters to describe the system in an attempt to avoid over-fitting/over-parameterization.
    /// </para>
    /// <para>
    /// The "default" process model is identified using a linear-in-parameters parameterization (parameters a,b,c), so that it can be solved by linear regression
    /// and identification should thus be both fast and stable. The issue with the parameterization (a,b,c) is that the meaning of each parameter is less
    /// intuitive, for instance the time constant depends on a, but linear gain depends on both a and b, while curvature depends on a and c.
    /// Looking at the unceratinty of each parameter to determine if the model should be dynamic or static or what the uncertainty of the time constant is,
    /// is very hard, and this observation motivates re-parameterizing the model after identification.
    /// </para>
    /// <para>
    /// When assessing and simulating the model, parameters are converted into the more intuitive parameters "time constant", "linear gains" and "curvature gain"
    /// which are a different parameterization. The UnitIdentifier, UnitModel and UnitParameters classes handle this transition seamlessly to the user.
    /// Uncertainty is expressed in terms of this more intuitive parameterization, to allow for a more intuitive assessment of the parameters.
    /// </para>
    /// <para>
    /// Another advantage of the parameterization is that the model internally separates between steady-state and transient state. You can at any instance
    /// "turn off" dynamics and request the steady-state model output for the current input. This is useful if you have transient data that you want to 
    /// analyze in the steady-state, as you can then fit the model to all available data-points without having to select what data points you believe are at 
    /// steady state, then you can disable dynamic terms to do a static analysis of the dynamic model.
    /// </para>
    /// <para>
    /// Time-delay is an integer parameter, and finding the time-delay alongside continuous parameters
    /// turns the identification problem into a linear mixed-integer problem. 
    /// The time delay identification is done by splitting the time-delay estimation from continuous parameter
    /// identification, turning the solver into a sequential optimization solver. 
    /// This logic to re-run estimation for multiple time-delays and selecting the best estimate of time delay 
    /// is deferred to <seealso cref="UnitTimeDelayIdentifier"/>.
    /// </para>
    /// <para>
    /// Since the aim is to identify transients/dynamics, the regression is done on model differences rather than absolute values.
    /// </para>
    /// </summary>

    public static class UnitIdentifier 
    {
        const double obFunDiff_MinImprovement = 0.0001;
        const double rSquaredDiff_MinImprovement = 0.001;
        const int nDigits = 5;// number of significant digits in result parameters
        const bool doUnityUNorm = true;// if set true, then Unorm is always one


        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="fittingSpecs">optional fitting specs object for tuning data</param>
        /// <param name="doEstimateTimeDelay">(default:true) if set to false, time delay estimation is disabled (can drastically speeed up identification)</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public static UnitModel Identify(ref UnitDataSet dataSet,
            FittingSpecs fittingSpecs= null, bool doEstimateTimeDelay=true)
        {
            return Identify_Internal(ref dataSet,fittingSpecs, doEstimateTimeDelay);
        }

        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given, but no time-constants
        /// </summary>
        /// <param name="dataSet">The dataset containing the <c>ymeas</c> and <c>U</c> that is to be fitted against, 
        /// a new element <c>y_sim</c> is also added to this dataset</param>
        /// <param name="fittingSpecs"></param>
         /// <returns> the identified model parameters and some information about the fit</returns>
        public static UnitModel IdentifyStatic(ref UnitDataSet dataSet, FittingSpecs fittingSpecs = null)
        {
            return Identify_Internal(ref dataSet, fittingSpecs,false);
        }

        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given, but disables curvatures
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="fittingSpecs">optionally set constraints on the identification </param>
        /// <param name="doEstimateTimeDelay">if set to false, estimation of time delays are disabled</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public static UnitModel IdentifyLinear(ref UnitDataSet dataSet, FittingSpecs fittingSpecs,bool doEstimateTimeDelay = true  )
        {
            return Identify_Internal(ref dataSet, fittingSpecs, true,false, doEstimateTimeDelay);
        }

        /// <summary>
        /// Identifies the process model based on differences y[k]-y[k-1] that best fits the dataSet given, but disables curvatures
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="fittingSpecs">optionally set constraints on the identification </param>
        /// <param name="doEstimateTimeDelay">if set to false, estimation of time delays are disabled(default is true)</param>

        /// <returns> the identified model parameters and some information about the fit</returns>
        public static UnitModel IdentifyLinearDiff(ref UnitDataSet dataSet, FittingSpecs fittingSpecs, bool doEstimateTimeDelay = true)
        {
            var diffDataSet = new UnitDataSet(dataSet);
            ConvertDatasetToDiffForm(ref diffDataSet);
            var model =  Identify_Internal(ref diffDataSet, fittingSpecs, true, false, doEstimateTimeDelay);

            if (model.modelParameters.Fitting.WasAbleToIdentify)
            {
                PlantSimulator.SimulateSingleToYsim(dataSet, model);
                model.SetFittedDataSet(dataSet);
            }
            return model;

        }


        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given, but disables curvatures and time-constants
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="fittingSpecs">object of specifications for </param>
        /// <param name="doEstimateTimeDelay">set to false to disable time delay estimation (this speeds up identification manyfold) </param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public static UnitModel IdentifyLinearAndStatic(ref UnitDataSet dataSet, FittingSpecs fittingSpecs,bool doEstimateTimeDelay=true)
        {
            return Identify_Internal(ref dataSet, fittingSpecs, false, false, doEstimateTimeDelay);
        }

        /// <summary>
        /// Identifies the process model that best fits the dataSet given by minimizing differences y[k]-y[k-1], but disables curvatures and time-constants.
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="doEstimateTimeDelay">if set to false, modeling does not identify time-delays</param>
        /// <param name="fittingSpecs">Optionally sets the local working point, constraints etc. for id.</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public static UnitModel IdentifyLinearAndStaticDiff(ref UnitDataSet dataSet,FittingSpecs fittingSpecs, bool doEstimateTimeDelay = true)
        {
            ConvertDatasetToDiffForm(ref dataSet);
            return Identify_Internal(ref dataSet, fittingSpecs, false, false, doEstimateTimeDelay);
        }

        private static void ConvertDatasetToDiffForm(ref UnitDataSet dataSet)
        {
            Vec vec = new Vec(dataSet.BadDataID);
            double[] Y_meas_old = new double[dataSet.Y_meas.Length];
            dataSet.Y_meas.CopyTo(Y_meas_old, 0);
            dataSet.Y_meas = vec.Diff(Y_meas_old);

            double[,] U_old = new double[dataSet.U.GetNRows(), dataSet.U.GetNColumns()];
            for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
            {
                Matrix.ReplaceColumn(dataSet.U, colIdx, vec.Diff(dataSet.U.GetColumn(colIdx)));
            }
        }


        private static UnitModel Identify_Internal(ref UnitDataSet dataSet, FittingSpecs fittingSpecs,
            bool doUseDynamicModel=true, bool doEstimateCurvature = true, bool doEstimateTimeDelay = true)
        {
            var vec = new Vec(dataSet.BadDataID);

            // uminfit unit tests give correct time constant and time-delay when this line is commented out?
            dataSet.DetermineIndicesToIgnore(fittingSpecs);

            var constantInputInds = new List<int>();
            var correlatedInputInds = new List<int>();
            bool doNonzeroU0 = true;// should be: true
            double FilterTc_s = 0;// experimental: by default set to zero.
            bool assumeThatYkminusOneApproxXkminusOne = true;// by default this should be set to true


            double[] u0 = null;
            double[] uNorm = null;

            bool hasU0 = false;
            if (fittingSpecs == null)
            {
                hasU0 = false;
            }
            else if (fittingSpecs.u0 == null)
            {
                hasU0 = false;
            }
            else
            {
                hasU0 = true;
                u0 = fittingSpecs.u0;
            }

            bool hasUNorm = false;
            if (fittingSpecs == null)
            {
                hasUNorm = false;
            }
            else if (fittingSpecs.uNorm == null)
            {
                hasUNorm = false;
            }
            else
            {
                hasUNorm = true;
                uNorm = fittingSpecs.uNorm;
            }

            if (!hasU0)
            {
                u0 = Vec<double>.Fill(dataSet.U.GetNColumns(), 0);
                if (doNonzeroU0)
                {
                    u0 = SignificantDigits.Format(dataSet.GetAverageU(), nDigits);
                }
            }
            if (!hasUNorm)
            {
                uNorm = Vec<double>.Fill(1, dataSet.U.GetNColumns());
                for (int k = 0; k < dataSet.U.GetNColumns(); k++)
                {
                    var u = dataSet.U.GetColumn(k);
                    if (!doUnityUNorm)
                    {
                        uNorm[k] = Math.Max(Math.Abs(vec.Max(u) - u0[k]), Math.Abs(vec.Min(u) - u0[k]));
                        if (Double.IsInfinity(uNorm[k]))
                        {
                            uNorm[k] = 1;
                        }
                    }
                    //uNorm[k] = Math.Max(Math.Abs(vec.Max(u)), Math.Abs(vec.Min(u)));
                    if (vec.Max(u) == vec.Min(u))// input is constant
                    {
                        constantInputInds.Add(k);
                        uNorm[k] = 1;
                    }
                    if (uNorm[k] == 0)// avoid div by zero
                    {
                        constantInputInds.Add(k);
                        uNorm[k] = 1;
                    }
                }
                uNorm = SignificantDigits.Format(uNorm, nDigits);
            }
            




            // logic for all curves off an all curves on, treated as special cases
            bool[] allCurvesDisabled = new bool[u0.Count()];
            bool[] allCurvesEnabled = new bool[u0.Count()];
            for (int i = 0; i < u0.Count(); i++)
            {
                allCurvesDisabled[i] = false;
                allCurvesEnabled[i] = true;
            }
            /////////////////////////////////////////////////////////////////
            // Try turning off the dynamic parts and just see the static model
            // 
            var modelList = new List<UnitParameters>();
            int timeDelayIdx = 0;

            var dataset_copy = new UnitDataSet(dataSet);   

            // find a static model with no time delay or nonlinearity
            UnitParameters modelParams_StaticAndNoCurvature =
                EstimateProcessForAGivenTimeDelay
                (timeDelayIdx, dataSet, false, allCurvesDisabled,
                FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
            modelList.Add(modelParams_StaticAndNoCurvature);

            // find a dynamic model with no time delay or nonlinearity
            // (the time constant gives an upper bound on the time delay)
            /*    UnitParameters modelParams_DynamicAndNoCurvature =
                    EstimateProcessForAGivenTimeDelay
                    (timeDelayIdx, dataSet, true, allCurvesDisabled,
                    FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
                    modelList.Add(modelParams_DynamicAndNoCurvature);
            //    modelList.Add(modelParams_DynamicAndNoCurvature);
                   //   double maxExpectedTc_s = Math.Ceiling(modelParams_DynamicAndNoCurvature.TimeConstant_s/ dataSet.GetTimeBase())* dataSet.GetTimeBase() + dataSet.GetTimeBase()*3;

            */
            var warningList = new List<UnitdentWarnings>();

            //nb! this is quite a high uppper bound, in the worst case this can cause
            //  an obscene number of iterations of the below while loop. 
            TimeSpan span = dataSet.GetTimeSpan();
             double maxExpectedTc_s = span.TotalSeconds / 4;

             UnitTimeDelayIdentifier processTimeDelayIdentifyObj =
                new UnitTimeDelayIdentifier(dataSet.GetTimeBase(), maxExpectedTc_s);


            /////////////////////////////////////////////////////////////////
            // BEGIN WHILE loop to model process for different time delays               
            bool continueIncreasingTimeDelayEst = true;
            timeDelayIdx = 0;
            UnitParameters modelParams=null;
            while (continueIncreasingTimeDelayEst)
            {
                modelParams = null;
                UnitParameters modelParams_noCurvature =
                    EstimateProcessForAGivenTimeDelay
                    (timeDelayIdx, dataset_copy, doUseDynamicModel, allCurvesDisabled,
                    FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
                modelList.Add(modelParams_noCurvature);
                if (!modelParams_noCurvature.Fitting.WasAbleToIdentify)
                {
                    warningList.Add(UnitdentWarnings.DynamicModelEstimationFailed);
                }

                if (doEstimateCurvature && modelParams_noCurvature.Fitting.WasAbleToIdentify)
                {
                   UnitParameters modelParams_allCurvature =
                        EstimateProcessForAGivenTimeDelay
                        (timeDelayIdx, dataSet, doUseDynamicModel, allCurvesEnabled,
                        FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
                    

                    // only try rest of curvature models if it seems like it will help
                    // this is done to save on processing in case of many inputs (3 inputs= 8 identification runs)
                    //if (modelParams_noCurvature.FittingObjFunVal -modelParams_allCurvature.FittingObjFunVal > fitMinImprovement)
                    if (modelParams_allCurvature.Fitting.WasAbleToIdentify && 
                        modelParams_allCurvature.Fitting.RsqDiff - modelParams_noCurvature.Fitting.RsqDiff > rSquaredDiff_MinImprovement)
                    {
                        List<bool[]> allNonZeroCombinations = GetAllNonzeroBitArrays(u0.Count());
                        foreach (bool[] curCurveEnabledConfig in allNonZeroCombinations)
                        {
                            UnitParameters modelParams_withCurvature =
                                    EstimateProcessForAGivenTimeDelay
                                    (timeDelayIdx, dataSet, doUseDynamicModel, curCurveEnabledConfig,
                                    FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
                            modelList.Add(modelParams_withCurvature);
                        }
                        modelList.Add(modelParams_allCurvature);
                        modelParams = ChooseBestModel(modelParams_noCurvature, modelList);
                    }
                    else
                    {
                        modelParams = modelParams_noCurvature;
                    }
                }
                else
                {

                    modelParams = modelParams_noCurvature;
                }
                if (!continueIncreasingTimeDelayEst)
                    continue;
                // logic to deal with while loop
                timeDelayIdx++;
                processTimeDelayIdentifyObj.AddRun(modelParams);
                continueIncreasingTimeDelayEst = processTimeDelayIdentifyObj.
                    DetermineIfContinueIncreasingTimeDelay(timeDelayIdx);
                // fail-to-safe
                if (timeDelayIdx * dataSet.GetTimeBase() > maxExpectedTc_s)
                {
                    warningList.Add(UnitdentWarnings.TimeDelayAtMaximumConstraint);
                    continueIncreasingTimeDelayEst = false;
                }
                if (doEstimateTimeDelay == false)
                    continueIncreasingTimeDelayEst = false;

                // use for debugging
             /*   bool doDebugPlotting = false;//should normally be false.
                if (doDebugPlotting)
                { 
                    var debugModel = new UnitModel(modelParams, dataSet);
                    var sim = new UnitSimulator(debugModel);
                    var y_sim = sim.Simulate(ref dataSet);
                    Plot.FromList(new List<double[]> {y_sim,dataSet.Y_meas},new List<string> { "y1=ysim", "y1=ymeas" },
                        (int)dataSet.TimeBase_s, "iteration:"+ timeDelayIdx,default,"debug_it_" + timeDelayIdx);
                }*/
            }

            // the the time delay which caused the smallest object function value
            int bestTimeDelayIdx = processTimeDelayIdentifyObj.ChooseBestTimeDelay(
                out List<ProcessTimeDelayIdentWarnings> timeDelayWarnings);
            UnitParameters modelParameters =  
                (UnitParameters)processTimeDelayIdentifyObj.GetRun(bestTimeDelayIdx);
            // use static and no curvature model as fallback if more complex models failed
            if (!modelParameters.Fitting.WasAbleToIdentify && modelParams_StaticAndNoCurvature.Fitting.WasAbleToIdentify)
            {
                modelParameters = modelParams_StaticAndNoCurvature;
                timeDelayWarnings.Add(ProcessTimeDelayIdentWarnings.FallbackToLinearStaticModel);
            }
            // check if the static model is better than the dynamic model. 
            else if (modelParameters.Fitting.WasAbleToIdentify && modelParams_StaticAndNoCurvature.Fitting.WasAbleToIdentify)
            {

                // on real-world data it is sometimes obsrevd that RsqAbs and ObjFunAbs is NaN.
                // to be robust, check for nan and use any of the four different metrics in order of trust

                if (!Double.IsNaN(modelParams_StaticAndNoCurvature.Fitting.RsqAbs) &&
                    !Double.IsNaN(modelParameters.Fitting.RsqAbs))
                {
                    if (modelParams_StaticAndNoCurvature.Fitting.RsqAbs > modelParameters.Fitting.RsqAbs)
                        modelParameters = modelParams_StaticAndNoCurvature;
                }
                else if (!Double.IsNaN(modelParams_StaticAndNoCurvature.Fitting.RsqDiff) &&
                    !Double.IsNaN(modelParameters.Fitting.RsqDiff))
                {
                    if (modelParams_StaticAndNoCurvature.Fitting.RsqDiff > modelParameters.Fitting.RsqDiff)
                        modelParameters = modelParams_StaticAndNoCurvature;
                }
                else if (!Double.IsNaN(modelParams_StaticAndNoCurvature.Fitting.ObjFunValDiff) &&
                    !Double.IsNaN(modelParameters.Fitting.ObjFunValDiff))
                {
                    if (modelParams_StaticAndNoCurvature.Fitting.ObjFunValDiff < modelParameters.Fitting.ObjFunValDiff)
                        modelParameters = modelParams_StaticAndNoCurvature;
                }
                else if (!Double.IsNaN(modelParams_StaticAndNoCurvature.Fitting.ObjFunValAbs) &&
                    !Double.IsNaN(modelParameters.Fitting.ObjFunValAbs))
                {
                    if (modelParams_StaticAndNoCurvature.Fitting.ObjFunValAbs < modelParameters.Fitting.ObjFunValAbs)
                        modelParameters = modelParams_StaticAndNoCurvature;
                }
            }
            modelParameters.TimeDelayEstimationWarnings = timeDelayWarnings;
           foreach (var warning in warningList)
            {
                modelParameters.AddWarning(warning);
            }

            if (constantInputInds.Count > 0)
            {
                modelParameters.AddWarning(UnitdentWarnings.ConstantInputU);
            }
            if (correlatedInputInds.Count > 0)
            {
                modelParameters.AddWarning(UnitdentWarnings.CorrelatedInputsU);
            }
            modelParameters.FittingSpecs = fittingSpecs;
            // END While loop 
            /////////////////////////////////////////////////////////////////
            var model = new UnitModel(modelParameters, dataSet);

            // simulate
            if (modelParameters.Fitting.WasAbleToIdentify)
            {
                PlantSimulator.SimulateSingleToYsim(dataSet, model);
                model.SetFittedDataSet(dataSet);
             }
            return model;
        }


        // for three inputs, return every combination of true false
        // except false-false-false and true-true-true, (but the other six)
        private static List<bool[]> GetAllNonzeroBitArrays(int size)
        {
            List<bool[]> list = new List<bool[]>();

            var ints = new List<int>();
            for (int i = 1; i < Math.Max(size * size-1,2-1); i++)
            {
                var boolArray = new bool[size];
                BitArray bitArray = new BitArray ( new int[] { i } );
                for (int k = 0; k < size; k++)
                {
                    boolArray[k] = bitArray.Get(k);
                }
                list.Add(boolArray);
            }
            return list;
        }

        private static UnitParameters ChooseBestModel(UnitParameters fallbackModel,List<UnitParameters> allModels)
        {
            UnitParameters bestModel = fallbackModel;
            // models will be arranged from least to most numbre of curvature terms
            // in case of doubt, do not add in extra curvature that does not significantly improve the objective function
            foreach (UnitParameters curModel in allModels)
            {
                // Rsquared: higher is better
                double RsqFittingDiff_improvement = curModel.Fitting.RsqDiff - bestModel.Fitting.RsqDiff ;
                double RsqFittingAbs_improvement = curModel.Fitting.RsqAbs - bestModel.Fitting.RsqAbs;
                // objective function: lower is better
                double objFunDiff_improvement = bestModel.Fitting.ObjFunValDiff  - curModel.Fitting.ObjFunValDiff ;// positive if curmodel improves on the current best
                double objFunAbs_improvement = bestModel.Fitting.ObjFunValAbs - curModel.Fitting.ObjFunValAbs ;// positive if curmodel improves on the current best

                if (Double.IsNaN(RsqFittingAbs_improvement) || Double.IsNaN(objFunAbs_improvement))
                {
                    if (objFunDiff_improvement >= obFunDiff_MinImprovement &&
                       RsqFittingDiff_improvement >= rSquaredDiff_MinImprovement &&
                       curModel.Fitting.WasAbleToIdentify
                       )
                    {
                        bestModel = curModel;
                    }
                }
                else if (Double.IsNaN(RsqFittingDiff_improvement) || Double.IsNaN(objFunDiff_improvement))
                {
                    if (objFunAbs_improvement >= 0 &&
                        RsqFittingAbs_improvement >= 0 &&
                       curModel.Fitting.WasAbleToIdentify
                       )
                    {
                        bestModel = curModel;
                    }
                }
                else
                {
                    if (objFunDiff_improvement >= obFunDiff_MinImprovement &&
                       RsqFittingDiff_improvement >= rSquaredDiff_MinImprovement &&
                       objFunAbs_improvement >= 0 &&
                       RsqFittingAbs_improvement >= 0 &&
                       curModel.Fitting.WasAbleToIdentify
                       )
                    {
                        bestModel = curModel;
                    }
                }
            }
            return bestModel;
        }

        private static UnitParameters EstimateProcessForAGivenTimeDelay
            (int timeDelay_samples, UnitDataSet dataSet,
            bool useDynamicModel,bool[] doEstimateCurvature, 
            double FilterTc_s, double[] u0, double[] uNorm, bool assumeThatYkminusOneApproxXkminusOne
            )
        {
            Vec vec = new Vec(dataSet.BadDataID);
            var inputIndicesToRegularize = new List<int> { 1 };
            double[] ycur, yprev = null, dcur, dprev = null;
            List<double[]> ucurList = new List<double[]>();
            string solverID = "";
            if (assumeThatYkminusOneApproxXkminusOne)
                solverID += "v1.";
            else
                solverID += "v2.";
            int idxEnd = dataSet.Y_meas.Length - 1;
            int idxStart = timeDelay_samples + 1;
            if (useDynamicModel)
            {
                solverID += "Dynamic";
                for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
                {
                    ucurList.Add(Vec<double>.SubArray(dataSet.U.GetColumn(colIdx), 
                        idxStart - timeDelay_samples, idxEnd - timeDelay_samples));
                }
                ycur = Vec<double>.SubArray(dataSet.Y_meas, idxStart, idxEnd);
                yprev = Vec<double>.SubArray(dataSet.Y_meas, idxStart - 1, idxEnd - 1);
                dcur = null;
                dprev = null;
                if (dataSet.D != null)
                {
                    dcur = Vec<double>.SubArray(dataSet.D, idxStart, idxEnd);
                    dprev = Vec<double>.SubArray(dataSet.D, idxStart - 1, idxEnd - 1);
                }
            }
            else
            {
                // NB! for static model, y and u are not shifted by one sample!
                idxStart = timeDelay_samples;
                solverID += "Static";
                for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
                {
                    ucurList.Add(Vec<double>.SubArray(dataSet.U.GetColumn(colIdx), 
                        idxStart - timeDelay_samples, idxEnd - timeDelay_samples));
                }
                ycur = Vec<double>.SubArray(dataSet.Y_meas, idxStart, idxEnd);
                dcur = Vec<double>.SubArray(dataSet.D, idxStart, idxEnd);
            }

            // find instances of "badDataID" value in u or y
            var indUbad = new List<int>();
            for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
            {
                indUbad = indUbad.Union(SysIdBadDataFinder.GetAllBadIndicesPlussNext(dataSet.U.GetColumn(colIdx),
                    dataSet.BadDataID)).ToList();
            }
            List<int> indYcurBad = vec.FindValues(ycur, dataSet.BadDataID, VectorFindValueType.NaN);

            List<int> yIndicesToIgnore = new List<int>();
            if (dataSet.IndicesToIgnore != null)
                if (dataSet.IndicesToIgnore.Count > 0)
                {
                    yIndicesToIgnore = new List<int>(dataSet.IndicesToIgnore);
                }

            // the above code misses the special case that y_prev[0] is bad, as it only looks at y_cur
            if (useDynamicModel)
            {
                if (Double.IsNaN(yprev[0]) || yprev[0] == dataSet.BadDataID)
                {
                    yIndicesToIgnore.Add(0);
                }
            }
            yIndicesToIgnore = yIndicesToIgnore.Union(indUbad).ToList();
            yIndicesToIgnore = yIndicesToIgnore.Union(Index.AppendTrailingIndices(indYcurBad)).ToList();
            if (dataSet.IndicesToIgnore != null)
            {
                var indicesMinusOne = Index.Max(Index.Subtract(dataSet.IndicesToIgnore.ToArray(), 1),0).Distinct<int>();
                yIndicesToIgnore = yIndicesToIgnore.Union(
                    Index.AppendTrailingIndices(indicesMinusOne.ToList()) ).ToList();
            }
            yIndicesToIgnore.Sort();

            if (FilterTc_s > 0)
            {
                LowPass yLp = new LowPass(dataSet.GetTimeBase());
                LowPass yLpPrev = new LowPass(dataSet.GetTimeBase());
                LowPass uLp = new LowPass(dataSet.GetTimeBase());
                ycur = yLp.Filter(ycur, FilterTc_s);//todo:disturbance
                yprev = yLpPrev.Filter(yprev, FilterTc_s);//todo:disturbance
            }

            RegressionResults regResults;
            double timeConstant_s = Double.NaN;
            double[] linearProcessGains = Vec<double>.Fill(Double.NaN, ucurList.Count);
            double[] processCurvatures = Vec<double>.Fill(Double.NaN, ucurList.Count);

            List<int> phiIndicesToRegularize = new List<int>();

            if (useDynamicModel)
            {
                // Tc *xdot = x[k-1] + B*u[k-1]
                // Tc/Ts *(x[k]-x[k-1])  = x[k-1] + B*u[k-1]

                // a first order differential equation 
                // x[k] = a*x[k-1]+b*u[k-1]

                // has steady state
                // x_ss = b/(1-a)
                // and the time-constant is related to a as
                // a = 1/(1+Ts/Tc)
                // where Ts is the sampling time 

                // y[k]= a * y[k-1] + b*u
                // where a  is related to time constants and sampling rate by a = 1/(1 + Ts/Tc)
                //      ==> TimeConstant Tc = Ts*/(1/a +1)
                // and "b"  is related to steady state gain and "a" by : b = ProcessGain*(1-a)
                //      == > ProcessGain = b/(1-a)

                // actually, the a and b are related to internal states x, while y is a measurement 
                // that is subject to noise and disturbances, and it is important to formulate the identification 
                // so that noise and disturances do not influence Tc estimates.
                // one alternative is to pre-filter y, another is to formulate problems so that noise if averaged out by

                // ----------------------------------------
                //formulation2:(seems to be more robust to disturbances)
                //try to formulate the y in terms of ycur-yprev
                // y[k] = x[x] + d[k]


                // ----------------------------------------
                // to improve the perforamnce in estimating process dynamics when distrubances are in effect
                //formulation2: without the assumption y[k-1] (approx=) x[k-1]

                // to guess at the process disturbances :

                // either you need to subtract d for Y or you need to add it to Ymod
                double[] x_mod_cur_raw = new double[0];
                // -----------------v1 -------------
                // APPROXIMATE x[k-1] = y[k-1] then (NOTE THAT THIS MAY NOT BE A GOOD ASSUMPTION!) 
                // x[k] = a*y[k-1]+ b*u[k]
                // y[k]= a*y[k-1] + b*u[k] + d[k] means that 
                // y[k]-a*y[k-1]-d[k]= b*u 
                // y[k]-a*y[k-1]-(1-a)*y[k-1]-d[k]=-(1-a)*y[k-1]+ b*u    (subtract -(1-a)y[k-1] on both sides)
                // y[k]-y[k-1]-d[k]=-(1-a)*y[k-1] b*u 
                // y[k]-y[k-1]-d[k]=(a-1)*y[k-1] + b*u (RESULTING FORMUALE TO BE IDENTIFIED)
                // note that the above is not completely correct, model appears better if 
                // subtracting  Y_ols = vec.Subtract(deltaY, vec.Subtract(dcur,dprev));
                // rather than //Y_ols = vec.Subtract(deltaY, dcur);
                if (assumeThatYkminusOneApproxXkminusOne)
                {
                    double[] deltaY = vec.Subtract(ycur, yprev);
                    double[] phi1_ols = yprev;
                    double[] Y_ols = deltaY;
                    if (dcur != null && dprev!= null)
                    {
                        solverID += "(d subtracted)";
                        phi1_ols = vec.Subtract(yprev, dprev);
                        Y_ols = vec.Subtract(deltaY, vec.Subtract(dcur,dprev));
                    }
                    double[,] phi_ols2D = new double[ycur.Length, ucurList.Count + 1];
                    if (doEstimateCurvature.Contains(true))
                    {
                        int nCurvatures = 0;//
                        for (int i = 0; i < doEstimateCurvature.Count(); i++)
                        {
                            if (doEstimateCurvature[i])
                                nCurvatures++;
                        }
                        phi_ols2D = new double[ycur.Length, ucurList.Count + nCurvatures + 1];
                    }
                    phi_ols2D.WriteColumn(0, phi1_ols);
                    for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                    {
                        double uNormCur = uNorm[curIdx];
                        if (Double.IsInfinity(uNormCur) || Double.IsNaN(uNormCur) || uNormCur == 0)
                        {
                            phi_ols2D.WriteColumn(curIdx + 1, Vec<double>.Fill(0,ucurList[curIdx].Length));
                        }
                        else
                        {
                            phi_ols2D.WriteColumn(curIdx + 1, vec.Subtract(ucurList[curIdx], u0[curIdx]));
                        }
                    }
                    if (doEstimateCurvature.Contains(true))
                    {
                        int curCurvature = 0;
                        for (int curIdx = 0; curIdx < doEstimateCurvature.Count(); curIdx++)
                        {
                            if (!doEstimateCurvature[curIdx])
                                continue;
                            double uNormCur = 1;
                            if (uNorm != null)
                            {
                                if (curIdx<uNorm.Length )
                                {
                                    if (uNorm[curIdx] <= 0)
                                    {
                                        Shared.GetParserObj().AddError("uNorm illegal value, should be positive and nonzero:" 
                                            + uNorm[curIdx]);
                                    }
                                    else
                                    {
                                        uNormCur = uNorm[curIdx];
                                    }
                                }
                            }
                            if (Double.IsInfinity(uNormCur) || Double.IsNaN(uNormCur) || uNormCur == 0)
                            {
                                phi_ols2D.WriteColumn(curIdx + 1, Vec<double>.Fill(0, ucurList[curIdx].Length));
                            }
                            else
                            {
                                phi_ols2D.WriteColumn(curCurvature + ucurList.Count + 1,
                                    vec.Div(vec.Pow(vec.Subtract(ucurList[curIdx], u0[curIdx]), 2), uNormCur));
                            }
                            curCurvature++;
                        }
                    }
                    double[][] phi_ols = phi_ols2D.Transpose().Convert2DtoJagged();
                    regResults = vec.RegressRegularized(Y_ols, phi_ols, yIndicesToIgnore.ToArray(),phiIndicesToRegularize);
                }
                // ----------------- v2 -----------
                // APPROXIMATE x[k-1] = y[k-1]-d[k-1]
                // y[k] = a * (y[k-1]-d[k-1]) + b*u[k] + d[k]
                // y[k]-a*y[k-1]-d[k]= -a*d[k-1] + b*u 
                // y[k]-a*y[k-1]-(1-a)*y[k-1]-d[k]=-(1-a)*y[k-1]+a*d[k-1]+ b*u  (subtract -(1-a)y[k-1] on both sides)
                // y[k] -y[k-1]-d[k] = (a-1)*y[k-1] -a*d[k-1] +b*u (an extra "-a*d[k-1]" term)
                // or a better formulation may be 
                // y[k]=a*(y[k-1]-d[k-1]) + b*u[k] + d[k] +q
                // y[k]-d[k]=a*(y[k-1]-d[k-1])+ b*u[k] + q
                else
                {
                    double[] phi1_ols = vec.Subtract(yprev, dprev);
                    double[,] phi_ols2D = new double[ycur.Length, ucurList.Count + 1];
                    phi_ols2D.WriteColumn(0, phi1_ols);
                    for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                    {
                        phi_ols2D.WriteColumn(curIdx + 1, vec.Subtract(ucurList[curIdx], u0[curIdx]));
                    }
                    double[][] phi_ols = phi_ols2D.Transpose().Convert2DtoJagged();
                    double[] Y_ols = ycur;
                    if (dcur != null)
                    {
                        solverID += "(d subtracted)";
                        vec.Subtract(ycur, dcur);
                    }
                    regResults = vec.RegressRegularized(Y_ols, phi_ols, yIndicesToIgnore.ToArray(), inputIndicesToRegularize);
                }

                if (regResults.Param != null)
                {
                    double a;
                    if (assumeThatYkminusOneApproxXkminusOne)
                    {
                        a = regResults.Param[0] + 1;
                    }
                    else
                    {
                        a = regResults.Param[0];
                    }
                    double[] b = Vec<double>.SubArray(regResults.Param, 1, regResults.Param.Length - 2);
                    double[] c = null;
                    if (doEstimateCurvature.Contains(true))
                    {
                        b = Vec<double>.SubArray(regResults.Param, 1, ucurList.Count);
                        c = Vec<double>.SubArray(regResults.Param, ucurList.Count+1, regResults.Param.Length - 2);
                    }

                    if (a > 1)
                        a = 0;

                    // the estimation finds "a" in the difference equation 
                    // a = 1/(1 + Ts/Tc)
                    // so that 
                    // Tc = Ts/(1/a-1)

                    if (a != 0)
                        timeConstant_s = dataSet.GetTimeBase() / (1 / a - 1);
                    else
                        timeConstant_s = 0;
                    if (timeConstant_s < 0)
                        timeConstant_s = 0;

                    if (a != 0)
                        linearProcessGains = vec.Div(b, 1 - a);
                    else
                        linearProcessGains = b;

                    if (doEstimateCurvature.Contains(true) && c != null)
                    {
                        processCurvatures = Vec<double>.Fill(0, ucurList.Count);
                        int curCurvature = 0;
                        for (int curU = 0; curU < doEstimateCurvature.Count(); curU++)
                        {
                            if (!doEstimateCurvature[curU])
                                continue;
                            processCurvatures[curU] = c[curCurvature] / (1 - a);

                            if (uNorm != null)
                            {
                                processCurvatures[curU] = processCurvatures[curU] * uNorm[curU];
                            }
                            curCurvature++;
                        }
                    }
                    else
                    {
                        processCurvatures = null;
                    }
                }
            }
            else//static model
            {
                // y[k] = Kc*u[k]+ P0
                double[,] inputs2D = new double[ycur.Length, ucurList.Count];
                for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                {
                    inputs2D.WriteColumn(curIdx, vec.Subtract(ucurList[curIdx], u0[curIdx]));
                }
                double[][] inputs = inputs2D.Transpose().Convert2DtoJagged();
                double[] Y_ols = ycur;
                if (dcur != null)
                {
                    solverID += "(d subtracted)";
                    Y_ols = vec.Subtract(ycur, dcur);
                }
                regResults = vec.RegressRegularized(Y_ols, inputs, yIndicesToIgnore.ToArray(), inputIndicesToRegularize);
                timeConstant_s = 0;
                if (regResults.Param != null)
                {
                    linearProcessGains = Vec<double>.SubArray(regResults.Param, 0, regResults.Param.Length - 2);
                }
            }
            UnitParameters parameters = new UnitParameters();
        
            parameters.Fitting = new FittingInfo();
            parameters.Fitting.SolverID = solverID;
            if (dataSet.Times != null)
            {
                if (dataSet.Times.Count() > 0)
                {
                    parameters.Fitting.StartTime = dataSet.Times.First();
                    parameters.Fitting.EndTime = dataSet.Times.Last();
                }
            }
            // Vec.Regress can return very large values if y is noisy and u is stationary. 
            // in these cases varCovarMatrix is null

            const double maxAbsValueRegression = 10000;

            parameters.Fitting.NFittingTotalDataPoints = regResults.NfittingTotalDataPoints;
            parameters.Fitting.NFittingBadDataPoints = regResults.NfittingBadDataPoints;

            var uMaxList = new List<double>();
            var uMinList = new List<double>();

            for (int i = 0; i < dataSet.U.GetNColumns(); i++)
            {
                uMaxList.Add(vec.Max(dataSet.U.GetColumn(i)));
                uMinList.Add(vec.Min(dataSet.U.GetColumn(i)));
            }
            parameters.Fitting.Umax = uMaxList.ToArray();
            parameters.Fitting.Umin = uMinList.ToArray();

            if (regResults.Param == null || !regResults.AbleToIdentify)
            {
                parameters.Fitting.WasAbleToIdentify = false;
                parameters.AddWarning(UnitdentWarnings.RegressionProblemFailedToYieldSolution);
                return parameters;
            }
            else if (regResults.Param.Contains(Double.NaN) || linearProcessGains.Contains(Double.NaN))
            {
                parameters.Fitting.WasAbleToIdentify = false;
                parameters.AddWarning(UnitdentWarnings.RegressionProblemNaNSolution);
                return parameters;
            }
            else if (Math.Abs(regResults.Param[1]) > maxAbsValueRegression)
            {
                parameters.Fitting.WasAbleToIdentify = false;
                parameters.AddWarning(UnitdentWarnings.NotPossibleToIdentify);
                return parameters;
            }
            else // able to identify
            {
                parameters.Fitting.WasAbleToIdentify = true;
                parameters.TimeDelay_s = timeDelay_samples * dataSet.GetTimeBase();
                parameters.TimeConstant_s = SignificantDigits.Format(timeConstant_s, nDigits);
                parameters.LinearGains = SignificantDigits.Format(linearProcessGains, nDigits);
                parameters.Curvatures = SignificantDigits.Format(processCurvatures, nDigits);
                parameters.U0 = u0;
                parameters.UNorm = uNorm;

                (double? recalcBias, double[] y_sim_recalc) =
                    SimulateAndReEstimateBias(dataSet, parameters);
                dataSet.Y_sim = y_sim_recalc;
                if (recalcBias.HasValue)
                {
                    parameters.Bias = SignificantDigits.Format(recalcBias.Value, nDigits);
                }
                else
                {
                    parameters.AddWarning(UnitdentWarnings.ReEstimateBiasFailed);
                    parameters.Bias = SignificantDigits.Format(regResults.Param.Last(), nDigits);
                }

                parameters.Fitting.CalcCommonFitMetricsFromYmeasDataset(dataSet, yIndicesToIgnore);

                // add inn uncertainty
                if (useDynamicModel)
                    CalculateDynamicUncertainty(regResults, dataSet.GetTimeBase(), ref parameters);
                else
                    CalculateStaticUncertainty(regResults, ref parameters);

                // round uncertainty to certain number of digits
                parameters.LinearGainUnc = SignificantDigits.Format(parameters.LinearGainUnc, nDigits);
                if (parameters.BiasUnc.HasValue)
                    parameters.BiasUnc = SignificantDigits.Format(parameters.BiasUnc.Value, nDigits);
                if(parameters.TimeConstantUnc_s.HasValue)
                    parameters.TimeConstantUnc_s = SignificantDigits.Format(parameters.TimeConstantUnc_s.Value, nDigits);

                if (parameters.TimeConstant_s < 0)
                {
                    parameters.AddWarning(UnitdentWarnings.NonCausalNegativeTimeConstant);
                }

                return parameters;
            }
        }




        /// <summary>
        /// Provided that regResults is the result of fitting a statix equation x[k] = B*U}
        /// </summary>
        /// <param name="regResults">regression results, where first paramter is the "a" forgetting term</param>
        /// <param name="parameters"></param>
        private static void CalculateStaticUncertainty(RegressionResults regResults, ref UnitParameters parameters)
        {
            if (regResults.VarCovarMatrix == null)
                return;
            double a = regResults.Param[0];
            double varA = regResults.VarCovarMatrix[0][0];
            double sqrtN = Math.Sqrt(regResults.NfittingTotalDataPoints - regResults.NfittingBadDataPoints);
            /////////////////////////////////////////////////////
            // linear gain unceratinty
            var LinearGainUnc = new List<double>();
            for (int inputIdx = 0; inputIdx < parameters.GetNumInputs(); inputIdx++)
            {
                double b = regResults.Param[ inputIdx];
                double varB = regResults.VarCovarMatrix[inputIdx ][inputIdx ];
                // standard error is the population standard deviation divided by square root of the number of samples N
                double standardError_processGain = varB / sqrtN;
                // the 95% conf intern is 1.96 times the standard error for a normal distribution
                LinearGainUnc.Add(standardError_processGain * 1.96);//95% uncertainty 
            }
            parameters.LinearGainUnc = LinearGainUnc.ToArray();
            /////////////////////////////////////////////////
            // curvature unceratinty
            // 
            var CurvatureUnc = new List<double>();
            if (parameters.Curvatures != null)
            {
                for (int inputIdx = 0; inputIdx < parameters.Curvatures.Count(); inputIdx++)
                {
                    //NB! THIS CANNOT BE RIGHT, CURVATURE UNCERTANTY DOES NOT DEPEND ON VARIANE MATRIX AT ALL
                    if (Double.IsNaN(parameters.Curvatures[inputIdx]))
                    {
                        CurvatureUnc.Add(double.NaN);
                        continue;
                    }
                    // the "gain" of u is dy/du
                    // d/du [c*(u-u0)^2)]=
                    //      d/du [c*(u^2-2*u*u0 +2u0))]= 
                    //      d/du [c*(u^2-2u*u0)] =
                    //      c*[2*u-2u*u0]

                    double curC = parameters.Curvatures[inputIdx];
                    double curU0 = parameters.U0[inputIdx];
                    double curUnorm = parameters.UNorm[inputIdx];
                    double varCurCurvature = Math.Abs(curC / curUnorm * (2 * curU0 - 2 * Math.Pow(curU0, 2)));
                    double standarerrorCurvUnc = varCurCurvature / sqrtN;
                    CurvatureUnc.Add(standarerrorCurvUnc * 1.96);
                }
            }
            parameters.CurvatureUnc = CurvatureUnc.ToArray();
            parameters.TimeConstantUnc_s = null;//95 prc unceratinty
            // bias uncertainty 
            int biasInd = regResults.Param.Length - 1;
            //parameters.BiasUnc = regResults.VarCovarMatrix[biasInd][biasInd];
            parameters.BiasUnc = regResults.VarCovarMatrix[biasInd][biasInd] / sqrtN * 1.96;
        }


        // references:
        // http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat406_2004/Notes/variance.pdf
        //https://stats.stackexchange.com/questions/41896/varx-is-known-how-to-calculate-var1-x
        /// <summary>
        /// Provided that regResults is the result of fitting a dynamic equation x[k] = a*x[k-1]+B*U}
        /// </summary>
        /// <param name="regResults">regression results, where first paramter is the "a" forgetting term</param>
        /// <param name="timeBase_s"></param>
        /// <param name="parameters"></param>
        private static void CalculateDynamicUncertainty(RegressionResults regResults,double timeBase_s, ref UnitParameters parameters)
        {
            if (regResults.VarCovarMatrix == null)
                return;

            double a = regResults.Param[0];
            double varA = regResults.VarCovarMatrix[0][0];
            double sqrtN = Math.Sqrt(regResults.NfittingTotalDataPoints - regResults.NfittingBadDataPoints);

            /////////////////////////////////////////////////////
            // linear gain unceratinty
            
            var LinearGainUnc = new List<double>();
            for (int inputIdx= 0; inputIdx< parameters.GetNumInputs(); inputIdx++)
            { 
                double b = regResults.Param[1+inputIdx];
                double varB = regResults.VarCovarMatrix[inputIdx+1][inputIdx+1];
                double covAB1 = regResults.VarCovarMatrix[0][inputIdx+1];
                // first approach:
                // process gain uncertainty 
                // process gain dy/du: b /(1- a) 
                // idea to take first order taylor( where mu_x = mean value of x):
                //var(g(x)) approx (dg(mu_x)/dx)^2 * varx
               // var(x1+x2) = var(x1) + var(x2) +2*cov(x1,x2)
                //var(g(x1,x2)) (approx=) (dg/dx1)^2 *var(x1) + (dg/dx2)^2 *var(x2) +dg/dx1dx2 *cov(a,b1) 
                // for
                // g(a,b) = b/(1-a)
                // var(g(a,b)) (approx=) (dg/da)^2 *var(a) + (dg/db)^2 *var(b) +dg/dadb *cov(a,b) 
                 double dg_da = b  * -a* Math.Pow(1 - a, -2);//chain rule
                 double dg_db = 1 / (1 - a);
                 double covTerm = a* Math.Pow(1 - a, -2);
                 double varbdivby1minusA = 
                    (Math.Pow(dg_da, 2) * varA + Math.Pow(dg_db, 2) * varB + covTerm * covAB1) ;
                // second approach : 
                // variance of multipled vairables : var(XY) = var(x)*var(y) +var(x)*(E(Y))^2 + varY*E(X)^2
                // variance of var (b*(1/(1-a))) = var(b)*var(1/(1-a)) + var(b)*E(1/(1-a))^2 +var(1/(1-a))^2 * b
                // var(1/(1-a)) - >first order linear tayolor approximation.
                // 
                // https://stats.stackexchange.com/questions/41896/varx-is-known-how-to-calculate-var1-x
                // if you use first order taylor expanison, 
                // Var[g(X)]≈g′(μ)^2Var(X)
                // var(1/x) = 1/(mu^4)*var(x), where mu is the E
                //  double var1divBya = Math.Pow(1-a, -4) * varA;
                // double varbdivby1minusA = varB * var1divBya + varB * Math.Pow(1 / (1 - a), 2) + Math.Pow(var1divBya, 2) * b;

                // common to both appraoches:
                // standard error is the population standard deviation divided by square root of the number of samples N
                double standardError_processGain = varbdivby1minusA / sqrtN; 

                // the 95% conf intern is 1.96 times the standard error for a normal distribution
                LinearGainUnc.Add(standardError_processGain * 1.96);//95% uncertainty 
            }
            parameters.LinearGainUnc = LinearGainUnc.ToArray();

            /////////////////////////////////////////////////
            // curvature unceratinty
            // 
            var CurvatureUnc = new List<double>();
            if (parameters.Curvatures != null)
            {
                for (int inputIdx = 0; inputIdx < parameters.Curvatures.Count(); inputIdx++)
                {
                    if (Double.IsNaN(parameters.Curvatures[inputIdx]))
                    {
                        CurvatureUnc.Add(double.NaN);
                        continue;
                    }
                    // the "gain" of u is dy/du
                    // d/du [c/(1-a)*(u-u0)^2)]=
                    //      d/du [c/(1-a)*(u^2-2*u*u0 +2u0))]= 
                    //      d/du [c/(1-a)*(u^2-2u*u0)] =
                    //      c/(1-a)*[2*u-2u*u0]

                    double curC = parameters.Curvatures[inputIdx];
                    double curU0 = parameters.U0[inputIdx];
                    double curUnorm = parameters.UNorm[inputIdx];
                    double varCurCurvature = Math.Abs(curC/ (1 - a) / curUnorm * (2 * curU0 - 2 * Math.Pow(curU0, 2)));
                    double standarerrorCurvUnc = varCurCurvature / sqrtN;
                    CurvatureUnc.Add(standarerrorCurvUnc*1.96);
                }
            }
            parameters.CurvatureUnc = CurvatureUnc.ToArray();
            /////////////////////////////////////////////////
            // time constant uncertainty
            // Tc = TimeBase_s * (1/a - 1)^1
            // var(1/(1/a - 1)) - > first order linear tayolor approximation.
            // // Var[g(X)]≈dg/dmu(μ)^2 * Var(X)

            // derivate(chain rule) : d[(1/a - 1)^-1]/da == (1/a-1)^-2*(a^-2)

            // remmember that Var(cX) = c^2*Var(X)!
            // thus since Tc = TimeBase_s * (1/a - 1)^-1
            //var(Tc = TimeBase_s * (1/a - 1)) = TimeBase_s^2 * var(1/a - 1)

            // uncertainty = var(Tc*(1/a-1)^-1)*sqrt(N)*1.96
            double varTc = (Math.Pow(timeBase_s, 2) * Math.Pow(1 / a - 1, -2) * Math.Pow(a, -2) * varA);
            double standardErrorTc = varTc/ sqrtN;
             parameters.TimeConstantUnc_s = standardErrorTc *1.96;//95 prc unceratinty
            //parameters.TimeConstantUnc_s = varTc;

            // bias uncertainty 
            int biasInd = regResults.Param.Length-1;
            //parameters.BiasUnc = regResults.VarCovarMatrix[biasInd][biasInd];
             parameters.BiasUnc = regResults.VarCovarMatrix[biasInd][biasInd] /sqrtN * 1.96;
        }

        /// <summary>
        /// For a given set of paramters and a dataset, find the bias which gives the lowest mean offset
        /// (this can be especially useful when identification uses the "difference" formulation)
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        static public (double?, double[]) SimulateAndReEstimateBias(UnitDataSet dataSet, UnitParameters parameters)
        {
            UnitDataSet internalData = new UnitDataSet(dataSet);
            parameters.Bias = 0;
            double nanValue = internalData.BadDataID;
            var model = new UnitModel(parameters);
            var simulator = new UnitSimulator(model); // TODO: remove last UnitSimulator reference.
            var y_sim = simulator.Simulate(ref internalData);
           // (var isOk, var y_sim) = PlantSimulator.SimulateSingle(internalData, model,false, 0, true);

            var yMeas_exceptIgnoredValues = internalData.Y_meas;
            var ySim_exceptIgnoredValues = y_sim;
            if (dataSet.IndicesToIgnore != null)
            {
                for (int ind = 0; ind < dataSet.IndicesToIgnore.Count(); ind++)
                {
                    int indToIgnore = dataSet.IndicesToIgnore.ElementAt(ind);
                    yMeas_exceptIgnoredValues[indToIgnore] = Double.NaN;//nan values are ignored by Vec.Means
                    if (ySim_exceptIgnoredValues != null)
                    {
                        ySim_exceptIgnoredValues[indToIgnore] = Double.NaN;//nan values are ignored by Vec.Means
                    }
                }
            }
            double[] diff = (new Vec(nanValue)).Subtract(yMeas_exceptIgnoredValues, ySim_exceptIgnoredValues);
            double? bias = (new Vec(nanValue)).Mean(diff);
            double[] y_sim_ret = null;
            if (bias.HasValue && y_sim != null)
            {
                y_sim_ret = (new Vec(nanValue)).Add(y_sim, bias.Value);
            }
            return (bias,y_sim_ret);
        }

        /// <summary>
        /// Freezed one input to a given pre-determined value, but re-identifies other static paramters.
        /// This is useful if doing a "global search" where varying a single gain.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="inputIdxToFix">the index of the value to freeze</param>
        /// <param name="inputProcessGainValueToFix">the linear gain to freeze the at</param>
        /// <param name="u0_fixedInput"></param>
        /// <param name="uNorm_fixedInput"></param>
        /// <returns>identified model, to check if identification suceeded, check 
        /// .modelParameters.Fitting.WasAbleToIdentify</returns>
        public static UnitModel IdentifyLinearAndStaticWhileKeepingLinearGainFixed(UnitDataSet dataSet, int inputIdxToFix, 
            double inputProcessGainValueToFix, double u0_fixedInput, double uNorm_fixedInput )
        {

            var fittingSpecs = new FittingSpecs();
            var internalDataset = new UnitDataSet(dataSet);
            var vec = new Vec(dataSet.BadDataID);
            var D_fixedInput = vec.Multiply(vec.Multiply(vec.Subtract(dataSet.U.GetColumn(inputIdxToFix), u0_fixedInput),uNorm_fixedInput),
                inputProcessGainValueToFix);
            if (dataSet.D != null)
            {
                internalDataset.D = vec.Add(internalDataset.D, D_fixedInput);
            }
            else
            {
                internalDataset.D = D_fixedInput;
            }



            // remove the input that is frozen from the dataset given to the "identify" algorithm
            double[,] newU = new double[internalDataset.U.GetNRows(),internalDataset.U.GetNColumns()-1];
            int writeColIdx = 0;
            for (int colIdx = 0; colIdx < internalDataset.U.GetNColumns(); colIdx++)
            {
                if (colIdx == inputIdxToFix)
                    continue;
                newU.WriteColumn(writeColIdx, dataSet.U.GetColumn(colIdx));
                writeColIdx++;
            }
            internalDataset.U = newU;
            
            var idUnitModel = IdentifyLinearAndStatic(ref internalDataset, fittingSpecs, false);

            if (!idUnitModel.modelParameters.Fitting.WasAbleToIdentify)
                return idUnitModel;
            //trick now is to add back the paramters that are fixed to the returned model:
            var idLinGains   = idUnitModel.modelParameters.LinearGains;
            var idU          = idUnitModel.modelParameters.U0;
            var idUnorm      = idUnitModel.modelParameters.UNorm;

            var newLinGainsList = new List<double>();
            var newU0List       = new List<double>();
            var newUNormList    = new List<double>();

            var curIdInput = 0;
            for (int curInputIdx = 0; curInputIdx < idUnitModel.modelParameters.LinearGains.Length + 1; curInputIdx++)
            {
                if (curInputIdx == inputIdxToFix)
                {
                    newLinGainsList.Add(inputProcessGainValueToFix);
                    newU0List.Add(u0_fixedInput);
                    newUNormList.Add(uNorm_fixedInput);
                }
                else
                {
                    newLinGainsList.Add(idLinGains.ElementAt(curIdInput));
                    newU0List.Add(idU.ElementAt(curIdInput));
                    newUNormList.Add(idUnorm.ElementAt(curIdInput));
                    curIdInput++;
                }
            }

            var newParams = new UnitParameters();
            newParams.Curvatures = Vec<double>.Fill(double.NaN, newLinGainsList.Count);
            newParams.LinearGains = newLinGainsList.ToArray();
            newParams.LinearGainUnc = Vec<double>.Fill(double.NaN, newLinGainsList.Count); //
            int counter = 0;
            for (int idx = 0; idx < newLinGainsList.Count; idx++)
            {
                if (idx == inputIdxToFix)
                {
                    newParams.LinearGainUnc[idx] = double.NaN;
                }
                else
                {
                    newParams.LinearGainUnc[idx] = idUnitModel.modelParameters.LinearGainUnc[counter];
                    counter++;
                }

            }
            newParams.U0 = newU0List.ToArray();
            newParams.UNorm = newUNormList.ToArray();
            newParams.Bias = idUnitModel.modelParameters.Bias;
            newParams.Fitting = idUnitModel.modelParameters.Fitting;
            newParams.Fitting.SolverID = "Linear,static while fixing index:" + inputIdxToFix;
            var retUnitModel = new UnitModel(newParams);
            retUnitModel.SetFittedDataSet(idUnitModel.GetFittedDataSet());
            return retUnitModel;
        }
    }
}
