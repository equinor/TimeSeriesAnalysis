using Accord.Math;
using System;
using System.Collections.Generic;
using System.Text;
using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{

    /// <summary>
    /// FittingInfo
    /// 
    /// Be careful as the objective function is different for the static estimation that considers the absolute values,
    /// while dynamic estimation considers "diffs"- for this reason it is best to use RsqDiff and RsqAbs
    /// when comparing different model runs which can be a combination fo static and dynamic
    /// </summary>
    public class FittingInfo
    {

        const int nDigits = 5;// number of significant digits in result parameters

        /// <summary>
        /// True if identification was able to identify, otherwise false.
        /// Note that this flag is not an indication that the model is good, i.e. that the data
        /// had sufficient information to determine unique paramters that describe the dataset well. 
        /// This flag only indicates that regression did not crash during identification.
        /// </summary>
        public bool WasAbleToIdentify { get; set; }

        /// <summary>
        /// A string that identifies the solver that was used to find the model
        /// </summary>
        public string SolverID { get; set; }


        /// <summary>
        /// A score that is 100% if model describes all variations 
        /// and 0% if model is no better at describing variation than the flat average line.
        /// Negative if the model is worse than a flat average line.
        /// </summary>

        public double FitScorePrc { get; set; }


        /// <summary>
        /// Number of bad data points ignored during fitting
        /// </summary>
        /// 
        public double NFittingBadDataPoints { get; set; }

        /// <summary>
        /// Number of total data points (good and bad) available for fitting
        /// </summary>
        public double NFittingTotalDataPoints { get; set; }

        /// <summary>
        /// Start time of fitting data set
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of fitting data set
        /// </summary>
        public DateTime EndTime { get; set; }


        /// <summary>
        /// The value of the R2 or root mean square of fitting,higher is better (used to choose among models)
        /// <para>
        /// This is the R-squared of the "differences" sum(ymeas[k]-ymeas[k-1] -(ymod[k]-ymod[k-1]) )
        /// </para>>
        /// </summary>
        public double RsqDiff { get; set; }

        /// <summary>
        /// The value of the objective function during fitting, lower is better(used to choose among models)
        /// <para>
        /// This is the R-squared of the "differences"  sum(ymeas[k]-ymeas[k-1] -(ymod[k]-ymod[k-1]) )
        /// </para>>
        /// </summary>
        public double ObjFunValDiff { get; set; }

        /// <summary>
        /// The time base of the fitting dataset (model can still be run on other timebases)
        /// </summary>

        public double TimeBase_s;


        /// <summary>
        /// The minimum value of u seen in the data set
        /// </summary>
        public double[] Umin;

        /// <summary>
        /// The maximum value of u seen in the data set
        /// </summary>
        public double[] Umax;

        /// <summary>
        /// A string containting detailed output of the solver, may include line-breaks
        /// </summary>
        public string SolverOutput;

        /// <summary>
        /// Counter of how many times the simulator has re-started over the course of the dataset due to periods of bad data
        /// </summary>

        public int NumSimulatorRestarts = 0;


        /// <summary>
        /// NB! this code seems to have an error with negative rsqdiff for cases when there yIndicesToIgnore is not empty.
        /// It may be preferable to use the output of the regression, as this avoids duplicating logic.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="yIndicesToIgnore"></param>

        public void CalcCommonFitMetricsFromYmeasDataset(UnitDataSet dataSet, List<int> yIndicesToIgnore= null)
        {
            if (yIndicesToIgnore == null)
            {
                yIndicesToIgnore = dataSet.IndicesToIgnore;
            }

            Vec vec = new Vec(dataSet.BadDataID);

            var ymeas_diff = vec.Diff(dataSet.Y_meas, yIndicesToIgnore);
            var ysim_diff = vec.Diff(dataSet.Y_sim, yIndicesToIgnore);

            var ymeas_vals = vec.GetValues(dataSet.Y_meas, yIndicesToIgnore);
            var ysim_vals = vec.GetValues(dataSet.Y_sim, yIndicesToIgnore);

            // if fitting against a value that is itself simulated, the vector may have different
            // size by one, and this may cause RSquared to return NAN if not caught.
            if (ymeas_vals.Length +1 == ysim_vals.Length )
            {
                ysim_vals = Vec<double>.SubArray(ysim_vals, 1);
            }
            if (ymeas_diff.Length +1 == ysim_diff.Length )
            {
                ysim_diff = Vec<double>.SubArray(ysim_diff, 1);
            }
            this.RsqDiff = SignificantDigits.Format(vec.RSquared(ymeas_diff, ysim_diff) * 100, nDigits);

            // objective function " average of absolute model deviation
            var avgErrorObj = vec.Mean(vec.Abs(vec.Subtract(ymeas_vals, ysim_vals)), yIndicesToIgnore);

            // "objective function" average of absolute diffs of model deviation
            var diffObj = vec.Mean(vec.Abs(vec.Diff(vec.Subtract(ymeas_vals, ysim_vals), yIndicesToIgnore)));
            if (diffObj.HasValue)
            {
                this.ObjFunValDiff = SignificantDigits.Format(diffObj.Value, nDigits);
            }
            else
            {
                this.ObjFunValDiff = Double.NaN;
            }
            if (yIndicesToIgnore != null)
            {
                this.NFittingBadDataPoints = yIndicesToIgnore.Count;
            }

            var fitScore = FitScoreCalculator.Calc(ymeas_vals, ysim_vals, dataSet.BadDataID, yIndicesToIgnore );
            this.FitScorePrc = SignificantDigits.Format(fitScore, nDigits);
            this.TimeBase_s = dataSet.GetTimeBase();

        }
    }
}
