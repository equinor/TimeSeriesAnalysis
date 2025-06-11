using System;
using System.Collections.Generic;
using System.Text;
using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Quickly serialize both PlantSimulator object and data associated with it.
    /// </summary>
    public  static class SerializeHelper
    {
        /// <summary>
        /// Writes plantSim, inputData and simData to disk
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plantSim"></param>
        /// <param name="inputData"></param>
        /// <param name="simData"></param>
        static public void  Serialize(string name,PlantSimulator plantSim,
            TimeSeriesDataSet inputData, TimeSeriesDataSet simData)
        {
            string namePrefix = "_";
            name = namePrefix + name;
            plantSim.Serialize(name);
            var combinedData = inputData.Combine(simData);
            combinedData.ToCsv(name);

        }

    }
}
