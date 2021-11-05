using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Abstract base class for ISimulatableModel classes
    /// </summary>
    abstract public class ModelParametersBaseClass
    {
        // TODO:add isfitted? some models are not fitte and then not everything here makes sense
        /// <summary>
        /// True if identification was able to identify, otherwise false.
        /// </summary>
        public bool WasAbleToIdentify { get; set; }
        /// <summary>
        /// The value of the R2 or root mean square of fitting,higher is better (used to choose among models)
        /// </summary>
        public double FittingRsq { get; set; }

        /// <summary>
        /// The value of the objective function during fitting, lower is better(used to choose among models)
        /// </summary>
        public double FittingObjFunVal { get; set; }

        /// <summary>
        /// Number of bad data points ignored during fitting
        /// </summary>
        public double NFittingBadDataPoints { get; set; }

        /// <summary>
        /// Number of total data points (good and bad) available for fitting
        /// </summary>
        public double NFittingTotalDataPoints { get; set; }

        public double GetFittingR2()
        {
            return FittingRsq;
        }

        public double GetFittingObjFunVal()
        {
            return FittingObjFunVal;
        }

        public bool AbleToIdentify()
        {
            return WasAbleToIdentify;
        }



    }
}
