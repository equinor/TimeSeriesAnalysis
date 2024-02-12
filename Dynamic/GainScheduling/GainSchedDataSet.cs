using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// The data for a porition of a process, containg only one output and one or multiple inputs that influence it
    /// </summary>
    public class GainSchedDataSet
    {
        /// <summary>
        /// list of warings during identification
        /// </summary>
        public List<GainSchedWarnings> Warnings { get; set; }
        /// <summary>
        /// Name
        /// </summary>
        public string ProcessName { get; }
        /// <summary>
        /// Timestamps 
        /// </summary>
        public DateTime[] Times { get; set; }
        /// <summary>
        /// Output Y (measured)
        /// </summary>
        public double[] Y_meas { get; set; }
        /// <summary>
        /// Output Y (simulated)
        /// </summary>
        public double[] Y_sim { get; set; }

        /// <summary>
        /// Input U(simulated) - in the case of PID-control
        /// </summary>
        public double[,] U_sim { get; set; }

        /// <summary>
        /// Setpoint - (if sub-process includes a PID-controller)
        /// </summary>
        public double[] Y_setpoint { get; set; } = null;

        /// <summary>
        /// Additve output disturbance D (Y = X+ D)
        /// </summary>
        public double[] D { get; set; }

        /// <summary>
        /// Input U (given)
        /// </summary>
        public double[,] U { get; set; }

        /// <summary>
        /// Indices that are ignored in Y during fitting.
        /// </summary>
        public List<int> IndicesToIgnore = null;

        /// <summary>
        /// Some systems for storing data do not support "NaN", but instead some other magic 
        /// value is reserved for indicating that a value is bad or missing. 
        /// </summary>
        public double BadDataID { get; set; } = -9999;


        /// <summary>
        /// Constructor for data set without inputs - for "autonomous" processes such as sinusoids, 
        /// rand walks or other disturbancs.
        /// </summary>
        /// <param name="name">optional internal name of dataset</param>
        public GainSchedDataSet(string name = null)
        {
            this.Warnings = new List<GainSchedWarnings>();
            this.Y_meas = null;
            this.U = null;
            this.ProcessName = name;
        }

        /// <summary>
        /// Create a copy of an existing data set
        /// </summary>
        /// <param name="otherDataSet"></param>
        public GainSchedDataSet(GainSchedDataSet otherDataSet)
        {
            this.ProcessName = otherDataSet.ProcessName + "copy";

            if (otherDataSet.Y_meas == null)
                this.Y_meas = null;
            else
                this.Y_meas = otherDataSet.Y_meas.Clone() as double[];
            if (otherDataSet.Y_setpoint == null)
                this.Y_setpoint = null;
            else
                this.Y_setpoint = otherDataSet.Y_setpoint.Clone() as double[];

            if (otherDataSet.Y_sim == null)
            {
                this.Y_sim = null;
            }
            else
            {
                this.Y_sim = otherDataSet.Y_sim.Clone() as double[];
            }
            if (otherDataSet.U == null)
                this.U = null;
            else
                this.U = otherDataSet.U.Clone() as double[,];
            if (otherDataSet.U_sim == null)
                this.U_sim = null;
            else
                this.U_sim = otherDataSet.U_sim.Clone() as double[,];
            if (otherDataSet.Times == null)
                this.Times = null;
            else
                this.Times = otherDataSet.Times.Clone() as DateTime[];
            if (otherDataSet.D == null)
                this.D = null;
            else
                this.D = otherDataSet.D.Clone() as double[];

            if (otherDataSet.IndicesToIgnore == null)
                this.IndicesToIgnore = null;
            else
                this.IndicesToIgnore = new List<int>(otherDataSet.IndicesToIgnore);

            this.BadDataID = otherDataSet.BadDataID;
        }

        /// <summary>
        /// Create a downsampled copy of an existing data set
        /// </summary>
        /// <param name="originalDataSet"></param>
        /// <param name="downsampleFactor">factor by which to downsample the original dataset</param>
        public GainSchedDataSet(GainSchedDataSet originalDataSet, int downsampleFactor)
        {
            this.ProcessName = originalDataSet.ProcessName + "downsampledFactor" + downsampleFactor;

            this.Y_meas = Vec<double>.Downsample(originalDataSet.Y_meas, downsampleFactor);
            this.Y_setpoint = Vec<double>.Downsample(originalDataSet.Y_setpoint, downsampleFactor); 
            this.Y_sim = Vec<double>.Downsample(originalDataSet.Y_sim, downsampleFactor); 
            this.U = Array2D<double>.Downsample(originalDataSet.U, downsampleFactor);
            this.U_sim = Array2D<double>.Downsample(originalDataSet.U_sim, downsampleFactor); 
            this.Times = Vec<DateTime>.Downsample(originalDataSet.Times, downsampleFactor); 
        }

        /// <summary>
        /// Get the number of data points
        /// </summary>
        /// <returns></returns>
        public int GetNumDataPoints ()
        {
            if (U != null)
                return U.GetNRows();
            else if (Times != null)
                return Times.Length;
            else if (Y_meas != null)
                return Y_meas.Length;
            else if (Y_setpoint!= null)
                return Y_setpoint.Length;
            else
                return 0;
        }

        /// Tags indices to be removed if either of the output is outside the range defined by 
        /// [Y_min,Y_max], an input is outside [u_min, umax] or if any data matches badDataId
        /// 
        public void DetermineIndicesToIgnore(FittingSpecs fittingSpecs)
        {
            if (fittingSpecs == null)
            {
                return;
            }
            var newIndToExclude = new List<int>();
            var vec = new Vec();

            // find values below minimum for each input
            if (fittingSpecs.Y_min_fit.HasValue)
            {
                if (!Double.IsNaN(fittingSpecs.Y_min_fit.Value) && fittingSpecs.Y_min_fit.Value != BadDataID 
                    && !Double.IsNegativeInfinity(fittingSpecs.Y_min_fit.Value))
                {
                    var indices =
                        vec.FindValues(Y_meas, fittingSpecs.Y_min_fit.Value, VectorFindValueType.SmallerThan, IndicesToIgnore);
                    newIndToExclude.AddRange(indices);
                }
            }
            if (fittingSpecs.Y_max_fit.HasValue)
            {
                if (!Double.IsNaN(fittingSpecs.Y_max_fit.Value) && fittingSpecs.Y_max_fit.Value != BadDataID 
                    && !Double.IsPositiveInfinity(fittingSpecs.Y_max_fit.Value))
                {
                    var indices =
                        vec.FindValues(Y_meas, fittingSpecs.Y_max_fit.Value, VectorFindValueType.BiggerThan, IndicesToIgnore);
                    newIndToExclude.AddRange(indices);
                }
            }
            // find values below minimum for each input
            if (fittingSpecs.U_min_fit != null)
            {
                for (int idx = 0; idx < Math.Min(fittingSpecs.U_min_fit.Length, U.GetNColumns()); idx++)
                {
                    if (Double.IsNaN(fittingSpecs.U_min_fit[idx]) || fittingSpecs.U_min_fit[idx] == BadDataID 
                        || Double.IsNegativeInfinity(fittingSpecs.U_min_fit[idx]))
                        continue;
                    var indices =
                        vec.FindValues(U.GetColumn(idx), fittingSpecs.U_min_fit[idx], VectorFindValueType.SmallerThan, IndicesToIgnore);
                    newIndToExclude.AddRange(indices);
                }
            }
            if (fittingSpecs.U_max_fit != null)
            {
                for (int idx = 0; idx < Math.Min(fittingSpecs.U_max_fit.Length, U.GetNColumns()); idx++)
                {
                    if (Double.IsNaN(fittingSpecs.U_max_fit[idx]) || fittingSpecs.U_max_fit[idx] == BadDataID 
                        || Double.IsNegativeInfinity(fittingSpecs.U_max_fit[idx]))
                        continue;
                    var indices =
                        vec.FindValues(U.GetColumn(idx), fittingSpecs.U_max_fit[idx], 
                        VectorFindValueType.BiggerThan, IndicesToIgnore);
                    newIndToExclude.AddRange(indices);
                }
            }
            if (newIndToExclude.Count > 0)
            {
                var result = Vec<int>.Sort(newIndToExclude.ToArray(), VectorSortType.Ascending);
                newIndToExclude = result.ToList();
                var newIndToExcludeDistinct = newIndToExclude.Distinct();
                newIndToExclude = newIndToExcludeDistinct.ToList();
            }

            if (IndicesToIgnore != null)
            {
                if (newIndToExclude.Count > 0)
                {
                    IndicesToIgnore.AddRange(newIndToExclude);
                }
            }
            else
            {
                IndicesToIgnore = newIndToExclude;
            }
        }


        /// <summary>
        /// Tags indices to be removed if either of the inputs are outside the range defined by 
        /// [uMinFit,uMaxFit].
        /// 
        /// uMinFit,uMaxFit may include NaN or BadDataID for values if no max/min applies to the specific input
        /// </summary>
        /// <param name="uMinFit">vector of minimum values for each element in U</param>
        /// <param name="uMaxFit">vector of maximum values for each element in U</param>
        public void SetInputUFitMaxAndMin(double[] uMinFit, double[] uMaxFit)
        {
            if ((uMinFit == null && uMaxFit == null) || this.U == null)
                return;

            var newIndToExclude = new List<int>();
            var vec = new Vec();
            // find values below minimum for each input
            if (uMinFit != null)
            {
                for (int idx = 0; idx < Math.Min(uMinFit.Length,U.GetNColumns()); idx++)
                {
                    if (Double.IsNaN(uMinFit[idx]) || uMinFit[idx] == BadDataID || Double.IsNegativeInfinity(uMinFit[idx]))
                        continue;
                    var indices = 
                        vec.FindValues(U.GetColumn(idx), uMinFit[idx], VectorFindValueType.SmallerThan, IndicesToIgnore);
                    newIndToExclude.AddRange(indices);
                }
            }
            if (uMaxFit != null)
            {
                for (int idx = 0; idx < Math.Min(uMaxFit.Length, U.GetNColumns()); idx++)
                {
                    if (Double.IsNaN(uMaxFit[idx]) || uMaxFit[idx] == BadDataID || Double.IsNegativeInfinity(uMaxFit[idx]))
                        continue;
                    var indices =
                        vec.FindValues(U.GetColumn(idx), uMaxFit[idx], VectorFindValueType.BiggerThan, IndicesToIgnore);
                    newIndToExclude.AddRange(indices);
                }
            }
            if (newIndToExclude.Count > 0)
            {
                var result  = Vec<int>.Sort(newIndToExclude.ToArray(), VectorSortType.Ascending);
                newIndToExclude = result.ToList();
                var newIndToExcludeDistinct = newIndToExclude.Distinct();
                newIndToExclude = newIndToExcludeDistinct.ToList();
            }

            if (IndicesToIgnore != null)
            {
                if (newIndToExclude.Count > 0)
                {
                    IndicesToIgnore.AddRange(newIndToExclude);
                }
            }
            IndicesToIgnore = newIndToExclude;
        }

        /// <summary>
        /// Gets the time between samples in seconds, returns  zero if times are not set
        /// </summary>
        /// <returns></returns>
        public double GetTimeBase()
        {
            if (Times != null)
            {
                if (Times.Length > 2)
                    return (Times.Last() - Times.First()).TotalSeconds / (Times.Length - 1);
                else
                    return 0;
            }
            return 0;
        }

        /// <summary>
        /// Create a dataset for single-input system from two signals that have separate but overlapping
        /// time-series(each given as value-date tuples)
        /// </summary>
        /// <param name="u">tuple of values and dates describing u</param>
        /// <param name="y_meas">tuple of values and dates describing y</param>
        /// <param name="name">name of dataset</param>
        public GainSchedDataSet((double[], DateTime[]) u, (double[], DateTime[]) y_meas, string name = null/*,
            int? timeBase_s=null*/)
        {
            var jointTime = Vec<DateTime>.Intersect(u.Item2.ToList(),y_meas.Item2.ToList());
            var indU = Vec<DateTime>.GetIndicesOfValues(u.Item2.ToList(), jointTime);
            var indY = Vec<DateTime>.GetIndicesOfValues(y_meas.Item2.ToList(), jointTime);
            this.Times = jointTime.ToArray();

            this.Y_meas = Vec<double>.GetValuesAtIndices(y_meas.Item1,indY);
            var newU = Vec<double>.GetValuesAtIndices(u.Item1, indU);
            this.U = Array2D<double>.CreateFromList(new List<double[]> { newU });
            this.ProcessName = name;
        }

        /// <summary>
        /// Get the time spanned by the dataset
        /// </summary>
        /// <returns>The time spanned by the dataset, or null if times are not set</returns>
        public TimeSpan GetTimeSpan()
        {
            if (this.Times == null)
            {
                return new TimeSpan();
            }
            if (this.Times.Length == 0)
            {
                return new TimeSpan();
            }
            return Times.Last() - Times.First();
        }
        /// <summary>
        /// Get the average value of each input in the dataset. 
        /// This is useful when defining model local around a working point.
        /// </summary>
        /// <returns>an array of averages, each corrsponding to one column of U. 
        /// Returns null if it was not possible to calculate averages</returns>
        public double[] GetAverageU()
        {
            if (U == null)
            {
                return null;
            }
            List<double> averages = new List<double>();

            for (int i = 0; i < U.GetNColumns(); i++)
            {
                double? avg = (new Vec(BadDataID)).Mean(U.GetColumn(i));
                if (!avg.HasValue)
                    return null;
                averages.Add(avg.Value);
            }
            return averages.ToArray();
        }


    }
}
