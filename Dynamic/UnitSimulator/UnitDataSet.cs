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
    public class UnitDataSet
    {
        /// <summary>
        /// list of warings during identification
        /// </summary>
        public List<UnitWarnings> warnings{ get; set; } 
        /// <summary>
        /// Name
        /// </summary>
        public  string ProcessName { get;}
        /// <summary>
        /// Timestamps 
        /// </summary>
        public DateTime[] Times { get; }
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
        /// The number of data points 
        /// </summary>
        public int NumDataPoints { get; }

        /// <summary>
        /// The sampling time
        /// </summary>
        public double TimeBase_s { get; set; }

        /// <summary>
        /// The time stamp of the start of the dataset
        /// </summary>
        public DateTime t0;

        /// <summary>
        /// Some systems for storing data do not support "NaN", but instead some other magic 
        /// value is reserved for indicating that a value is bad or missing. 
        /// </summary>
        public double  BadDataID{ get; set; } = -9999;

    /// <summary>
    /// Constructor for data set without inputs - for "autonomous" processes such as sinusoids, 
    /// rand walks or other disturbancs.
    /// </summary>
    /// <param name="timeBase_s">the time base in seconds</param>
    /// <param name="numDataPoints">the desired nubmer of datapoints of the dataset</param>
    /// <param name="name">optional internal name of dataset</param>
    public UnitDataSet(double timeBase_s, int numDataPoints, string name = null)
        {
            this.warnings = new List<UnitWarnings>(); 
            this.NumDataPoints = numDataPoints;
            this.Y_meas = null;
            this.U = null;
            this.TimeBase_s = timeBase_s;
            this.ProcessName = name;
        }

        /// <summary>
        /// Constructor for data set with inputs <c>U</c>, i.e. where a relationship 
        /// that at least partially explains <c>y_meas</c> is konwn
        /// </summary>
        /// <param name="timeBase_s">the time base in seconds</param>
        /// <param name="U">The number of rows of the 2D-array U determines the duration dataset</param>
        /// <param name="y_meas">the measured output of the system, can be null </param>
        /// <param name="name">optional internal name of dataset</param>
        public UnitDataSet(double timeBase_s, double[,] U, double[] y_meas= null, string name=null)
        {
            this.Y_meas = y_meas;
            NumDataPoints = U.GetNRows();
            this.U = U;
            this.TimeBase_s = timeBase_s;
            this.ProcessName = name;
        }

        /// <summary>
        /// Constructor for when data set only has a single inut u
        /// </summary>
        /// <param name="timeBase_s"></param>
        /// <param name="u"></param>
        /// <param name="y_meas"></param>
        /// <param name="name"></param>
        public UnitDataSet(double timeBase_s, double[] u, double[] y_meas = null, string name = null)
        {
            this.Y_meas = y_meas;
            NumDataPoints = u.Count();
            this.U = Array2D<double>.InitFromColumnList(new List<double[]> { u });
            this.TimeBase_s = timeBase_s;
            this.ProcessName = name;
        }

        /// <summary>
        /// Create a dataset for single-input system from two signals that have separate but overlapping
        /// time-series(each given as value-date tuples)
        /// </summary>
        /// <param name="u">tuple of values and dates describing u</param>
        /// <param name="y_meas">tuple of values and dates describing y</param>
        /// <param name="name">name of dataset</param>
        /// <param name="timeBase_s">optinally, hard code the timebase in seconds</param>
        public UnitDataSet((double[], DateTime[]) u, (double[], DateTime[]) y_meas, string name = null,
            int? timeBase_s=null)
        {
            var jointTime = Vec<DateTime>.Intersect(u.Item2.ToList(),y_meas.Item2.ToList());
            var indU = Vec<DateTime>.GetIndicesOfValues(u.Item2.ToList(), jointTime);
            var indY = Vec<DateTime>.GetIndicesOfValues(y_meas.Item2.ToList(), jointTime);
            this.Times = jointTime.ToArray();
            this.NumDataPoints = jointTime.Count();
            if (timeBase_s.HasValue)
            {
                TimeBase_s = timeBase_s.Value;
            }
            else
            {
                TimeBase_s = (jointTime.Last() - jointTime.First()).TotalSeconds / (jointTime.Count-1);
            }

            this.Y_meas = Vec<double>.GetValuesAtIndices(y_meas.Item1,indY);
            var newU = Vec<double>.GetValuesAtIndices(u.Item1, indU);
            this.U = Array2D<double>.InitFromColumnList(new List<double[]> { newU });
            this.ProcessName = name;
        }

        /// <summary>
        /// Get the time spanned by the dataset
        /// </summary>
        /// <returns>The time spanned by the dataset</returns>
        public TimeSpan GetTimeSpan()
        {
            if (Times == null)
            {
                return new TimeSpan(0, 0, (int)Math.Ceiling((double)NumDataPoints * TimeBase_s));
            }
            else
            {
                return Times.Last() - Times.First();
            }
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
                double? avg = (new Vec()).Mean(U.GetColumn(i));
                if (!avg.HasValue)
                    return null;
                averages.Add(avg.Value);
            }
            return averages.ToArray();
        }


    }
}
