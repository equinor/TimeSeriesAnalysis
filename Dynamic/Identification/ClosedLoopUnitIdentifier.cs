using System;
using System.Collections.Generic;
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
    /// are estimted together.
    /// 
    /// </summary>
    public class ClosedLoopUnitIdentifier
    {
        const bool doDebuggingPlot = false;
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
        /// <param name="inputIdx">the index of the input</param>
        /// <returns>The unit model, with the name of the newly created disturbance added to the additiveInputSignals</returns>
        public (UnitModel,double[]) Identify(UnitDataSet dataSet, PidParameters pidParams=null, int inputIdx = 0 )
        {


            // this variable holds the "newest" unit model run and is updated
            // over multiple runs, and as it improves, the 
            // estimate of the disturbance improves along with it.

            List<DisturbanceIdResult> idDisturbancesList = new List<DisturbanceIdResult>();
            List<UnitModel> idUnitModelsList = new List<UnitModel>();
            List<double> processGainList = new List<double>();

            double[] u0 = dataSet.U.GetRow(0);
            double y0 = dataSet.Y_meas[0];

            bool isOK;
            Vec vec = new Vec();

            var dataSet1 = new UnitDataSet(dataSet);
            var dataSet2 = new UnitDataSet(dataSet);
            var dataSet3 = new UnitDataSet(dataSet);

            // ----------------
            // run1: no process model assumed, let disturbance estimator guesstimate a process gains, 
            // to give afirst estimate of the disturbance
            // this is often a quite good estimate of the process gain, which is the most improtant parameter
            // withtout a reference model which has the correct time constant and time delays, 
            // dynamic "overshoots" will enter into the estimated disturbance, try to fix this by doing 
            // "refinement" runs afterwards.
            DisturbanceIdResult distIdResult1 = DisturbanceIdentifier.EstimateDisturbance
                (dataSet1, null,inputIdx);
            var id = new UnitIdentifier();

            dataSet1.D = distIdResult1.d_est;
            var unitModel_run1 = id.IdentifyLinearAndStatic(ref dataSet1,false,u0);
            idDisturbancesList.Add(distIdResult1);
            idUnitModelsList.Add(unitModel_run1);
            isOK = ClosedLoopSim(dataSet1,unitModel_run1.GetModelParameters(), pidParams, distIdResult1.d_est,"run1");
            // ----------------
            // run 2: now we have a decent first estimate of the distubance and the process gain, but 
            // we have disturbance vector estimate that has some process dynamics in it, so we need to refine the 
            // model to get the correct dynamics.

            DisturbanceIdResult distIdResult2 = DisturbanceIdentifier.EstimateDisturbance(
                dataSet2, unitModel_run1, inputIdx);

            dataSet2.D = distIdResult2.d_est;
            // IdentifyLinear appears to give quite a poor model for Static_Longstep 
            var unitModel_run2 = id.IdentifyLinearAndStatic(ref dataSet2,true, u0);
            idDisturbancesList.Add(distIdResult2);
            idUnitModelsList.Add(unitModel_run2);
            isOK = ClosedLoopSim(dataSet2, unitModel_run2.GetModelParameters(), pidParams, distIdResult2.d_est, "run2");

            // NB! in some cases such as if there is a step in the setpoint and the disturbance is small and 
            //      be approximated as zero, then the estimation will not keep improving beyond this point.

            // ----------------
            // run 3: use the result of the last run to try to improve the disturbance estimate and take 
            // out some of the dynamics from the disturbance vector and see if this improves estiamtion.

            DisturbanceIdResult distIdResult3 = DisturbanceIdentifier.EstimateDisturbance
                (dataSet3, unitModel_run2, inputIdx);

            dataSet3.D = distIdResult3.d_est;
            var unitModel_run3 = id.IdentifyLinearAndStatic(ref dataSet3, true, u0);
            idDisturbancesList.Add(distIdResult3);
            idUnitModelsList.Add(unitModel_run3);
            isOK = ClosedLoopSim(dataSet3, unitModel_run3.GetModelParameters(), pidParams, distIdResult3.d_est, "run3");
            // ----------------
            // after run 3:
            // if (the data shows just excited by disturbances)
            //      then the process and disturbance gain and general shape is broadly correct
            //      (Tc an timeDealay_s may not be known yet  - that is the point of run 4)
            // else if (if the data is contains excitation in setpoints)
            //      then the disturbance vector will be zero
            //      process gain, transient process parameters are then also found
            // else if (the data contains excitiaton in setpoint, _but_ the disturbance are also non-negligable)
            //      then the disturbance will have been set to zero and the model may have tried to "over-fit"
            //      the process model to describe both disturbances and setpoint changes with the same model
            //      a disturbance will cause both y and u to change and the process model may erronously fit to it.
            //      Generally it seems that this causes the following
            //      - a very large time constant that does not match the validation (unclear why)
            //      - the gain is likely a little too big in these cases.

            /*   if (referenceProcessIDresult.GetWarningList().Contains(ProcessIdentWarnings.TimeConstantEstimateNotConsistent))
               {
                   referenceProcessIDresult = processIDresult_run1;// run1 is static, re-use that.
                   bool tryToModelDisturbanceIfSetpointChangesInDataset = true;
                   bool estimateTimeDelay_EX = false;
                   bool useDynamicModel_EX = false;
                   DisturbanceIdResult distIdResult_ex0 = DisturbanceIdentifierInternal.EstimateDisturbance
                       (dataSet, pidIDresults, referenceProcessIDresult, tryToModelDisturbanceIfSetpointChangesInDataset);
                   ProcessIdResults processIDresult_runEX0 = Ident_internal(dataSet, useDynamicModel_EX, distIdResult_ex0, 0,
                      estimateTimeDelay_EX, false);

                   idDisturbancesList.Add(distIdResult_ex0);
                   idUnitModelsList.Add(processIDresult_runEX0);
                   processGainList.Add(processIDresult_runEX0.processGain);
                   processIDresult = processIDresult_runEX0;

                   DisturbanceIdResult distIdResult_ex = distIdResult_ex0.Copy();
                   distIdResult_ex.dest_f1 = vec.Add(distIdResult_ex0.dest_f1, -distIdResult_ex0.dest_f1[0]);
                   distIdResult_ex.d_LF = vec.Add(distIdResult_ex0.d_LF, -distIdResult_ex0.dest_f1[0]);

                   ProcessIdResults processIDresult_runEX = Ident_internal(dataSet, useDynamicModel_EX, distIdResult_ex, 0,
                       estimateTimeDelay_EX, false);

                   idDisturbancesList.Add(distIdResult_ex);
                   idUnitModelsList.Add(processIDresult_runEX);
                   processGainList.Add(processIDresult_runEX.processGain);
                   processIDresult = processIDresult_runEX;
               }
               else*/
            {


                // - issue is that after run 4 modelled output does not match measurement.
                // - the reason that we want this run is that after run3 the time constant and 
                // time delays are way off if the process is excited mainly by disturbances.
                // - the reason that we cannot do run4 immediately, is that that formulation 
                // does not appear to give a solution if the guess disturbance vector is bad.

                bool doRunNumberFour = false;
                if (doRunNumberFour)
                {
                    // run4: do a run where it is no longer assumed that x[k-1] = y[k], 
                    // this run has the best chance of estimating correct time constants, but it requires a good inital guess of d
                    DisturbanceIdResult distIdResult4 = DisturbanceIdentifier.EstimateDisturbance
                        (dataSet, unitModel_run3);
                    dataSet.D = distIdResult4.d_est;
                    var unitModel_run4 = id.Identify(ref dataSet);
                    idDisturbancesList.Add(distIdResult4);
                    idUnitModelsList.Add(unitModel_run4);
                  }
                /*else
                {
                    // give the result out 
                    identUnitModel = unitModel_run3;//nb! update as more steps are added.
                }*/
            }

            if (doDebuggingPlot)
            {
                Console.WriteLine("run1");
                Console.WriteLine(idUnitModelsList[0].ToString());
                Console.WriteLine("run2");
                Console.WriteLine(idUnitModelsList[1].ToString());
                Console.WriteLine("run3");
                Console.WriteLine(idUnitModelsList[2].ToString());

                Plot.FromList(
                    new List<double[]> { 
                        idDisturbancesList[0].d_est,
                        idDisturbancesList[1].d_est,
                        idDisturbancesList[2].d_est,
                        idDisturbancesList[0].d_HF,
                        idDisturbancesList[1].d_HF,
                        idDisturbancesList[2].d_HF,
                        idDisturbancesList[0].d_u,
                        idDisturbancesList[1].d_u,
                        idDisturbancesList[2].d_u,
                    },
                    new List<string> {"y1=d_run1", "y1=d_run2", "y1=d_run3",
                    "y3=dHF_run1", "y3=dHF_run2", "y3=dHF_run3",
                    "y3=dLF_run1", "y3=dLF_run2", "y3=dLF_run3"
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
            }

            // - note that by tuning rules often Kp = 0.5 * Kc approximately. 
            // - this corresponds to roughly 50% of a disturbance being taken up by P-term and 50% by I-term.
            // - does it make sense to filter e to create an estiamte of d?? this would mean that ymod and ymeas no longer 
            // match exactly(a good thing? coudl give insight on model fit?)
            // - could we make a function that gives out the closed loop step response of the 
            // pid controller/process in closed loop, to see if the closed loop has overshoot/undershoot? 
            double[] disturbance = idDisturbancesList.ToArray()[idDisturbancesList.Count-1].d_est;
            UnitModel  identUnitModel = idUnitModelsList.ToArray()[idUnitModelsList.Count - 1];
            return (identUnitModel,disturbance);
        }

       // TODO: replace this with a "closed-loop" simulator that uses the PlantSimulator.
       // 
        public bool ClosedLoopSim(UnitDataSet unitData, UnitParameters modelParams, PidParameters pidParams,
            double[] disturbance,string name)
        {
            // used for debugging:
        //l    modelParams.LinearGains = new double[] { 1 };

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