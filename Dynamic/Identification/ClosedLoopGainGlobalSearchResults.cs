using Accord.Math;
using System;
using System.Collections.Generic;
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
        public List<double> uPidVarianceList;

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
            uPidVarianceList = new List<double>();
            dEstVarianceList = new List<double>();
            covBtwDestAndYsetList = new List<double>();
            pidLinearProcessGainList = new List<double>();
            covBtwDestAndUexternal = new List<double>();
        }

        public void Add(double gain, UnitParameters unitParameters, double covBtwDestAndYset, double upidVariance,double eEstVariance,
            double covBtwDestAndUexternal)
        {
          //  var newParams = unitParameters.CreateCopy();

            pidLinearProcessGainList.Add(gain);
            unitParametersList.Add(unitParameters);
            covBtwDestAndYsetList.Add(covBtwDestAndYset);
            uPidVarianceList.Add(upidVariance);
            dEstVarianceList.Add(eEstVariance);
            this.covBtwDestAndUexternal.Add(covBtwDestAndUexternal);
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
        /// <param name="initalGainEstimate"></param>
        /// <returns></returns>
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

            var v1 = uPidVarianceList.ToArray();
            var v2 = covBtwDestAndYsetList.ToArray();
            var v3 = covBtwDestAndUexternal.ToArray();
            (double v1_Strength, int min_ind) = MinimumStrength(v1);
            (double v2_Strength, int min_ind_v2) = MinimumStrength(v2);
            (double v3_Strength,int min_ind_v3) = MinimumStrength(v3);

            var v4 = dEstVarianceList.ToArray();
            (double v4_Strength, int min_ind_v4) = MinimumStrength(v4);
            // add a scaled v4 to the objective only when v1 is very flat around the minimum
            // as a "tiebreaker"

            if (v1_Strength == 0 && v2_Strength == 0 && v3_Strength == 0)
            {
                // quite frequently the algorithm arrives here, where all the three "strengths" are zero. 
                // if there is a persistent disturbance like a randomwalk or sinus and no changes in the 
                // setpoint or in external inputs to the process model. 
                
                // only thing is that in randowmalk case, this tend to over-estimate gain consistently by about 10-40%
                if (v4_Strength > 0)
                {
                    var unitPara = unitParametersList.ElementAt(min_ind_v4);
              //      unitPara.Fitting = new FittingInfo();
               //     unitPara.Fitting.SolverID = "ClosedLoopId (global-search:v4)";
                    return  new Tuple<UnitModel, bool>(new UnitModel(unitPara), true);
                }
                else
                {
                    return new Tuple<UnitModel, bool>(null, false);
                }
                //modelParameters.AddWarning(UnitdentWarnings.ClosedLoopEst_GlobalSearchFailedToFindLocalMinima);
       
            }
            double[] objFun = v1;
          //  string vString = "v1";

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
             //       vString = "v2";
                }

                // if the system has external inputs, and they change in value
                if (!Vec.IsAllValue(v3, 0))
                {
                    var v3_scaled = Scale(v3);
                    objFun = vec.Add(objFun, vec.Multiply(v3_scaled, v3_factor));
           //         vString = "v3";
                }
            }

            vec.Min(objFun, out min_ind);
            var unitParams = unitParametersList.ElementAt(min_ind);
       //     unitParams.Fitting = new FittingInfo();
         //   unitParams.Fitting.SolverID = "ClosedLoopId (global-search:" + vString + ")";

            return new Tuple<UnitModel, bool>(new UnitModel(unitParams), true);
         }
    }
}