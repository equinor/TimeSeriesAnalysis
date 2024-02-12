using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Warnings related to regression
    /// </summary>
    public enum RegressionWarnings
    {
        /// <summary>
        /// No warnings
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Input matrix is rank deficient
        /// </summary>
        InputMatrixIsRankDeficient = 1,
        /// <summary>
        /// Input matrix has a constant input, a type of rank deficiency
        /// </summary>
        InputMatrixHasConstantInput =2 
    }


    /// <summary>
    /// Class that holds the results of a run of <c>Vec.Regress</c>.
    /// </summary>
    public class RegressionResults
    {
        /// <summary>
        /// Default constructor, sets all values to null or zero.
        /// </summary>
        public RegressionResults()
        {
            AbleToIdentify = false;
            Rsq = 0;
            ObjectiveFunctionValue = Double.PositiveInfinity;
            Param95prcConfidence = null;
            Y_modelled = null;
            VarCovarMatrix = null;
            NfittingBadDataPoints = 0;
            RegressionWarnings = new List<RegressionWarnings>();
        }
        /// <summary>
        /// R2-root-means-squared between Y and Y_modelled for the tuning dataset(a value between 0 and 100, higher is better)
        /// </summary>
        public double Rsq { get; set; }
        /// <summary>
        /// The value of the objective function after regression
        /// </summary>
        public double ObjectiveFunctionValue { get; set; }
        /// <summary>
        /// All regression parameters, first the gains, then the bias term.
        /// </summary>
        public double[] Param { get; set; }
        /// <summary>
        /// The 95 percent confidence  of parameters 
        /// The confidence interval will be Param +/- Param95prcConfidence
        /// </summary>
        public double[] Param95prcConfidence { get; set; }
        /// <summary>
        /// The variance/covariance matrix of the regression run
        /// </summary>
        public double[][] VarCovarMatrix { get; set; }
        /// <summary>
        /// The modelled output
        /// </summary>
        public double[] Y_modelled { get; set; }
        /// <summary>
        /// The bias term of the linear regression
        /// </summary>
        public double Bias { get; set; }
        /// <summary>
        /// The gains of the linear regression
        /// </summary>
        public double[] Gains { get; set; }

        /// <summary>
        /// True if able to identify, otherwise false
        /// </summary>
        public bool AbleToIdentify { get; set; }

        /// <summary>
        /// Number of bad data point ignored in the fitting data set
        /// </summary>
        public int NfittingBadDataPoints { get; set; }
        /// <summary>
        /// Total number of data points in the fitting data set
        /// </summary>
        public int NfittingTotalDataPoints { get; set; }

        /// <summary>
        /// Regression warnings
        /// </summary>
        public List<RegressionWarnings> RegressionWarnings { get; set; }



    }
}
