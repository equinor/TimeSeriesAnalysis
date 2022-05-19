using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


using Newtonsoft.Json;

namespace TimeSeriesAnalysis.Dynamic
{
    public class PlantSimulatorHelper
    {
        /// <summary>
        /// Re-construct a PlantSimulator object from the json-code created by this class' .Serialize()
        /// </summary>
        /// <param name="serializedPlantSimulatorJson"></param>
        static public PlantSimulator LoadFromJsonTxt(string serializedPlantSimulatorJson)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;

            return JsonConvert.DeserializeObject<PlantSimulator>(serializedPlantSimulatorJson, settings);
        }

        /// <summary>
        /// Re-construct a PlantSimulator object from the json-code created by this class' .Serialize()
        /// </summary>
        /// <param name="serializedPlantSimulatorJson"></param>
        static public PlantSimulator LoadFromJsonFile(string fileName)
        {
            var serializedPlantSimulatorJson  = File.ReadAllText(fileName);

            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;

            return JsonConvert.DeserializeObject<PlantSimulator>(serializedPlantSimulatorJson, settings);
        }

    }
}
