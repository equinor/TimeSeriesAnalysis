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
        /// <summary>
        /// The fitting information of the model, set during identification
        /// </summary>
        [JsonInclude]
        [JsonProperty("Fitting")]
        public FittingInfo Fitting { get  ; internal set; }

    }

}
