using Accord.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// The data for a porition of a process, containg only one output and one or multiple inputs that influence it
    /// </summary>
    public class UnitDataSet
    {
        /// <summary>
        /// list of warings during identification
        /// </summary>
        public List<UnitWarnings> Warnings { get; set; }
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
        /// Output Y(simulated)
        /// </summary>
        public double[] Y_sim { get; set; }

        /// <summary>
        /// The simulated output of the process, this is a non-measured variable that excludes the disturbance
        /// </summary>
        public double[] Y_proc { get; set; }

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
        public UnitDataSet(string name = null)
        {
            this.Warnings = new List<UnitWarnings>();
            this.Y_meas = null;
            this.U = null;
            this.ProcessName = name;
        }

        /// <summary>
        /// Create a copy of an existing data set
        /// </summary>
        /// <param name="otherDataSet"></param>
        public UnitDataSet(UnitDataSet otherDataSet)
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
        /// <param name="keyIndex">index around which to center the downsampling</param>
        public UnitDataSet(UnitDataSet originalDataSet, double downsampleFactor, int keyIndex=0)
        {
            this.ProcessName = originalDataSet.ProcessName + "downsampledFactor" + downsampleFactor;

            this.Y_meas = Vec<double>.Downsample(originalDataSet.Y_meas, downsampleFactor, keyIndex);
            this.Y_setpoint = Vec<double>.Downsample(originalDataSet.Y_setpoint, downsampleFactor, keyIndex);
            this.Y_sim = Vec<double>.Downsample(originalDataSet.Y_sim, downsampleFactor, keyIndex);
            this.U = Array2D<double>.Downsample(originalDataSet.U, downsampleFactor, keyIndex);
            this.U_sim = Array2D<double>.Downsample(originalDataSet.U_sim, downsampleFactor, keyIndex);
            this.Times = Vec<DateTime>.Downsample(originalDataSet.Times, downsampleFactor, keyIndex);
        }

        /// <summary>
        /// Create a dataset for single-input system from two signals that have separate but overlapping
        /// time-series(each given as value-date tuples)
        /// </summary>
        /// <param name="u">tuple of values and dates describing u</param>
        /// <param name="y_meas">tuple of values and dates describing y</param>
        /// <param name="name">name of dataset</param>
        public UnitDataSet((double[], DateTime[]) u, (double[], DateTime[]) y_meas, string name = null)
        {
            var jointTime = Vec<DateTime>.Intersect(u.Item2.ToList(), y_meas.Item2.ToList());
            var indU = Vec<DateTime>.GetIndicesOfValues(u.Item2.ToList(), jointTime);
            var indY = Vec<DateTime>.GetIndicesOfValues(y_meas.Item2.ToList(), jointTime);
            this.Times = jointTime.ToArray();

            this.Y_meas = Vec<double>.GetValuesAtIndices(y_meas.Item1, indY);
            var newU = Vec<double>.GetValuesAtIndices(u.Item1, indU);
            this.U = Array2D<double>.CreateFromList(new List<double[]> { newU });
            this.ProcessName = name;
        }

        /// <summary>
        /// Appends/concatenate another dataset to the end of this one
        /// </summary>
        /// <param name="otherDataSet"></param>
        public bool Concat(UnitDataSet otherDataSet)
        {
            if (otherDataSet.U != null && U != null)
            { 
                if (otherDataSet.U.GetNColumns() != U.GetNColumns())
                return false;
            }
            if (otherDataSet.Y_meas != null)
                this.Y_meas = Vec<double>.Concat(this.Y_meas, otherDataSet.Y_meas);    
            if (otherDataSet.Times != null)
                this.Times = Vec<DateTime>.Concat(this.Times, otherDataSet.Times);
            if (otherDataSet.Y_setpoint != null)
                this.Y_setpoint = Vec<double>.Concat(this.Y_setpoint, otherDataSet.Y_setpoint);
            if (otherDataSet.Y_sim != null)
                this.Y_sim = Vec<double>.Concat(this.Y_sim, otherDataSet.Y_sim);
            if (otherDataSet.D != null)
                this.D = Vec<double>.Concat(this.D, otherDataSet.D);
            if (otherDataSet.IndicesToIgnore != null)
            {
                if (IndicesToIgnore == null) 
                    IndicesToIgnore = new List<int>(otherDataSet.IndicesToIgnore);
                else
                    IndicesToIgnore.AddRange(otherDataSet.IndicesToIgnore);
            }
            if (otherDataSet.U != null)
            {
                var uList = new List<double[]>();
                for (int idx = 0; idx < U.GetNColumns(); idx++)
                {
                    var vec = Array2D<double>.GetColumn(U, idx);
                    var vec2 = Array2D<double>.GetColumn(otherDataSet.U, idx);
                    uList.Add(Vec<double>.Concat(vec,vec2) );
                }
                U = Array2D<double>.CreateFromList(uList);
            }
            return true;
        }





        /// <summary>
        /// If data is already given to object, this method will fill out the timstamps member
        /// </summary>
        /// <param name="timeBase_s">the time in seconds bewteen time samples</param>
        /// <param name="t0">the date of the first datapoint in the dataset</param>
        public void CreateTimeStamps(double timeBase_s, DateTime? t0 = null)
        {
            if (t0 == null)
            {
                t0 = new DateTime(2010, 1, 1);//intended for testing
            }

            var times = new List<DateTime>();
            //times.Add(t0.Value);
            DateTime time = t0.Value;
            for (int i = 0; i < GetNumDataPoints(); i++)
            {
                times.Add(time);
                time = time.AddSeconds(timeBase_s);
            }
            this.Times = times.ToArray();
        }

        /// <summary>
        /// Tags indices to be removed if either of the output is outside the range defined by 
        /// [Y_min,Y_max], an input is outside [u_min, umax] or if any data matches badDataId
        /// 
        /// Results are stored in "IndicesToIgnore", not outputted.
        /// </summary>
        /// <param name="fittingSpecs"></param>
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
        /// Returns the number of datapoints in the dataset
        /// </summary>
        /// <returns></returns>
        public int GetNumDataPoints()
        {
            if (U != null)
                return U.GetNRows();
            else if (Times != null)
                return Times.Length;
            else if (Y_meas != null)
                return Y_meas.Length;
            else if (Y_setpoint != null)
                return Y_setpoint.Length;
            else
                return 0;
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

        /// <summary>
        /// Helper to set column of U to uValues. 
        /// </summary>
        /// <param name="uValues1">first input array</param>
        /// <param name="uValues2">second input array(can be null)</param>
        /// <param name="uValues3">third input array(can be null)</param>
        /// <param name="uValues4">third input array(can be null)</param>
        /// <param name="uValues5">third input array(can be null)</param>
        /// <param name="uValues6">third input array(can be null)</param>
        /// <returns>true if succesful, otherwise false</returns>
        public bool SetU(double[] uValues1, double[] uValues2 = null, double[] uValues3 = null, double[] uValues4 = null,
            double[] uValues5 = null, double[] uValues6 = null)
        {
            var listOfUs = new List<double[]>();
            if (uValues1 == null)
                return false;

            listOfUs.Add(uValues1);
            if (uValues2 != null)
                listOfUs.Add(uValues2);
            if (uValues3 != null)
                listOfUs.Add(uValues3);
            if (uValues4 != null)
                listOfUs.Add(uValues4);
            if (uValues5 != null)
                listOfUs.Add(uValues5);
            if (uValues6 != null)
                listOfUs.Add(uValues6);
            U = Array2D<double>.CreateFromList(listOfUs);
            return true;
        }


        /// <summary>
        /// Create a subset of the dataset that is defined in terms of percentages
        /// </summary>
        /// <param name="startPrc">A value 0-100%. Should be a value smaller than endPrc </param>
        /// <param name="endPrc">A value 0-100%. Should be a value bigger than startPrc</param>
        /// <returns></returns>
        public UnitDataSet SubsetPrc(double startPrc, double endPrc)
        {
            if (startPrc > 100)
                startPrc = 100;
            if (endPrc > 100)
                endPrc = 100;
            if (startPrc < 0)
                startPrc = 0;
            if (endPrc < 0)
                endPrc = 0;

            int N = GetNumDataPoints();
            if (N == 0)
                return new UnitDataSet();

            int startInd = (int)Math.Floor(startPrc / 100 * N);
            int endInd = (int)Math.Floor(endPrc / 100 * N);
            return SubsetInd(startInd, endInd);
        }

        /// <summary>
        /// Create a copy of the data set that is a subset with given start/end indices
        /// </summary>
        /// <param name="startInd"></param>
        /// <param name="endInd"></param>
        /// <returns></returns>
        public UnitDataSet SubsetInd(int startInd, int endInd)
        {
            int N = GetNumDataPoints();
            if (endInd > N - 1)
                endInd = N - 1;
            if (startInd < 0)
                startInd = 0;

            var returnData = new UnitDataSet();
            returnData.BadDataID = BadDataID;
            returnData.Times = Vec<DateTime>.SubArray(Times,startInd,endInd);
            var uList = new List<double[]>();
            for (int idx = 0; idx < U.GetNColumns(); idx++)
            {
                var vec = Array2D<double>.GetColumn(U,idx);
                uList.Add(Vec<double>.SubArray(vec, startInd, endInd));
            }
            returnData.U = Array2D<double>.CreateFromList(uList);
            returnData.Y_meas = Vec<double>.SubArray(Y_meas, startInd, endInd);
            returnData.Y_sim = Vec<double>.SubArray(Y_sim, startInd, endInd);
            returnData.D = Vec<double>.SubArray(D, startInd, endInd);
            return returnData;
        }




    }
}
