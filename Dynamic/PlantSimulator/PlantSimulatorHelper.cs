using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace TimeSeriesAnalysis.Dynamic
{/*
    public class KnownTypesBinder : ISerializationBinder
    {
        public IList<Type> KnownTypes { get; set; }

        public Type BindToType(string assemblyName, string typeName)
        {
            return KnownTypes.SingleOrDefault(t => t.Name == typeName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
    */



    public class PlantSimulatorHelper
    {
        private static JsonSerializerSettings SerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.All;
            settings.NullValueHandling = NullValueHandling.Include;
            //settings.TypeNameHandling = TypeNameHandling.Objects;
            //settings.SerializationBinder = plantSimulatorSerializationBinder;
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

            // workaround for plantName and plantDescription begin null
            if (obj.plantDescription == null)
            {
                string pattern = "\"plantDescription\": \"([^\"]+)\"";
                var matches = Regex.Matches(serializedPlantSimulatorJson,pattern);
                foreach (Match match in matches)
                {
                    obj.plantDescription = match.Groups[1].Value.Replace("plantDescription", "").Replace(":", "").Replace("\"", "");
                }
            }
            // workaround for plantName and plantDescription begin null
            if (obj.plantName == null)
            {
                string pattern = "\"plantName\": \"([^\"]+)\"";
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
        /// <param name="serializedPlantSimulatorJson"></param>
        static public PlantSimulator LoadFromJsonFile(string fileName)
        {
            var serializedPlantSimulatorJson  = File.ReadAllText(fileName);
            var settings = SerializationSettings();

            return JsonConvert.DeserializeObject<PlantSimulator>(serializedPlantSimulatorJson, settings);
        }

    }
}
