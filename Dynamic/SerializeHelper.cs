using System;
using System.Collections.Generic;
using System.Text;

namespace TimeSeriesAnalysis.Dynamic
{
    public  static class SerializeHelper
    {
        const bool writeTestDataToDisk = true;


        static public void  Serialize(string name,PlantSimulator plantSim,
            LoadFromCsv inputData, LoadFromCsv simData)
        {
            string namePrefix = "_";
            if (writeTestDataToDisk)
            {
                name = namePrefix + name;

                plantSim.Serialize(name);
                var combinedData = inputData.Combine(simData);
                combinedData.ToCsv(name);
            }
        }

    }
}
