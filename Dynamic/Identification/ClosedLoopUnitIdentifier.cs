using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

using TimeSeriesAnalysis;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{
    internal class ClosedLoopGainGlobalSearchResults
    {
        /// <summary>
        /// list of linear gains tried in global search
        /// </summary>
        public List<double> pidLinearProcessGainList; 

        /// <summary>
        /// list of covariance between d_Est and y_Set, calculated for each linear gains
        /// </summary>
        public List<double> covarianceBtwDistAndYsetList;
        /// <summary>
        /// self-variance of d_est, calculated for each linear gains
        /// </summary>
        public List<double> dEstVarianceList;

        /// <summary>
        /// self-variance of d_est, calculated for each linear gains
        /// </summary>
        public List<double> linregGainYsetToDestList;


        /// <summary>
        /// process unit model used in each iteration of the global search(return one of these, the one that is "best")
        /// </summary>
        public List<UnitModel> unitModelList;

        public ClosedLoopGainGlobalSearchResults()
        {
            unitModelList= new List<UnitModel>();
            dEstVarianceList = new List<double>();
            covarianceBtwDistAndYsetList = new List<double>();
            pidLinearProcessGainList = new List<double>();
            linregGainYsetToDestList = new List<double>();
        }

        public void Add(double gain, UnitModel unitModel, double covarianceBtwDistAndYset, double dest_variance,
            double linregGainYsetToDest)
        {
            pidLinearProcessGainList.Add(gain);
            unitModelList.Add(unitModel);
            covarianceBtwDistAndYsetList.Add(covarianceBtwDistAndYset);
            dEstVarianceList.Add(dest_variance);
            linregGainYsetToDestList.Add(linregGainYsetToDest);
        }

        public UnitModel GetBestModel(double initalGainEstimate)
        {
            if (unitModelList == null)
                return null;
            if (unitModelList.Count == 0)
                return null;

            const double dEstVarianceTolerance = 0.00001;// 

            // if there is a local minimum in the list of dEstVarianceList then use that
            Vec vec = new Vec();
            vec.Min(dEstVarianceList.ToArray(),out int ind1);
            vec.Min(covarianceBtwDistAndYsetList.ToArray(), out int ind2);
            vec.Min(vec.Abs(linregGainYsetToDestList.ToArray()), out int ind3);

            // if one or more of the three methods has non-zero "ind" then that is more likely correct than 
            // the zero ind, as this indicates non-convex search space.
            if (ind1 > 0)
            {
                if (ind2 == 0 && ind3 == 0)
                    return unitModelList.ElementAt(ind1);
                else
                {
                    if (ind1 < dEstVarianceList.Count()-1)
                    {
                        // use dEstVariance local minima only if the objective space is not "flat"
                        var deltaToLower = dEstVarianceList.ElementAt(ind1 - 1) - dEstVarianceList.ElementAt(ind1);
                        var deltaToHigher = dEstVarianceList.ElementAt(ind1 + 1) - dEstVarianceList.ElementAt(ind1);
                        if (Math.Max(deltaToLower, deltaToHigher) > dEstVarianceTolerance)
                        {
                            return unitModelList.ElementAt(ind1);
                        }
                    }
                }
            }

            if (ind1 == 0 && ind2 == 0 && ind3>0)
            {
                return unitModelList.ElementAt(ind3);
            }
            if (ind1 == 0 && ind2 > 0 && ind3 == 0)
            {
                return unitModelList.ElementAt(ind3);
            }
            // in some cases ind2 and ind3, in which case, go with that.
            if (ind2 > 0 && ind2 == ind3)
            {
                return unitModelList.ElementAt(ind2);
            }
            if (ind1 > 0 && ind1 == ind3)
            {
                return unitModelList.ElementAt(ind1);
            }
            if (ind1 > 0 && ind1 == ind2)
            {
                return unitModelList.ElementAt(ind1);
            }
            //otherwise, we fall back to looking at the covariance between d_est and yset
            var defaultModel = unitModelList.ElementAt(ind2);
            defaultModel.modelParameters.AddWarning(UnitdentWarnings.ClosedLoopEst_GlobalSearchFailedToFindLocalMinima);

            return defaultModel;

        }
    }


    /// <summary>
    /// Identification that attempts to identify a unit model jointly with 
    /// estimating the additive signal acting on the output(disturbance signal) yet is
    /// counter-acted by closed-loop (feedback)control, such as with PID-control.
    /// 
    /// The approach requires combining information in the measured output signal with the 
    /// information in the manipulated variable(determined by active control)
    /// that is inputted into the process 
    /// 
    /// In order to accomplish this estimation, the process model and the disturbance signal 
    /// are estimated together.
    /// 
    /// </summary>
    public class ClosedLoopUnitIdentifier
    {
        /////////////////////////
        // NB!! These two are somewhat "magic numbers", that need to be changed only after
        // testing over a wide array of cases
        const int firstPassNumIterations = 40;
        const int secondPassNumIterations = 15;
        ////////////////////////
        const bool doDebuggingPlot = false;
        /// <summary>
        /// Identify the unit model of a closed-loop system and the distrubance (additive output signal)
        /// </summary>
        /// <remarks>
        /// <para>
        /// Currently always assumes that PID-index is first index of unit model(can be improved)
        /// </para>
        /// </remarks>
        /// <param name = "dataSet">the unit data set, containing both the input to the unit and the output</param>
        /// <param name = "plantSim">an optional PidModel that is used to co-simulate the model and disturbance, improving identification</param>
        /// <param name = "pidParams">if the setpoint of the control changes in the time-set, then the paramters of pid control need to be given.</param>
        /// <param name = "pidInputIdx">the index of the PID-input to the unit model</param>
        /// 
        /// <returns>The unit model, with the name of the newly created disturbance added to the additiveInputSignals</returns>
        public (UnitModel, double[]) Identify(UnitDataSet dataSet, PidParameters pidParams = null, int pidInputIdx = 0)
        {
            bool wasGainGlobalSearchDone = false;
            bool onlyDidTwoSteps = false;
            bool doTimeDelayEstOnRun1 = false;
            if (dataSet.Y_setpoint == null || dataSet.Y_meas == null || dataSet.U == null)
            {
                return (null, null);
            }
            var vec = new Vec();
            //
            // In some cases the first data point given is has a non-physical 
            // drop/increase that is caused by an outside error.
            // drops/increases in the setpoint will cause a "global search" for the process gain in 
            // closed loop and thus will significantly affect the result
            // 
            {
                if (dataSet.IndicesToIgnore == null)
                {
                    dataSet.IndicesToIgnore = new List<int>();
                }
                bool doesSetpointChange = !(vec.Max(dataSet.Y_setpoint, dataSet.IndicesToIgnore) == 
                    vec.Min(dataSet.Y_setpoint, dataSet.IndicesToIgnore));
                if (doesSetpointChange)
                {
                    var ind = vec.FindValues(vec.Diff(dataSet.Y_setpoint),0,VectorFindValueType.NotEqual, dataSet.IndicesToIgnore);
                    // if the only setpoint step is in the first iteration,ignore it.
                    if (ind.Count() < dataSet.GetNumDataPoints()/4 && ind.First() <= 1)
                    {
                        dataSet.IndicesToIgnore.Add(ind.First()-1);//because "diff",subtract 1
                      //  dataSet.IndicesToIgnore.Add(ind.First());
                    }
                }
                dataSet.IndicesToIgnore.AddRange(vec.FindValues(dataSet.Y_setpoint, 0, VectorFindValueType.NaN));
                dataSet.IndicesToIgnore.AddRange(vec.FindValues(dataSet.Y_meas, 0, VectorFindValueType.NaN));
                for (int colIdx = 0; colIdx < dataSet.U.GetNColumns(); colIdx++)
                {
                    dataSet.IndicesToIgnore.AddRange(vec.FindValues(dataSet.U.GetColumn(colIdx), 0, VectorFindValueType.NaN));
                }
            }
            // this variable holds the "newest" unit model run and is updated
            // over multiple runs, and as it improves, the 
            // estimate of the disturbance improves along with it.
            List<DisturbanceIdResult> idDisturbancesList = new List<DisturbanceIdResult>();
            List<UnitModel> idUnitModelsList = new List<UnitModel>();
            List<double> processGainList = new List<double>();

            double[] u0 = dataSet.U.GetRow(0);
            double y0 = dataSet.Y_meas[0];
            bool isOK;
            var dataSet1 = new UnitDataSet(dataSet);
            var dataSet2 = new UnitDataSet(dataSet);
            var dataSet3 = new UnitDataSet(dataSet);
            var dataSet4 = new UnitDataSet(dataSet);

            var id = new UnitIdentifier();

            int nGains=1;
            // ----------------
            // run1: no process model assumed, let disturbance estimator guesstimate a process gains, 
            // to give afirst estimate of the disturbance
            // this is often a quite good estimate of the process gain, which is the most improtant parameter
            // withtout a reference model which has the correct time constant and time delays, 
            // dynamic "overshoots" will enter into the estimated disturbance, try to fix this by doing 
            // "refinement" runs afterwards.
            { 
                DisturbanceIdResult distIdResult1 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet1, null, pidInputIdx, pidParams);

                dataSet1.D = distIdResult1.d_est;
                var unitModel_run1 = id.IdentifyLinearAndStatic(ref dataSet1, doTimeDelayEstOnRun1, u0);
                nGains = unitModel_run1.modelParameters.GetProcessGains().Length;
                idDisturbancesList.Add(distIdResult1);
                idUnitModelsList.Add(unitModel_run1);
                isOK = ClosedLoopSim(dataSet1, unitModel_run1.GetModelParameters(), pidParams, distIdResult1.d_est, "run1");

                // see if varying gain to get the lowest correlation between setpoint and disturbance 
                // only needed if setpoint varies. "step1 global search"
                bool doesSetpointChange = !(vec.Max(dataSet.Y_setpoint,dataSet.IndicesToIgnore) == vec.Min(dataSet.Y_setpoint, dataSet.IndicesToIgnore));
                if (doesSetpointChange && unitModel_run1.modelParameters.GetProcessGains()!= null)
                {
                    // magic numbers of the global search
   
                    const double initalGuessFactor_lowerbound = 0.1;//set a bit lower than 0.5
                    const double initalGuessFactor_higherbound = 2;

                    wasGainGlobalSearchDone = true;
                    double pidProcessInputInitalGainEstimate = unitModel_run1.modelParameters.GetProcessGains()[pidInputIdx];
                    double initalCorrelation = CorrelationCalculator.Calculate(distIdResult1.d_est, dataSet.Y_setpoint, dataSet.IndicesToIgnore);
                    var gainAndCorrDict = new Dictionary<double, ClosedLoopGainGlobalSearchResults>();
                    //
                    // looking to find the process gain that "decouples" d_est from Y_setpoint as much as possible.
                    //
                    // or antoher way to look at ti is that the output U with setpoint effects removed should be as decoupled 
                    // form Y_setpoint.
                    var min_gain = Math.Min(pidProcessInputInitalGainEstimate * initalGuessFactor_lowerbound, pidProcessInputInitalGainEstimate * initalGuessFactor_higherbound);
                    var max_gain = Math.Max(pidProcessInputInitalGainEstimate * initalGuessFactor_lowerbound, pidProcessInputInitalGainEstimate * initalGuessFactor_higherbound);


                    ///
                    // first run:
                    var retPass1 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx, 
                         unitModel_run1, pidProcessInputInitalGainEstimate, 
                        min_gain, max_gain, firstPassNumIterations);
                    var bestUnitModel = retPass1.Item1;

                    // second pass:
                    if (retPass1.Item1.modelParameters.Fitting.WasAbleToIdentify && secondPassNumIterations>0)
                    {
                        var gainPass1 = retPass1.Item1.modelParameters.LinearGains[pidInputIdx];
                        var retPass2 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx,
                           retPass1.Item1, gainPass1, gainPass1 - retPass1.Item2, gainPass1 + retPass1.Item2, secondPassNumIterations);
                        bestUnitModel = retPass2.Item1;
                    }
                    // add the "best" model to be used in the next model run
                    if (bestUnitModel != null)
                    {
                        idUnitModelsList.Add(bestUnitModel);
                    }
                }
            }
            bool doRun2 = true, doRun3 = true, doRun4 = true;
            // if the unit model has more than one input, then the runs2 and 3 and 4 do not appear to improve the estimate.
            if (nGains > 1)
            {
                doRun2 = false;
                doRun3 = false;
                doRun4 = true;
                onlyDidTwoSteps = true;
            }

            // ----------------
            // run 2: now we have a decent first empircal estimate of the distubance and the process gain, now try to use identification
            if(doRun2 && idUnitModelsList.Last() != null)
            {
                bool DO_DEBUG_RUN_2 = false;
                DisturbanceIdResult distIdResult2 = DisturbanceIdentifier.EstimateDisturbance(
                    dataSet2, idUnitModelsList.Last(), pidInputIdx, pidParams,DO_DEBUG_RUN_2);

                dataSet2.D = distIdResult2.d_est;
                // IdentifyLinear appears to give quite a poor model for Static_Longstep 
                var unitModel_run2 = id.IdentifyLinearAndStatic(ref dataSet2, false, u0);
                idDisturbancesList.Add(distIdResult2);
                idUnitModelsList.Add(unitModel_run2);
                isOK = ClosedLoopSim(dataSet2, unitModel_run2.GetModelParameters(), pidParams, distIdResult2.d_est, "run2");
            }
            // ----------------
            // run 3: use the result of the last run to try to improve the disturbance estimate and take 
            // out some of the dynamics from the disturbance vector and see if this improves estimation.
            if (doRun3 && idUnitModelsList.Last() != null)
            {
                DisturbanceIdResult distIdResult3 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet3, idUnitModelsList.Last(), pidInputIdx, pidParams);

                dataSet3.D = distIdResult3.d_est;
                var unitModel_run3 = id.IdentifyLinearAndStatic(ref dataSet3, true, u0);
                idDisturbancesList.Add(distIdResult3);
                idUnitModelsList.Add(unitModel_run3);
                isOK = ClosedLoopSim(dataSet3, unitModel_run3.GetModelParameters(), pidParams, distIdResult3.d_est, "run3");
            }
            // - issue is that after run 4 modelled output does not match measurement.
            // - the reason that we want this run is that after run3 the time constant and 
            // time delays are way off if the process is excited mainly by disturbances.
            // - the reason that we cannot do run4 immediately, is that that formulation 
            // does not appear to give a solution if the guess disturbance vector is bad.

            // run4: do a run where it is no longer assumed that x[k-1] = y[k], 
            // this run has the best chance of estimating correct time constants, but it requires a good inital guess of d
            if (doRun4 && idUnitModelsList.Last() != null)
            {
                var model = idUnitModelsList.Last();

                DisturbanceIdResult distIdResult4 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet4, model, pidInputIdx, pidParams);
                List<double[]> estDisturbances = new List<double[]>();
                List<double> distDevs = new List<double>();

                estDisturbances.Add(distIdResult4.d_est);
                var timeBase = dataSet4.GetTimeBase();
                double candiateTc_s = 0;
                bool curDevIsDecreasing = true;
                double firstDev = vec.Sum(vec.Abs(vec.Diff(distIdResult4.d_est))).Value;
                distDevs.Add(firstDev);
                while (candiateTc_s < 30 * timeBase && curDevIsDecreasing)
                {
                    candiateTc_s += timeBase;
                    var newParams = model.GetModelParameters().CreateCopy();
                    newParams.TimeConstant_s = candiateTc_s;
                    var newModel = new UnitModel(newParams);
                    DisturbanceIdResult distIdResult_Test = DisturbanceIdentifier.EstimateDisturbance
                        (dataSet4, newModel,pidInputIdx,pidParams);
                    estDisturbances.Add(distIdResult_Test.d_est);
                    double curDev = vec.Sum(vec.Abs(vec.Diff(distIdResult_Test.d_est))).Value;
                    if (curDev < distDevs.Last<double>())
                        curDevIsDecreasing = true;
                    else
                        curDevIsDecreasing = false;
                    distDevs.Add(curDev);
                }
                if (candiateTc_s > 0)
                {
                    candiateTc_s -= timeBase;
                }
                var step4params = model.GetModelParameters().CreateCopy();
                step4params.TimeConstant_s = candiateTc_s;
                var step4Model = new UnitModel(step4params);
                idUnitModelsList.Add(step4Model);
                DisturbanceIdResult distIdResult_step4 = DisturbanceIdentifier.EstimateDisturbance
                        (dataSet4, step4Model, pidInputIdx, pidParams);
                idDisturbancesList.Add(distIdResult_step4);
            }

            // debugging plots, should normally be off
            if (false)
            {
                Shared.EnablePlots();
                Console.WriteLine("run1");
                Console.WriteLine(idUnitModelsList[0].ToString());
                Console.WriteLine("run2");
                Console.WriteLine(idUnitModelsList[1].ToString());
                Console.WriteLine("run3");
                Console.WriteLine(idUnitModelsList[2].ToString());
                Console.WriteLine("run4");
                Console.WriteLine(idUnitModelsList[3].ToString());
                Plot.FromList(
                    new List<double[]> { 
                        idDisturbancesList[0].d_est,
                        idDisturbancesList[1].d_est,
                        idDisturbancesList[2].d_est,
                        idDisturbancesList[3].d_est,

                        idDisturbancesList[0].d_HF,
                        idDisturbancesList[1].d_HF,
                        idDisturbancesList[2].d_HF,
                        idDisturbancesList[3].d_HF,

                        idDisturbancesList[0].d_LF,
                        idDisturbancesList[1].d_LF,
                        idDisturbancesList[2].d_LF,
                        idDisturbancesList[3].d_LF,

                    },
                    new List<string> {"y1=d_run1", "y1=d_run2", "y1=d_run3","y1=d_run4",
                    "y3=dHF_run1", "y3=dHF_run2", "y3=dHF_run3","y3=dHF_run4",
                    "y3=dLF_run1", "y3=dLF_run2", "y3=dLF_run3","y3=dLF_run4"
                    },
                    dataSet.GetTimeBase(), "doDebuggingPlot_disturbanceHLandLF");
                dataSet.D = null;

                var sim1 = new UnitSimulator(idUnitModelsList[0]);
                var sim1results = sim1.Simulate(ref dataSet,false,true);
                var sim2 = new UnitSimulator(idUnitModelsList[1]);
                var sim2results = sim2.Simulate(ref dataSet, false, true);
                var sim3 = new UnitSimulator(idUnitModelsList[2]);
                var sim3results = sim3.Simulate(ref dataSet, false, true);

                Plot.FromList(
                    new List<double[]> {
                        dataSet.Y_meas,
                        vec.Add(sim1results, idDisturbancesList[0].d_est),
                        vec.Add(sim2results, idDisturbancesList[1].d_est),
                        vec.Add(sim3results, idDisturbancesList[2].d_est),
                        sim1results,
                        sim2results,
                        sim3results,
                    },
                    new List<string> {"y1=y_meas","y1=xd_run1", "y1=xd_run2", "y1=xd_run3",
                    "y3=x_run1","y3=x_run2","y3=x_run3"},
                    dataSet.GetTimeBase(), "doDebuggingPlot_states");
               Shared.DisablePlots();
            }

            // - note that by tuning rules often Kp = 0.5 * Kc approximately. 
            // - this corresponds to roughly 50% of a disturbance being taken up by P-term and 50% by I-term.
            // - does it make sense to filter e to create an estiamte of d?? this would mean that ymod and ymeas no longer 
            // match exactly(a good thing? coudl give insight on model fit?)
            // - could we make a function that gives out the closed loop step response of the 
            // pid controller/process in closed loop, to see if the closed loop has overshoot/undershoot? 
            double[] disturbance = idDisturbancesList.ToArray()[idDisturbancesList.Count-1].d_est;
            UnitModel  identUnitModel = idUnitModelsList.ToArray()[idUnitModelsList.Count - 1];

            if (identUnitModel.modelParameters.Fitting == null)
            {
                identUnitModel.modelParameters.Fitting = new FittingInfo();
                identUnitModel.modelParameters.Fitting.WasAbleToIdentify = true;
                identUnitModel.modelParameters.Fitting.StartTime = dataSet.Times.First();
                identUnitModel.modelParameters.Fitting.EndTime = dataSet.Times.Last();
                if (wasGainGlobalSearchDone)
                {
                    if (onlyDidTwoSteps)
                    {
                        identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop/w gain global search/2 step";
                    }
                    else
                    {
                        identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop/w gain global search/4 step";
                    }
                }
                else
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop v1.0";
                identUnitModel.modelParameters.Fitting.NFittingTotalDataPoints = dataSet.GetNumDataPoints();
            }


            return (identUnitModel,disturbance);
        }

        private Tuple<UnitModel,double> GlobalSearchLinearPidGain(UnitDataSet dataSet, PidParameters pidParams, int pidInputIdx, 
            UnitModel unitModel_run1, 
            double pidProcessInputInitalGainEstimate, double min_gain, double max_gain, int numberOfGlobalSearchIterations = 40)
        {
            bool isOK;
            var range = max_gain - min_gain;
            var searchResults = new ClosedLoopGainGlobalSearchResults();
            var gainUnc = range / numberOfGlobalSearchIterations;
            int nGains = unitModel_run1.modelParameters.GetProcessGains().Length;
            Vec vec = new Vec();
            for (var pidLinProcessGain = min_gain; pidLinProcessGain < max_gain; pidLinProcessGain += range / numberOfGlobalSearchIterations)
            {
                var dataSet_alt = new UnitDataSet(dataSet);
                //  dataSet_alt.D = null;
                var alternativeModel = new UnitModel(unitModel_run1.GetModelParameters().CreateCopy(), "alternative");
                if (nGains == 1)
                {
                    alternativeModel.modelParameters.LinearGains = new double[] { pidLinProcessGain };
                    alternativeModel.modelParameters.LinearGainUnc = new double[] { gainUnc };
                    alternativeModel.modelParameters.Fitting = new FittingInfo();
                    alternativeModel.modelParameters.Fitting.WasAbleToIdentify = true;
                }
                else
                {
                    var pidProcess_u0 = unitModel_run1.modelParameters.U0[pidInputIdx];
                    var pidProcess_unorm = unitModel_run1.modelParameters.UNorm[pidInputIdx];
                    var ident = new UnitIdentifier();
                    alternativeModel = ident.IdentifyLinearAndStaticWhileKeepingLinearGainFixed(dataSet_alt, pidInputIdx,
                        pidLinProcessGain, pidProcess_u0, pidProcess_unorm);
                    if (!alternativeModel.IsModelSimulatable(out _))
                        continue;
                    if (!alternativeModel.modelParameters.Fitting.WasAbleToIdentify)
                        continue;
                    alternativeModel.SetID("altModelGlobalSearch");
                }
                DisturbanceIdResult distIdResultAlt = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet_alt, alternativeModel, pidInputIdx, pidParams);
                var d_est = distIdResultAlt.d_est;
                isOK = ClosedLoopSim
                    (dataSet_alt, alternativeModel.GetModelParameters(), pidParams, d_est, "run_alt");
                if (!isOK)
                    continue;

                // for the cases when d is a step and yset is a sinus, v7 seems to work best
                // for the caeses when d is sinus and yset is a step, either of v1 or v2 seem to work better,
                // but more so when the step in yset is fairly big compared to the disturbance.

                //v1: use correlation(gives best gain: 0.7(crosses from positive to negative around there)
                // double covarianceBtwDistAndYsetList = Math.Abs(CorrelationCalculator.Calculate(dataSet.Y_setpoint, d_est));
                // v2: try to use covarince(gives highest gain best(no local minimum))
                double covarianceBtwDistAndYsetList = Math.Abs(Measures.Covariance(dataSet.Y_setpoint, d_est, false));
                // v3: just choose the gain that gives the least "variance" in d_est?
                var dest_variance = vec.Mean(vec.Abs(vec.Diff(distIdResultAlt.adjustedUnitDataSet.U.GetColumn(pidInputIdx)))).Value;

                double y_set_mean = vec.Mean(dataSet.Y_setpoint).Value;
                var lowIndices = vec.FindValues(dataSet.Y_setpoint, y_set_mean, VectorFindValueType.SmallerThan);
                var highIndices = vec.FindValues(dataSet.Y_setpoint, y_set_mean, VectorFindValueType.BiggerThan);

                var y_set_low = vec.Mean(Vec<double>.GetValuesAtIndices(dataSet.Y_setpoint, lowIndices)).Value;
                var y_set_high = vec.Mean(Vec<double>.GetValuesAtIndices(dataSet.Y_setpoint, highIndices)).Value;
                var d_est_low = vec.Mean(Vec<double>.GetValuesAtIndices(d_est, lowIndices)).Value;
                var d_est_high = vec.Mean(Vec<double>.GetValuesAtIndices(d_est, highIndices)).Value;
                double linregGainYsetToDest = (d_est_high - d_est_low) / (y_set_high - y_set_low);

                // finally,save all results!
                searchResults.Add(pidLinProcessGain, alternativeModel, covarianceBtwDistAndYsetList, dest_variance, linregGainYsetToDest);
            }
            UnitModel bestUnitModel = searchResults.GetBestModel(pidProcessInputInitalGainEstimate);
            if (bestUnitModel.modelParameters != null)
            {
                if (bestUnitModel.modelParameters.LinearGains.Length > 0)
                {
                    if (bestUnitModel.modelParameters.LinearGainUnc == null)
                    {
                        bestUnitModel.modelParameters.LinearGainUnc = new double[bestUnitModel.modelParameters.LinearGains.Length];
                    }
                    bestUnitModel.modelParameters.LinearGainUnc[pidInputIdx] = gainUnc;
                }
            }
            return new Tuple<UnitModel,double>(bestUnitModel, gainUnc);
        }

        // TODO: replace this with a "closed-loop" simulator that uses the PlantSimulator.
        // 
        public bool ClosedLoopSim(UnitDataSet unitData, UnitParameters modelParams, PidParameters pidParams,
            double[] disturbance,string name)
        {
            if (pidParams == null)
            {
                return false;
            }
            var sim = new UnitSimulator(new UnitModel(modelParams));
            unitData.D = disturbance;

            var pid = new PidModel(pidParams);

            bool isOk = sim.CoSimulate(pid, ref unitData);
            if (doDebuggingPlot)
            {
                Plot.FromList(new List<double[]> { unitData.Y_sim,unitData.Y_meas, 
                    unitData.U_sim.GetColumn(0),  unitData.U.GetColumn(0)},
                    new List<string> { "y1=y_sim", "y1=y_meas", "y3=u_sim","y3=u_meas" },
                    unitData.GetTimeBase(), "doDebuggingPlot" + "_closedlooopsim_"+name); 
            }
            return isOk;
        }
    }
}