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
        /// </summary>
        public bool WasAbleToIdentify { get; set; }


        /// <summary>
        /// A string that identifies the solver that was used to find the model
        /// </summary>
        public string SolverID { get; set; }

        /// <summary>
        /// The value of the R2 or root mean square of fitting,higher is better (used to choose among models)
        /// <para>
        /// This is the R-squared of the "differences" sum(ymeas[k]-ymeas[k-1] -(ymod[k]-ymod[k-1]) )
        /// </para>>
        /// </summary>
        public double RsqDiff { get; set; }

        /// <summary>
        /// The value of the R2 or root mean square
        /// <para>
        /// This is the R-squared of the "absolute" sum(ymeas[k] - ymod[k] )
        /// </para>>
        /// </summary>
        public double RsqAbs { get; set; }

        /// <summary>
        /// The value of the objective function during fitting, lower is better(used to choose among models)
        /// <para>
        /// This is the R-squared of the "differences"  sum(ymeas[k]-ymeas[k-1] -(ymod[k]-ymod[k-1]) )
        /// </para>>
        /// </summary>
        public double ObjFunValDiff { get; set; }

        /// <summary>
        /// The value of the objective function during fitting, lower is better(used to choose among models)
        /// <para>
        /// This is the R-squared of the "absolute" sum(ymeas[k]-ymod[k-1])
        /// </para>>
        /// </summary>

        public double ObjFunValAbs { get; set; }

        /// <summary>
        /// Number of bad data points ignored during fitting
        /// </summary>
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


        /*
        public void CalcCommonFitMetricsFromDiffData(double RsqDiff, double objValFunDiff, UnitDataSet dataSet)
        {
            Vec vec = new Vec();
            var ymeas_diff = vec.Diff(dataSet.Y_meas, dataSet.IndicesToIgnore);
            var ysim_diff = vec.Diff(dataSet.Y_sim, dataSet.IndicesToIgnore);
            this.ObjFunValDiff = SignificantDigits.Format(objValFunDiff, nDigits);
            this.RsqDiff = SignificantDigits.Format(vec.RSquared(ymeas_diff, ysim_diff) * 100, nDigits);
            this.ObjFunValDiff = SignificantDigits.Format(objValFunDiff, nDigits);
            this.ObjFunValAbs = vec.SumOfSquareErr(dataSet.Y_meas, dataSet.Y_sim, 0);
            this.ObjFunValAbs = SignificantDigits.Format(ObjFunValAbs, nDigits);
            this.RsqAbs = vec.RSquared(dataSet.Y_meas, dataSet.Y_sim, dataSet.IndicesToIgnore, 0) * 100;
            this.RsqAbs = SignificantDigits.Format(this.RsqAbs, nDigits);
        }

        public void CalcCommonFitMetricsFromDataset(double RsqAbs, double objValAbs, UnitDataSet dataSet)
        {
            Vec vec = new Vec();
            this.ObjFunValAbs = SignificantDigits.Format(objValAbs, nDigits);
              this.RsqAbs = SignificantDigits.Format(vec.RSquared(dataSet.Y_meas, dataSet.Y_sim, dataSet.IndicesToIgnore, 0) * 100, nDigits);
            var ymeas_diff = vec.Diff(dataSet.Y_meas, dataSet.IndicesToIgnore);
            var ysim_diff = vec.Diff(dataSet.Y_sim, dataSet.IndicesToIgnore);
            this.RsqDiff = SignificantDigits.Format(vec.RSquared(ymeas_diff, ysim_diff) * 100, nDigits);
            this.ObjFunValDiff = SignificantDigits.Format(
                vec.SumOfSquareErr(ymeas_diff, ysim_diff), nDigits);
        }*/

        public void CalcCommonFitMetricsFromDataset(UnitDataSet dataSet, List<int> yIndicesToIgnore)
        {
            Vec vec = new Vec(dataSet.BadDataID);


            var ymeas_diff = vec.Diff(dataSet.Y_meas, yIndicesToIgnore);
            var ysim_diff = vec.Diff(dataSet.Y_sim, yIndicesToIgnore);

            var ymeas_vals = vec.GetValues(dataSet.Y_meas, yIndicesToIgnore);
            var ysim_vals = vec.GetValues(dataSet.Y_sim, yIndicesToIgnore);

            // if fitting against a value that is itself simulated, the vector may have different
            // size by one, and this may cause RSquared to return NAN if not caught.
            if (ymeas_vals.Length == ysim_vals.Length + 1)
            {
                ymeas_vals.RemoveAt(0);
            }
            if (ymeas_diff.Length == ysim_diff.Length + 1)
            {
                ymeas_diff.RemoveAt(0);
            }

            this.RsqAbs = SignificantDigits.Format(vec.RSquared(ymeas_vals, ysim_vals) * 100, nDigits);

            this.RsqDiff = SignificantDigits.Format(vec.RSquared(ymeas_diff, ysim_diff) * 100, nDigits);

            // objective function " average of absolute model deviation
            //avgErrorObj = vec.SumOfSquareErr(ymeas_vals, ysim_vals);
            var avgErrorObj = vec.Mean(vec.Abs(vec.Subtract(ymeas_vals, ysim_vals)));
            if (avgErrorObj.HasValue)
            {
                this.ObjFunValAbs = SignificantDigits.Format(avgErrorObj.Value, nDigits); ;
            }
            else
            {
                this.ObjFunValAbs = Double.NaN;
            }

            // "objective function" average of absolute diffs of model deviation
            //  var diffObj = vec.SumOfSquareErr(ymeas_diff, ysim_diff)
            var diffObj = vec.Mean(vec.Abs(vec.Diff(vec.Subtract(ymeas_vals, ysim_vals), dataSet.IndicesToIgnore)));
            if (diffObj.HasValue)
            {
                this.ObjFunValDiff = SignificantDigits.Format(diffObj.Value, nDigits);
            }
            else
            {
                this.ObjFunValDiff = Double.NaN;
            }

        }
    }
}
