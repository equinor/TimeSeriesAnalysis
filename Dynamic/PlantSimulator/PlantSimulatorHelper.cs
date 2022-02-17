using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace TimeSeriesAnalysis.Dynamic
{
    public class PlantSimulatorHelper
    {
        /// <summary>
        /// Re-construct a PlantSimulator object from the json-code created by this class' .Serialize()
        /// </summary>
        /// <param name="serializedPlantSimulatorJson"></param>
        static public PlantSimulator LoadFromJson(string serializedPlantSimulatorJson)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;

            return JsonConvert.DeserializeObject<PlantSimulator>(serializedPlantSimulatorJson, settings);
        }
    }
}
