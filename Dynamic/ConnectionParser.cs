using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Tracking which model is connected to which in a set of model. 
    /// This is important when traversing the models when simulating.
    /// 
    /// </summary>
    public class ConnectionParser
    {

        Dictionary<string, ISimulatableModel> modelDict;

        List<(string, string)> connections;

        public ConnectionParser()
        {
            connections = new List<(string, string)>();
            modelDict = new Dictionary<string, ISimulatableModel>();
        }

        /// <summary>
        /// Adds a list of all the model IDs that make up a process simulation
        /// </summary>
        /// <param name="allModels"></param>
        public void AddAllModelObjects( Dictionary<string, ISimulatableModel> allModels)
        {
            this.modelDict = allModels;
        }

        private string[] GetAllModelIDs()
        {
            return this.modelDict.Keys.ToArray();
        }


        /// <summary>
        /// Adds a connection betweent the models with the given IDs
        /// </summary>
        /// <param name="upstreamID"></param>
        /// <param name="downstreamID"></param>
        public void AddConnection(string upstreamID, string downstreamID)
        {
            connections.Add((upstreamID,downstreamID));
        }

        /// <summary>
        /// Get all the models which are connected to a given model one level directly downstream of it
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        public List<string> GetDownstreamModels(string modelID)
        {
            var downstreamModels = new List<string>();
            foreach ((string, string) connection in connections)
            {
                if (connection.Item1 == modelID)
                {
                    downstreamModels.Add(connection.Item2);
                }
            }
            return downstreamModels.ToList();
        }

        /// <summary>
        /// Get all the models which are connected to a given model one level directly upstream of it
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        public List<string> GetUpstreamModels(string modelID)
        {
            var upstreamModels = new List<string>();
            foreach ((string, string) connection in connections)
            {
                if (connection.Item2 == modelID)
                {
                    upstreamModels.Add(connection.Item1);
                }
            }
            return upstreamModels.ToList();
        }

        /// <summary>
        /// Gets all the models that do not have any models upstream of them.
        /// (models are then either signal generators or get their input from external signals)
        /// </summary>
        /// <returns></returns>
        public List<string> GetModelsWithNoUpstreamConnections()
        {
            var modelsIDsToReturn = new List<string>(GetAllModelIDs());
            foreach ((string, string) connection in connections)
            {
                modelsIDsToReturn.Remove(connection.Item2);
            }
            return modelsIDsToReturn.ToList();
        }

        /// <summary>
        /// Determine the order in which the models must be solved
        /// </summary>
        /// <returns>returns the string of sorted model IDs, the order in which modelDict models are to be run</returns>
        public List<string> DetermineCalculationOrderOfModels()
        {
            // TODO: if controllers are in cascade, then calculation order must be to start with the outermost loop and go inwards!

            List<string> unprocessedModels = modelDict.Keys.ToList();
            List<string> orderedModels = new List<string>();
            List<string> pidModels = new List<string>();

            // 1.any purely forward-coupled models should be processed from left->righ 
            List<string> forwardModelIDs = GetModelsWithNoUpstreamConnections ();
            foreach (string forwardModelID in forwardModelIDs)
            {
                orderedModels.Add(forwardModelID);
                unprocessedModels.Remove(forwardModelID);
            }

            // 2 add any models downstream of the above that depend only on said upstream models
            int whileIterations = 0;
            int whileIterationsMax = 100;
            while (forwardModelIDs.Count()>0 && whileIterations< whileIterationsMax)
            {
                string  forwardModelId = forwardModelIDs.First();
                forwardModelIDs.Remove(forwardModelId);

                // get the models downstream of forwardModelId, and see of any of them can be calculated
                List<string> downstreamModelIDs = GetDownstreamModels(forwardModelId);
                foreach (string downstreamModelID in downstreamModelIDs)
                {
                    List<string> upstreamModelIDs = GetUpstreamModels(downstreamModelID);
                    if (DoesArrayContainAll(orderedModels, upstreamModelIDs))
                    {
                        orderedModels.Add(downstreamModelID);
                        unprocessedModels.Remove(downstreamModelID);
                        // you can have many serial models, model1->model2->model3->modle4 etc.
                        // thus add to "forwardModelIDs" recursively. 
                        forwardModelIDs.Add(downstreamModelID);
                    }
                }
                whileIterations++;
            }
            // 3. find all the PID-controller models, these should be run first in any feedback loops, as the
            // look back to the past data point and are easy to initalize based on their setpoint.
            List<string> unprocessedModelsCopy = new List<string>(unprocessedModels);
            foreach (string modelID in unprocessedModelsCopy)
            {
                if (modelDict[modelID].GetProcessModelType() == ProcessModelType.PID)
                {
                    orderedModels.Add(modelID);
                    pidModels.Add(modelID);
                    unprocessedModels.Remove(modelID);
                }
            }
            // 4. models that are left will be inside feedback loops. 
            // these models should also be added left->right

            // if there are multiple pid loops, then these should be added "left-to-right"
            // but "pidModels" is unordered.
            List<string> pidModelsLeftToParse = pidModels;

            int pidModelIdx = -1;
            int whileLoopIterations = 0;
            int whileLoopIterationsMax = 500;
            while (pidModelsLeftToParse.Count>0 && whileLoopIterations< whileLoopIterationsMax)
            {
                whileLoopIterations++;
                if (pidModelIdx >= pidModelsLeftToParse.Count - 1)
                {
                    pidModelIdx = 0;
                }
                else
                {
                    pidModelIdx++;
                }
                string pidModelID = pidModelsLeftToParse.ElementAt(pidModelIdx);
                // try to parse through entire model loop
                bool pidLoopCompletedOk    = false;
                bool pidLoopDone        = false;
                string currentModelID   = pidModelID;
                int whileLoopSafetyCounter = 0; // fail-to-safe:avoid endless while loops.
                int whileLoopSafetyCounterMax = 20;
                // try to follow the entire pid loop, adding models as you go
                HashSet<string> modelsIDLeftToParse = new HashSet<string>();
                foreach (string ID in GetDownstreamModels(pidModelID))
                {
                    modelsIDLeftToParse.Add(ID);
                }
                while (!pidLoopDone && whileLoopSafetyCounter < whileLoopSafetyCounterMax)
                {
                    whileLoopSafetyCounter++;
                    // if stack is empty, finish.
                    if (modelsIDLeftToParse.Count() == 0)
                    {
                        pidLoopDone = true;
                        continue;
                    }
                    // pick first item, and remove it from stack
                    currentModelID = modelsIDLeftToParse.ElementAt(0);
                    modelsIDLeftToParse.Remove(currentModelID);
                    // get all downstream items from current
                    foreach (string ID in GetDownstreamModels(currentModelID))
                    {
                        if (ID == pidModelID)
                        {
                            pidLoopCompletedOk = true;
                        }
                        else// avoid-looping around the same loop more than once!
                        {
                            modelsIDLeftToParse.Add(ID);
                        }
                    }
                    // add model if it only depends on already solved models.
                    if (orderedModels.Contains(currentModelID))
                    {
                        continue;
                    }
                    if (DoesModelDependOnlyOnGivenModels(currentModelID, orderedModels))
                    {
                        orderedModels.Add(currentModelID);
                        unprocessedModels.Remove(currentModelID);
                        modelsIDLeftToParse.Remove(currentModelID);
                    }
                }
                // remove modelId from "left to parse" stack if we successfully traversed it.
                if (pidLoopCompletedOk)
                {
                    pidModelsLeftToParse.Remove(pidModelID);
                }
            }

            // final sanity check
            if (unprocessedModels.Count() > 0)
            {
                Shared.GetParserObj().AddError("CalculationParser.DetermineCalculationOrderOfModels() did not parse all models."); 
            }
            return orderedModels;
        }

        /// <summary>
        /// Determine if array1 contains all of the member of array2
        /// </summary>
        /// <param name="array1">(the bigger array)</param>
        /// <param name="array2">(the smaller array)</param>
        /// <returns></returns>
        private bool DoesArrayContainAll(List<string> array1, List<string> array2)
        {
            bool ret = true;
            foreach (string val in array2)
            {
                if (!array1.Contains(val))
                {
                    ret = false;
                }
            }
            return ret;
        }

        /// <summary>
        /// Determine if the modelId output is determined completely by the givenModelIds (calculation order)
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="givenModelIDs"></param>
        /// <returns>return true if model can be calculated if the givenModelIds are given, otherwise false</returns>
        private bool DoesModelDependOnlyOnGivenModels(string modelId, List<string> givenModelIDs)
        { 
            List<string> upstreamModelIds = GetUpstreamModels(modelId);
            // return DoesArrayContainAll(upstreamModelIds, givenModelIDs);
            return DoesArrayContainAll(givenModelIDs,upstreamModelIds);
        }

    }
}
