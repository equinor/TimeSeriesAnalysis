using Accord.IO;
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
        // "magic numbers"
        // this factor is how much to weigh covBtwDestAndYset into a joint objective function with dEstVariance
        // covBtwDestAndYset sometimes can be a "tiebreaker" in cases where dEstVariance has a "flat" region and no clear 
        // minimum, but should not be too large either to overpower clear minima in dEstVariance
        const double v2_factor = 0.01;// should be much smaller than one
        const double v3_factor = 0.0;

        const double v1_Strength_Threshold = 0.2;// if below this value, then v2 and v3 are added to the objective function.

        /// <summary>
        /// list of linear gains tried in global search
        /// </summary>
        public List<double> pidLinearProcessGainList; 

        /// <summary>
        /// list of covariance between d_Est and y_Set, calculated for each linear gains
        /// </summary>
        public List<double> covBtwDestAndYsetList;
        /// <summary>
        /// self-variance of d_est, calculated for each linear gains
        /// </summary>
        public List<double> dEstVarianceList;

        /// <summary>
        /// self-variance of d_est, calculated for each linear gains
        /// </summary>
        public List<double> covBtwDestAndUexternal;


        /// <summary>
        /// process unit model used in each iteration of the global search(return one of these, the one that is "best")
        /// </summary>
        public List<UnitParameters> unitParametersList;

        /// <summary>
        /// Holds the results of a global search for process gains as performed by ClosedLoopIdentifier
        /// </summary>
        internal ClosedLoopGainGlobalSearchResults()
        {
            unitParametersList= new List<UnitParameters>();
            dEstVarianceList = new List<double>();
            covBtwDestAndYsetList = new List<double>();
            pidLinearProcessGainList = new List<double>();
            covBtwDestAndUexternal = new List<double>();
        }

        public void Add(double gain, UnitParameters unitParameters, double covBtwDestAndYset, double dest_variance,
            double covBtwDestAndUexternal)
        {
          //  var newParams = unitParameters.CreateCopy();

            pidLinearProcessGainList.Add(gain);
            unitParametersList.Add(unitParameters);
            covBtwDestAndYsetList.Add(covBtwDestAndYset);
            dEstVarianceList.Add(dest_variance);
            this.covBtwDestAndUexternal.Add(covBtwDestAndUexternal);
        }

        public Tuple<UnitModel,bool> GetBestModel(double initalGainEstimate)
        {
            if (unitParametersList.Count()== 0)
                return new Tuple<UnitModel,bool>(null,false);

            // calculate strenght of a minimum - strength is value between 0 and 100, higher is stronger
            Tuple<double,int> MinimumStrength(double[] values)
            {
                Vec vec2 = new Vec();
                vec2.Min(values, out int minIndex);

                if (minIndex == 0)
                    return new Tuple<double,int>(0,minIndex);
                if (minIndex == values.Length - 1)
                    return new Tuple<double, int>(0, minIndex);

                double val = values.ElementAt(minIndex);
                double valAbove = values.ElementAt(minIndex + 1);
                double valBelow = values.ElementAt(minIndex - 1);

                // note that in some cases, the "true" value is between two points on the grid,
                // so it is not unexpected to have a vaue on one side be quite close to the value at "minIndex"
                double valBeside = Math.Max(valAbove, valBelow);
                return new Tuple<double,int>(100 * (1 - val / valBeside),minIndex);
              //  return new Tuple<double, int>(valBeside-val, minIndex);
            }

            double[] Scale(double[] v_in)
            {
                Vec vec2 = new Vec();
                if (vec2.Min(v_in) > 0)
                {
                    return vec2.Div(vec2.Subtract(v_in, vec2.Min(v_in)), vec2.Max(v_in) - vec2.Min(v_in));
                }
                else
                {
                    return vec2.Div(v_in, vec2.Max(v_in));
                }
            }
            Vec vec = new Vec();

            var v1 = dEstVarianceList.ToArray();
            var v2 = covBtwDestAndYsetList.ToArray();
            var v3 = covBtwDestAndUexternal.ToArray();
            (double v1_Strength, int min_ind) = MinimumStrength(v1);
            (double v2_Strength, int min_ind_v2) = MinimumStrength(v2);
            (double v3_Strength,int min_ind_v3) = MinimumStrength(v3);
            // add a scaled v2 to the objective only when v1 is very flat around the minimum
            // as a "tiebreaker"

            if (v1_Strength == 0 && v2_Strength == 0 && v3_Strength == 0)
            {
                // defaultModel.modelParameters.AddWarning(UnitdentWarnings.ClosedLoopEst_GlobalSearchFailedToFindLocalMinima);
                return new Tuple<UnitModel, bool>(null, false);
            }
            double[] objFun = v1;

            if (v1_Strength < v1_Strength_Threshold)
            {
                var v1_scaled = Scale(v1);
                   
                // if setpoint changes then v2 will be non-all-zero
                if (!Vec.IsAllValue(v2, 0))
                {
                    var v2_scaled = Scale(v2);
                    // var v3 = linregGainYsetToDestList.ToArray();
                    // var v3_scaled = vec.Div(vec.Subtract(v3, vec.Min(v3)), vec.Max(v3) - vec.Min(v3));
                    objFun = vec.Add(objFun, vec.Multiply(v2_scaled, v2_factor));
                }

                // if the system has external inputs, and they change in value
                if (!Vec.IsAllValue(v3, 0))
                {
                    var v3_scaled = Scale(v3);
                    objFun = vec.Add(objFun, vec.Multiply(v3_scaled, v3_factor));
                }
            }
            vec.Min(objFun, out min_ind);
                return new Tuple<UnitModel, bool>(new UnitModel(unitParametersList.ElementAt(min_ind)), true);
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
        // NB!! These three are somewhat "magic numbers", that need to be changed only after
        // testing over a wide array of cases
        const int firstPassNumIterations = 50;
        const int secondPassNumIterations = 10;
        const double initalGuessFactor_higherbound = 2.5;// 2 is a bit low, should be a bit higher
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

            var id = new UnitIdentifier();

            int nGains=1;
            // ----------------
            // run1: no process model assumed, let disturbance estimator guesstimate a pid-process gain, 
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

                //  "step1" global search" for linear pid-gainsgains
                if(unitModel_run1.modelParameters.GetProcessGains()!= null)
                {
                    wasGainGlobalSearchDone = true;
                    double pidProcessInputInitalGainEstimate = unitModel_run1.modelParameters.GetProcessGains()[pidInputIdx];
                    var gainAndCorrDict = new Dictionary<double, ClosedLoopGainGlobalSearchResults>();
                    //
                    // looking to find the process gain that "decouples" d_est from Y_setpoint as much as possible.
                    //
                    // or antoher way to look at ti is that the output U with setpoint effects removed should be as decoupled 
                    // form Y_setpoint.

                    // in some cases the first linear regression done without any estimate of the disutrbance can even have the wrong
                    // sign of the linear gains, although the amplitude will in general be of the right order, thus 
                    // min_gain = -max_gain;  is a more robust choice than some small same-sign value or 0.
                    var max_gain =  pidProcessInputInitalGainEstimate * initalGuessFactor_higherbound;
                    var min_gain = - max_gain;

                    // when debugging, it might be advantageous to set min_gain equal to the known true value
                    //TODO:Remvoe!!!
                   // min_gain = 0.5;

                    // first pass(wider grid with larger grid size)
                    var retPass1 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx, 
                         unitModel_run1, pidProcessInputInitalGainEstimate, 
                        min_gain, max_gain, firstPassNumIterations);
                    var bestUnitModel = retPass1.Item1;

                    if (bestUnitModel != null)
                    {
                        // second pass(finer grid around best result of first pass)
                        if (retPass1.Item1.modelParameters.Fitting.WasAbleToIdentify && secondPassNumIterations > 0)
                        {
                            var gainPass1 = retPass1.Item1.modelParameters.LinearGains[pidInputIdx];
                            var retPass2 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx,
                               retPass1.Item1, gainPass1, gainPass1 - retPass1.Item2, gainPass1 + retPass1.Item2, secondPassNumIterations);
                            bestUnitModel = retPass2.Item1;
                        }
                    }
                    // add the "best" model to be used in the next model run
                    if (bestUnitModel != null)
                    {
                        idUnitModelsList.Add(bestUnitModel);
                    }
                }
            }
            bool doRun2 = true;

            // ----------------

            // - issue is that after run 4 modelled output does not match measurement.
            // - the reason that we want this run is that after run3 the time constant and 
            // time delays are way off if the process is excited mainly by disturbances.
            // - the reason that we cannot do run4 immediately, is that that formulation 
            // does not appear to give a solution if the guess disturbance vector is bad.

            // run2: do a run where it is no longer assumed that x[k-1] = y[k], 
            // this run has the best chance of estimating correct time constants, but it requires a good inital guess of d
            if (doRun2 && idUnitModelsList.Last() != null)
            {
                var model = idUnitModelsList.Last();

                DisturbanceIdResult distIdResult2 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet2, model, pidInputIdx, pidParams);
                List<double[]> estDisturbances = new List<double[]>();
                List<double> distDevs = new List<double>();

                estDisturbances.Add(distIdResult2.d_est);
                var timeBase = dataSet2.GetTimeBase();
                double candiateTc_s = 0;
                bool curDevIsDecreasing = true;
                double firstDev = vec.Sum(vec.Abs(vec.Diff(distIdResult2.d_est))).Value;
                distDevs.Add(firstDev);
                while (candiateTc_s < 30 * timeBase && curDevIsDecreasing)
                {
                    candiateTc_s += timeBase;
                    var newParams = model.GetModelParameters().CreateCopy();
                    newParams.TimeConstant_s = candiateTc_s;
                    var newModel = new UnitModel(newParams);
                    DisturbanceIdResult distIdResult_Test = DisturbanceIdentifier.EstimateDisturbance
                        (dataSet2, newModel,pidInputIdx,pidParams);
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
                var step2params = model.GetModelParameters().CreateCopy();
                step2params.TimeConstant_s = candiateTc_s;
                var step2Model = new UnitModel(step2params);
                idUnitModelsList.Add(step2Model);
                DisturbanceIdResult distIdResult_step4 = DisturbanceIdentifier.EstimateDisturbance
                        (dataSet2, step2Model, pidInputIdx, pidParams);
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
                Plot.FromList(
                    new List<double[]> { 
                        idDisturbancesList[0].d_est,
                        idDisturbancesList[1].d_est,

                        idDisturbancesList[0].d_HF,
                        idDisturbancesList[1].d_HF,

                        idDisturbancesList[0].d_LF,
                        idDisturbancesList[1].d_LF,
                    },
                    new List<string> {"y1=d_run1", "y1=d_run2", 
                    "y3=dHF_run1", "y3=dHF_run2",
                    "y3=dLF_run1", "y3=dLF_run2",
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
                        sim1results,
                        sim2results,
                    },
                    new List<string> {"y1=y_meas","y1=xd_run1", "y1=xd_run2",
                    "y3=x_run1","y3=x_run2"},
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
            UnitModel unitModel_run1, double pidProcessInputInitalGainEstimate, double minPidProcessGain,
            double maxPidProcessGain, int numberOfGlobalSearchIterations = 40)
        {
             // bool isOK;
            var range = maxPidProcessGain - minPidProcessGain;
            var searchResults = new ClosedLoopGainGlobalSearchResults();
            var gainUnc = range / numberOfGlobalSearchIterations;
            int nGains = unitModel_run1.modelParameters.GetProcessGains().Length;
            Vec vec = new Vec();
            bool doesSetpointChange = !(vec.Max(dataSet.Y_setpoint, dataSet.IndicesToIgnore) == vec.Min(dataSet.Y_setpoint, dataSet.IndicesToIgnore));
            var isOK = true;
            double[] d_prev = null;
            for (var pidLinProcessGain = minPidProcessGain; pidLinProcessGain <= maxPidProcessGain; pidLinProcessGain += range / numberOfGlobalSearchIterations)
            {
                // Single-input-single output disturbance: will include transients in response to changes in yset and u_external
                var dataSet_alt = new UnitDataSet(dataSet);
                
                (UnitModel alternativeSISOModel ,DisturbanceIdResult distIdResultAlt_SISO) = 
                    EstimateSISOdisturbanceForProcGain(unitModel_run1, 
                        pidLinProcessGain,pidInputIdx,dataSet_alt,pidParams);

                var d_est = distIdResultAlt_SISO.d_est;
                var u_pid_adjusted = distIdResultAlt_SISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);
                var alternativeModelParameters = alternativeSISOModel.modelParameters.CreateCopy();
                var d_est_adjusted = d_est;

                // Multiple-input single-output modeling
                if (nGains> 1)
                {
                    bool doIncludeYsetpointAsInput = true;
                    var alternativeMISOModel = new UnitModel(unitModel_run1.GetModelParameters().CreateCopy(), "MISO");
                    var pidProcess_u0 = unitModel_run1.modelParameters.U0[pidInputIdx];
                    var pidProcess_unorm = unitModel_run1.modelParameters.UNorm[pidInputIdx];
                    // part 1: analysis of SISO estimate above, used to find inital estimtes
                    // the d_est from above will give a d_est that also include terms related to any changes in y_set
                    // but also in the non-pid input ("external" inputs u)
                    {
                        var ident_d = new UnitIdentifier();
                        var dataSet_d = new UnitDataSet("dist");
                        dataSet_d.Y_meas = d_est;
                        var inputList = new List<double[]>();
                        if (doIncludeYsetpointAsInput)
                            inputList.Add(dataSet.Y_setpoint);
                        for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                        {
                            if (inputIdx == pidInputIdx)
                                continue;
                            inputList.Add(dataSet.U.GetColumn(inputIdx));
                        }
                        dataSet_d.U = Array2D<double>.CreateFromList(inputList);
                        // this problem is perhaps have been better to solve on "diff" form?
                        // otherwise it tends to find paramters to minimize the total squared error to account for 
                        // non-zero disturbances

                        // it seems that if there is setpoint changes the regular regression works 
                        // better, but if the data is mainly excited by an unknown disturbance, 
                        // then the "diff" version of the regression works better. 
                        UnitModel model_dist;
                        if (doesSetpointChange)
                        {
                            model_dist = ident_d.IdentifyLinear(ref dataSet_d, false);
                        }
                        else
                        {
                            model_dist = ident_d.IdentifyLinearDiff(ref dataSet_d, false);
                        }
                        // debug plot:
                        if (false)
                        {
                            var variableList = new List<double[]> { d_est, dataSet_d.Y_sim };
                            var variableNameList = new List<string> { "y1=d_est_SISO", "y1_d_est_modelled" };
                            for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                            {
                                variableList.Add(dataSet_d.U.GetColumn(inputIdx));
                                variableNameList.Add("y3=dataSet_d[" + inputIdx + "]");
                            }
                            Shared.EnablePlots();
                            Plot.FromList(
                                variableList, variableNameList, dataSet.GetTimeBase(), "ClosedLoopGlobal_model_dist");
                            Shared.DisablePlots();
                        }
                        // it seems that the gains for external inputs u in model_dist are accurate if 
                        // and when pidLinProcessGain matches the "true" estimate.
                        d_est_adjusted = vec.Subtract(d_est, dataSet_d.Y_sim);
                        int curModelDistInputIdx = 0;
                        if(doIncludeYsetpointAsInput)
                            curModelDistInputIdx = 1;
                        for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                        {
                            if (inputIdx == pidInputIdx)
                            {
                                alternativeMISOModel.modelParameters.LinearGains[inputIdx] = pidLinProcessGain;
                            }
                            else
                            {
                                alternativeMISOModel.modelParameters.LinearGains[inputIdx] =
                                    model_dist.modelParameters.LinearGains[curModelDistInputIdx];
                                curModelDistInputIdx++;
                            }
                        }
                    }
                    // part 2: actual disturbance modeling
                   var dataSet_altMISO = new UnitDataSet(dataSet);
                   DisturbanceIdResult distIdResultAlt_MISO = DisturbanceIdentifier.EstimateDisturbance
                        (dataSet_altMISO, alternativeMISOModel, pidInputIdx, pidParams);
                    var d_est_MISO = distIdResultAlt_MISO.d_est;
                    isOK = ClosedLoopSim
                       (dataSet_altMISO, alternativeMISOModel.GetModelParameters(), pidParams, d_est_MISO, "run_altMISO");

                    if (false)
                    {
                        // d_est_SISO: first pass, does not account for changes in u_externals and y_set
                        // d_est_adj: second pass, is d_est_SISO where influence has been modelled and subtracted (this will not be perfect but should move less in repsonse to u_Ext and yset)
                        // d_est_MISO: third pass, hopefully improves on d_est_adj-  this estimate ideally does not move at all to changes in u_external and y_set
                        var variableList = new List<double[]> { d_est, d_est_adjusted, distIdResultAlt_MISO.d_est,dataSet.Y_setpoint };
                        var variableNameList = new List<string> { "y1=d_est_SISO", "y1=d_est_adj", "y1=d_est_MISO", "y4=y_set" };
                        for (int inputIdx=0; inputIdx < nGains; inputIdx++)
                        {
                            if (pidInputIdx == inputIdx)
                                continue;
                            variableList.Add(dataSet.U.GetColumn(inputIdx));
                            variableNameList.Add("y3=u["+inputIdx+"]");
                        }
                        Shared.EnablePlots();
                        Plot.FromList(
                            variableList, variableNameList, dataSet_altMISO.GetTimeBase(), "ClosedLoopGlobal_d_est");
                        Shared.DisablePlots();
                    }
                    if (isOK)
                    {
                        d_est = d_est_MISO;
                        u_pid_adjusted = distIdResultAlt_MISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);
                        alternativeModelParameters = alternativeMISOModel.modelParameters.CreateCopy();
                        alternativeModelParameters.Fitting = new FittingInfo();
                        alternativeModelParameters.Fitting.WasAbleToIdentify = true;
                    }
                }
                if (!isOK)
                    continue;

                double covarianceBtwDestAndYset = Math.Abs(CorrelationCalculator.CorrelateTwoVectors(d_est, dataSet.Y_setpoint, dataSet.IndicesToIgnore));

                // v3: just choose the gain that gives the least "variance" in d_est
                var dest_variance = vec.Mean(vec.Abs(vec.Diff(u_pid_adjusted))).Value;

                // v4:  if MISO: the disturbance should be as indpendent of the external inputs as possible
                double covarianceBtwDestAndUexternal = 0;
                if (dataSet.U.GetNColumns() > 1)
                {
                    for (int inputIdx = 0; inputIdx < dataSet.U.GetNColumns(); inputIdx++)
                    {
                        if (inputIdx == pidInputIdx )
                        {
                            continue;
                        }
                        covarianceBtwDestAndUexternal +=
                            Math.Abs(CorrelationCalculator.CorrelateTwoVectors(d_est, dataSet.U.GetColumn(inputIdx), dataSet.IndicesToIgnore));
                        //Math.Abs(Measures.Covariance(d_est, dataSet.U.GetColumn(inputIdx), false));
                        // Math.Abs(CorrelationCalculator.CorrelateTwoVectors(d_est, distIdResultAlt.adjustedUnitDataSet.U.GetColumn(nonPidIdx), dataSet.IndicesToIgnore));
                    }
                }
                // finally,save all kpis of of this run.
                searchResults.Add(pidLinProcessGain,alternativeModelParameters, covarianceBtwDestAndYset, dest_variance, covarianceBtwDestAndUexternal);
                d_prev = d_est;
            }

            // finally, select the best model from all the tested models by looking across all the kpis stored
            (UnitModel bestUnitModel,bool didFindMimimum) = searchResults.GetBestModel(pidProcessInputInitalGainEstimate);
            if (bestUnitModel != null)
            {
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
                bestUnitModel.modelParameters.Fitting = new FittingInfo();
                bestUnitModel.modelParameters.Fitting.WasAbleToIdentify = true;
            }

            return new Tuple<UnitModel,double>(bestUnitModel, gainUnc);
        }

        private Tuple<UnitModel, DisturbanceIdResult> EstimateSISOdisturbanceForProcGain( UnitModel referenceMISOmodel, 
            double pidLinProcessGain, int pidInputIdx, UnitDataSet dataSet, PidParameters pidParams)
        {
            var alternativeModel = new UnitModel(referenceMISOmodel.modelParameters.CreateCopy(), "SISO");

            alternativeModel.modelParameters.LinearGains = new double[] { pidLinProcessGain };
            alternativeModel.ModelInputIDs = new string[] { referenceMISOmodel.GetModelInputIDs()[pidInputIdx] };

            alternativeModel.modelParameters.U0 = 
                new double[] { referenceMISOmodel.modelParameters.U0[pidInputIdx] };
            alternativeModel.modelParameters.UNorm = 
                new double[] { referenceMISOmodel.modelParameters.UNorm[pidInputIdx] };
            alternativeModel.modelParameters.Curvatures =
                new double[] { referenceMISOmodel.modelParameters.Curvatures[pidInputIdx] };
            alternativeModel.modelParameters.CurvatureUnc =
                new double[] { referenceMISOmodel.modelParameters.CurvatureUnc[pidInputIdx] };
            alternativeModel.modelParameters.LinearGainUnc = 
                new double[] { referenceMISOmodel.modelParameters.LinearGainUnc[pidInputIdx] };
            alternativeModel.modelParameters.Fitting = new FittingInfo();
            alternativeModel.modelParameters.Fitting.WasAbleToIdentify = true;

            var dataSet_SISO = new UnitDataSet(dataSet);
            dataSet_SISO.U = Array2D<double>.CreateFromList( new List<double[]> { dataSet.U.GetColumn(pidInputIdx) } );

            // pidInputIdx=0 always for the new SISO system:
            DisturbanceIdResult distIdResultAlt = DisturbanceIdentifier.EstimateDisturbance
                (dataSet_SISO, alternativeModel, 0, pidParams);

            var d_est = distIdResultAlt.d_est;
            var isOK = ClosedLoopSim
                (dataSet_SISO, alternativeModel.GetModelParameters(), pidParams, d_est, "run_altSISO");

            if (isOK)
                return new Tuple<UnitModel, DisturbanceIdResult>(alternativeModel, distIdResultAlt);
            else
                return new Tuple<UnitModel, DisturbanceIdResult>(null, distIdResultAlt);

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