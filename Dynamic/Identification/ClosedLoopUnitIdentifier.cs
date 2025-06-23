using Accord.Math;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
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
        /*
         *  Algorithm 
         *  
        
         *  - intial step (model-free guess of process gain)
         *  multiple passes: ass of the algorithm is
         *  - step1 ("global search" for linear pid-gains), starting with result of last step
         *  - step2(estimate time constant), starting with result of last step.
         *  
         *  for each subsequent pass, the search area of step 2 narrows around the the result of the previous pass
         *  
         */

        const int MAX_NUM_PASSES = 2;
        const double LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE = 60 + 1;
        static int[] step1GlobalSearchNumIterations =  new int[] { 10 ,20};// 50 total iterations usually enough, maybe even lower, something like 30 is probable
        // these are given for each pass.
        static double[] step1GainGlobalSearchUpperBoundPrc = new double[] { 150, 40 } ;
        static double[] step1GainGlobalSearchLowerBoundPrc = new double[] { 90, 40 };
      //  static double[] step2GainGlobalSearchUpperBoundPrc = new double[] { 150, 150 } ;
      //   static double[] step2GainGlobalSearchLowerBoundPrc = new double[] { 90, 90 };

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
        public static (UnitModel, double[]) Identify(UnitDataSet dataSet, 
            PidParameters pidParams = null, int pidInputIdx = 0)
        {
            var  sbSolverOutput = new StringBuilder();

            bool isMISO = dataSet.U.GetNColumns() > 1 ? true : false;
            if (dataSet.Y_setpoint == null || dataSet.Y_meas == null || dataSet.U == null)
            {
                return (null, null);
            }
            var vec = new Vec(dataSet.BadDataID);

            bool didStep1Succeed = false;
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
                // TODO: if detectFrozenData is set to true, then model seems to fail for some unit tests
                dataSet.IndicesToIgnore = CommonDataPreprocessor.ChooseIndicesToIgnore(
                    dataSet,detectBadData:true,detectFrozenData:false); 
            }
            // set "indicestoignore" to exclude values outside ymin/ymax_fit and umin_fit,u_max_fit
            // 
            //dataSet.DetermineIndicesToIgnore(fittingSpecs);

            // this variable holds the "newest" unit model run and is updated
            // over multiple runs, and as it improves, the 
            // estimate of the disturbance improves along with it.
            var idDisturbancesList = new List<DisturbanceIdResult>();
            var idUnitModelsList = new List<UnitModel>();
            void SaveSearchResult(UnitModel unitModel)
            {
                if (unitModel == null)
                {
                    idDisturbancesList.Add(null);
                    idUnitModelsList.Add(null);
                }
                else
                {
                    var dataSetCopy = new UnitDataSet(dataSet);
                    var distIdResult = DisturbanceCalculator.CalculateDisturbanceVector
                                       (dataSetCopy, unitModel, pidInputIdx, pidParams);
                    if (distIdResult.ErrorType != DisturbanceEstimationError.None)
                        sbSolverOutput.AppendLine("Error:" + distIdResult.ErrorType);
                    idDisturbancesList.Add(distIdResult);
                    idUnitModelsList.Add(unitModel);
                }
            }
            (UnitModel bestModel, double[] bestDisturbance) ChooseBestEstimates()
            {
                UnitModel bestModel = null;
                double[] bestDisturbance = null;
                int idx = idDisturbancesList.Count()-1;
                bool valueFound = false;
                while (idx >= 0 && !valueFound)
                {
                    if (idDisturbancesList.ElementAt(idx) != null && idUnitModelsList.ElementAt(idx) != null)
                    {
                        bestModel = idUnitModelsList.ElementAt(idx);
                        bestDisturbance = idDisturbancesList.ElementAt(idx).d_est;
                        valueFound = true;
                    }
                    idx--;
                }
                return (bestModel, bestDisturbance);
            }

            double[] u0 = SignificantDigits.Format(dataSet.U.GetRow(0), 5);
            double y0 = dataSet.Y_meas[0];
            var fittingSpecs = new FittingSpecs();
            fittingSpecs.u0 = u0;
            var GSdescription = "";
            // ----------------
            // init: no process model assumed, let disturbance estimator guesstimate a pid-process gain, 
            // to give a first estimate of the disturbance (used to initalize steps 2 and 3 below)
            var unitModel_init = InitModelFreeEstimateClosedLoopProcessGain(dataSet, pidParams, pidInputIdx);
            ConsoleDebugOut(sbSolverOutput,unitModel_init, "Init");
            SaveSearchResult(unitModel_init);
            // step1, MISO: ident (add inital estimates of any other inputs to the above model-free estiamte) 
           /* if (isMISO)
            {
                dataSetRun1.D = idDisturbancesList.Last().d_est;
                var unitModel_step1Miso = UnitIdentifier.IdentifyLinearAndStatic(ref dataSetRun1, fittingSpecs, false);// no time-delay estimation.
                unitModel_step1Miso.modelParameters.LinearGainUnc = null;
                if (doConsoleDebugOut)
                    ConsoleDebugOut(unitModel_step1Miso, "Step 1,MISO");
                SaveSearchResult(unitModel_step1Miso);
            }*/

            // ----------------
            // steps 1 and 2 (run sequentially for multiple passes.)
            for (int passNumber = 1;passNumber<= MAX_NUM_PASSES; passNumber++)//NB! passNumber starts at 1.
            {
                // /////////////////////
                //   "step1" : "global search" for linear pid-gains
                {
                    if (idUnitModelsList.Last() == null)
                        continue;
                    if (idUnitModelsList.Last().modelParameters.GetProcessGains() == null)
                        continue;
                    double pidProcGainPrevEstimate = idUnitModelsList.Last().modelParameters.GetProcessGains()[pidInputIdx];
                    double max_gain, min_gain;
                    if (pidProcGainPrevEstimate > 0)
                    {
                        max_gain = pidProcGainPrevEstimate * (1 + step1GainGlobalSearchUpperBoundPrc.ElementAt(passNumber - 1) / 100);
                        min_gain = pidProcGainPrevEstimate * (1 - step1GainGlobalSearchLowerBoundPrc.ElementAt(passNumber - 1) / 100);
                    }
                    else
                    {
                        min_gain = pidProcGainPrevEstimate * (1 + step1GainGlobalSearchUpperBoundPrc.ElementAt(passNumber - 1) / 100);
                        max_gain = pidProcGainPrevEstimate * (1 - step1GainGlobalSearchLowerBoundPrc.ElementAt(passNumber - 1) / 100);
                    }

                    sbSolverOutput.AppendLine("Pass " + passNumber + " Step 1 " + "pid-process gain bounds: " + min_gain.ToString("F3", CultureInfo.InvariantCulture)
                            + " to " + max_gain.ToString("F3", CultureInfo.InvariantCulture));


                    var step1model = Step1GlobalSearchLinearPidGain(dataSet, pidParams, pidInputIdx,
                        idUnitModelsList.Last(), min_gain, max_gain, fittingSpecs, step1GlobalSearchNumIterations.ElementAt(passNumber - 1));
                    // note that step 1 may return 
                    if (step1model != null)
                    {
                        GSdescription = step1model.GetModelParameters().Fitting.SolverID;
                        didStep1Succeed = true;
                    }
                    else
                    {
                        GSdescription = "Pass" + passNumber + ",Step 1 global-search had a flat objective space and returned null - model uncertain.";
                    }
                    if (step1model != null)
                        ConsoleDebugOut(sbSolverOutput,step1model, "Pass " + passNumber + " Step 1", " GS output: " + GSdescription);
                    else
                        sbSolverOutput.AppendLine("pass" + passNumber + "Step1,GS1: FAILED");
                    SaveSearchResult(step1model);
                }
                //
                // "Step 2" : global search for time constant.
                // 

                if (idUnitModelsList.Count() > 0)
                {
                    if (idUnitModelsList.Last() != null)
                    {
                        var unitModel = idUnitModelsList.Last();
                       // version 1
                        var step2Model = Step2GlobalSearchTimeConstant(dataSet, pidParams, pidInputIdx, unitModel, idDisturbancesList.Last().d_est,passNumber);
                        // version 2:  find the time contant that gives the lowest FitScore in closed loop simulations.
                      //  step2Model = Step2GlobalSearchTimeConstantFitScoreVersion(dataSet, pidParams, pidInputIdx, unitModel, idDisturbancesList.Last().d_est);
                        SaveSearchResult(step2Model);
                        ConsoleDebugOut(sbSolverOutput,step2Model, "Pass " + passNumber + " Step 2");
                    }
                }
            }// end PASS
            ///////////////////////////

            (var identUnitModel, var retDisturbance) = ChooseBestEstimates();
            var retModel = CleanUpModel(identUnitModel,dataSet, pidParams, pidInputIdx, didStep1Succeed,  GSdescription, sbSolverOutput);
            return  (identUnitModel, retDisturbance);
        }


        private static UnitModel CleanUpModel(UnitModel identUnitModel,UnitDataSet dataSet, PidParameters pidParams,int pidInputIdx, bool didStep1Succeed, string GSdescription, 
            StringBuilder solverOutput)
        {
            const int nDigits = 5; //number of significant digits in results.

            var vec = new Vec(dataSet.BadDataID);

            if (identUnitModel != null)
            {

                if (identUnitModel.modelParameters.Fitting == null)
                {
                    identUnitModel.modelParameters.Fitting = new FittingInfo();
                }
                identUnitModel.modelParameters.Fitting.SolverOutput = solverOutput.ToString();

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

                if (didStep1Succeed)
                {
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop/w gain 2-step global search " + GSdescription;
                }
                else
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop init heuristic (global search no minimum found)";
                identUnitModel.modelParameters.Fitting.NFittingTotalDataPoints = dataSet.GetNumDataPoints();
                identUnitModel.modelParameters.Fitting.NFittingBadDataPoints = dataSet.IndicesToIgnore.Count();

                // closed-loop simulation, adds U_sim and Y_sim to dataset
                ClosedLoopSim(dataSet, identUnitModel.modelParameters, pidParams, pidInputIdx);

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
                return identUnitModel;
            }
            else
            {
                var retModel = new UnitModel();
                retModel.modelParameters = new UnitParameters();
                retModel.modelParameters.Fitting = new FittingInfo();
                retModel.modelParameters.Fitting.WasAbleToIdentify = false;
                retModel.modelParameters.Fitting.SolverID = "ClosedLoopUnitIdentifier (unable to identify)";

                return retModel;
            }
        }



        private static UnitModel Step2GlobalSearchTimeConstantFitScoreVersion(UnitDataSet dataSet, PidParameters pidParams, int pidInputIdx, UnitModel unitModel, double[] d_est)
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

            (var plantSim, var inputData) = PlantSimulatorHelper.CreateFeedbackLoopWithEstimatedDisturbance(dataSet, pidModel, unitModel, pidInputIdx);
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
            return null;//todo
        }

        /// <summary>
        /// "look for the time-constant that gives the disturbance that changes the least"
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="pidParams"></param>
        /// <param name="pidInputIdx"></param>
        /// <param name="unitModel_prev"></param>
        /// <param name="d_est"></param>
        /// <param name="passNumber"></param>
        /// <returns></returns>
        private static UnitModel Step2GlobalSearchTimeConstant(UnitDataSet dataSet, PidParameters pidParams, int pidInputIdx,
            UnitModel unitModel_prev, double[] d_est, int passNumber)
        {
            var distDevs = new List<double>();
            var candidateTc = new List<double>();

            var vec = new Vec(dataSet.BadDataID);

            double CalcDev(double[] disturbance)
            {
                return vec.Mean(vec.Abs(vec.Diff(disturbance))).Value;
            }

            var timeBase = dataSet.GetTimeBase();
            double candiateTc_s = 0;
            candidateTc.Add(candiateTc_s);
            bool curDevIsDecreasing = true;
            double firstDev = CalcDev(d_est);
            distDevs.Add(firstDev);

            // for the first pass, start with Tc= 0 and increase as long as this causes CalcDev(d_est) to keep decreasing
            if (passNumber == 1)
            {
                while (candiateTc_s < LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE * timeBase && curDevIsDecreasing)
                {
                    candiateTc_s += timeBase;
                    var newParams = unitModel_prev.GetModelParameters().CreateCopy();
                    newParams.TimeConstant_s = candiateTc_s;
                    var newModel = new UnitModel(newParams);
                    var distIdResult_Test = DisturbanceCalculator.CalculateDisturbanceVector
                        (dataSet, newModel, pidInputIdx, pidParams);
                    var curDev = CalcDev(distIdResult_Test.d_est);
                    if (curDev < distDevs.Last<double>())
                        curDevIsDecreasing = true;
                    else
                        curDevIsDecreasing = false;
                    distDevs.Add(curDev);
                    candidateTc.Add(candiateTc_s);
                }
                if (candiateTc_s == LARGEST_TIME_CONSTANT_TO_CONSIDER_TIMEBASE_MULTIPLE)
                {
                    // you may get here when the disturbance is continous and noisy
                    Console.WriteLine("warning: ClosedLoopIdentifierFailedToFindAUniqueProcessTc");
                }
                if (candiateTc_s > 0)
                {
                    candiateTc_s -= timeBase;
                }
            }
            else
            {
                // for any subsequent passes, the search should at least consider [0, Tcest_s+timebase_S]
                // and only after searching through all of these should the Tc with the smallest CalcDev(d_est)
                // be selected.

                var TcLast = unitModel_prev.GetModelParameters().TimeConstant_s;
                var minDistDevVal_TcLast = Double.PositiveInfinity;
                for (double Tc = timeBase; Tc < TcLast + timeBase * 3; Tc++)
                {
                    var newParams = unitModel_prev.GetModelParameters().CreateCopy();
                    newParams.TimeConstant_s = Tc;
                    var newModel = new UnitModel(newParams);
                    var distIdResult_Test = DisturbanceCalculator.CalculateDisturbanceVector
                        (dataSet, newModel, pidInputIdx, pidParams);
                    var curDev = CalcDev(distIdResult_Test.d_est);
                    distDevs.Add(curDev);
                    candidateTc.Add(Tc);
                    if (Tc == TcLast)
                    {
                        minDistDevVal_TcLast = curDev;
                    }
                }
                // have observed that Tc=0 and Tc=Tclast have the same value for curDev, in that case choose TcLast
                var minDistDevVal = vec.Min(distDevs.ToArray(), out int minInd);
                if (minDistDevVal == minDistDevVal_TcLast)
                    candiateTc_s = TcLast;
                else
                    candiateTc_s = candidateTc.ElementAt(minInd);
            }

            // TODO: it would be possible to divide the time-constant into a time-delay and a time constant 
            var step2params = unitModel_prev.GetModelParameters().CreateCopy();
            step2params.TimeConstant_s = candiateTc_s;
            var step2Model = new UnitModel(step2params);

            return step2Model;
        }


        /*
        private static double EstimateDisturbanceLF(UnitDataSet dataSet, UnitModel unitModel, int pidInputIdx, PidParameters pidParams)
        {
            var vec = new Vec(dataSet.BadDataID);

            var umInternal = (UnitModel)unitModel.Clone();
            var d_HF = vec.Subtract(dataSet.Y_meas, dataSet.Y_setpoint);
            var unitParams = umInternal.GetModelParameters();

            var dList = new List<double[]>();
            var dNameList = new List<string>();
            var v2List = new List<double[]>();
            var v2NameList = new List<string>();

            dList.Add(d_HF);
            dNameList.Add("y1=dHF");
            v2List.Add(dataSet.Y_meas);
            v2NameList.Add("y3=y_meas");

            var KpList = new List<double> { 0.5, 1, 2, 4 };

            //    var dHF_energy = vec.Mean(d_HF).Value;
            foreach (var Kp in KpList)
            {
                unitParams.LinearGains = new double[] { Kp };
                umInternal.SetModelParameters(unitParams);
                (var isOk, var y_proc) = PlantSimulatorHelper.SimulateSingle(dataSet, umInternal);
                var d_LF = vec.Multiply(vec.Subtract(y_proc, y_proc[0]), -1);
                var d_est1 = vec.Add(d_HF, d_LF);
                var d_est2 = vec.Subtract(dataSet.Y_meas, y_proc);
                dList.Add(d_est2);
                dNameList.Add("y1=destKp" + Kp.ToString("F2", CultureInfo.InvariantCulture));
                // var dLF_energy = vec.Mean(d_LF).Value;

                //Debug.WriteLine("Kp="+Kp.ToString("F2", CultureInfo.InvariantCulture) + "dLFenergy:"+ dLF_energy.ToString("F2", CultureInfo.InvariantCulture) 
                //   + "dHFenergy" +dHF_energy.ToString("F2", CultureInfo.InvariantCulture)) ;

                v2List.Add(y_proc);
                v2NameList.Add("y3=yProcKp" + Kp.ToString("F2", CultureInfo.InvariantCulture));

                // if the energy of t

            }
            dList.AddRange(v2List);
            dNameList.AddRange(v2NameList);
            Shared.EnablePlots();
            Plot.FromList(dList, dNameList, dataSet.GetTimeBase(), "exp_Kp");
            Shared.DisablePlots();
            return 0;
        }*/

        private static void ConsoleDebugOut(StringBuilder sb, UnitModel unitModel, string description,string addOnTxt ="")
        {
            // note that unitModel can be NULL!
            if (unitModel != null)
            {
                if (unitModel.GetModelParameters().LinearGains.Count() == 1)
                    sb.AppendLine(description + " Gain:" + unitModel.GetModelParameters().LinearGains.First().ToString("F3", CultureInfo.InvariantCulture) +
                      " Tc:" + unitModel.GetModelParameters().TimeConstant_s.ToString("F1", CultureInfo.InvariantCulture) + addOnTxt);
                else
                {
                    string outstr = description + " Gain:[";
                    int index = 0;
                    foreach (var gain in unitModel.GetModelParameters().LinearGains)
                    {
                        outstr += gain.ToString("F2", CultureInfo.InvariantCulture);
                        if (index < unitModel.GetModelParameters().LinearGains.Count()-1)
                            outstr += ";";
                        index++;
                    }
                    outstr += "]";
                    outstr += " Tc:" + unitModel.GetModelParameters().TimeConstant_s.ToString("F1", CultureInfo.InvariantCulture) + addOnTxt;
                    sb.AppendLine(outstr);
                }
            }
            else
                sb.AppendLine(description + " returned NULL " + addOnTxt);
        }


        /// <summary>
        /// Try to model the effects of other non-pid inputs in a MISO-system, given an inital estimate of d_est and unitModel, 
        /// when the gain of the pid-output is forced equat to pidProcGain
        /// </summary>
        /// <param name="pidProcGain">the estimated gain of the the pid-controlled input (which has input pidInputIdx)</param>
        /// <param name="d_est">the estimated disturbance </param>
        /// <param name="unitModel">a prior estimate of the model for all inputs</param>
        /// <param name="unitDataSet"></param>
        /// <param name="pidInputIdx">what input in the unitDataSet is controlled by the pid-controller?</param>
        /// <param name="fittingSpecs"></param>
        /// <param name="pidParams">paramters of the pid-controller</param>
        /// <returns></returns>
        private static (double[], UnitParameters,double[]) GlobalSearchMisoModelEstimatedDisturbance(double pidProcGain, double[] d_est, 
            UnitModel unitModel, UnitDataSet unitDataSet, int pidInputIdx, FittingSpecs fittingSpecs, PidParameters pidParams)
        {
            var vec = new Vec(unitDataSet.BadDataID);
            int nGains = unitModel.modelParameters.GetProcessGains().Length;
            bool doesSetpointChange = !(vec.Max(unitDataSet.Y_setpoint, unitDataSet.IndicesToIgnore)
                == vec.Min(unitDataSet.Y_setpoint, unitDataSet.IndicesToIgnore));

            bool doIncludeYsetpointAsInput = true;
            if (!doesSetpointChange)
                doIncludeYsetpointAsInput = false;
            var alternativeMISOModel = (UnitModel)unitModel.Clone("MISO");

            // part 1: analysis of SISO estimate above, used to find inital estimtes
            // the d_est from above will give a d_est that also include terms related to any changes in y_set
            // but also in the non-pid input ("external" inputs u)
            {
                var dataSet_d = new UnitDataSet("dist");
                dataSet_d.Y_meas = d_est;
                var inputList = new List<double[]>();
                if (doIncludeYsetpointAsInput)
                    inputList.Add(unitDataSet.Y_setpoint);
                for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                {
                    if (inputIdx == pidInputIdx)
                        continue;
                    inputList.Add(unitDataSet.U.GetColumn(inputIdx));
                }
                dataSet_d.U = Array2D<double>.CreateFromList(inputList);
                // this problem is perhaps have been better to solve on "diff" form?
                // otherwise it tends to find parameters to minimize the total squared error to account for 
                // non-zero disturbances

                // it seems that if there is setpoint changes the regular regression works 
                // better, but if the data is mainly excited by an unknown disturbance, 
                // then the "diff" version of the regression works better. 
                UnitModel model_dist;
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
                        if (model_dist_diff.GetModelParameters().Fitting.ObjFunValDiff >
                            model_dist_abs.GetModelParameters().Fitting.ObjFunValDiff)

                            model_dist = model_dist_diff;
                        else
                            model_dist = model_dist_abs;
                    }
                }
                // debug plot:
               /* if (false)
                {
                    var variableList = new List<double[]> { d_est, dataSet_d.Y_sim };
                    var variableNameList = new List<string> { "y1=d_est_SISO", "y1_d_est_modelled" };
                    for (int inputIdx = 0; inputIdx < inputList.Count; inputIdx++)
                    {
                        variableList.Add(dataSet_d.U.GetColumn(inputIdx));
                        variableNameList.Add("y3=dataSet_u[" + inputIdx + "]");
                    }
                    Shared.EnablePlots();
                    Plot.FromList(
                        variableList, variableNameList, unitDataSet.GetTimeBase(), "ClosedLoopGlobal_model_dist_gain_"+ pidProcGain.ToString("F2",CultureInfo.InvariantCulture));
                    Shared.DisablePlots();
                }*/
                // it seems that the gains for external inputs u in model_dist are accurate if 
                // and when pidLinProcessGain matches the "true" estimate.
                //    d_est_adjusted = vec.Subtract(dEst, dataSet_d.Y_sim);
                int curModelDistInputIdx = 0;
                if (doIncludeYsetpointAsInput)
                    curModelDistInputIdx = 1;
                for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                {
                    if (inputIdx == pidInputIdx)
                    {
                        alternativeMISOModel.modelParameters.LinearGains[inputIdx] = pidProcGain;
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
            var dataSet_altMISO = new UnitDataSet(unitDataSet);
            var distIdResultAlt_MISO = DisturbanceCalculator.CalculateDisturbanceVector
                 (dataSet_altMISO, alternativeMISOModel, pidInputIdx, pidParams);


            /*if (false)
            {
                // d_est_SISO: first pass, does not account for changes in u_externals and y_set
                // d_est_adj: second pass, is d_est_SISO where influence has been modelled and subtracted (this will not be perfect but should move less in repsonse to u_Ext and yset)
                // d_est_MISO: third pass, hopefully improves on d_est_adj-  this estimate ideally does not move at all to changes in u_external and y_set
                var variableList = new List<double[]> { dEst, distIdResultAlt_MISO.d_est, dataSet.Y_setpoint };
                var variableNameList = new List<string> { "y1=d_est_SISO", "y1=d_est_MISO", "y4=y_set" };
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
            }*/
            var isOK = ClosedLoopSim(dataSet_altMISO, alternativeMISOModel.GetModelParameters(), pidParams, pidInputIdx, "run_altMISO");

            if (isOK)
            {
                var dEst = distIdResultAlt_MISO.d_est;
                var u_pid_adjusted = distIdResultAlt_MISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);
                var candidateModelParameters = alternativeMISOModel.modelParameters.CreateCopy();
                candidateModelParameters.Fitting = new FittingInfo();
                candidateModelParameters.Fitting.WasAbleToIdentify = true;
                return (u_pid_adjusted, candidateModelParameters, dEst);
            }
            else
            {
                return (null, null,null);
            }



    


        }



        /// <summary>
        /// tries  <c>N=numberOfGlobalSearchIterations</c>  process gains of the <c>unitModel</c> in the range[minPidProcessGain,maxPidProcessGain], using a closed-loop simulation 
        /// that includes the PID-model with paramters pidParams that acts on input <c>pidInputIdx</c> of the unit model.
        /// 
        /// The method also accepts an inital guess of the unit model, <c>unitModel_run1</c>
        /// 
        /// if Y_setpoint changes:
        /// looking to find the process gain that "decouples" d_est from Y_setpoint as much as possible.
        /// or antoher way to look at ti is that the output U with setpoint effects removed should be as decoupled 
        /// form Y_setpoint.
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="pidParams"></param>
        /// <param name="pidInputIdx"></param>
        /// <param name="unitModel_prev">The best previous estimate of the unitModel</param>
        /// <param name="minPidProcessGain"></param>
        /// <param name="maxPidProcessGain"></param>
        /// <param name="fittingSpecs"></param>
        /// <param name="numberOfGlobalSearchIterations"></param>
        /// <returns>the best model and the step size used as a tuple</returns>
        private static UnitModel Step1GlobalSearchLinearPidGain(UnitDataSet dataSet, PidParameters pidParams, int pidInputIdx,
            UnitModel unitModel_prev, double minPidProcessGain, double maxPidProcessGain, FittingSpecs fittingSpecs, int numberOfGlobalSearchIterations = 40)
        {
            var range = maxPidProcessGain - minPidProcessGain;
            var searchResults = new ClosedLoopGainGlobalSearchResults();
            var gainStepSize = range / numberOfGlobalSearchIterations;
            int nGains = unitModel_prev.modelParameters.GetProcessGains().Length;
            var vec = new Vec(dataSet.BadDataID);
            bool doesSetpointChange = !(vec.Max(dataSet.Y_setpoint, dataSet.IndicesToIgnore) == vec.Min(dataSet.Y_setpoint, dataSet.IndicesToIgnore));

            // //////////////////////////////////////////////////
            // try all the process gains between the min and the max and rank them
            // 
            var dEstPlotNames = new List<string>();
            var uSimPlotNames = new List<string>();
            var yProcPlotNames = new List<string>();
            var dEstList = new List<double[]>();
            var uPidAdjList = new List<double[]>();
            var candPidGainList = new List<double>();
            var yProcessList = new List<double[]>(); // the internal process output
            bool doDebugPlot = false;

            // //////////////////////////////////////////////////
            // try all the process gains between the min and the max and rank them 
            // 
            for (var curCandPidProcGain = minPidProcessGain; curCandPidProcGain <= maxPidProcessGain; curCandPidProcGain += range / numberOfGlobalSearchIterations)
            {
                double[] u_pid_adjusted = null;
                UnitParameters candParameters;
                double[] dEst;
                // SISO
                if (nGains == 1)
                {
                    (var curCandSISOModel, var curCandDistEst_SISO) =
                       Step1GlobalSearchEstimateSISOdisturbanceForProcGain(curCandPidProcGain, unitModel_prev, pidInputIdx, dataSet, pidParams);
                    dEst = curCandDistEst_SISO.d_est;
                    if (curCandSISOModel == null)
                    {
                        Console.WriteLine("warning: EstimateSISOdisturbanceForProcGain returned null ");
                        continue;
                    }
                    u_pid_adjusted = curCandDistEst_SISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);
                    candParameters = curCandSISOModel.modelParameters.CreateCopy();
                }
                else // (nGains > 1)
                {
                    // Single-input-single output disturbance will include transients in response to changes in yset and u_external
                    // Multiple-input single-output modeling: try to model the above estimate of disturbance against the external inputs and setpoint change
                    /*(u_pid_adjusted, candParameters, dEst) = GlobalSearchMisoModelEstimatedDisturbance(curCandPidProcGain, dEst, unitModel_prev,
                         dataSet, pidInputIdx, fittingSpecs, pidParams);
                     if (u_pid_adjusted == null)
                         continue;*/

                    // v2: TODO: not sure about this one, the below method does not yet work properly if the disturbance is non-zero
                    // try to rewrite inspired by the deprecated GlobalSearchMisoModelEstimatedDisturbance

                    (var curCandMISOModel, var curCandDistEst_MISO) =
                        Step1GlobalSearchEstimateMISOdisturbanceForProcGain(curCandPidProcGain, unitModel_prev, pidInputIdx, dataSet, pidParams);
                    dEst = curCandDistEst_MISO.d_est;
                    u_pid_adjusted = curCandDistEst_MISO.adjustedUnitDataSet.U.GetColumn(pidInputIdx);
                    candParameters = curCandMISOModel.modelParameters.CreateCopy();

                }
                searchResults.Add(candParameters, dataSet, dEst, u_pid_adjusted, pidInputIdx);

                // save the time-series for debug-plotting
                if (doDebugPlot)
                {
                    yProcPlotNames.Add("y1=y_p(Kp" + curCandPidProcGain.ToString("F2", CultureInfo.InvariantCulture));
                    uSimPlotNames.Add("y3=u_sim(Kp" + curCandPidProcGain.ToString("F2", CultureInfo.InvariantCulture));
                    dEstPlotNames.Add("y2=d(Kp=" + curCandPidProcGain.ToString("F2", CultureInfo.InvariantCulture) + ")");
                    candPidGainList.Add(curCandPidProcGain);
                    dEstList.Add(dEst);
                    uPidAdjList.Add(u_pid_adjusted);
                    var yProc = vec.Subtract(dataSet.Y_meas, dEst);
                    yProcessList.Add(yProc);
                    Thread.Sleep(100);
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
                    return vec.Mean(vec.Abs(vec.Diff(inSignal))).Value;

               /*     var mean = vec.Mean(inSignal).Value;
                    var max = vec.Max(inSignal);
                    var min = vec.Min(inSignal);
                    double scale = Math.Max(Math.Abs(max - mean), Math.Abs(min - mean));
                    return vec.Mean(vec.Abs(vec.Subtract(inSignal, vec.Mean(inSignal).Value))).Value / scale;*/
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
                        "candPidProcGain=" + candPidGainList.ElementAt(i).ToString("F2", CultureInfo.InvariantCulture)
                        + " dEstPower=" + dEstPower.ToString("F5", CultureInfo.InvariantCulture)
                        + " yProcPower=" + yProcPower.ToString("F5", CultureInfo.InvariantCulture)
                        + " yMeasPower=" + yMeasPower.ToString("F3", CultureInfo.InvariantCulture)
                         + " uMeasPower=" + uMeasPower.ToString("F3", CultureInfo.InvariantCulture)
                    + " uMeanDest=" + meanDest.ToString("F3", CultureInfo.InvariantCulture);

                    Shared.EnablePlots();
                    Plot.FromList(new List<double[]> { dEstList.ElementAt(i), vec.Subtract(yProcessList.ElementAt(i), vec.Mean(yProcessList.ElementAt(i)).Value),
                        vec.Subtract(dataSet.Y_meas, vec.Mean(dataSet.Y_meas).Value),
                        dataSet.Y_meas,
                        vec.Subtract(dataSet.U.GetColumn(0),vec.Mean(dataSet.U.GetColumn(0)).Value),
                        dataSet.U.GetColumn(0)},
                        new List<string> { "y1=d_est", "y1=y_processDetrend", "y1=y_meas", "y1=u_pidDetrend", "y3=u_pid" }, dataSet.Times, comment);
                    Shared.DisablePlots();
                    Thread.Sleep(200);
                }
            }


            // finally, select the best model from all the tested models by looking across all the kpis stored
            (var bestUnitModel, var gsMiniumType) = searchResults.GetBestModel();
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
                if (bestUnitModel.modelParameters.Fitting == null)
                    bestUnitModel.modelParameters.Fitting = new FittingInfo();
                bestUnitModel.modelParameters.Fitting.WasAbleToIdentify = true;
                bestUnitModel.modelParameters.Fitting.SolverID = gsMiniumType;
            }
            return bestUnitModel;
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
        /// <returns>returns 1 if gain is positive or -1 if gain is negative.</returns>
        private static double GuessSignOfProcessGain(UnitDataSet unitDataSet, PidParameters pidParams, int pidInputIdx)
        {
            if (pidParams.Kp != 0 && !Double.IsNaN(pidParams.Kp) && pidParams.Kp != -9999)
            {
                if (pidParams.Kp > 0)
                    return 1;
                else
                    return -1;
            }

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
        /// This method ignores all data in unitDataSet.IndicesToIgnore in the analysis.
        /// </summary>
        /// <param name="unitDataSet">the dataset</param>
        /// <param name="pidParams">an estimate of the PID-parameters</param>
        /// <param name="pidInputIdx">the index of U in the above dataset that is driven by the PID-controller.</param>
        /// <returns></returns>
        private static UnitModel InitModelFreeEstimateClosedLoopProcessGain(UnitDataSet unitDataSet, PidParameters pidParams, int pidInputIdx)
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

            // TODO  note that this fails in the case of external input signals u, may need to subtract the influence of external inputs in this case. 
            var pidInput_processGainSign = GuessSignOfProcessGain(unitDataSet, pidParams,pidInputIdx);

            // try to find a rough first estimate by heuristics
            {
                double[] pidInput_u0 = Vec<double>.Fill(unitDataSet.U[pidInputIdx, 0], unitDataSet.GetNumDataPoints());
                double yset0 = unitDataSet.Y_setpoint[0];

                // y0,u0 is at the first data point
                // disadvantage, is that you are not sure that the time series starts at steady state
                // but works better than candiate 2 when disturbance is a step
                double FilterTc_s = 0;

                var lowPass = new LowPass(unitDataSet.GetTimeBase());
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

        // new MISO verison 
        private static Tuple<UnitModel, DisturbanceIdResult> Step1GlobalSearchEstimateMISOdisturbanceForProcGain(double pidProcGain, UnitModel prevModel,
            int pidInputIdx, UnitDataSet unitDataSet, PidParameters pidParams)
        {
            // start with a "siso" estimate
            /*       (var sisoModel, var disturbanceIDresult) = Step1GlobalSearchEstimateSISOdisturbanceForProcGain(pidProcGain, prevModel,
                     pidInputIdx, unitDataSet, pidParams);

                   var vec = new Vec(unitDataSet.BadDataID);

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

                   int nGains = sisoModel.modelParameters.GetProcessGains().Length;
                   bool doesSetpointChange = !(vec.Max(unitDataSet.Y_setpoint, unitDataSet.IndicesToIgnore)
                       == vec.Min(unitDataSet.Y_setpoint, unitDataSet.IndicesToIgnore));

                   bool doIncludeYsetpointAsInput = true;
                   if (!doesSetpointChange)
                       doIncludeYsetpointAsInput = false;

                   var dataSet_d = new UnitDataSet("dist");
                   dataSet_d.Y_meas = disturbanceIDresult.d_est;
                   var inputList = new List<double[]>();
                   if (doIncludeYsetpointAsInput)
                       inputList.Add(unitDataSet.Y_setpoint);
                   for (int inputIdx = 0; inputIdx < nGains; inputIdx++)
                   {
                       if (inputIdx == pidInputIdx)
                           continue;
                       inputList.Add(unitDataSet.U.GetColumn(inputIdx));
                   }
                   dataSet_d.U = Array2D<double>.CreateFromList(inputList);
            */
            //     double[] pidInput_u0 = Vec<double>.Fill(unitDataSet.U[pidInputIdx, 0], unitDataSet.GetNumDataPoints());
            //   prevModel.GetModelParameters().U0(pidInputIdx)
            //   var newUnitModel = UnitIdentifier.IdentifyLinearAndStaticWhileKeepingLinearGainFixed(dataSet_d, pidInputIdx, pidProcGain,
            //       prevModel.GetModelParameters().U0.ElementAt(pidInputIdx), 1);


            // this below model is fairly accurate when there is no disturbance, but even in this case there are "blips" in the resulting 
            // d_est that result from 

            var newUnitModel = UnitIdentifier.IdentifyLinearAndStaticWhileKeepingLinearGainFixed(unitDataSet, pidInputIdx, pidProcGain,
                 prevModel.GetModelParameters().U0.ElementAt(pidInputIdx), 1);


            // TEMPRORARY:FORCE MODEL TO BE ACCURATE 
            /*var newUnitModel = (UnitModel)prevModel.Clone("test");
            var parameters = newUnitModel.GetModelParameters();
            parameters.LinearGains = new double[] { 0.5, 0.25, -1.00 };
            parameters.LinearGains[pidInputIdx] = pidProcGain;
            parameters.TimeConstant_s = 0;
            parameters.Bias = 10;
            parameters.U0 = new double[] { 0, 0, 0 };
            newUnitModel.SetModelParameters(parameters);
            */
            var disturbanceIdresult = DisturbanceCalculator.CalculateDisturbanceVector(unitDataSet, newUnitModel, pidInputIdx, pidParams);
            return new Tuple<UnitModel, DisturbanceIdResult> (newUnitModel, disturbanceIdresult);
        }

        private static Tuple<UnitModel, DisturbanceIdResult> Step1GlobalSearchEstimateSISOdisturbanceForProcGain(double pidProcGain, UnitModel referenceMISOmodel, 
             int pidInputIdx, UnitDataSet dataSet, PidParameters pidParams)
        {
            var candidateModel = new UnitModel(referenceMISOmodel.modelParameters.CreateCopy(), "SISO");

            candidateModel.modelParameters.LinearGains = new double[] { pidProcGain };
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
         
            int pidInputIdx_NewSystem = 0;   // pidInputIdx=0 always for the new SISO system:
            var candidateModelDisturbance = DisturbanceCalculator.CalculateDisturbanceVector
                (dataSet_SISO, candidateModel, pidInputIdx_NewSystem, pidParams);

            var d_est = candidateModelDisturbance.d_est;
            var isOK = ClosedLoopSim
                (dataSet_SISO, candidateModel.GetModelParameters(), pidParams, pidInputIdx_NewSystem, "run_altSISO");

            if (isOK)
                return new Tuple<UnitModel, DisturbanceIdResult>(candidateModel, candidateModelDisturbance);
            else
                return new Tuple<UnitModel, DisturbanceIdResult>(null, candidateModelDisturbance);

        }



        /// <summary>
        /// Closed-loop simulation of a unit model and pid model, for a given unit data set.
        /// Adds the U_sim and Y_sim to the 
        /// </summary>
        /// <param name="unitData">unit dataset for simulation</param>
        /// <param name="modelParams">paramters or UnitModel</param>
        /// <param name="pidParams">parameters of PidModel</param>
        ///  <param name="pidInputIdx">index of the pid input in the process model</param>
        /// <param name="name">optional name used for plotting</param>
        /// <returns></returns>
        private static bool ClosedLoopSim(UnitDataSet unitData, UnitParameters modelParams, PidParameters pidParams,
            int pidInputIdx, string name="")
        {
            if (pidParams == null)
            {
                return false;
            }
            //TODO: this does not ignore bad datapoints?
            var pidModel = new PidModel(pidParams,"PidModel");
            var unitModel = new UnitModel(modelParams, "ProcModel");
            (var plantSim, var inputDataSet) = PlantSimulatorHelper.CreateFeedbackLoopWithEstimatedDisturbance(unitData, pidModel,
                unitModel, pidInputIdx);

            var isOk = plantSim.Simulate(inputDataSet, out var simData);

            if (isOk)
            {
                // todo: what about external inputs?
                unitData.U_sim = Array2D<double>.CreateFromList(new List<double[]> { 
                    simData.GetValues(pidModel.GetID(), SignalType.PID_U, 0)} ); 
                unitData.Y_sim = simData.GetValues(unitModel.GetID(), SignalType.Output_Y, 0);
                unitData.Y_proc = simData.GetValues(unitModel.GetID());
            }
            return isOk;
        }
    }
}