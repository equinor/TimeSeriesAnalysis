using System;
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
    public class DefaultProcessModelIdentifier 
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
         public DefaultProcessModelIdentifier()
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
        public DefaultProcessModel Identify(ref UnitDataSet dataSet, double[] u0 = null, double[] uNorm= null)
        {
            bool doNonzeroU0 = true;// should be: true
            bool doUseDynamicModel = true;// should be:true
            bool doEstimateTimeDelay = true; // should be:true
            bool doEstimateCurvature = true;// experimental
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
            ProcessTimeDelayIdentifier processTimeDelayIdentifyObj =
                new ProcessTimeDelayIdentifier(dataSet.TimeBase_s, maxExpectedTc_s);
            /////////////////////////////////////////////////////////////////
            // BEGIN WHILE loop to model process for different time delays               
            bool continueIncreasingTimeDelayEst = true;
            int timeDelayIdx = 0;
            DefaultProcessModelParameters modelParams;
            
            while (continueIncreasingTimeDelayEst)
            {
                // for a dynamic model y will depend on yprev
                // therefore it is difficult to have ycur the same length as ymeas
                {
                    DefaultProcessModelParameters modelParams_noCurvature =
                       EstimateProcessForAGivenTimeDelay
                       (timeDelayIdx, dataSet, doUseDynamicModel, false,
                       FilterTc_s, u0, uNorm, assumeThatYkminusOneApproxXkminusOne);

                    if (doEstimateCurvature)
                    {
                        DefaultProcessModelParameters modelParams_withCurvature =
                              EstimateProcessForAGivenTimeDelay
                              (timeDelayIdx, dataSet, doUseDynamicModel, true,
                              FilterTc_s, u0, uNorm,assumeThatYkminusOneApproxXkminusOne);

                        // chose the best fitting model: with or without curvature
                        // if in doubt, just use model without curvature (thus multiple criteria need to pass)
                        if (modelParams_noCurvature.FittingRsq< modelParams_withCurvature.FittingRsq &&
                            modelParams_noCurvature.FittingObjFunVal> modelParams_withCurvature.FittingObjFunVal &&
                            modelParams_withCurvature.WasAbleToIdentify
                            )
                        {
                            modelParams = modelParams_withCurvature;
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
                bool doDebugPlotting = false;//should normally be false.
                if (doDebugPlotting)
                { 
                    var debugModel = new DefaultProcessModel(modelParams, dataSet);
                    var sim = new UnitSimulator(debugModel);
                    var y_sim = sim.Simulate(ref dataSet);
                    Plot.FromList(new List<double[]> {y_sim,dataSet.Y_meas},new List<string> { "y1=ysim", "y1=ymeas" },
                        (int)dataSet.TimeBase_s, "iteration:"+ timeDelayIdx,default,"debug_it_" + timeDelayIdx);
                }
            }

            // the the time delay which caused the smallest object function value
            int bestTimeDelayIdx = processTimeDelayIdentifyObj.ChooseBestTimeDelay(
                out List<ProcessTimeDelayIdentWarnings> timeDelayWarnings);
            DefaultProcessModelParameters modelParameters =
                (DefaultProcessModelParameters)processTimeDelayIdentifyObj.GetRun(bestTimeDelayIdx);
            modelParameters.TimeDelayEstimationWarnings = timeDelayWarnings;
            // END While loop 
            /////////////////////////////////////////////////////////////////
           
            var model = new DefaultProcessModel(modelParameters, dataSet);
            var simulator = new UnitSimulator(model);
            simulator.Simulate(ref dataSet,default,true);// overwrite any y_sim
            model.FittedDataSet = dataSet;

            return model;
        }


        private DefaultProcessModelParameters EstimateProcessForAGivenTimeDelay
            (int timeDelay_samples, UnitDataSet dataSet,
            bool useDynamicModel,bool doEstimateCurvature, 
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
            if (Double.IsNaN(yprev[0])|| yprev[0] == dataSet.BadDataID)
            {
                yIndicesToIgnore.Add(0);
            }
            yIndicesToIgnore = yIndicesToIgnore.Union(indUbad).ToList();
            yIndicesToIgnore = yIndicesToIgnore.Union(Vec.AppendTrailingIndices(indYcurBad)).ToList(); 
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

            double[] x_mod_cur = null;//, y_mod_cur = null;
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
                    if (doEstimateCurvature)
                    {
                        phi_ols2D = new double[ycur.Length, ucurList.Count*2 + 1];
                    }
                    phi_ols2D.WriteColumn(0, phi1_ols);
                    for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                    {
                        phi_ols2D.WriteColumn(curIdx + 1, vec.Subtract(ucurList[curIdx], u0[curIdx]));
                    }
                    if (doEstimateCurvature)
                    {
                        for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                        {
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
                            phi_ols2D.WriteColumn(curIdx + ucurList.Count+ 1,
                                vec.Div(vec.Pow(vec.Subtract(ucurList[curIdx], u0[curIdx]),2), uNormCur));
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
                    if (doEstimateCurvature)
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

                    if (doEstimateCurvature && c != null)
                    {
                        processCurvatures = vec.Div(c, 1 - a);
                        if (uNorm != null)
                        {
                            if (processCurvatures.Length == uNorm.Length)
                            {
                                processCurvatures = vec.Multiply (processCurvatures, uNorm);
                            }
                        }
                    }
                    else
                    {
                        processCurvatures = null;
                    }

                    #region move to DefaultProcessModel.cs
                    x_mod_cur = new double[x_mod_cur_raw.Length];

                    for (int curTidx = 0; curTidx < x_mod_cur.Count(); curTidx++)
                    {
                        if (assumeThatYkminusOneApproxXkminusOne)
                        {
                            x_mod_cur[curTidx] += a * yprev[curTidx];
                            for (int curUidx = 0; curUidx < ucurList.Count; curUidx++)
                            {
                                double curUval = ucurList[curUidx][curTidx];
                                if (Double.IsNaN(curUidx))
                                {
                                    x_mod_cur[curTidx] = x_mod_cur[curTidx - 1];
                                }
                                else
                                {

                                    x_mod_cur[curTidx] += b[curUidx] * (curUval - u0[curUidx]);
                                }
                            }
                            x_mod_cur[curTidx] += regResults.Bias;
                        }
                        else
                        {
                            /* if (i == 0)
                            {
                                double x_start = b/(1-a)*(ucur[i]-u0);
                                x_mod_cur[i] = a * x_start + b * (ucur[i] - u0) + param[2];
                            }
                            else*/
                            x_mod_cur[curTidx] += a * yprev[curTidx];
                            for (int curUidx = 0; curUidx < ucurList.Count; curUidx++)
                            {
                                double curUval = ucurList[curUidx][curTidx];
                                if (Double.IsNaN(curUidx))
                                {
                                    x_mod_cur[curTidx] = x_mod_cur[curTidx - 1];
                                }
                                else
                                {
                                    x_mod_cur[curTidx] += b[curUidx] * (curUval - u0[curUidx]);
                                }
                            }
                            x_mod_cur[curTidx] += regResults.Bias;
                        }//endif
                    }//enfor
                    #endregion
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
            DefaultProcessModelParameters parameters = new DefaultProcessModelParameters();
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
                parameters.ProcessGains = processGains;
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
                parameters.FittingRsq = regResults.Rsq;
                parameters.FittingObjFunVal = regResults.ObjectiveFunctionValue;
                return parameters;
            }
        }
        // 
        // bias is not always accurate for dynamic model identification 
        // as it is as "difference equation" that matches the changes in the 
        //
        private double? ReEstimateBias(UnitDataSet dataSet, DefaultProcessModelParameters parameters)
        {
            parameters.Bias = 0;
            double nanValue = dataSet.BadDataID;
            var model = new DefaultProcessModel(parameters, dataSet.TimeBase_s);
            var simulator = new UnitSimulator((ISimulatableModel)model);
            var y_sim = simulator.Simulate(ref dataSet);

            double[] diff = (new Vec(nanValue)).Subtract(dataSet.Y_meas, y_sim);
            double? bias = (new Vec(nanValue)).Mean(diff) ;
            return bias;
        }
    }
}
