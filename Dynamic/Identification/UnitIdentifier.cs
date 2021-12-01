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
    /// The "default" process model is linear-in-paramters, so that it can be solved by linear regression
    /// and identification should thus be both fast and stable. 
    /// </para>
    /// <para>
    /// Time-delay is an integer parameter, and finding the time-delay alongside continous paramters
    /// turns the identification problem into a linear mixed-integer problem. 
    /// The time delay identification is done by splitting the time-delay estimation from continous parameter
    /// identification, turning the solver into a sequential optimization solver. 
    /// This logic to re-run estimation for multiple time-delays and selecting the best estiamte of time delay 
    /// is deferred to <seealso cref="ProcessTimeDelayIdentifier"/>
    /// </para>
    /// 
    /// </summary>
    public class UnitIdentifier 
    {
        const double fitMinImprovement = 0.0001;
        const double rSquaredMinImprovement = 0.001;



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

            bool doNonzeroU0 = true;// should be: true
           // bool doUseDynamicModel = true;// should be:true
          //  bool doEstimateTimeDelay = true; // should be:true
        //    bool doEstimateCurvature = true;// experimental
            double FilterTc_s = 0;// experimental: by default set to zero.
            bool assumeThatYkminusOneApproxXkminusOne = true;// by default this should be set to true
            double maxExpectedTc_s = dataSet.GetTimeSpan().TotalSeconds / 4;
            if (u0 == null)
            {
                u0 = Vec<double>.Fill(dataSet.U.GetNColumns(), 0);
                if (doNonzeroU0)
                {
                    u0 = dataSet.GetAverageU();
                }
            }
            if (uNorm == null)
            {
                uNorm = Vec<double>.Fill(1, dataSet.U.GetNColumns());
                for (int k=0;k<dataSet.U.GetNColumns(); k++)
                {
                    var u = dataSet.U.GetColumn(k);
                    uNorm[k] = Math.Max(Math.Abs(vec.Max(u) - u0[k]), Math.Abs(vec.Min(u) - u0[k]));
                    if (uNorm[k] == 0)
                    {
                        uNorm[k] = 1;
                    }
                }
            }

            ProcessTimeDelayIdentifier processTimeDelayIdentifyObj =
                new ProcessTimeDelayIdentifier(dataSet.TimeBase_s, maxExpectedTc_s);

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
            int timeDelayIdx = 0;
            UnitParameters modelParams_StaticAndNoCurvature =
                EstimateProcessForAGivenTimeDelay
                (timeDelayIdx, dataSet, false, allCurvesDisabled,
                FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);
           // processTimeDelayIdentifyObj.AddRun(modelParams_StaticAndNoCurvature);

            /////////////////////////////////////////////////////////////////
            // BEGIN WHILE loop to model process for different time delays               
            bool continueIncreasingTimeDelayEst = true;
            timeDelayIdx = 0;
            UnitParameters modelParams=null;

            while (continueIncreasingTimeDelayEst)
            {
                modelParams = null;
                var modelList = new List<UnitParameters>();
                UnitParameters modelParams_noCurvature =
                    EstimateProcessForAGivenTimeDelay
                    (timeDelayIdx, dataSet, doUseDynamicModel, allCurvesDisabled,
                    FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);

                if (doEstimateCurvature && modelParams_noCurvature.WasAbleToIdentify)
                {
                   UnitParameters modelParams_allCurvature =
                        EstimateProcessForAGivenTimeDelay
                        (timeDelayIdx, dataSet, doUseDynamicModel, allCurvesEnabled,
                        FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);

                    // only try rest of curvature models if it seems like it will help
                    // this is done to save on processing in case of many inputs (3 inputs= 8 identification runs)
                    //if (modelParams_noCurvature.FittingObjFunVal -modelParams_allCurvature.FittingObjFunVal > fitMinImprovement)
                    if (modelParams_allCurvature.WasAbleToIdentify && 
                        modelParams_allCurvature.RsqFittingDiff - modelParams_noCurvature.RsqFittingDiff > rSquaredMinImprovement)
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
                if (timeDelayIdx * dataSet.TimeBase_s > maxExpectedTc_s)
                {
                    modelParams.AddWarning(ProcessIdentWarnings.TimeDelayAtMaximumConstraint);
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
            UnitParameters modelParameters = //modelParams_StaticAndNoCurvature;//TODO:temporary 
                (UnitParameters)processTimeDelayIdentifyObj.GetRun(bestTimeDelayIdx);
            // use static and no curvature model as fallback if more complex models failed
            if (!modelParameters.WasAbleToIdentify && modelParams_StaticAndNoCurvature.WasAbleToIdentify)
            {
                modelParameters = modelParams_StaticAndNoCurvature;
                timeDelayWarnings.Add(ProcessTimeDelayIdentWarnings.FallbackToLinearStaticModel);
            }

            modelParameters.TimeDelayEstimationWarnings = timeDelayWarnings;
            // END While loop 
            /////////////////////////////////////////////////////////////////

            var model = new UnitModel(modelParameters, dataSet);
            if (modelParameters.WasAbleToIdentify)
            {
                var simulator = new UnitSimulator(model);
                simulator.Simulate(ref dataSet, default, true);// overwrite any y_sim
                model.FittedDataSet = dataSet;
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
                // objective function: lower is better
                double improvedRsquared = curModel.RsqFittingDiff - bestModel.RsqFittingDiff ;// positive if curmodel improves on the current best
               
                // Rsquared: higher is better
                double improvedFit = bestModel.ObjFunValFittingDiff- curModel.ObjFunValFittingDiff ;// positive if curmodel improves on the current best


                if (improvedFit> fitMinImprovement &&
                   improvedRsquared > rSquaredMinImprovement &&
                   curModel.WasAbleToIdentify
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
                LowPass yLp = new LowPass(dataSet.TimeBase_s);
                LowPass yLpPrev = new LowPass(dataSet.TimeBase_s);
                LowPass uLp = new LowPass(dataSet.TimeBase_s);
                ycur = yLp.Filter(ycur, FilterTc_s);//todo:disturbance
                yprev = yLpPrev.Filter(yprev, FilterTc_s);//todo:disturbance
            }

            RegressionResults regResults;
            double timeConstant_s = Double.NaN;
            double[] processGains = Vec<double>.Fill(Double.NaN, ucurList.Count);
            double[] processCurvatures = Vec<double>.Fill(Double.NaN, ucurList.Count);

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
                        phi_ols2D.WriteColumn(curIdx + 1, vec.Subtract(ucurList[curIdx], u0[curIdx]));
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
                                if (uNorm.Length - 1 > curIdx)
                                {
                                    if (uNorm[curIdx] <= 0)
                                    {
                                        Shared.GetParserObj().AddError("uNorm illegal value, shoudl be positive and nonzero:" 
                                            + uNorm[curIdx]);
                                    }
                                    else
                                    {
                                        uNormCur = uNorm[curIdx];
                                    }
                                }
                            }
                            phi_ols2D.WriteColumn(curCurvature + ucurList.Count+ 1,
                                vec.Div(vec.Pow(vec.Subtract(ucurList[curIdx], u0[curIdx]),2), uNormCur));
                            curCurvature++;
                        }
                    }
                    double[][] phi_ols = phi_ols2D.Transpose().Convert2DtoJagged();
                    regResults = vec.Regress(Y_ols, phi_ols, yIndicesToIgnore.ToArray());
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
                    regResults = vec.Regress(Y_ols, phi_ols, yIndicesToIgnore.ToArray());
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
                    //d_mod_cur = d;

                    // the estimation finds "a" in the difference equation 
                    // a = 1/(1 + Ts/Tc)
                    // so that 
                    // Tc = Ts/(1/a-1)

                    if (a != 0)
                        timeConstant_s = dataSet.TimeBase_s / (1 / a - 1);
                    else
                        timeConstant_s = 0;
                    if (timeConstant_s < 0)
                        timeConstant_s = 0;
                    processGains = vec.Div(b,1-a);

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
                regResults = vec.Regress(Y_ols, inputs, yIndicesToIgnore.ToArray());
                timeConstant_s = 0;
                if (regResults.Param != null)
                {
                    processGains = Vec<double>.SubArray(regResults.Param, 0, regResults.Param.Length - 2);
                }
            }
            UnitParameters parameters = new UnitParameters();
            parameters.SolverID = solverID;

            // Vec.Regress can return very large values if y is noisy and u is stationary. 
            // in these cases varCovarMatrix is null

            const double maxAbsValueRegression = 10000;
            if (regResults.Param == null || !regResults.AbleToIdentify)
            {
                parameters.WasAbleToIdentify = false;
                parameters.AddWarning(ProcessIdentWarnings.RegressionProblemFailedToYieldSolution);
                return parameters;
            }
            else if (Math.Abs(regResults.Param[1]) > maxAbsValueRegression)
            {
                parameters.WasAbleToIdentify = false;
                parameters.AddWarning(ProcessIdentWarnings.NotPossibleToIdentify);
                return parameters;
            }
            else // able to identify
            {
                parameters.WasAbleToIdentify = true;
                parameters.TimeDelay_s = timeDelay_samples * dataSet.TimeBase_s;
                parameters.TimeConstant_s = timeConstant_s;
                parameters.LinearGains = processGains;
                parameters.Curvatures = processCurvatures;
                parameters.U0 = u0;
                parameters.UNorm = uNorm;

                double? recalcBias = ReEstimateBias(dataSet, parameters);
                if (recalcBias.HasValue)
                {
                    parameters.Bias = recalcBias.Value;
                }
                else
                {
                    parameters.AddWarning(ProcessIdentWarnings.ReEstimateBiasFailed);
                    parameters.Bias = regResults.Param.Last();
                }
                // TODO:add back uncertainty estimates
                parameters.NFittingTotalDataPoints = regResults.NfittingTotalDataPoints;
                parameters.NFittingBadDataPoints = regResults.NfittingBadDataPoints;
                //
                parameters.RsqFittingDiff = regResults.Rsq;
                parameters.ObjFunValFittingDiff = regResults.ObjectiveFunctionValue;
                // 
            //   Plot.FromList(new List<double[]> { dataSet.Y_meas, dataSet.Y_sim }, new List<string> { "y1=xmod", "y1=xmeas" },
            //          TimeSeriesCreator.CreateDateStampArray(new DateTime(2000,1,1),1, dataSet.Y_meas.Length), "test");

                parameters.RsqFittingAbs = vec.RSquared(dataSet.Y_meas, dataSet.Y_sim,null,0);

                return parameters;
            }
        }
        // 
        // bias is not always accurate for dynamic model identification 
        // as it is as "difference equation" that matches the changes in the 
        //
        private double? ReEstimateBias(UnitDataSet dataSet, UnitParameters parameters)
        {
            UnitDataSet internalData = new UnitDataSet(dataSet);

            parameters.Bias = 0;
            double nanValue = internalData.BadDataID;
            var model = new UnitModel(parameters, internalData.TimeBase_s);
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
            double? bias = (new Vec(nanValue)).Mean(diff) ;
            return bias;
        }
    }
}
