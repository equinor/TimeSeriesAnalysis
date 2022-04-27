using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// Identifier of the "Default" process model - a dynamic process model with time-constant, time-delay, 
    /// linear process gain and optional (nonlinear)curvature process gains.
    /// <para>
    /// This model class is sufficent for real-world linear or weakly nonlinear dynamic systems, yet also introduces the fewest possible 
    /// parameters to describe the system in an attempt to avoiding over-fitting/over-parametrization
    /// </para>
    /// <para>
    /// The "default" process model is identified using a linear-in-parameters paramterization(paramters a,b,c), so that it can be solved by linear regression
    /// and identification should thus be both fast and stable. The issue with the parametriation(a,b,c) is that the meaning of each paramter is less
    /// inutitive, for instance the time constant depends on a, but linear gain depends on both a and b, while curvature depends on a and c.
    /// Looking at the unceratinty of each parameter to determine if the model should be dynamic or static or what the uncertainty of the time constant is,
    /// is very hard, and this observation motivates re-paramtrizing the model after identification.
    /// </para>
    /// <para>
    /// When assessing and simulating the model, parmaters are converted into more intuitive paramters "time constant", "linear gains" and "curvature gain"
    /// which are a different parametrization. The UnitIdentifier, UnitModel and UnitParamters classes handle this transition seamlessly to the user.
    /// Uncertainty is expressed in terms of this more intuitive parametrization, to allow for a more intuitive assessment of the parameters.
    /// </para>
    /// <para>
    /// Another advantage of the paramterization, is that the model internally separates betwen stedy-state and transient state, you can at any instance
    /// "turn off" dynamics and request the steady-state model output for the current input. This is useful if you have transient data that you want to 
    /// analyze in the steady-state, as you can then fit the model to all available data-points without having to select what data points you beleive are at 
    /// steady state, then you can disable dynamic terms to do a static analysis of the dynamic model.
    /// </para>
    /// <para>
    /// Time-delay is an integer parameter, and finding the time-delay alongside continous paramters
    /// turns the identification problem into a linear mixed-integer problem. 
    /// The time delay identification is done by splitting the time-delay estimation from continous parameter
    /// identification, turning the solver into a sequential optimization solver. 
    /// This logic to re-run estimation for multiple time-delays and selecting the best estiamte of time delay 
    /// is deferred to <seealso cref="UnitTimeDelayIdentifier"/>
    /// </para>
    /// <para>
    /// Since the aim is to identify transients/dynamics, the regression is done on model differences rather than absolute values
    /// </para>
    /// </summary>
    /// make static?
    public class UnitIdentifier 
    {
        const double obFunDiff_MinImprovement = 0.0001;
        const double rSquaredDiff_MinImprovement = 0.001;

        const int nDigits = 5;// number of significant digits in result parameters

        const bool doUnityUNorm = true;// if set true, then Unorm is always one

        /// <summary>
        /// Default Constructor
        /// </summary>
        public UnitIdentifier()
        {
        }

        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="u0">Optionally sets the local working point for the inputs
        /// around which the model is to be designed(can be set to <c>null</c>)</param>
        /// <param name="uNorm">normalizing paramter for u-u0 (its range)</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public UnitModel Identify(ref UnitDataSet dataSet, double[] u0 = null, double[] uNorm = null)
        {
            return Identify_Internal(ref dataSet,u0,uNorm);
        }

        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given, but no time-constants
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="u0">Optionally sets the local working point for the inputs
        /// around which the model is to be designed(can be set to <c>null</c>)</param>
        /// <param name="uNorm">normalizing paramter for u-u0 (its range)</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public UnitModel IdentifyStatic(ref UnitDataSet dataSet, double[] u0 = null, double[] uNorm = null)
        {
            return Identify_Internal(ref dataSet, u0, uNorm,false);
        }

        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given, but disables curvatures
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="doEstimateTimeDelay">if set to false, estimation of time delays are disabled</param>
        /// <param name="u0">Optionally sets the local working point for the inputs
        /// around which the model is to be designed(can be set to <c>null</c>)</param>
        /// <param name="uNorm">normalizing paramter for u-u0 (its range)</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public UnitModel IdentifyLinear(ref UnitDataSet dataSet, bool doEstimateTimeDelay = true, double[] u0 = null, double[] uNorm = null)
        {
            return Identify_Internal(ref dataSet, u0, uNorm, true,false, doEstimateTimeDelay);
        }
        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given, but disables curvatures and time-constants
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="doEstimateTimeDelay">if set to false, modeling does not identify time-delays</param>
        /// <param name="u0">Optionally sets the local working point for the inputs
        /// around which the model is to be designed(can be set to <c>null</c>)</param>
        /// <param name="uNorm">normalizing paramter for u-u0 (its range)</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        public UnitModel IdentifyLinearAndStatic(ref UnitDataSet dataSet, bool doEstimateTimeDelay=true,  double[] u0 = null, double[] uNorm = null)
        {
            return Identify_Internal(ref dataSet, u0, uNorm, false, false, doEstimateTimeDelay);
        }

        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <param name="u0">Optionally sets the local working point for the inputs
        /// around which the model is to be designed(can be set to <c>null</c>)</param>
        /// <param name="uNorm">normalizing paramter for u-u0 (its range)</param>
        /// <returns> the identified model parameters and some information about the fit</returns>
        private UnitModel Identify_Internal(ref UnitDataSet dataSet, double[] u0 = null, double[] uNorm= null,
            bool doUseDynamicModel=true, bool doEstimateCurvature = true, bool doEstimateTimeDelay = true)
        {
            var vec = new Vec(dataSet.BadDataID);

            var constantInputInds = new List<int>();
            var correlatedInputInds = new List<int>();

            bool doNonzeroU0 = true;// should be: true
            double FilterTc_s = 0;// experimental: by default set to zero.
            bool assumeThatYkminusOneApproxXkminusOne = true;// by default this should be set to true
            double maxExpectedTc_s = dataSet.GetTimeSpan().TotalSeconds / 4;
            if (u0 == null)
            {
                u0 = Vec<double>.Fill(dataSet.U.GetNColumns(), 0);
                if (doNonzeroU0)
                {
                    u0 = SignificantDigits.Format(dataSet.GetAverageU(),nDigits);
                }
            }
            if (uNorm == null)
            {
                uNorm = Vec<double>.Fill(1, dataSet.U.GetNColumns());

                    for (int k = 0; k < dataSet.U.GetNColumns(); k++)
                    {
                        var u = dataSet.U.GetColumn(k);
                        if (!doUnityUNorm)
                        {
                            uNorm[k] = Math.Max(Math.Abs(vec.Max(u) - u0[k]), Math.Abs(vec.Min(u) - u0[k]));
                        }
                        //uNorm[k] = Math.Max(Math.Abs(vec.Max(u)), Math.Abs(vec.Min(u)));
                        if (vec.Max(u) == vec.Min(u))// input is constnat
                        {
                            constantInputInds.Add(k);
                            uNorm[k] = Double.PositiveInfinity;
                        }
                        if (uNorm[k] == 0)// avoid div by zero
                        {
                            constantInputInds.Add(k);
                            uNorm[k] = 0;
                        }
                    }
                
                uNorm = SignificantDigits.Format(uNorm, nDigits);
            }

            UnitTimeDelayIdentifier processTimeDelayIdentifyObj =
                new UnitTimeDelayIdentifier(dataSet.GetTimeBase(), maxExpectedTc_s);

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
            UnitParameters modelParams_StaticAndNoCurvature =
                EstimateProcessForAGivenTimeDelay
                (timeDelayIdx, dataSet, false, allCurvesDisabled,
                FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
            modelList.Add(modelParams_StaticAndNoCurvature);
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
                    (timeDelayIdx, dataSet, doUseDynamicModel, allCurvesDisabled,
                    FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
                modelList.Add(modelParams_noCurvature);

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
                        modelParams_allCurvature.Fitting.RsqFittingDiff - modelParams_noCurvature.Fitting.RsqFittingDiff > rSquaredDiff_MinImprovement)
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
                    modelParams.AddWarning(UnitdentWarnings.TimeDelayAtMaximumConstraint);
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

            modelParameters.TimeDelayEstimationWarnings = timeDelayWarnings;
            if (constantInputInds.Count > 0)
            {
                modelParameters.AddWarning(UnitdentWarnings.ConstantInputU);
            }
            if (correlatedInputInds.Count > 0)
            {
                modelParameters.AddWarning(UnitdentWarnings.CorrelatedInputsU);
            }
            // END While loop 
            /////////////////////////////////////////////////////////////////

            var model = new UnitModel(modelParameters, dataSet);
            if (modelParameters.Fitting.WasAbleToIdentify)
            {
                var simulator = new UnitSimulator(model);
                simulator.Simulate(ref dataSet, default, true);// overwrite any y_sim
                model.SetFittedDataSet(dataSet);
            }
            return model;

        }
        // for three inputs, return every combination of true false
        // except false-false-false and true-true-true, (but the other six)
        private List<bool[]> GetAllNonzeroBitArrays(int size)
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




        private UnitParameters ChooseBestModel(UnitParameters fallbackModel,List<UnitParameters> allModels)
        {


            UnitParameters bestModel = fallbackModel;

            // models will be arranged from least to most numbre of curvature terms
            // in case of doubt, do not add in extra curvature that does not significantly improve the objective function

            foreach (UnitParameters curModel in allModels)
            {
                // Rsquared: higher is better
                double RsqFittingDiff_improvement = curModel.Fitting.RsqFittingDiff - bestModel.Fitting.RsqFittingDiff ;
                double RsqFittingAbs_improvement = curModel.Fitting.RsqFittingAbs - bestModel.Fitting.RsqFittingAbs;


                // objective function: lower is better
                double objFunDiff_improvement = bestModel.Fitting.ObjFunValFittingDiff  - curModel.Fitting.ObjFunValFittingDiff ;// positive if curmodel improves on the current best
                double objFunAbs_improvement = bestModel.Fitting.ObjFunValFittingAbs - curModel.Fitting.ObjFunValFittingAbs ;// positive if curmodel improves on the current best


                if (objFunDiff_improvement>= obFunDiff_MinImprovement && 
                   RsqFittingDiff_improvement >= rSquaredDiff_MinImprovement &&
                   objFunAbs_improvement>=0 &&
                   RsqFittingAbs_improvement>=0 &&
                   curModel.Fitting.WasAbleToIdentify
                   )
                {
                    bestModel = curModel;
                }
            }
            return bestModel;
        }

        private UnitParameters EstimateProcessForAGivenTimeDelay
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
                solverID += "Static";
                for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
                {
                    ucurList.Add(Vec<double>.SubArray(dataSet.U.GetColumn(colIdx), 
                        idxStart - timeDelay_samples, idxEnd - timeDelay_samples));
                }
                ycur = Vec<double>.SubArray(dataSet.Y_meas, idxStart, idxEnd);
                dcur = Vec<double>.SubArray(dataSet.D, idxStart, idxEnd);
            }

            var indUbad = new List<int>();
            for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
            {
                indUbad = indUbad.Union(SysIdBadDataFinder.GetAllBadIndicesPlussNext(dataSet.U.GetColumn(colIdx),
                    dataSet.BadDataID)).ToList();
            }
            List<int> indYcurBad = vec.FindValues(ycur, dataSet.BadDataID, VectorFindValueType.NaN);

            List<int> yIndicesToIgnore = new List<int>();
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
                // Tc/Ts *(x[k]-x[k-1])  = x[k-1]*B*u[k-1]

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
                    regResults = vec.Regress(Y_ols, phi_ols, yIndicesToIgnore.ToArray(),phiIndicesToRegularize);
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
                        vec.Subtract(ycur, dcur);
                    }
                    regResults = vec.Regress(Y_ols, phi_ols, yIndicesToIgnore.ToArray(), inputIndicesToRegularize);
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

                    linearProcessGains = vec.Div(b, 1 - a);

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
                    Y_ols = vec.Subtract(ycur, dcur);
                }
                regResults = vec.Regress(Y_ols, inputs, yIndicesToIgnore.ToArray(), inputIndicesToRegularize);
                timeConstant_s = 0;
                if (regResults.Param != null)
                {
                    linearProcessGains = Vec<double>.SubArray(regResults.Param, 0, regResults.Param.Length - 2);
                }
            }
            UnitParameters parameters = new UnitParameters();
        
            parameters.Fitting = new FittingInfo();
            parameters.Fitting.SolverID = solverID;
            // Vec.Regress can return very large values if y is noisy and u is stationary. 
            // in these cases varCovarMatrix is null

            const double maxAbsValueRegression = 10000;
            if (regResults.Param == null || !regResults.AbleToIdentify)
            {
                parameters.Fitting.WasAbleToIdentify = false;
                parameters.AddWarning(UnitdentWarnings.RegressionProblemFailedToYieldSolution);
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

              //  if (dataSet.D == null)
                {
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
                        parameters.Bias = SignificantDigits.Format(regResults.Param.Last(),nDigits);
                    }
                }
         /*       else
                {
                    // if system has disturbance, then bias seems to be better set at original value
                    parameters.AddWarning(UnitdentWarnings.ReEstimateBiasDisabledDueToNonzeroDisturbance);
                    parameters.Bias = SignificantDigits.Format(regResults.Param.Last(), nDigits);

                    var model = new UnitModel(parameters);
                    var simulator = new UnitSimulator((ISimulatableModel)model);
                    var internalData = new UnitDataSet(dataSet);
                    dataSet.Y_sim = simulator.Simulate(ref internalData);
                }*/

                parameters.Fitting.NFittingTotalDataPoints = regResults.NfittingTotalDataPoints;
                parameters.Fitting.NFittingBadDataPoints = regResults.NfittingBadDataPoints;
                //
                parameters.Fitting.RsqFittingDiff = regResults.Rsq;
                parameters.Fitting.ObjFunValFittingDiff = regResults.ObjectiveFunctionValue;
                parameters.Fitting.ObjFunValFittingAbs = vec.SumOfSquareErr(dataSet.Y_meas, dataSet.Y_sim,0);
                // 
                //   Plot.FromList(new List<double[]> { dataSet.Y_meas, dataSet.Y_sim }, new List<string> { "y1=xmod", "y1=xmeas" },
                //          TimeSeriesCreator.CreateDateStampArray(new DateTime(2000,1,1),1, dataSet.Y_meas.Length), "test");

                parameters.Fitting.RsqFittingAbs = vec.RSquared(dataSet.Y_meas, dataSet.Y_sim,null,0)*100;

                // add inn uncertainty
                CalculateUncertainty(regResults, dataSet.GetTimeBase(), ref parameters);

                return parameters;
            }
        }

        // references:
        // http://dept.stat.lsa.umich.edu/~kshedden/Courses/Stat406_2004/Notes/variance.pdf
        //https://stats.stackexchange.com/questions/41896/varx-is-known-how-to-calculate-var1-x
        private void CalculateUncertainty(RegressionResults regResults,double timeBase_s, ref UnitParameters parameters)
        {
            if (regResults.VarCovarMatrix == null)
                return;

            double a = regResults.Param[0];
            double varA = regResults.VarCovarMatrix[0][0];
            double sqrtN = Math.Sqrt(regResults.NfittingTotalDataPoints - regResults.NfittingBadDataPoints);

            /////////////////////////////////////////////////////
            /// linear gain unceratinty
            
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
            /// curvature unceratinty
            /// 
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

        // 
        // bias is not always accurate for dynamic model identification 
        // as it is as "difference equation" that matches the changes in the 
        //
        private (double?, double[]) SimulateAndReEstimateBias(UnitDataSet dataSet, UnitParameters parameters)
        {
            UnitDataSet internalData = new UnitDataSet(dataSet);

            parameters.Bias = 0;
            double nanValue = internalData.BadDataID;
            var model = new UnitModel(parameters);
            var simulator = new UnitSimulator((ISimulatableModel)model);
            var y_sim = simulator.Simulate(ref internalData);

            var yMeas_exceptIgnoredValues = internalData.Y_meas;
            var ySim_exceptIgnoredValues = y_sim;
            if (dataSet.IndicesToIgnore != null)
            {
                for (int ind = 0; ind < dataSet.IndicesToIgnore.Count(); ind++)
                {
                    int indToIgnore = dataSet.IndicesToIgnore.ElementAt(ind);
                    yMeas_exceptIgnoredValues[indToIgnore] = Double.NaN;//nan values are ignored by Vec.Means
                    ySim_exceptIgnoredValues[indToIgnore] = Double.NaN;//nan values are ignored by Vec.Means
                }
            }

            double[] diff = (new Vec(nanValue)).Subtract(yMeas_exceptIgnoredValues, ySim_exceptIgnoredValues);
            double? bias = (new Vec(nanValue)).Mean(diff);
            double[] y_sim_ret = null;
            if (bias.HasValue)
            {
                y_sim_ret = (new Vec(nanValue)).Add(y_sim, bias.Value);
            }
            return (bias,y_sim_ret);
        }
    }
}
