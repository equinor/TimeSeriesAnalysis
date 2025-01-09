using Accord.Math;
using Accord.Statistics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TimeSeriesAnalysis.Dynamic
{
    internal class ClosedLoopGainGlobalSearchResults
    {
        // "magic numbers"
        // this factor is how much to weigh covBtwDestAndYset into a joint objective function with dEstVariance
        // covBtwDestAndYset sometimes can be a "tiebreaker" in cases where dEstVariance has a "flat" region and no clear 
        // minimum, but should not be too large either to overpower clear minima in dEstVariance
        const double v2_factor = 0.01;// should be much smaller than one
        const double v3_factor = 0.10;

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
        public List<double> uPidDistanceList;

        /// <summary>
        /// self-variance of d_est, calculated for each linear gains
        /// </summary>
        public List<double> dEstDistanceList;

        /// <summary>
        /// self-variance of d_est, calculated for each linear gains
        /// </summary>
        public List<double> covBtwDestAndUexternal;


        /// <summary>
        /// process unit model used in each iteration of the global search(return one of these, the one that is "best")
        /// </summary>
        public List<UnitParameters> unitParametersList;


        /// <summary>
        /// the covariance between the uPid and Dest (experimental)
        /// </summary>
        public List<double> covBtwUPidAdjustedAndDestList;


        /// <summary>
        /// Holds the results of a global search for process gains as performed by ClosedLoopIdentifier
        /// </summary>
        internal ClosedLoopGainGlobalSearchResults()
        {
            unitParametersList= new List<UnitParameters>();
            uPidDistanceList = new List<double>();
            dEstDistanceList = new List<double>();
            covBtwDestAndYsetList = new List<double>();
            covBtwDestAndUexternal = new List<double>();
            covBtwUPidAdjustedAndDestList = new List<double>();
        }

        /// <summary>
        /// Add the result of one paramter study as part of the global search
        /// </summary>
        /// <param name="unitParameters">paramters that have been studied</param>
        /// <param name="dataSet"></param>
        /// <param name="dEst"></param>
        /// <param name="u_pid_adjusted"></param>
        /// <param name="pidInputIdx"></param>
        internal void Add(UnitParameters unitParameters, UnitDataSet dataSet, double[] dEst, double[] u_pid_adjusted, int pidInputIdx)
        {
            var vec = new Vec(dataSet.BadDataID);

            double covarianceBtwDestAndYset = Math.Abs(CorrelationCalculator.CorrelateTwoVectors(
             dEst, dataSet.Y_setpoint, dataSet.IndicesToIgnore));

            // v3: just choose the gain that gives the least "variance" in u_pid when the disturbance is simulated
            // in closed loop with the given PID-model and the assumed candiate process model. 

            var uPidDistance = vec.Mean(vec.Abs(vec.Diff(u_pid_adjusted))).Value;

            // v4: just choose the gain that gives the least "distance travelled" in d_est 

            /*double Power(double[] inSignal)
            {
                var mean = vec.Mean(inSignal).Value;
                var max = vec.Max(inSignal);
                var min = vec.Min(inSignal);
                double scale = Math.Max(Math.Abs(max - mean), Math.Abs(min - mean));
                return vec.Mean(vec.Abs(vec.Subtract(inSignal, vec.Mean(inSignal).Value))).Value / scale;
            }
            var dEstDistance = Power(dEst);*/

            var dEstDistance = vec.Mean(vec.Abs(vec.Diff(dEst))).Value; 

            var covBtwUPidAdjustedAndDest = Math.Abs(Measures.Covariance(dEst, u_pid_adjusted, false));

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

            unitParametersList.Add(unitParameters);
            covBtwDestAndYsetList.Add(covarianceBtwDestAndYset);
            uPidDistanceList.Add(uPidDistance);
            dEstDistanceList.Add(dEstDistance);
            covBtwDestAndUexternal.Add(covarianceBtwDestAndUexternal);
            covBtwUPidAdjustedAndDestList.Add(covBtwUPidAdjustedAndDest);
        }

        /// <summary>
        /// Determine the best model after finished the global search for the process gain of the pid-input, by evaluating all the 
        /// saved KPIs in a structured way. This method includes some heuristics.
        /// 
        /// Remember that "excitation" in a closed-loop system can happen in noe of three ways:
        ///  1) a change in the disturbance signal 
        ///  2) a change in the setpoint of the PID-controller or
        ///  3) a change in external input u 
        ///  4) any combination of the above three.
        /// 
        /// It is assumed that the disturbance does not depend on the external u nor on the setpoint y, 
        /// and this motivates searching for the minima in v2 and v3, respectively. 
        /// 
        /// 
        /// </summary>

        /// <returns></returns>
        public Tuple<UnitModel,string> GetBestModel()
        {
            bool doV4 = true;

            if (unitParametersList.Count()== 0)
                return new Tuple<UnitModel,string>(null,"");

            // calculate strength of a minimum - strength is value between 0 and 100, higher is stronger
            // this is a metric of how flat the objective space is, the lower the strenght, the flatter the objective function
            Tuple<double,int> CalcStrengthOfObjectiveMinimum(double[] values)
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
            }

            // look at the values of the objective either side of the minimum, and use this to indicate if the
            // if the actual true value is higher or lower

            string Direction(double[] v_in, int minIdx)
            {

                if (minIdx == 0)
                    return "";
                if (minIdx >= v_in.Length)
                    return "";
                double v_min = (new Vec()).Min(v_in);
                double v_max = (new Vec()).Max(v_in);
                double v_range = v_max - v_min;
                if (minIdx == 0 )
                    return "@min";
                if (minIdx >= v_in.Length)
                    return "@max";

                if (v_in[minIdx + 1] == v_in[minIdx - 1])
                {
                    return "(><)";
                }
                else
                if (v_in[minIdx + 1] > v_in[minIdx - 1] )
                {
                    double directionPower = (v_in[minIdx - 1] - v_min) / (v_in[minIdx + 1] - v_min) * 100;
                    return "(<-)" + directionPower.ToString("F1", CultureInfo.InvariantCulture)+"%";
                }
                else if (v_in[minIdx + 1] < v_in[minIdx - 1])
                {
                    double directionPower = (v_in[minIdx + 1] - v_min) / (v_in[minIdx - 1] - v_min)*100;
                    return "(->)" + directionPower.ToString("F1", CultureInfo.InvariantCulture) + "%";
                }
                else
                {
                    return "";
                }
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
            var v1 = uPidDistanceList.ToArray();
            var v2 = covBtwDestAndYsetList.ToArray();
            var v3 = covBtwDestAndUexternal.ToArray();
            (double v1_Strength, int min_ind_v1) = CalcStrengthOfObjectiveMinimum(v1);
            (double v2_Strength, int min_ind_v2) = CalcStrengthOfObjectiveMinimum(v2);
            (double v3_Strength, int min_ind_v3) = CalcStrengthOfObjectiveMinimum(v3);

            var v4 = dEstDistanceList.ToArray();
            (double v4_Strength, int min_ind_v4) = CalcStrengthOfObjectiveMinimum(v4);
            // add a scaled v4 to the objective only when v1 is very flat around the minimum
            // as a "tiebreaker"

            double strength_cutoff = 0.001;

            if (v1_Strength < strength_cutoff && v2_Strength < strength_cutoff && v3_Strength < strength_cutoff)
            {
                // quite frequently the algorithm arrives here, where all the three "strengths" are zero. 
                // if there is a persistent disturbance like a randomwalk or sinus and no changes in the 
                // setpoint or in external inputs to the process model. 

                if (doV4)
                {
                    if (v4_Strength > 0)
                    {
                        var unitPara = unitParametersList.ElementAt(min_ind_v4);
                        return new Tuple<UnitModel, string>(new UnitModel(unitPara), "Kp:v4 (strength:"+ v4_Strength.ToString("F3",CultureInfo.InvariantCulture) + ") "
                            + Direction(v4, min_ind_v4));
                    }
                    else
                    {
                        return new Tuple<UnitModel, string>(null, "");
                    }
                }
      
            }
            double[] objFun = v1;
            var retString = "Kp:v1(strength: " + v1_Strength.ToString("F2",CultureInfo.InvariantCulture) + ") "+ Direction(v1, min_ind_v1); ;

            if (v1_Strength < v1_Strength_Threshold)
            {
                var v1_scaled = Scale(v1);
                   
                // if setpoint changes then v2 will be non-all-zero
                if (!Vec.IsAllValue(v2, 0) && v2_Strength>0)
                {
                    var v2_scaled = Scale(v2);
                    objFun = vec.Add(objFun, vec.Multiply(v2_scaled, v2_factor));
                     retString = "Kp:v2(strength: " + v2_Strength.ToString("F2", CultureInfo.InvariantCulture) + ")" ;
                }

                // if the system has external inputs, and they change in value
                if (!Vec.IsAllValue(v3, 0) && v3_Strength > 0)
                {
                    var v3_scaled = Scale(v3);
                    objFun = vec.Add(objFun, vec.Multiply(v3_scaled, v3_factor));
                    retString = "Kp:v3(strength: " + v3_Strength.ToString("F2", CultureInfo.InvariantCulture) + ")";
                }
            }

            vec.Min(objFun, out min_ind_v1);
            var unitParams = unitParametersList.ElementAt(min_ind_v1);

            return new Tuple<UnitModel, string>(new UnitModel(unitParams), retString);
         }


    }
}