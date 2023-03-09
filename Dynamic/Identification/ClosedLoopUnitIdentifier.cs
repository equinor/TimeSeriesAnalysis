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
    internal class GainGlobalSearchResults
    {
        public List<double> linGainList;
        public List<double> covarianceBtwDistAndYsetList;
        public List<double> dEstVarianceList;
        public List<UnitModel> unitModelList;

        public GainGlobalSearchResults()
        {
            unitModelList= new List<UnitModel>();
            dEstVarianceList = new List<double>();
            covarianceBtwDistAndYsetList = new List<double>();
            linGainList = new List<double>();
        }

        public void Add(double gain, UnitModel unitModel, double covarianceBtwDistAndYset, double dest_variance)
        {
            linGainList.Add(gain);
            unitModelList.Add(unitModel);
            covarianceBtwDistAndYsetList.Add(covarianceBtwDistAndYset);
            dEstVarianceList.Add(dest_variance);
        }

        public UnitModel GetBestModel(double initalGainEstimate)
        {
            // if there is a local minimum in the list of dEstVarianceList then use that
            Vec vec = new Vec();
            vec.Min(dEstVarianceList.ToArray(),out int ind);
            vec.Min(covarianceBtwDistAndYsetList.ToArray(), out int ind2);
            if (ind > 0)
            {
                return unitModelList.ElementAt(ind);
            }
            // if both ind1 and ind2 are zero, it indicates an error.
            if (ind == ind2)
            {
                if (ind == 0 && ind2 == 0)
                {
                    unitModelList.ElementAt(ind).modelParameters.AddWarning(UnitdentWarnings.ClosedLoopEst_GlobalSearchFailedToFindLocalMinima);
                }
                return unitModelList.ElementAt(ind);
                //both models gave same result!!
            }
            
            //otherwise, we fall back to looking at the covariance between d_est and yset
            return unitModelList.ElementAt(ind2);

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
        const bool doDebuggingPlot = true;
        /// <summary>
        /// Identify the unit model of a closed-loop system and the distrubance (additive output signal)
        /// </summary>
        /// <remarks>
        /// <para>
        /// Currently always assumes that PID-index is first index of unit model(can be improved)
        /// </para>
        /// </remarks>
        /// <param name="dataSet">the unit data set, containing both the input to the unit and the output</param>
        /// <param name="plantSim">an optional PidModel that is used to co-simulate the model and disturbance, improving identification</param>
        /// <param name = "pidParams"> if the setpoint of the control changes in the time-set, then the paramters of pid control need to be given.</param>
        /// <param name="inputIdx">the index of the input</param>
        /// 
        /// <returns>The unit model, with the name of the newly created disturbance added to the additiveInputSignals</returns>
        public (UnitModel, double[]) Identify(UnitDataSet dataSet, PidParameters pidParams = null, int inputIdx = 0)
        {
            bool wasGainGlobalSearchDone = false;
            bool doTimeDelayEstOnRun1 = false;
            if (dataSet.Y_setpoint == null || dataSet.Y_meas == null || dataSet.U == null)
            {
                return (null, null);
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
            var vec = new Vec();
            var id = new UnitIdentifier();

            // ----------------
            // run1: no process model assumed, let disturbance estimator guesstimate a process gains, 
            // to give afirst estimate of the disturbance
            // this is often a quite good estimate of the process gain, which is the most improtant parameter
            // withtout a reference model which has the correct time constant and time delays, 
            // dynamic "overshoots" will enter into the estimated disturbance, try to fix this by doing 
            // "refinement" runs afterwards.
            { 
                DisturbanceIdResult distIdResult1 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet1, null, inputIdx, pidParams);

                dataSet1.D = distIdResult1.d_est;
                var unitModel_run1 = id.IdentifyLinearAndStatic(ref dataSet1, doTimeDelayEstOnRun1, u0);
                idDisturbancesList.Add(distIdResult1);
                idUnitModelsList.Add(unitModel_run1);
                isOK = ClosedLoopSim(dataSet1, unitModel_run1.GetModelParameters(), pidParams, distIdResult1.d_est, "run1");

                // experimental: see if varying gain to get the lowest correlation between setpoint and disturbance 
                // only needed if setpoint varies. "step1 global search"
                bool doesSetpointChange = !(vec.Max(dataSet.Y_setpoint) == vec.Min(dataSet.Y_setpoint));
                if (doesSetpointChange)
                {
                    wasGainGlobalSearchDone = true;
                    double initalGainEstimate = unitModel_run1.modelParameters.GetProcessGains().First();
                    double initalCorrelation = CorrelationCalculator.Calculate(distIdResult1.d_est, dataSet.Y_setpoint);
                    var gainAndCorrDict = new Dictionary<double, GainGlobalSearchResults>();
                    //
                    // looking to find the process gain that "decouples" d_est from Y_setpoint as much as possible.
                    //
                    // or antoher way to look at ti is that the output U with setpoint effects removed should be as decoupled 
                    // form Y_setpoint.
                    var min_gain = initalGainEstimate / 4;
                    var max_gain = initalGainEstimate * 2;
                    var range = max_gain - min_gain;
                    var searchResults = new GainGlobalSearchResults();

                    for (var linGain = min_gain; linGain < max_gain; linGain += range / 40)
                    {
                        var dataSet_alt = new UnitDataSet(dataSet);
                        var alternativeModel = new UnitModel(unitModel_run1.GetModelParameters().CreateCopy(), "alternative");
                        alternativeModel.modelParameters.LinearGains = new double[] { linGain };

                        DisturbanceIdResult distIdResultAlt = DisturbanceIdentifier.EstimateDisturbance
                            (dataSet_alt, alternativeModel, inputIdx, pidParams);
                        var d_est = distIdResultAlt.d_est;
                        isOK = ClosedLoopSim
                            (dataSet_alt, alternativeModel.GetModelParameters(), pidParams, d_est, "run_alt");

                        // for the cases when d is a step and yset is a sinus, v7 seems to work best
                        // for the caeses when d is sinus and yset is a step, either of v1 or v2 seem to work better,
                        // but more so when the step in yset is fairly big compared to the disturbance.

                        //v1: use correlation(gives best gain: 0.7(crosses from positive to negative around there)
                        // double covarianceBtwDistAndYsetList = Math.Abs(CorrelationCalculator.Calculate(dataSet.Y_setpoint, d_est));
                        // v2: try to use covarince(gives highest gain best(no local minimum))
                        double covarianceBtwDistAndYsetList = Math.Abs(Measures.Covariance(dataSet.Y_setpoint, d_est, false));
                        // v3: try to use regression gains(gives best gain: 0.7)
                        /*  var testDataSet = new UnitDataSet();
                          testDataSet.U = Array2D<double>.CreateFromList(new List<double[]> { dataSet.Y_setpoint, distIdResultAlt.adjustedUnitDataSet.U.GetColumn(inputIdx) });
                          testDataSet.Y_meas = d_est;
                          UnitIdentifier ident = new UnitIdentifier();
                          double[] u0_loc = { vec.Mean(dataSet.Y_setpoint).Value, vec.Mean(distIdResultAlt.adjustedUnitDataSet.U.GetColumn(inputIdx)).Value };
                          double[] uNorm = { vec.Range(dataSet.Y_setpoint), vec.Range(distIdResultAlt.adjustedUnitDataSet.U.GetColumn(inputIdx)) };
                          var identModel = ident.IdentifyLinearAndStatic(ref testDataSet, false,u0_loc, uNorm);
                          double unitModelKPI = Math.Abs(identModel.modelParameters.LinearGains[0]);
                          gainAndCorrDict.Add(linGain, unitModelKPI);
                          // debugging only        
                          Shared.EnablePlots();
                          Plot.FromList(
                          new List<double[]> {
                              testDataSet.U.GetColumn(0),
                              testDataSet.U.GetColumn(1),
                              testDataSet.Y_meas,
                              testDataSet.Y_sim,
                          },
                          new List<string> {"y1=y_setpoint(u1)","y2=u_setpointremoved(u2)","y3=d(y)", "y3=y_sim",
                          },
                          dataSet.GetTimeBase(), "ClosedLoopId_step1Regression_gain"+ linGain); ;
                          Shared.DisablePlots();
                          
                        */

                        //v4: regression and the covariance matrix:
                        /*                        double[,] phi_ols2D = Array2D<double>.CreateFromList(new List<double[]> { d_est, dataSet.Y_setpoint });
                                                double[] Y_ols = corVar;
                                                double[][] phi_ols = phi_ols2D.Transpose().Convert2DtoJagged();
                                                var regResults = vec.RegressRegularized(Y_ols, phi_ols);
                                                double corrFactor = regResults.VarCovarMatrix[0][1];
                        */

                        // v5: orthogonality(should be maximum then)
                        //Double[,] matrix = Array2D<double>.CreateFromList(new List<double[]> { dataSet.Y_setpoint, distIdResultAlt.adjustedUnitDataSet.U.GetColumn(inputIdx) });
                        /*   Double[,] matrix = Array2D<double>.CreateFromList(new List<double[]> { dataSet.Y_setpoint, d_est });
                           var svd = new SingularValueDecomposition(matrix);
                           var test = svd.Diagonal;
                           gainAndCorrDict.Add(linGain, test[0]);*/

                        // v6: just choose the gain that gives the least "variance" in d_est?
                        // v6 works well when the disturbance is a step, but does not wark at all when the disturbance is a sinus!!!
                        // var unitModelKPI = vec.Mean(vec.Abs(vec.Diff(d_est))).Value;
                        //  var unitModelKPI = vec.Mean(vec.Abs(vec.Diff(vec.Div(d_est, vec.Max(vec.Abs(d_est)) )))).Value;

                        // v7: just choose the gain that gives the least "variance" in d_est?
                        var dest_variance = vec.Mean(vec.Abs(vec.Diff(distIdResultAlt.adjustedUnitDataSet.U.GetColumn(inputIdx)))).Value;// /vec.Max(vec.Abs(d_est));

                         searchResults.Add(linGain,alternativeModel, covarianceBtwDistAndYsetList, dest_variance);
                    }
                    UnitModel bestUnitModel = searchResults.GetBestModel(initalGainEstimate) ;

                    // add the "best" model to be used in the next model run
                    idUnitModelsList.Add(bestUnitModel);
                }
            }
            // ----------------
            // run 2: now we have a decent first empircal estimate of the distubance and the process gain, now try to use identification
            {
                DisturbanceIdResult distIdResult2 = DisturbanceIdentifier.EstimateDisturbance(
                    dataSet2, idUnitModelsList.Last(), inputIdx, pidParams);

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
            {
                DisturbanceIdResult distIdResult3 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet3, idUnitModelsList.Last(), inputIdx, pidParams);

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
            {
                var model = idUnitModelsList.Last();

                DisturbanceIdResult distIdResult4 = DisturbanceIdentifier.EstimateDisturbance
                    (dataSet4, model, inputIdx, pidParams);
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
                        (dataSet4, newModel,inputIdx,pidParams);
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
                        (dataSet4, step4Model, inputIdx, pidParams);
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
                identUnitModel.modelParameters.Fitting.StartTime = dataSet.Times.Last();
                if (wasGainGlobalSearchDone)
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop/w gain global search";
                else
                    identUnitModel.modelParameters.Fitting.SolverID = "ClosedLoop v1.0";
                identUnitModel.modelParameters.Fitting.NFittingTotalDataPoints = dataSet.GetNumDataPoints();
            }


            return (identUnitModel,disturbance);
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