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
        const int firstPassNumIterations = 60;//TODO:change back to 50!
        const int secondPassNumIterations = 10;
        const double initalGuessFactor_higherbound = 2.5;// 2 is a bit low, should be a bit higher
        const int nDigits = 5; //number of significant digits in results.
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
        /// <param name = "pidParams">if the setpoint of the control changes in the time-set, then the paramters of pid control need to be given.</param>
        /// <param name = "pidInputIdx">the index of the PID-input to the unit model</param>
        /// 
        /// <returns>The unit model, with the name of the newly created disturbance added to the additiveInputSignals</returns>
        public static (UnitModel, double[]) Identify(UnitDataSet dataSet, PidParameters pidParams = null, int pidInputIdx = 0)
        {
            bool wasGainGlobalSearchDone = false;
            bool doTimeDelayEstOnRun1 = false;
            if (dataSet.Y_setpoint == null || dataSet.Y_meas == null || dataSet.U == null)
            {
                return (null, null);
            }
            var vec = new Vec(dataSet.BadDataID);
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
            // set "indicestoignore" to exclude values outside ymin/ymax_fit and umin_fit,u_max_fit
             // 
           // dataSet.DetermineIndicesToIgnore(fittingSpecs);


            // this variable holds the "newest" unit model run and is updated
            // over multiple runs, and as it improves, the 
            // estimate of the disturbance improves along with it.
            var idDisturbancesList = new List<DisturbanceIdResult>();
            var idUnitModelsList = new List<UnitModel>();
            var processGainList = new List<double>();

            double[] u0 = SignificantDigits.Format(dataSet.U.GetRow(0), nDigits); 
            double y0 = dataSet.Y_meas[0];
            bool isOK;
            var dataSetRun1 = new UnitDataSet(dataSet);
            var dataSetRun2 = new UnitDataSet(dataSet);
            var fittingSpecs = new FittingSpecs();
            fittingSpecs.u0 = u0;

            // ----------------
            // run1: no process model assumed, let disturbance estimator guesstimate a pid-process gain, 
            // to give afirst estimate of the disturbance
            // this is often a quite good estimate of the process gain, which is the most improtant parameter
            // withtout a reference model which has the correct time constant and time delays, 
            // dynamic "overshoots" will enter into the estimated disturbance, try to fix this by doing 
            // "refinement" runs afterwards.
            { 

                /////////////////
                // run1, step1: 
                var distIdResult1 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSetRun1, null, pidInputIdx, pidParams);

                dataSetRun1.D = distIdResult1.d_est;
                var unitModel_run1 = UnitIdentifier.IdentifyLinearAndStatic(ref dataSetRun1, fittingSpecs, doTimeDelayEstOnRun1);
                idDisturbancesList.Add(distIdResult1);
                idUnitModelsList.Add(unitModel_run1);
                isOK = ClosedLoopSim(dataSetRun1, unitModel_run1.GetModelParameters(), pidParams, distIdResult1.d_est, "run1");

                //  run1, "step2" : "global search" for linear pid-gainsgains
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
                    var max_gain =  Math.Abs(pidProcessInputInitalGainEstimate * initalGuessFactor_higherbound);
                    var min_gain = - max_gain;

                    //  min_gain = 0;      // when debugging, it might be advantageous to set min_gain equal to the known true value

                    // first pass(wider grid with larger grid size)
                    var retPass1 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx, 
                         unitModel_run1, pidProcessInputInitalGainEstimate, 
                        min_gain, max_gain, fittingSpecs,firstPassNumIterations);
                    var bestUnitModel = retPass1.Item1;

                    if (bestUnitModel != null)
                    {
                        // second pass(finer grid around best result of first pass)
                        if (retPass1.Item1.modelParameters.Fitting.WasAbleToIdentify && secondPassNumIterations > 0)
                        {
                            var gainPass1 = retPass1.Item1.modelParameters.LinearGains[pidInputIdx];
                            var retPass2 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx,
                               retPass1.Item1, gainPass1, gainPass1 - retPass1.Item2, gainPass1 + retPass1.Item2, 
                               fittingSpecs,secondPassNumIterations);
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


            // ----------------
            // - issue is that after run 1 modelled output does not match measurement.
            // - the reason that we want this run is that after run1 the time constant and 
            // time delays are way off if the process is excited mainly by disturbances.
            // - the reason that we cannot do run2 immediately, is that that formulation 
            // does not appear to give a solution if the guess disturbance vector is bad.

            const bool doRun2 = true;
            const double LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE = 30;//too low

            // run2: do a run where it is no longer assumed that x[k-1] = y[k], 
            // this run has the best chance of estimating correct time constants, but it requires a good inital guess of d

            // run 2 version 1: try looking for the time constant that gives the smallest average disturbance amplitude

            const bool doRun2Version2 = false;// TODO: implement version 2 and change over to improve Tc estimate

            if (doRun2  && idUnitModelsList.Last() != null)
            {
                var unitModel = idUnitModelsList.Last();

                if (doRun2Version2)
                {
                    var pidModel = new PidModel(pidParams, "PID");
                    (var plantSim, var inputData) = PlantSimulator.CreateFeedbackLoop(dataSetRun2, pidModel, unitModel, pidInputIdx);

                    var fitScores = new List<double>();
                    var Tcs = new List<double>();   

                    var TcListTest = new List<double>() { 0,3,6,9,12,15,18,21};//for debugging, generalize
                    var modelParams = ((UnitModel)plantSim.modelDict[unitModel.ID]).GetModelParameters();

                    // TODO: need to estimate disturbance and add it to the simulation in each case.
                    foreach ( var Tc in TcListTest)
                    {
                        modelParams.TimeConstant_s = Tc; 
                        ((UnitModel)plantSim.modelDict[unitModel.ID]).SetModelParameters(modelParams);

                        var distId = DisturbanceIdentifier.EstimateDisturbance
                           (dataSetRun2, ((UnitModel)plantSim.modelDict[unitModel.ID]), pidInputIdx, pidParams);

                        inputData.Add(plantSim.AddExternalSignal(unitModel, SignalType.Disturbance_D), distId.d_est);

                        bool isSimOK = plantSim.Simulate(inputData, out var simData); 
                        if (!isSimOK)
                            Console.WriteLine("SIMULATE FAILED"); ;
                        fitScores.Add(plantSim.PlantFitScore);
                        Tcs.Add(Tc);
                    }
                    // todo: determne the Tc that has the higest plant fit score
                    Console.WriteLine("t");
                }
                else
                {
                    var distIdResult2 = DisturbanceIdentifier.EstimateDisturbance
                        (dataSetRun2, unitModel, pidInputIdx, pidParams);
                    var estDisturbances = new List<double[]>();
                    var distDevs = new List<double>();

                    estDisturbances.Add(distIdResult2.d_est);
                    var timeBase = dataSetRun2.GetTimeBase();
                    double candiateTc_s = 0;
                    bool curDevIsDecreasing = true;
                    double firstDev = vec.Sum(vec.Abs(vec.Diff(distIdResult2.d_est))).Value;
                    distDevs.Add(firstDev);
                    while (candiateTc_s < LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE * timeBase && curDevIsDecreasing)
                    {
                        candiateTc_s += timeBase;
                        var newParams = unitModel.GetModelParameters().CreateCopy();
                        newParams.TimeConstant_s = candiateTc_s;
                        var newModel = new UnitModel(newParams);
                        var distIdResult_Test = DisturbanceIdentifier.EstimateDisturbance
                            (dataSetRun2, newModel, pidInputIdx, pidParams);
                        estDisturbances.Add(distIdResult_Test.d_est);
                        var curDev = vec.Sum(vec.Abs(vec.Diff(distIdResult_Test.d_est))).Value;
                        if (curDev < distDevs.Last<double>())
                            curDevIsDecreasing = true;
                        else
                            curDevIsDecreasing = false;
                        distDevs.Add(curDev);
                    }

                    // TODO: it would be possible to divide the time-constant into a time-delay and a time constant 

                    if (candiateTc_s > 0)
                    {
                        candiateTc_s -= timeBase;
                    }
                    var step2params = unitModel.GetModelParameters().CreateCopy();
                    step2params.TimeConstant_s = candiateTc_s;
                    var step2Model = new UnitModel(step2params);
                    idUnitModelsList.Add(step2Model);
                    var distIdResult_step4 = DisturbanceIdentifier.EstimateDisturbance
                            (dataSetRun2, step2Model, pidInputIdx, pidParams);
                    idDisturbancesList.Add(distIdResult_step4);
                }
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

                (var isOk1,var sim1results) = PlantSimulator.SimulateSingle(dataSet, idUnitModelsList[0], false);
                (var isOk2, var sim2results) = PlantSimulator.SimulateSingle(dataSet, idUnitModelsList[1], false);
                (var isOk3, var sim3results) = PlantSimulator.SimulateSingle(dataSet, idUnitModelsList[2], false);

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
                identUnitModel.modelParameters.Fitting.TimeBase_s = dataSet.GetTimeBase();

                var uMaxList = new List<double>();
                var uMinList = new List<double>();

                for (int i = 0; i < dataSet.U.GetNColumns(); i++)
                {
                    uMaxList.Add(SignificantDigits.Format(vec.Max(dataSet.U.GetColumn(i)), nDigits));
                    uMinList.Add(SignificantDigits.Format(vec.Min(dataSet.U.GetColumn(i)), nDigits));
                }
                identUnitModel.modelParameters.Fitting.Umax = uMaxList.ToArray();
                identUnitModel.modelParameters.Fitting.Umin = uMinList.ToArray();

                if (wasGainGlobalSearchDone)
                {
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop/w gain global search/2 step";
                }
                else
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop local (NO global search)";
                identUnitModel.modelParameters.Fitting.NFittingTotalDataPoints = dataSet.GetNumDataPoints();
                identUnitModel.modelParameters.Fitting.NFittingBadDataPoints = dataSet.IndicesToIgnore.Count();


            }
            // closed-loop simulation, adds U_sim and Y_sim to "dataset"
            {
                ClosedLoopSim(dataSet, identUnitModel.modelParameters, pidParams, disturbance);
            }
            // round resulting parameters

            identUnitModel.modelParameters.LinearGains = SignificantDigits.Format(identUnitModel.modelParameters.LinearGains, nDigits);
            identUnitModel.modelParameters.LinearGainUnc = SignificantDigits.Format(identUnitModel.modelParameters.LinearGainUnc, nDigits);
            identUnitModel.modelParameters.Bias = SignificantDigits.Format(identUnitModel.modelParameters.Bias, nDigits);
            if (identUnitModel.modelParameters.BiasUnc.HasValue)
                identUnitModel.modelParameters.BiasUnc = SignificantDigits.Format(identUnitModel.modelParameters.BiasUnc.Value, nDigits);

            identUnitModel.modelParameters.TimeConstant_s = SignificantDigits.Format(identUnitModel.modelParameters.TimeConstant_s, nDigits);
            if (identUnitModel.modelParameters.TimeConstantUnc_s.HasValue)
                identUnitModel.modelParameters.TimeConstantUnc_s = SignificantDigits.Format(identUnitModel.modelParameters.TimeConstantUnc_s.Value, nDigits);
            identUnitModel.modelParameters.UNorm = SignificantDigits.Format(identUnitModel.modelParameters.UNorm, nDigits);


            return (identUnitModel,disturbance);
        }

        private static Tuple<UnitModel,double> GlobalSearchLinearPidGain(UnitDataSet dataSet, PidParameters pidParams, int pidInputIdx, 
            UnitModel unitModel_run1, double pidProcessInputInitalGainEstimate, double minPidProcessGain,
            double maxPidProcessGain, FittingSpecs fittingSpecs, int numberOfGlobalSearchIterations = 40)
        {
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
                    if(!doesSetpointChange)
                        doIncludeYsetpointAsInput = false;
                    var alternativeMISOModel = new UnitModel(unitModel_run1.GetModelParameters().CreateCopy(), "MISO");
                    var pidProcess_u0 = unitModel_run1.modelParameters.U0[pidInputIdx];
                    var pidProcess_unorm = unitModel_run1.modelParameters.UNorm[pidInputIdx];
                    // part 1: analysis of SISO estimate above, used to find inital estimtes
                    // the d_est from above will give a d_est that also include terms related to any changes in y_set
                    // but also in the non-pid input ("external" inputs u)
                    {
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
                        // really unsure about if it is better to use one or the other here!
                        // new: try both and choose best.
                        {
                            var dataSet_diff = new UnitDataSet(dataSet_d);
                            var model_dist_diff = UnitIdentifier.IdentifyLinearDiff(ref dataSet_diff, fittingSpecs, false);
                            var model_dist_abs = UnitIdentifier.IdentifyLinear(ref dataSet_d, fittingSpecs, false);

                            var diffParams = model_dist_diff.GetModelParameters().Fitting;
                            var absParams = model_dist_abs.GetModelParameters().Fitting;

                            if (diffParams.WasAbleToIdentify && !absParams.WasAbleToIdentify)
                            {
                                model_dist = model_dist_diff;
                            }
                            else if (!diffParams.WasAbleToIdentify && absParams.WasAbleToIdentify)
                            {
                                model_dist = model_dist_abs;
                            }
                            else
                            {
                                // RsqDiff does not a choose best method when acutal system has dynamics
                                // RsqAbs and ObjFunvalAbs is better for dynamic systems, but sometimes faisl for static systems
                                
                                // this works equally well:
                                //    if( vec.Sum(model_dist_diff.GetModelParameters().LinearGainUnc).Value<
                                //      vec.Sum(model_dist_abs.GetModelParameters().LinearGainUnc).Value)
                                if (model_dist_diff.GetModelParameters().Fitting.ObjFunValDiff >
                                    model_dist_abs.GetModelParameters().Fitting.ObjFunValDiff)
                          
                                    model_dist = model_dist_diff;
                                else
                                    model_dist = model_dist_abs;
                            }
                        }
                        /*


                         if (doesSetpointChange)
                         {
                             model_dist = ident_d.IdentifyLinearDiff(ref dataSet_d, fittingSpecs,false);
                         }
                         else
                         {
                            // need to enable time delay estimation here, otherwise 
                            // "diff" estimate is very sensitive to incorrect dynamics.
                            model_dist = ident_d.IdentifyLinearDiff(ref dataSet_d, fittingSpecs, true);
                        }*/

                        // model_dist = ident_d.IdentifyLinear(ref dataSet_d, false);

                        // debug plot:
                        bool doDebugPlot = false;
                        if (doDebugPlot)
                        {
                            var variableList = new List<double[]> { d_est, dataSet_d.Y_sim };
                            var variableNameList = new List<string> { "y1=d_est_SISO", "y1_d_est_modelled" };
                            for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                            {
                                variableList.Add(dataSet_d.U.GetColumn(inputIdx));
                                variableNameList.Add("y3=dataSet_u[" + inputIdx + "]");
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

                double covarianceBtwDestAndYset = Math.Abs(CorrelationCalculator.CorrelateTwoVectors(
                    d_est, dataSet.Y_setpoint, dataSet.IndicesToIgnore));

                // v3: just choose the gain that gives the least "variance" in d_est
                // this is not completely general, if there is no change in the setpoint and zero disturbance, 
                // then a process gain of zero appears to give the smallest value here. 
                // u_pid_adjusted is the variance

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
                         //   Math.Abs(CorrelationCalculator.CorrelateTwoVectors(d_est, dataSet.U.GetColumn(inputIdx), dataSet.IndicesToIgnore));
                        Math.Abs(Measures.Covariance(d_est, dataSet.U.GetColumn(inputIdx), false));
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

        private static Tuple<UnitModel, DisturbanceIdResult> EstimateSISOdisturbanceForProcGain( UnitModel referenceMISOmodel, 
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

   

        /// <summary>
        /// Closed-loop simulation of a unit model and pid model, for a given unit data set
        /// </summary>
        /// <param name="unitData">unit dataset for simulation</param>
        /// <param name="modelParams">paramters or UnitModel</param>
        /// <param name="pidParams">parameters of PidModel</param>
        /// <param name="disturbance">disturbance vector</param>
        /// <param name="name">optional name used for plotting</param>
        /// <returns></returns>
        public static bool ClosedLoopSim(UnitDataSet unitData, UnitParameters modelParams, PidParameters pidParams,
            double[] disturbance,string name="")
        {
            if (pidParams == null)
            {
                return false;
            }

            unitData.D = disturbance;
            var pidModel = new PidModel(pidParams);
            var unitModel = new UnitModel(modelParams);
            // TODO: replace this with a "closed-loop" simulator that uses the PlantSimulator.
            //var ps = new PlantSimulator(new List<ISimulatableModel> { unitModel,pidModel });
            //var inputData = new TimeSeriesDataSet();// TODO: need to map signal names here.
            //ps.Simulate(inputData, out var simData);

            var sim = new UnitSimulator(unitModel);
            bool isOk = sim.CoSimulate(pidModel, ref unitData);
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