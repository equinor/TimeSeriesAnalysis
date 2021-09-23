using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.SysId
{

    /// <summary>
    /// Identifier of the "Default" process model - a dynamic process model with time-constant, time-delay, 
    /// linear process gain and optional (nonlinear)curvature process gains.
    /// This model class is sufficent for most real-world dynamic systems, yet also introduces the fewest possible 
    /// paramters to describe the system in an attempt to avoiding over-fitting/over-parametrization
    /// </summary>
    public class DefaultProcessModelIdentifier: IProcessModelIdentifier<DefaultProcessModel>
    {
        private double badValueIndicatingValue = -9999;//TODO: move to other class

        public DefaultProcessModelIdentifier()
        { 
        }


        /// <summary>
        /// Identifies the "Default" process model that best fits the dataSet given
        /// </summary>
        /// <param name="dataSet">The dataset containing the ymeas and U that is to be fitted against, 
        /// a new y_sim is also added</param>
        /// <returns> returning the model parameters </returns>
        public DefaultProcessModel Identify(ref ProcessDataSet dataSet)
        {
            bool doNonzeroU0 = true;// should be: true
            bool doUseDynamicModel = false;// should be:true
            bool doEstimateTimeDelay = false; // should be:true
            double FilterTc_s = 0;
            bool assumeThatYkminusOneApproxXkminusOne = true;

            double maxExpectedTc_s = dataSet.GetTimeSpan().TotalSeconds/4;

            double[] u0 = Vec<double>.Fill(dataSet.U.GetNColumns(),0);

            if (doNonzeroU0)
            {
                u0 = dataSet.GetAverageU();
            }

            ProcessTimeDelayIdentifier processTimeDelayIdentifyObj = new ProcessTimeDelayIdentifier(dataSet.TimeBase_s, maxExpectedTc_s);

            /////////////////////////////////////////////////////////////////
            // BEGIN WHILE loop to model process for different time delays               
            bool continueIncreasingTimeDelayEst = true;
            int timeDelayIdx = 0;
            DefaultProcessModelParameters modelParams;
            while (continueIncreasingTimeDelayEst)
            {
                // for a dynamic model y will depend on yprev
                // therefore it is difficult to have ycur the same length as ymeas
                /*continueIncreasingTimeDelayEst = */
                 modelParams = 
                    EstimateProcessForAGivenTimeDelay
                    (timeDelayIdx, dataSet,  doUseDynamicModel,
                    FilterTc_s, u0, assumeThatYkminusOneApproxXkminusOne/*ref result, ref processTimeDelayIdentifyObj*/);


                if (!continueIncreasingTimeDelayEst)
                    continue;
                // logic to deal with while loop
                timeDelayIdx++;
                processTimeDelayIdentifyObj.AddRun(modelParams);
                continueIncreasingTimeDelayEst = processTimeDelayIdentifyObj.DetermineIfContinueIncreasingTimeDelay(timeDelayIdx);
                // fail-to-safe
                if (timeDelayIdx * dataSet.TimeBase_s > maxExpectedTc_s)
                {
                    modelParams.AddWarning(ProcessIdentWarnings.TimeDelayAtMaximumConstraint);
                    continueIncreasingTimeDelayEst = false;
                }
                if (doEstimateTimeDelay == false)
                    continueIncreasingTimeDelayEst = false;
                if (modelParams.GetWarningList().Contains(ProcessIdentWarnings.NotPossibleToIdentify))
                    return new DefaultProcessModel(modelParams, dataSet);
            }

            // the the time delay which caused the smallest object function value
            int bestTimeDelayIdx = processTimeDelayIdentifyObj.ChooseBestTimeDelay();
            DefaultProcessModelParameters modelParameters =
                (DefaultProcessModelParameters)processTimeDelayIdentifyObj.GetRun(bestTimeDelayIdx);
            // END While loop 
            /////////////////////////////////////////////////////////////////

            return new DefaultProcessModel(modelParameters, dataSet);

        }


        private DefaultProcessModelParameters EstimateProcessForAGivenTimeDelay(int timeDelay_samples, ProcessDataSet dataSet,
           /* DisturbanceIdResult distEstResult,*/
            bool useDynamicModel, double FilterTc_s, double[] u0, bool assumeThatYkminusOneApproxXkminusOne/*,
            ref ProcessIdResults result,*/ /*ref ProcessTimeDelayIdentifier processTimeDelayIdentifyObj*/)
        {
            double Rsq = Double.NaN;

            double[]  ycur, yprev = null, /*dcur,*/ dprev = null;

            List<double[]> ucurList = new List<double[]>();

            int idxEnd = dataSet.Y_meas.Length - 1;
            int idxStart = timeDelay_samples + 1;
            if (useDynamicModel)
            {
                //   int idxStart = timeDelay_samples + 1;
                //ucur = Vec<double>.SubArray(dataSet.U, idxStart - timeDelay_samples, idxEnd - timeDelay_samples);
                for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
                {
                    ucurList.Add(Vec<double>.SubArray(dataSet.U.GetColumn(colIdx), idxStart - timeDelay_samples, idxEnd - timeDelay_samples));
                }
                ycur = Vec<double>.SubArray(dataSet.Y_meas, idxStart, idxEnd);
                yprev = Vec<double>.SubArray(dataSet.Y_meas, idxStart - 1, idxEnd - 1);
            //    dcur = null;
             //   dprev = null;
                /*if (distEstResult != null)
                {
                    dcur = Vec<double>.SubArray(distEstResult.dest_f1, idxStart, idxEnd);
                    dprev = Vec<double>.SubArray(distEstResult.dest_f1, idxStart - 1, idxEnd - 1);
                }*/
            }
            else
            {
                //  int idxStart = 0;
                //   ucur = Vec<double>.SubArray(dataSet.U, idxStart - timeDelay_samples, idxEnd - timeDelay_samples);
                for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
                {
                    ucurList.Add(Vec<double>.SubArray(dataSet.U.GetColumn(colIdx), idxStart - timeDelay_samples, idxEnd - timeDelay_samples));
                }
                ycur = Vec<double>.SubArray(dataSet.Y_meas, idxStart, idxEnd);
            //    dcur = Vec<double>.SubArray(distEstResult.dest_f1, idxStart, idxEnd);
            }

            //TODO: add back indUbad;
            //    List<int> indUbad = SysIdBadDataFinder.GetAllBadIndicesPlussNext(dataSet.U());
            List<int> indYcurBad = Vec.FindValues(ycur, badValueIndicatingValue, VectorFindValueType.NaN);
            List<int> indYprevBad = Vec.Add(indYcurBad.ToArray(), 1).ToList();

            List<int> yIndicesToIgnore = new List<int>();
      //      yIndicesToIgnore = yIndicesToIgnore.Union(indUbad).ToList();
            yIndicesToIgnore = yIndicesToIgnore.Union(indYcurBad).ToList();
            yIndicesToIgnore.Sort();

            double[] param = null, param_95prcUnc = null;
            double[][] varCovarMatrix = null;
            double[] x_mod_cur = null, y_mod_cur = null;

            if (FilterTc_s > 0)
            {
                LowPass yLp = new LowPass(dataSet.TimeBase_s);
                LowPass yLpPrev = new LowPass(dataSet.TimeBase_s);
                LowPass uLp = new LowPass(dataSet.TimeBase_s);
                ycur = yLp.Filter(ycur, FilterTc_s);//todo:disturbance
                yprev = yLpPrev.Filter(yprev, FilterTc_s);//todo:disturbance
            }

            // try to "set to zero" instad of sending -9999 into regression and relying on weighting.
            // not 
            for (int i = 0; i < indYcurBad.Count(); i++)
            {
                ycur[indYcurBad[i]] = 0;
                for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                {
                    ucurList.ElementAt(curIdx)[indYcurBad[i]] = 0;
                }
            }
            // todo: add back
            /*
            for (int i = 0; i < indUbad.Count(); i++)
            {
                ycur[indUbad[i]] = 0;
                ucur[indUbad[i]] = 0;
                if (yprev != null)
                    yprev[indUbad[i]] = 0;
            }*/

            for (int i = 0; i < indYcurBad.Count(); i++)
            {
                if (yprev != null)
                    yprev[Math.Min(yprev.Length - 1, indYprevBad[i])] = 0;
            }

            double timeConstant_s = Double.NaN,/* processGains = Double.NaN,*/ bias = Double.NaN;

            double[] processGains = Vec<double>.Fill(Double.NaN,ucurList.Count);

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
                if (assumeThatYkminusOneApproxXkminusOne)
                {
                    double[] deltaY = Vec.Sub(ycur, yprev);
                    double[] phi1_ols = yprev;
                    double[] Y_ols = deltaY;//Vec.Sub(deltaY, dcur);
                    //  double[] phi2_ols = Vec.Sub(ucur, u0);
                    //           double[][] phi_ols = { phi1_ols, phi2_ols };
                    double[,] phi_ols2D = new double[ycur.Length, ucurList.Count+1];
                    phi_ols2D.WriteColumn(0, phi1_ols);
                    for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                    {
                        phi_ols2D.WriteColumn(curIdx+1, Vec.Sub(ucurList[curIdx], u0[curIdx]));
                    }
                    double[][] phi_ols = phi_ols2D.Convert2DtoJagged();
                    param = Vec.Regress(Y_ols, phi_ols, yIndicesToIgnore.ToArray(), out param_95prcUnc,
                        out varCovarMatrix, out x_mod_cur_raw, out Rsq);
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
                    double[] phi1_ols = Vec.Sub(yprev, dprev);
                    //double[] phi2_ols = Vec.Sub(ucur, u0);
                    //double[][] phi_ols = { phi1_ols, phi2_ols };
                    double[,] phi_ols2D = new double[ycur.Length, ucurList.Count + 1];
                    phi_ols2D.WriteColumn(0, phi1_ols);
                    for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                    {
                        phi_ols2D.WriteColumn(curIdx + 1, Vec.Sub(ucurList[curIdx], u0[curIdx]));
                    }
                    double[][] phi_ols = phi_ols2D.Convert2DtoJagged();
                    double[] Y_ols = ycur;// Vec.Sub(ycur, dcur);
                   
                    param = Vec.Regress(Y_ols, phi_ols, yIndicesToIgnore.ToArray(), out param_95prcUnc,
                        out varCovarMatrix, out x_mod_cur_raw, out Rsq);
                }
                /*     if (param != null)
                     {
                         double objFunValTest = ObjFun(Vec.Add(x_mod_cur_raw, dcur), ycur);
                     }*/

                //

                if (param == null)
                {
                    Debug.WriteLine("Vec.Regress returned null - dynamic");
                }
                if (param != null)
                {
                    double a;
                    if (assumeThatYkminusOneApproxXkminusOne)
                    {
                        a = param[0] + 1;
                    }
                    else
                    {
                        a = param[0];
                    }
                    double[] b = Vec<double>.SubArray(param,1, param.Length-2);
                    if (a > 1)
                        a = 0;
                    //d_mod_cur = d;
                    // for clarity:
                    bias = param[2];
                    // the estimation finds "a" in the difference equation 
                    // a = 1/(1 + Ts/Tc)
                    // so that 
                    // Tc = Ts/(1/a-1)
                    timeConstant_s = Double.NaN;
                    if (a != 0)
                        timeConstant_s = dataSet.TimeBase_s / (1 / a - 1);
                    if (timeConstant_s < 0)
                        timeConstant_s = 0;
                    processGains = Vec.Div(b,1-a); //b / (1 - a);

                    x_mod_cur = new double[x_mod_cur_raw.Length];

                    // TODO: does not work, unsure why.
                    //x_mod_cur = Vec.Add(ProcessModel.GetModelX_withoutBias(ucur, u0, timeBase_s,
                    //    timeConstant_s, processGain, timeDelay_samples), bias);

                    for (int i = 0; i < x_mod_cur.Count(); i++)
                    {
                        // todo:add back bad value check!
                        //if (ucur[i] != badValueIndicatingValue)
                        if(true)
                        {
                            if (assumeThatYkminusOneApproxXkminusOne)
                            {
                                x_mod_cur[i] += a * yprev[i];
                                for (int curU = 0; curU < ucurList.Count; curU++)
                                {
                                    x_mod_cur[i] += b[curU] * (ucurList[curU][i] - u0[curU]);
                                }
                                x_mod_cur[i] += param[2];
                            }
                            else
                            {
                                /* if (i == 0)
                                {
                                    double x_start = b/(1-a)*(ucur[i]-u0);
                                    x_mod_cur[i] = a * x_start + b * (ucur[i] - u0) + param[2];
                                }
                                else*/
                                x_mod_cur[i] += a * yprev[i];
                                for (int curU = 0; curU < ucurList.Count; curU++)
                                {
                                    x_mod_cur[i] += b[curU] * (ucurList[curU][i] - u0[curU]);
                                }
                                x_mod_cur[i] +=  param[2];
                            }
                        }
                        else
                            x_mod_cur[i] = x_mod_cur[i - 1];
                    }
                }
            }
            else
            {
                // y[k] = Kc*u[k]+ P0
                //double[] X_ols = Vec.Sub(ucur, u0);
                //double[][] inputs = { X_ols };
                double[,] inputs2D = new double[ycur.Length, ucurList.Count];
                for (int curIdx = 0; curIdx < ucurList.Count; curIdx++)
                {
                    inputs2D.WriteColumn(curIdx, Vec.Sub(ucurList[curIdx], u0[curIdx]));
                }
                double[][] inputs = inputs2D.Transpose().Convert2DtoJagged();
                double[] Y_ols = ycur;// Vec.Sub(ycur, dcur);
                
                param = Vec.Regress(Y_ols, inputs, yIndicesToIgnore.ToArray(),
                    out param_95prcUnc, out varCovarMatrix, out x_mod_cur, out Rsq);
                timeConstant_s = 0;
                if (param == null)
                {
                    Debug.WriteLine("Vec.Regress returned null - dynamic");
                }
                if (param != null)
                {
                    timeConstant_s = 0;
                    processGains = Vec<double>.SubArray(param, 0, param.Length - 2);//param[0];
                    bias = param.Last();// param[1];
                }
            }

            //

            DefaultProcessModelParameters parameters = new DefaultProcessModelParameters();


            // Vec.Regress can return very large values if y is noisy and u is stationary. 
            // in these cases varCovarMatrix is null
            const double maxAbsValueRegression = 10000;


            if (param == null)
            {
                parameters.WasAbleToIdentify = false;
                //processTimeDelayIdentifyObj.AddFailedRun(y_mod_cur, x_mod_cur, dcur, Rsq);
                parameters.AddWarning(ProcessIdentWarnings.RegressionProblemFailedToYieldSolution);
                return parameters;
            }
            else if (Math.Abs(param[1]) > maxAbsValueRegression)
            {
                parameters.WasAbleToIdentify = false;
                parameters.AddWarning(ProcessIdentWarnings.NotPossibleToIdentify);
                return parameters;
            }
            else // able to identify
            {
              //  if (dcur == null)
                    y_mod_cur = x_mod_cur;
                //    else
                //       y_mod_cur = Vec.Add(x_mod_cur, dcur);
                // note that y_mod is often shorter than y_meas, and these two vector may need to be synchronized, 
                // this is very important to identify the correct dynamics. 

                parameters.TimeDelay_s = timeDelay_samples*dataSet.TimeBase_s;
                parameters.TimeConstant_s = timeConstant_s;
                parameters.Bias = bias;
                parameters.ProcessGains = processGains ;//todo: add multipple process gains
                parameters.U0 = u0;
                // TODO:add back uncertainty estimates




             /*   double objFunVal = Vec.SumOfSquareErr(y_mod_cur, ycur,-1);
                processTimeDelayIdentifyObj.AddSuccessfulRun(objFunVal, processGain, timeConstant_s, bias, Rsq, y_mod_cur, x_mod_cur, dcur);
                processTimeDelayIdentifyObj.AddUncertaintyEstiamtes(useDynamicModel, param, param_95prcUnc, varCovarMatrix,
                    dataSet.y_meas.Length);*/
                return parameters;
            }
        }



    }
}
