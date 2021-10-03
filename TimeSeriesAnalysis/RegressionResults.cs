using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Class that holds the results of a regression run.
    /// </summary>
    public class RegressionResults
    {
        public RegressionResults()
        {
            ableToIdentify = false;
            Rsq = 0;
            objectiveFunctionValue = Double.PositiveInfinity;
            param95prcConfInterval = null;
            Y_modelled = null;
            varCovarMatrix = null;// new double[thetaLength][];
        }
        /// <summary>
        /// R2-root-means-squared between Y and Y_modelled for the tuning dataset(a value between 0 and 100, higher is better)
        /// </summary>
        public double Rsq { get; set; }
        public double objectiveFunctionValue { get; set; }
        public double[] param { get; set; }
        public double[] param95prcConfInterval { get; set; }
        public double[][] varCovarMatrix { get; set; }
        public double[] Y_modelled { get; set; }
        public double Bias { get; set; }
        public double[] Gains { get; set; }

        public bool ableToIdentify { get; set; }

    }
}
