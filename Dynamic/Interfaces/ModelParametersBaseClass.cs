using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Abstract base class for ISimulatableModel classes
    /// </summary>
    abstract public class ModelParametersBaseClass
    {
        [JsonInclude]
        [JsonProperty("Fitting")]
        public FittingInfo Fitting { get  ; internal set; }

    }

    public class FittingInfo
    {

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
            public double RsqFittingDiff { get; set; }

            /// <summary>
            /// The value of the R2 or root mean square
            /// <para>
            /// This is the R-squared of the "absolute" sum(ymeas[k] - ymod[k] )
            /// </para>>
            /// </summary>
            public double RsqFittingAbs { get; set; }

            /// <summary>
            /// The value of the objective function during fitting, lower is better(used to choose among models)
            /// <para>
            /// This is the R-squared of the "differences"  sum(ymeas[k]-ymeas[k-1] -(ymod[k]-ymod[k-1]) )
            /// </para>>
            /// </summary>
            public double ObjFunValFittingDiff { get; set; }

            /// <summary>
            /// The value of the objective function during fitting, lower is better(used to choose among models)
            /// <para>
            /// This is the R-squared of the "absolute" sum(ymeas[k]-ymod[k-1])
            /// </para>>
            /// </summary>

            public double ObjFunValFittingAbs { get; set; }

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
    }


}
