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
            // todo: need to write jsonconverter to handle interface objects
            // https://www.c-sharpcorner.com/UploadFile/20c06b/deserializing-interface-properties-with-json-net/

            return JsonConvert.DeserializeObject<PlantSimulator>(serializedPlantSimulatorJson);
        }
    }
}
