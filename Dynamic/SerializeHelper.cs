using System;
using System.Collections.Generic;
using System.Text;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{
    public  static class SerializeHelper
    {
        const bool writeTestDataToDisk = true;


        static public void  Serialize(string name,PlantSimulator plantSim,
            TimeSeriesDataSet inputData, TimeSeriesDataSet simData)
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
