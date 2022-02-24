using System;
using System.Collections.Generic;
using System.Text;

using TimeSeriesAnalysis;

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
    public class ClosedLooopUnitIdentifier
    {

        /// <summary>
        /// Identify the unit model of a closed-loop system and the distrubance (additive output signal)
        /// </summary>
        /// <param name="dataSet">the unit data set, containing both the input to the unit and the output</param>
        /// <param name="simData">the data set of simulated variables, to which the algorithm adds the estimated disturbance signal</param>
        /// <returns>The unit model, with the name of the newly created disturbance added to the additiveInputSignals</returns>
        public UnitModel Identify(UnitDataSet dataSet, ref TimeSeriesDataSet simData/* PIDidResults pidIDresults,
            ProcessIdResults referenceProcessIDresult = null*/)
        {
            const bool estimateTimeDelay = true;// should be true, but can be set false for debugging only
                                                //  Warn.If(estiamteTimeDelay,"temproary turned off");
            ProcessIdResults bestResult = null;

            List<DisturbanceIdResult> d_est_List = new List<DisturbanceIdResult>();
            List<ProcessIdResults> processIdList = new List<ProcessIdResults>();
            List<PIDidResults> pidIDresultsList = new List<PIDidResults>();
            List<double> processGainList = new List<double>();

            DisturbanceIdResult noDisturbance = new DisturbanceIdResult(dataSet);

            Vec vec = new Vec();

            /*
            if (doDebugging)
            {
                string uStr = "u";
                if (dataSet.HasFeedforward())
                    uStr = "u_inclFF";

               Plot.Three(dataSet.ymeas, dataSet.yset, dataSet.GetUinclFF(), (int)timeBase_s,
                    "ymeas", "yset", uStr, true, true, "ProcessIDOls_cs_dataset");
            }
            */
            // ----------------
            // run1: no process model assumed, let disturbance estimator guesstimate a process gains, 
            // to give afirst estimate of the disturbance
            // this is often a quite good estimate of the process gain, which is the most improtant paramter
            // withtout a reference model which has the correct time constant and time delays, 
            // dynamic "overshoots" will enter into the estimated disturbance, try to fix this by doing 
            // "refinement" runs afterwards.
            DisturbanceIdResult distIdResult1 = DisturbanceIdentifierInternal.EstimateDisturbance(
                dataSet, pidIDresults, referenceProcessIDresult);
            // you would think you could turn dynamics off here, but this gives noticably worse performance
            bool useDynamicModel_run1 = false;
            ProcessIdResults processIDresult_run1 = Ident_internal(dataSet, useDynamicModel_run1, distIdResult1, 0, estimateTimeDelay, true);
            d_est_List.Add(distIdResult1);
            processIdList.Add(processIDresult_run1);
            pidIDresultsList.Add(pidIDresults);
            processGainList.Add(processIDresult_run1.processGain);

            // ----------------
            // run 2: now we have a decent first estimate of the distubance and the process gain, but 
            // we have disturbance vector estimate that has some process dynamics in it, so we need to refine the 
            // model to get the correct dynamics.

            referenceProcessIDresult = processIDresult_run1;// likely a good estimate of process gain and hence a good first approx of d here.
                                                            //     referenceProcessIDresult.timeConstant_s = 0; //these two will be false after run 0, just set to zero 
                                                            //     referenceProcessIDresult.timeDelay_s = 0;//these two will be false after run 0, just set to zero 
                                                            //    referenceProcessIDresult.UpdateModelledXandY(dataSet);

            DisturbanceIdResult distIdResult2 = DisturbanceIdentifierInternal.EstimateDisturbance(
                dataSet, pidIDresults, referenceProcessIDresult);
            bool useDynamicModel_run2 = true;
            ProcessIdResults processIDresult_run2 = Ident_internal(dataSet, useDynamicModel_run2, distIdResult1, 0, estimateTimeDelay, true);
            d_est_List.Add(distIdResult2);
            processIdList.Add(processIDresult_run2);
            processGainList.Add(processIDresult_run2.processGain);

            // NB! in some cases such as if there is a step in the setpoint and the disturbance is small and 
            //      be approximated as zero, then the estimation will not keep improving beyond this point.


            // run 3: use the result of the last run to try to improve the disturbance estimate and take 
            // out some of the dynamics from the disturbance vector and see if this improves estiamtion.

            referenceProcessIDresult = processIDresult_run2;// likely a good estimate of process gain and hence a good first approx of d here.

            DisturbanceIdResult distIdResult3 = DisturbanceIdentifierInternal.EstimateDisturbance
                (dataSet, pidIDresults, referenceProcessIDresult);
            ProcessIdResults processIDresult_run3 = Ident_internal(dataSet, true, distIdResult3, 0, estimateTimeDelay, true);
            d_est_List.Add(distIdResult3);
            processIdList.Add(processIDresult_run3);
            processGainList.Add(processIDresult_run3.processGain);

            referenceProcessIDresult = processIDresult_run3;// likely a good estimate of process gain and hence a good first approx of d here.



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

            ProcessIdResults processIDresult;

            if (referenceProcessIDresult.GetWarningList().Contains(ProcessIdentWarnings.TimeConstantEstimateNotConsistent))
            {
                referenceProcessIDresult = processIDresult_run1;// run1 is static, re-use that.
                bool tryToModelDisturbanceIfSetpointChangesInDataset = true;
                bool estimateTimeDelay_EX = false;
                bool useDynamicModel_EX = false;
                DisturbanceIdResult distIdResult_ex0 = DisturbanceIdentifierInternal.EstimateDisturbance
                    (dataSet, pidIDresults, referenceProcessIDresult, tryToModelDisturbanceIfSetpointChangesInDataset);
                ProcessIdResults processIDresult_runEX0 = Ident_internal(dataSet, useDynamicModel_EX, distIdResult_ex0, 0,
                   estimateTimeDelay_EX, false);

                d_est_List.Add(distIdResult_ex0);
                processIdList.Add(processIDresult_runEX0);
                processGainList.Add(processIDresult_runEX0.processGain);
                processIDresult = processIDresult_runEX0;

                DisturbanceIdResult distIdResult_ex = distIdResult_ex0.Copy();
                distIdResult_ex.dest_f1 = vec.Add(distIdResult_ex0.dest_f1, -distIdResult_ex0.dest_f1[0]);
                distIdResult_ex.d_LF = vec.Add(distIdResult_ex0.d_LF, -distIdResult_ex0.dest_f1[0]);

                ProcessIdResults processIDresult_runEX = Ident_internal(dataSet, useDynamicModel_EX, distIdResult_ex, 0,
                    estimateTimeDelay_EX, false);

                d_est_List.Add(distIdResult_ex);
                processIdList.Add(processIDresult_runEX);
                processGainList.Add(processIDresult_runEX.processGain);
                processIDresult = processIDresult_runEX;
            }
            else
            {
                // run4: do a run where it is no longer assumed that x[k-1] = y[k], 
                // this run has the best chance of estimating correct time constants, but it requires a good inital guess of d
                DisturbanceIdResult distIdResult4 = DisturbanceIdentifierInternal.EstimateDisturbance
                    (dataSet, pidIDresults, referenceProcessIDresult);
                ProcessIdResults processIDresult_run4 = Ident_internal(dataSet, true, distIdResult4, 0, estimateTimeDelay, false);
                d_est_List.Add(distIdResult4);
                processIdList.Add(processIDresult_run4);
                processGainList.Add(processIDresult_run4.processGain);

                // 18.06.2020: 
                // - issue is that after run 4 modelled output does not match measurement.
                // - the reason that we want this run is that after run3 the time constant and 
                // time delays are way off if the process is excited mainly by disturbances.
                // - the reason that we cannot do run4 immediately, is that that formulation 
                // does not appear to give a solution if the guess disturbance vector is bad.

                bool doRunNumberFour = false;
                if (doRunNumberFour)
                {
                    processIDresult = processIDresult_run4;//nb! update as more steps are added.
                }
                else
                {
                    // give the result out 
                    processIDresult = processIDresult_run3;//nb! update as more steps are added.
                }
            }
            // Todo:consider using Kc_lower/upper based on theory insted and taking min and max of them.
            processIDresult.Kc_upper = processGainList.ToArray().Max();
            processIDresult.Kc_lower = processGainList.ToArray().Min();


            string lastRunDesc = "EX";
            /*
              if (doDebugging)
              {
                  /*   //nb!!! dHF_run2 virker å være praktisk talt null!!
                     Plot.Four(distIdResult1.d_HF,
                          distIdResult2.d_HF, distIdResult3.d_HF, d_est_List.Last().d_HF,
                          (int)dataSet.timeBase_s, "dHF_run1", "dHF_run2", "dHF_run3", "dHF_last_run", true, false, "high-frequency disturbance");

                     Plot.Four(distIdResult1.d_LF,
                      distIdResult2.d_LF, distIdResult3.d_LF, d_est_List.Last().d_LF,
                      (int)dataSet.timeBase_s, "dLF_run1", "dLF_run2", "dLF_run3", "dLF_last_run", true, false, "low-frequency disturbance");
                  */
            /*             if (processIDresult_run1.modelledD != null)
                         {
                             Plot.Four(processIDresult_run1.modelledD,
                                 processIDresult_run2.modelledD, processIDresult_run3.modelledD, processIdList.Last().modelledD,
                                 (int)dataSet.timeBase_s, "d_run1", "d_run2", "d_run3", "d_last_run", true, false, "ProcessIDOls_cs_total disturbance");
                         }

                         Plot.Four(processIDresult_run1.modelledY, referenceProcessIDresult.modelledY,
                             processIDresult_run3.modelledY, processIDresult.modelledY,
                             (int)dataSet.timeBase_s, "ymod_run1", "ymod_run2", "ymod_run3", "ymod_last_run", true, false, "ProcessIDOls_cs_estimated output");

                         Plot.Four(processIDresult_run1.modelledX, referenceProcessIDresult.modelledX,
                             processIDresult_run3.modelledX, processIDresult.modelledX,
                              (int)dataSet.timeBase_s, "xmod_run1", "xmod_run2", "xmod_run3", "xmod_last_run", true, false, "ProcessIDOls_cs_estimated states");
                     }
                 */

            // - note that by tuning rules often Kp = 0.5 * Kc approximately. 
            // - this corresponds to roughly 50% of a disturbance being taken up by P-term and 50% by I-term.
            // - does it make sense to filter e to create an estiamte of d?? this would mean that ymod and ymeas no longer 
            // match exactly(a good thing? coudl give insight on model fit?)
            // - coudl we make a function that gives out the closed loop step response of the 
            // pid controller/process in closed loop, to see if the closed loop has overshoot/undershoot? 


            return processIDresult;

        }
    }
}