using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace TimeSeriesAnalysis.Dynamic
{ 


    /// <summary>
    /// Deals with loading a PlantSimulator from file
    /// </summary>
    public class PlantSimulatorSerializer
    {
        private static JsonSerializerSettings SerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;
            settings.NullValueHandling = NullValueHandling.Include;
            return settings;
        }

        /// <summary>
        /// Re-construct a PlantSimulator object from the json-code created by this class' .Serialize()
        /// </summary>
        /// <param name="serializedPlantSimulatorJson"></param>
        static public PlantSimulator LoadFromJsonTxt(string serializedPlantSimulatorJson)
        {

            var settings = SerializationSettings();
            PlantSimulator obj =  (PlantSimulator)JsonConvert.DeserializeObject(serializedPlantSimulatorJson, typeof(PlantSimulator),settings);

            if (obj == null)
                return obj;

            // workaround for plantName and plantDescription begin null
            if (obj.plantDescription == null)
            {
                string pattern = "\"plantDescription\"\\s?:\\s?\"([^\"]+)\"";
                var matches = Regex.Matches(serializedPlantSimulatorJson,pattern);
                foreach (Match match in matches)
                {
                    obj.plantDescription = match.Groups[1].Value.Replace("plantDescription", "").Replace(":", "").Replace("\"", "");
                }
            }
            // workaround for plantName and plantDescription begin null
            if (obj.plantName == null)
            {
                string pattern = "\"plantName\"\\s?:\\s?\"([^\"]+)\"";
                var matches = Regex.Matches(serializedPlantSimulatorJson, pattern);
                foreach (Match match in matches)
                {
                    obj.plantName = match.Groups[1].Value.Replace("plantName", "").Replace(":", "").Replace("\"", "");
                }
            }
            return obj;
        }

        /// <summary>
        /// Re-construct a PlantSimulator object from the json-code created by this class' .Serialize()
        /// </summary>
        /// <param name="fileName">the read object, or null if unable to read file</param>
        static public PlantSimulator LoadFromJsonFile(string fileName)
        {
            try
            {
                var serializedPlantSimulatorJson = File.ReadAllText(fileName);
                var settings = SerializationSettings();
                return JsonConvert.DeserializeObject<PlantSimulator>(serializedPlantSimulatorJson, settings);
            } 
            catch 
            {
                return null;
            }
        }

    }
}
