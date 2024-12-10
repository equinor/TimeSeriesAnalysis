using Accord.IO;
using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
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
        const int firstPassNumIterations = 20;
        const int secondPassNumIterations = 0;
        const double initalGuessFactor_higherbound = 2.5;// 2 is a bit low, should be a bit higher
        const int nDigits = 5; //number of significant digits in results.
        ////////////////////////
        const bool doDebuggingPlot = false;
        /// <summary>
        /// Identify the unit model of a closed-loop system and the disturbance (additive output signal)
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name = "dataSet">the unit data set, containing both the input to the unit and the output</param>
        /// <param name = "pidParams">if the setpoint of the control changes in the time-set, then the paramters of pid control need to be given.</param>
        /// <param name = "pidInputIdx">the index of the PID-input to the unit model</param>
        /// 
        /// <returns>The unit model, with the name of the newly created disturbance added to the additiveInputSignals</returns>
        public static (UnitModel, double[]) Identify(UnitDataSet dataSet, PidParameters pidParams = null, int pidInputIdx = 0)
        {
            bool doConsoleDebugOut = true;

            bool wasGainGlobalSearchDone = false;
            bool doTimeDelayEstOnRun1 = false;
            bool isMISO = dataSet.U.GetNColumns() > 1 ? true : false;
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
                    var ind = vec.FindValues(vec.Diff(dataSet.Y_setpoint), 0, VectorFindValueType.NotEqual, dataSet.IndicesToIgnore);
                    // if the only setpoint step is in the first iteration,ignore it.
                    if (ind.Count() < dataSet.GetNumDataPoints() / 4 && ind.First() <= 1)
                    {
                        dataSet.IndicesToIgnore.Add(ind.First() - 1);//because "diff",subtract 1
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
            {
                // ----------------
                // run1: no process model assumed, let disturbance estimator guesstimate a pid-process gain, 
                // to give afirst estimate of the disturbance

                var unitModel_step1 = EstimateClosedLoopProcessGain(dataSetRun1, pidParams, pidInputIdx);
                var distIdResult1 = DisturbanceIdentifier.CalculateDisturbanceVector(dataSetRun1, unitModel_step1, pidInputIdx, pidParams);
                if (doConsoleDebugOut)
                    Console.WriteLine("Step1: " + unitModel_step1.GetModelParameters().LinearGains.ElementAt(pidInputIdx).ToString("F3", CultureInfo.InvariantCulture));

                // run1: ident (attempt to identify any other inputs) 
                if (isMISO)
                {
                    dataSetRun1.D = distIdResult1.d_est;
                    unitModel_step1 = UnitIdentifier.IdentifyLinearAndStatic(ref dataSetRun1, fittingSpecs, doTimeDelayEstOnRun1);
                    unitModel_step1.modelParameters.LinearGainUnc = null;
                    idDisturbancesList.Add(distIdResult1);
                    idUnitModelsList.Add(unitModel_step1);
                    isOK = ClosedLoopSim(dataSetRun1, unitModel_step1.GetModelParameters(), pidParams, distIdResult1.d_est, "run1");
                    if (doConsoleDebugOut)
                        Console.WriteLine("Step1,ident: " + unitModel_step1.GetModelParameters().LinearGains.First().ToString("F3", CultureInfo.InvariantCulture));
                }

                //   "step2" : "global search" for linear pid-gainsgains
                if (unitModel_step1.modelParameters.GetProcessGains() != null)
                {
                    double pidProcessInputInitalGainEstimate = unitModel_step1.modelParameters.GetProcessGains()[pidInputIdx];
                    var gainAndCorrDict = new Dictionary<double, ClosedLoopGainGlobalSearchResults>();
                    //
                    // looking to find the process gain that "decouples" d_est from Y_setpoint as much as possible.
                    //
                    // or antoher way to look at ti is that the output U with setpoint effects removed should be as decoupled 
                    // form Y_setpoint.

                    // in some cases the first linear regression done without any estimate of the disturbance can even have the wrong
                    // sign of the linear gains, although the amplitude will in general be of the right order, thus 
                    // min_gain = -max_gain;  is a more robust choice than some small same-sign value or 0.

                    double max_gain, min_gain;

                    if (pidProcessInputInitalGainEstimate > 0)
                    {
                        max_gain = pidProcessInputInitalGainEstimate * initalGuessFactor_higherbound;
                        min_gain = pidProcessInputInitalGainEstimate * 1 / initalGuessFactor_higherbound;
                    }
                    else
                    {
                        min_gain = pidProcessInputInitalGainEstimate * initalGuessFactor_higherbound;
                        max_gain = pidProcessInputInitalGainEstimate * 1 / initalGuessFactor_higherbound;
                    }

                 //   var min_gain = - max_gain;
                    if (doConsoleDebugOut)
                    {
                        Console.WriteLine("Step2,GS : " + min_gain.ToString("F3", CultureInfo.InvariantCulture) + " to " + max_gain.ToString("F3", CultureInfo.InvariantCulture));
                    }

                    //  min_gain = 0;      // when debugging, it might be advantageous to set min_gain equal to the known true value

                    // first pass(wider grid with larger grid size)
                    var retGlobalSearch1 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx,
                        unitModel_step1, pidProcessInputInitalGainEstimate,
                       min_gain, max_gain, fittingSpecs, firstPassNumIterations);
                    var bestUnitModel = retGlobalSearch1.Item1;

                    if (doConsoleDebugOut && retGlobalSearch1.Item1 != null)
                        Console.WriteLine("Step2,GS1: " + retGlobalSearch1.Item1.GetModelParameters().LinearGains.First().ToString("F3", CultureInfo.InvariantCulture));
                    else
                        Console.WriteLine("Step2,GS1: FAILED");

                    if (bestUnitModel != null)
                    {
                        // second pass(finer grid around best result of first pass)
                        if (retGlobalSearch1.Item1.modelParameters.Fitting.WasAbleToIdentify && secondPassNumIterations > 0)
                        {
                            const int WIDTH_OF_SEARCH_PASS2 = 3;

                            var gainPass1 = retGlobalSearch1.Item1.modelParameters.LinearGains[pidInputIdx];
                            var retGlobalSearch2 = GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx,
                               retGlobalSearch1.Item1, gainPass1, gainPass1 - retGlobalSearch1.Item2 * WIDTH_OF_SEARCH_PASS2, gainPass1 + retGlobalSearch1.Item2 * WIDTH_OF_SEARCH_PASS2,
                               fittingSpecs, secondPassNumIterations);
                            bestUnitModel = retGlobalSearch2.Item1;
                            wasGainGlobalSearchDone = true;
                            if (doConsoleDebugOut)
                                Console.WriteLine("Step2,GS2: " + retGlobalSearch2.Item1.GetModelParameters().LinearGains.First().ToString("F3", CultureInfo.InvariantCulture));
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
            const double LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE = 60 + 1;//too low

            // run2: do a run where it is no longer assumed that x[k-1] = y[k], 
            // this run has the best chance of estimating correct time constants, but it requires a good inital guess of d

            // run 2 version 1: try looking for the time constant that gives the smallest average disturbance amplitude

            const bool doRun2Version2 = false;// TODO: implement version 2 and change over to improve Tc estimate

            if (idUnitModelsList.Count() >0)
            {
                if (doRun2 && idUnitModelsList.Last() != null)
                {
                    var unitModel = idUnitModelsList.Last();
                    unitModel.ID = "process";

                    if (doRun2Version2)
                    {
                        var pidModel = new PidModel(pidParams, "PID");

                        var fitScores = new List<double>();
                        var Tcs = new List<double>();

                        var TcListTest = new List<double>() { 0, 5, 10, 15 };//for debugging, generalize
                        var modelParams = unitModel.GetModelParameters();

                        // TODO: need to estimate disturbance and add it to the simulation in each case.
                        var runCounter = 0;
                        var d_estList = new List<double[]>();
                        var d_estDesc = new List<string>();
                        var y_simList = new List<double[]>();
                        var y_simDesc = new List<string>();
                        var u_simList = new List<double[]>();
                        var u_simDesc = new List<string>();
                        DateTime[] dateTimes = null;
                        bool doDebugPlot = true;

                        (var plantSim, var inputData) = PlantSimulator.CreateFeedbackLoopWithEstimatedDisturbance(dataSetRun2, pidModel, unitModel, pidInputIdx);
                        if (doDebugPlot)
                        {
                            y_simList.Add(inputData.GetValues(unitModel.ID, SignalType.Output_Y));
                            y_simDesc.Add("y1= y_meas");
                            u_simList.Add(inputData.GetValues(pidModel.ID, SignalType.PID_U));
                            u_simDesc.Add("y3= u_meas");
                        }

                        foreach (var Tc in TcListTest)
                        {
                            modelParams.TimeConstant_s = Tc;
                            ((UnitModel)plantSim.modelDict[unitModel.ID]).SetModelParameters(modelParams);

                            var inputDataCoSim = new TimeSeriesDataSet(inputData);
                            // 2. co-simulate with disturbance from 1.
                            bool isCoSimOK = plantSim.Simulate(inputDataCoSim, out var simData);
                            if (isCoSimOK)
                            {
                                fitScores.Add(plantSim.PlantFitScore); // issue: these are now always 100%???
                                Tcs.Add(Tc);
                            }
                            // debugging plot
                            if (doDebugPlot)
                            {
                                y_simList.Add(simData.GetValues(unitModel.ID, SignalType.Output_Y));
                                y_simDesc.Add("y1= y_sim" + runCounter);
                                u_simList.Add(simData.GetValues(pidModel.ID, SignalType.PID_U));
                                u_simDesc.Add("y3= u_sim" + runCounter);
                                d_estList.Add(simData.GetValues(unitModel.ID, SignalType.Disturbance_D));
                                d_estDesc.Add("y2= d_est" + runCounter);
                                dateTimes = inputDataCoSim.GetTimeStamps();
                            }
                            runCounter++;
                        }
                        if (doDebugPlot)
                        {
                            y_simList.AddRange(u_simList);
                            y_simList.AddRange(d_estList);
                            y_simDesc.AddRange(u_simDesc);
                            y_simDesc.AddRange(d_estDesc);
                            Shared.EnablePlots();
                            Plot.FromList(y_simList, y_simDesc, dateTimes, "debug");
                            Shared.DisablePlots();
                        }
                        // todo: determine the Tc that has the higest plant fit score
                        Console.WriteLine(fitScores.ToString());
                    }
                    else
                    {
                        // approach1:
                        // "look for the time-constant that gives the disturbance that changes the least"
                        //
                        var distIdResult2 = DisturbanceIdentifier.CalculateDisturbanceVector
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
                            var distIdResult_Test = DisturbanceIdentifier.CalculateDisturbanceVector
                                (dataSetRun2, newModel, pidInputIdx, pidParams);
                            estDisturbances.Add(distIdResult_Test.d_est);
                            var curDev = vec.Sum(vec.Abs(vec.Diff(distIdResult_Test.d_est))).Value;
                            if (curDev < distDevs.Last<double>())
                                curDevIsDecreasing = true;
                            else
                                curDevIsDecreasing = false;
                            distDevs.Add(curDev);
                        }

                        if (candiateTc_s == LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE)
                        {
                            // you may get here when the disturbance is continous and noisy
                            Console.WriteLine("warning: ClosedLoopIdentifierFailedToFindAUniqueProcessTc");
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
                        var distIdResult_step4 = DisturbanceIdentifier.CalculateDisturbanceVector
                                (dataSetRun2, step2Model, pidInputIdx, pidParams);
                        idDisturbancesList.Add(distIdResult_step4);
                        if (doConsoleDebugOut)
                            Console.WriteLine("Run2 " + step2Model.GetModelParameters().LinearGains.First().ToString("F3", CultureInfo.InvariantCulture));
                    }
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

            if (idDisturbancesList.Count > 0)
            {
                double[] disturbance = idDisturbancesList.ToArray()[idDisturbancesList.Count - 1].d_est;
                UnitModel identUnitModel = idUnitModelsList.ToArray()[idUnitModelsList.Count - 1];

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
                // closed-loop simulation, adds U_sim and Y_sim to dataset
                ClosedLoopSim(dataSet, identUnitModel.modelParameters, pidParams, disturbance);

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
                return (identUnitModel, disturbance);
            }
            else
            {
                var retModel = new UnitModel();
                retModel.modelParameters = new UnitParameters();
                retModel.modelParameters.Fitting = new FittingInfo();
                retModel.modelParameters.Fitting.WasAbleToIdentify = false;
                retModel.modelParameters.Fitting.SolverID = "ClosedLoop local (NO global search)";

                return (retModel, null);
            }

      
        }

        /// <summary>
        /// tries  <c>N=numberOfGlobalSearchIterations</c>  process gains of the <c>unitModel</c> in the range[minPidProcessGain,maxPidProcessGain], using a closed-loop simulation 
        /// that includes the PID-model with paramters pidParams that acts on input <c>pidInputIdx</c> of the unit model.
        /// 
        /// The method also accepts an inital guess of the unit model, <c>unitModel_run1</c>
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="pidParams"></param>
        /// <param name="pidInputIdx"></param>
        /// <param name="unitModel_run1"></param>
        /// <param name="pidProcessInputInitalGainEstimate"></param>
        /// <param name="minPidProcessGain"></param>
        /// <param name="maxPidProcessGain"></param>
        /// <param name="fittingSpecs"></param>
        /// <param name="numberOfGlobalSearchIterations"></param>
        /// <returns>the best model and the step size used as a tuple</returns>
        private static Tuple<UnitModel,double> GlobalSearchLinearPidGain(UnitDataSet dataSet, PidParameters pidParams, int pidInputIdx, 
            UnitModel unitModel_run1, double pidProcessInputInitalGainEstimate, double minPidProcessGain,
            double maxPidProcessGain, FittingSpecs fittingSpecs, int numberOfGlobalSearchIterations = 40)
        {
            var range = maxPidProcessGain - minPidProcessGain;
            var searchResults = new ClosedLoopGainGlobalSearchResults();
            var gainStepSize = range / numberOfGlobalSearchIterations;
            int nGains = unitModel_run1.modelParameters.GetProcessGains().Length;
            var vec = new Vec();
            bool doesSetpointChange = !(vec.Max(dataSet.Y_setpoint, dataSet.IndicesToIgnore) == vec.Min(dataSet.Y_setpoint, dataSet.IndicesToIgnore));

            var isOK = true;

            // //////////////////////////////////////////////////
            // try all the process gains between the min and the max and rank them
            // 

            var dEstPlotNames = new List<string>();
            var uSimPlotNames = new List<string>();
            var yProcPlotNames = new List<string>();
            var dEstList = new List<double[]>();
            var uSimList = new List<double[]>();
            var dLfList = new List<double[]>();
            var dHFList = new List<double[]>();
            var kpList = new List<double>();
            var yProcessList = new List<double[]>(); // the internal process output
            bool doDebugPlot = false;

            for (var curCandPidLinProcessGain = minPidProcessGain; curCandPidLinProcessGain <= maxPidProcessGain; curCandPidLinProcessGain += range / numberOfGlobalSearchIterations)
            {
                if (curCandPidLinProcessGain == 0)
                    continue;
                // Single-input-single output disturbance: will include transients in response to changes in yset and u_external
                var dataSetCopy = new UnitDataSet(dataSet);

                (var curCandidateSISOModel, var curCandDisturbanceEst_SISO) =
                    EstimateSISOdisturbanceForProcGain(unitModel_run1,
                        curCandPidLinProcessGain, pidInputIdx, dataSetCopy, pidParams);

                var dEst = curCandDisturbanceEst_SISO.d_est;
                // not sure about this one:
                var u_pid_adjusted = curCandDisturbanceEst_SISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);

                var alternativeModelParameters = curCandidateSISOModel.modelParameters.CreateCopy();
                var d_est_adjusted = dEst;

                // Multiple-input single-output modeling
                if (nGains > 1)
                {
                    bool doIncludeYsetpointAsInput = true;
                    if (!doesSetpointChange)
                        doIncludeYsetpointAsInput = false;
                    var alternativeMISOModel = new UnitModel(unitModel_run1.GetModelParameters().CreateCopy(), "MISO");
                    var pidProcess_u0 = unitModel_run1.modelParameters.U0[pidInputIdx];
                    var pidProcess_unorm = unitModel_run1.modelParameters.UNorm[pidInputIdx];
                    // part 1: analysis of SISO estimate above, used to find inital estimtes
                    // the d_est from above will give a d_est that also include terms related to any changes in y_set
                    // but also in the non-pid input ("external" inputs u)
                    {
                        var dataSet_d = new UnitDataSet("dist");
                        dataSet_d.Y_meas = dEst;
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
                        // debug plot:
                        if (false)
                        {
                            var variableList = new List<double[]> { dEst, dataSet_d.Y_sim };
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
                        d_est_adjusted = vec.Subtract(dEst, dataSet_d.Y_sim);
                        int curModelDistInputIdx = 0;
                        if (doIncludeYsetpointAsInput)
                            curModelDistInputIdx = 1;
                        for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                        {
                            if (inputIdx == pidInputIdx)
                            {
                                alternativeMISOModel.modelParameters.LinearGains[inputIdx] = curCandPidLinProcessGain;
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
                    var distIdResultAlt_MISO = DisturbanceIdentifier.CalculateDisturbanceVector
                         (dataSet_altMISO, alternativeMISOModel, pidInputIdx, pidParams);
                    var d_est_MISO = distIdResultAlt_MISO.d_est;
                    isOK = ClosedLoopSim(dataSet_altMISO, alternativeMISOModel.GetModelParameters(), pidParams, d_est_MISO, "run_altMISO");

                    if (false)
                    {
                        // d_est_SISO: first pass, does not account for changes in u_externals and y_set
                        // d_est_adj: second pass, is d_est_SISO where influence has been modelled and subtracted (this will not be perfect but should move less in repsonse to u_Ext and yset)
                        // d_est_MISO: third pass, hopefully improves on d_est_adj-  this estimate ideally does not move at all to changes in u_external and y_set
                        var variableList = new List<double[]> { dEst, d_est_adjusted, distIdResultAlt_MISO.d_est, dataSet.Y_setpoint };
                        var variableNameList = new List<string> { "y1=d_est_SISO", "y1=d_est_adj", "y1=d_est_MISO", "y4=y_set" };
                        for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                        {
                            if (pidInputIdx == inputIdx)
                                continue;
                            variableList.Add(dataSet.U.GetColumn(inputIdx));
                            variableNameList.Add("y3=u[" + inputIdx + "]");
                        }
                        Shared.EnablePlots();
                        Plot.FromList(
                            variableList, variableNameList, dataSet_altMISO.GetTimeBase(), "ClosedLoopGlobal_d_est");
                        Shared.DisablePlots();
                    }
                    if (isOK)
                    {
                        dEst = d_est_MISO;
                        u_pid_adjusted = distIdResultAlt_MISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);
                        alternativeModelParameters = alternativeMISOModel.modelParameters.CreateCopy();
                        alternativeModelParameters.Fitting = new FittingInfo();
                        alternativeModelParameters.Fitting.WasAbleToIdentify = true;
                    }
                }
                if (!isOK)
                    continue;

                double covarianceBtwDestAndYset = Math.Abs(CorrelationCalculator.CorrelateTwoVectors(
                    dEst, dataSet.Y_setpoint, dataSet.IndicesToIgnore));

                // v3: just choose the gain that gives the least "variance" in u_pid when the disturbance is simulated
                // in closed loop with the given PID-model and the assumed candiate process model. 

                var uPidVariance = vec.Mean(vec.Abs(vec.Diff(u_pid_adjusted))).Value;

                // v4: just choose the gain that gives the least "variance" in d_est 
                var dEstVariance = vec.Mean(vec.Abs(vec.Diff(dEst))).Value;

                // test:
                var covBtwUPidAdjustedAndDest = Math.Abs(Measures.Covariance(dEst, u_pid_adjusted, false));


                // v5:  if MISO: the disturbance should be as indpendent of the external inputs as possible
                double covarianceBtwDestAndUexternal = 0;
                if (dataSet.U.GetNColumns() > 1)
                {
                    for (int inputIdx = 0; inputIdx < dataSet.U.GetNColumns(); inputIdx++)
                    {
                        if (inputIdx == pidInputIdx)
                        {
                            continue;
                        }
                        covarianceBtwDestAndUexternal +=
                        //   Math.Abs(CorrelationCalculator.CorrelateTwoVectors(d_est, dataSet.U.GetColumn(inputIdx), dataSet.IndicesToIgnore));
                        Math.Abs(Measures.Covariance(dEst, dataSet.U.GetColumn(inputIdx), false));
                        // Math.Abs(CorrelationCalculator.CorrelateTwoVectors(d_est, distIdResultAlt.adjustedUnitDataSet.U.GetColumn(nonPidIdx), dataSet.IndicesToIgnore));
                    }
                }


                // finally,save all kpis of of this run.
                searchResults.Add(curCandPidLinProcessGain, alternativeModelParameters, covarianceBtwDestAndYset, 
                    uPidVariance, dEstVariance, covarianceBtwDestAndUexternal);

                // save the time-series for debug-plotting
                if (doDebugPlot == true)
                {
                    yProcPlotNames.Add("y1=y_p(Kp" + curCandPidLinProcessGain.ToString("F2", CultureInfo.InvariantCulture));
                    uSimPlotNames.Add("y3=u_sim(Kp" + curCandPidLinProcessGain.ToString("F2", CultureInfo.InvariantCulture));
                    dEstPlotNames.Add("y2=d(Kp=" + curCandPidLinProcessGain.ToString("F2", CultureInfo.InvariantCulture) + ")");
                    kpList.Add(curCandPidLinProcessGain);
                    dEstList.Add(dEst);
                    dLfList.Add(curCandDisturbanceEst_SISO.d_LF);
                    dHFList.Add(curCandDisturbanceEst_SISO.d_HF);
                    uSimList.Add(u_pid_adjusted);
                    var yProc = vec.Subtract(dataSet.Y_meas, dEst);
                    yProcessList.Add(yProc);
      
                 }
            }

            if (doDebugPlot)
            {
                /*  dEstList.AddRange(uSimList);
                  dEstList.AddRange(yProcessList);
                  dEstPlotNames.AddRange(uSimPlotNames);
                  dEstPlotNames.AddRange(yProcPlotNames);

                  Shared.EnablePlots();
                  Plot.FromList(dEstList, dEstPlotNames, dataSet.Times);
                  Shared.DisablePlots();*/

                double Power(double[] inSignal)
                {
                    var mean = vec.Mean(inSignal).Value;
                    var max = vec.Max(inSignal);
                    var min = vec.Min(inSignal);


                    double scale = Math.Max(Math.Abs(max - mean), Math.Abs(min - mean));

                    return vec.Mean( vec.Abs(vec.Subtract(inSignal, vec.Mean(inSignal).Value )) ).Value/ scale;
                }

                double Range(double[] inSignal)
                {
                    return vec.Max(inSignal) - vec.Min(inSignal);
                }


                for (var i = 0; i < dEstList.Count(); i++)
                {
                    // dEst has much more high frequency "noise" than yProc, which is low-pass filtered through the pid-controller and thus basically a scalend and possibly time-shifted version of the U_PID as measured.
                    // thus they two signals should not expect to have the same power...
                    //var dEstPower = Power(dEstList.ElementAt(i) );
                    // var yProcPower = Power(yProcessList.ElementAt(i) );

                    //
                    // these two signals have the same range!
                    //

                    //
                    // it is usually when yProcess has the same range as dHF, you are at or slightly above the true process gain!
                    //var dEstPower = Range(dHFList.ElementAt(i));
                    //var yProcPower = Range(yProcessList.ElementAt(i));

                    // this is a slightly better predictor, than the above range, when the power of the Yproc matches the power of d_HR, that is usually close to the process gain. 
                    // if anything, these two signals tend to match when the process gain is slighlty too low. 

                    var dEstPower = Power(dEstList.ElementAt(i));
                    var yProcPower = Power(yProcessList.ElementAt(i));
                    var yMeasPower = Power(dataSet.Y_meas);
                    var uMeasPower = Power(dataSet.U.GetColumn(0));

                    var meanDest = vec.Mean(dEstList.ElementAt(i)).Value;

                    // dEstPower, will always be slightly higher than yProcPower, due to the HF element in it
                    // but looking at dHFlist and its power is pointless, at will always match yProcPower exactly. 

                    // both dEstPower and yProcPower should be _above_ yMeasPower for the "true" process gain, but this is not a sufficient condition to find a unique Kp, there are many Kp that would fit this criteria. 
                    // IT SEEMS THAT WHEN yProcPower == uMeasPower, then the process gain is correct.
                    // it is acutally easier to think of disturbance d_LF and d_HF as d_y and d_u, as d_HF is really just e= ymeas-ysetp and d_u is depends on u. 
                    // this means, that d_y will not change for different guesses of the process gain or process time-constant, only d_u does that. 
                    // note that d_LF or "d_u" is basically the inverse of the y_process.
                    string comment = 
                        "Kp=" + kpList.ElementAt(i).ToString("F2", CultureInfo.InvariantCulture) 
                        + " dEstPower="+ dEstPower.ToString("F3", CultureInfo.InvariantCulture) 
                        + " yProcPower="+yProcPower.ToString("F3", CultureInfo.InvariantCulture) 
                        + " yMeasPower="+ yMeasPower.ToString("F3", CultureInfo.InvariantCulture)
                         +" uMeasPower=" + uMeasPower.ToString("F3", CultureInfo.InvariantCulture)
                    +" uMeanDest=" +meanDest.ToString("F3", CultureInfo.InvariantCulture);

                    Shared.EnablePlots();
                    Plot.FromList(new List<double[]> { dEstList.ElementAt(i), vec.Subtract(yProcessList.ElementAt(i), vec.Mean(yProcessList.ElementAt(i)).Value), 
                        vec.Subtract(dataSet.Y_meas, vec.Mean(dataSet.Y_meas).Value),
                        vec.Subtract(dataSet.U.GetColumn(0),vec.Mean(dataSet.U.GetColumn(0)).Value),
                        dataSet.U.GetColumn(0) },
                        new List<string> { "y1=d_est", "y1=y_process", "y1=y_meas","y1=u_pidDetrend","y3=u_pid" }, dataSet.Times,comment);
                    Shared.DisablePlots();
                    Thread.Sleep(100);
                }
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
                        bestUnitModel.modelParameters.LinearGainUnc[pidInputIdx] = gainStepSize;
                    }
                }
                bestUnitModel.modelParameters.Fitting = new FittingInfo();
                bestUnitModel.modelParameters.Fitting.WasAbleToIdentify = true;
            }
            return new Tuple<UnitModel,double>(bestUnitModel, gainStepSize);
        }

        /// <summary>
        /// Guess the sign of the pid-input 
        /// <para>
        /// Look at the correlation between u and y.
        ///  assuming that the sign of the Kp in PID controller is set correctly so that the process is not unstable: 
        ///  If an increase in _y(by means of a disturbance)_ causes PID-controller to _increase_ u then the processGainSign is negative
        ///  If an increase in y causes PID to _decrease_ u, then processGainSign is positive!
        /// </para></summary>
        /// <param name="unitDataSet"></param>
        /// <param name="pidParams"></param>
        /// <param name="pidInputIdx"></param>
        /// <returns></returns>
        private static double GuessSignOfProcessGain(UnitDataSet unitDataSet, PidParameters pidParams, int pidInputIdx)
        {
            var vec = new Vec(unitDataSet.BadDataID);
            double[] e = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_setpoint);
            double pidInput_processGainSign = 1;
            {
                var indGreaterThanZeroE = vec.FindValues(e, 0, VectorFindValueType.BiggerOrEqual, unitDataSet.IndicesToIgnore);
                var indLessThanZeroE = vec.FindValues(e, 0, VectorFindValueType.SmallerOrEqual, unitDataSet.IndicesToIgnore);

                var u_pid = unitDataSet.U.GetColumn(pidInputIdx);
                var uAvgWhenEgreatherThanZero = vec.Mean(Vec<double>.GetValuesAtIndices(u_pid, indGreaterThanZeroE));
                var uAvgWhenElessThanZero = vec.Mean(Vec<double>.GetValuesAtIndices(u_pid, indLessThanZeroE));

                if (uAvgWhenEgreatherThanZero != null && uAvgWhenElessThanZero != 0)
                {
                    if (uAvgWhenElessThanZero >= uAvgWhenEgreatherThanZero)
                    {
                        pidInput_processGainSign = 1;
                    }
                    else
                    {
                        pidInput_processGainSign = -1;
                    }
                }
            }
            return pidInput_processGainSign;

        }


        /// <summary>
        /// Initial estimate of the process model gain made by observing the range of e (ymeas-ysetpoint)
        /// and u_pid
        /// </summary>
        /// <param name="unitDataSet">the dataset</param>
        /// <param name="pidParams">an estimate of the PID-parameters</param>
        /// <param name="pidInputIdx">the index of U in the above dataset that is driven by the PID-controller.</param>
        /// <returns></returns>
        private static UnitModel EstimateClosedLoopProcessGain(UnitDataSet unitDataSet, PidParameters pidParams, int pidInputIdx)
        {
            var unitModel = new UnitModel();
            var vec = new Vec(unitDataSet.BadDataID);

            if (unitDataSet.Y_setpoint == null || unitDataSet.Y_meas == null || unitDataSet.U == null)
            {
                return null;
            }

            bool doesSetpointChange = !(vec.Max(unitDataSet.Y_setpoint, unitDataSet.IndicesToIgnore)
                == vec.Min(unitDataSet.Y_setpoint, unitDataSet.IndicesToIgnore));
            double estPidInputProcessGain = 0;
            var pidInput_processGainSign = GuessSignOfProcessGain(unitDataSet, pidParams,pidInputIdx);

            // try to find a rough first estimate by heuristics
            {
                double[] pidInput_u0 = Vec<double>.Fill(unitDataSet.U[pidInputIdx, 0], unitDataSet.GetNumDataPoints());
                double yset0 = unitDataSet.Y_setpoint[0];

                // y0,u0 is at the first data point
                // disadvantage, is that you are not sure that the time series starts at steady state
                // but works better than candiate 2 when disturbance is a step
                double FilterTc_s = 0;

                LowPass lowPass = new LowPass(unitDataSet.GetTimeBase());
                double[] e = vec.Subtract(unitDataSet.Y_meas, unitDataSet.Y_setpoint);
                // knowing the sign of the process gain is quite important!
                // if a system has negative gain and is given a positive process disturbance, then y and u will both increase in a way that is 
                // correlated 
                double[] pidInput_deltaU = vec.Subtract(unitDataSet.U.GetColumn(pidInputIdx), pidInput_u0);//TODO : U including feed-forward?
                double[] eFiltered = lowPass.Filter(e, FilterTc_s, 2, unitDataSet.IndicesToIgnore);
                double maxDE = vec.Max(vec.Abs(eFiltered), unitDataSet.IndicesToIgnore);       // this has to be sensitive to noise?
                double[] uFiltered = lowPass.Filter(pidInput_deltaU, FilterTc_s, 2, unitDataSet.IndicesToIgnore);
                double maxU = vec.Max(vec.Abs(uFiltered), unitDataSet.IndicesToIgnore);        // sensitive to output noise/controller overshoot
                double minU = vec.Min(vec.Abs(uFiltered), unitDataSet.IndicesToIgnore);        // sensitive to output noise/controller overshoot  
                estPidInputProcessGain = pidInput_processGainSign * maxDE / (maxU - minU);

                //    double avgDE = vec.Mean(vec.Abs(eFiltered), unitDataSet.IndicesToIgnore).Value;
                //    double avgU = vec.Mean(vec.Abs(pidInput_deltaU), unitDataSet.IndicesToIgnore).Value;
                //    double estPidInputProcessGainUB = avgDE / avgU;
                //    Console.WriteLine("process gain upper og lower boundbound:"+estPidInputProcessGainUB+ " uncertainty:" + Math.Abs(estPidInputProcessGain- estPidInputProcessGainUB));

                int indexOfFirstGoodValue = 0;
                if (unitDataSet.IndicesToIgnore != null)
                {
                    if (unitDataSet.GetNumDataPoints() > 0)
                    {
                        while (unitDataSet.IndicesToIgnore.Contains(indexOfFirstGoodValue) && indexOfFirstGoodValue <
                            unitDataSet.GetNumDataPoints() - 1)
                        {
                            indexOfFirstGoodValue++;
                        }
                    }
                }

                int nGains = unitDataSet.U.GetNColumns();
                if (nGains == 1)
                {
                    var unitParamters = new UnitParameters();
                    unitParamters.LinearGains = new double[nGains];
                    unitParamters.LinearGains[pidInputIdx] = estPidInputProcessGain;
                    unitParamters.U0 = new double[nGains];
                    unitParamters.U0[pidInputIdx] = pidInput_u0[pidInputIdx];
                    unitParamters.UNorm = Vec<double>.Fill(1, nGains);
                    unitParamters.Bias = unitDataSet.Y_meas[indexOfFirstGoodValue];
                    unitModel = new UnitModel(unitParamters);
                }
                else
                {
                    // experimental 
                    unitModel = UnitIdentifier.IdentifyLinearAndStaticWhileKeepingLinearGainFixed(unitDataSet, pidInputIdx, estPidInputProcessGain,
                        pidInput_u0[indexOfFirstGoodValue], 1);
                }
            }
            // END STEP 1
            ////////////////////////////

            return unitModel;
        }


        private static Tuple<UnitModel, DisturbanceIdResult> EstimateSISOdisturbanceForProcGain( UnitModel referenceMISOmodel, 
            double pidLinProcessGain, int pidInputIdx, UnitDataSet dataSet, PidParameters pidParams)
        {
            var candidateModel = new UnitModel(referenceMISOmodel.modelParameters.CreateCopy(), "SISO");

            candidateModel.modelParameters.LinearGains = new double[] { pidLinProcessGain };
            candidateModel.ModelInputIDs = new string[] { referenceMISOmodel.GetModelInputIDs()[pidInputIdx] };

            candidateModel.modelParameters.U0 = 
                new double[] { referenceMISOmodel.modelParameters.U0[pidInputIdx] };
            candidateModel.modelParameters.UNorm = 
                new double[] { referenceMISOmodel.modelParameters.UNorm[pidInputIdx] };
            if (referenceMISOmodel.modelParameters.Curvatures != null)
                candidateModel.modelParameters.Curvatures =
                new double[] { referenceMISOmodel.modelParameters.Curvatures[pidInputIdx] };
            if (referenceMISOmodel.modelParameters.CurvatureUnc != null)
                candidateModel.modelParameters.CurvatureUnc =
                new double[] { referenceMISOmodel.modelParameters.CurvatureUnc[pidInputIdx] };
            if (referenceMISOmodel.modelParameters.LinearGainUnc != null)
            {
                candidateModel.modelParameters.LinearGainUnc =
                    new double[] { referenceMISOmodel.modelParameters.LinearGainUnc[pidInputIdx] };
            }
            candidateModel.modelParameters.Fitting = new FittingInfo();
            candidateModel.modelParameters.Fitting.WasAbleToIdentify = true;

            var dataSet_SISO = new UnitDataSet(dataSet);
            dataSet_SISO.U = Array2D<double>.CreateFromList( new List<double[]> { dataSet.U.GetColumn(pidInputIdx) } );

            // pidInputIdx=0 always for the new SISO system:
            var candidateModelDisturbance = DisturbanceIdentifier.CalculateDisturbanceVector
                (dataSet_SISO, candidateModel, 0, pidParams);

            var d_est = candidateModelDisturbance.d_est;
            var isOK = ClosedLoopSim
                (dataSet_SISO, candidateModel.GetModelParameters(), pidParams, d_est, "run_altSISO");

            if (isOK)
                return new Tuple<UnitModel, DisturbanceIdResult>(candidateModel, candidateModelDisturbance);
            else
                return new Tuple<UnitModel, DisturbanceIdResult>(null, candidateModelDisturbance);

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