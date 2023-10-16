using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that tracks which model is connected to which in a set of models. 
    /// <para>
    /// This is important when traversing the models when simulating with the 
    /// <seealso cref="PlantSimulator"/>, as these models need to be
    /// run in a specific order</para>
    /// </summary>
    public class ConnectionParser
    {

        private string computationalLoopPrefix = "CompLoop_";

        [JsonInclude]
        public List<(string, string)> connections;

        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionParser()
        {
            if (connections == null)
            {
                connections = new List<(string, string)>();
            }
        }

        /// <summary>
        /// Parse a dictionary of models,and initalize the connections based on the names of inputsIDs/outputIDs
        /// </summary>
        /// <param name="modelDict"></param>
        private void Init(Dictionary<string, ISimulatableModel> modelDict)
        {
            var modelNames = modelDict.Keys;

            foreach (var modelID in modelNames)
            {
                var model = modelDict[modelID];
                var outputID = model.GetOutputID();
                foreach (var modelID2 in modelNames)
                {
                    if (modelID2 == modelID)
                        continue;

                    var model2 = modelDict[modelID2];
                    var inputIDs = model2.GetBothKindsOfInputIDs();

                    // special case: do not add tracking signals in pid-controllers as "connections"
                    if (model2.GetProcessModelType() == ModelType.PID)
                    {
                       if (inputIDs.Length >= (int)PidModelInputsIdx.Tracking+1)
                           inputIDs[(int)PidModelInputsIdx.Tracking] = null;
                    }

                    if (inputIDs.Contains<string>(outputID))
                    {
                        var upstreamID = modelID;
                        var downstreamID = modelID2;
                        if (!connections.Contains((upstreamID, downstreamID)))
                            connections.Add((upstreamID, downstreamID));
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Parses models and determines if there are co-dependent models
        /// </summary>
        /// <param name="modelDict"></param>
        /// <returns>a dictionary with an ID and a list of all involved modelIDs</returns>
        public Dictionary<string,List<string>> FindComputationalLoops(Dictionary<string, ISimulatableModel> modelDict)
        {
            Dictionary<string,List<string>> compLoopDict = new Dictionary<string,List<string>>(); 
            Dictionary<string,List<string>> eachModelsDependsOnDict = new Dictionary<string,List<string>>();

            // build eachModelsDependsOnDict  - a dictionay that for each modelID has a list
            // of all modelIds that it depends on
            foreach (var connection in connections)
            {
                string upstreamId = connection.Item1;
                string downstreamId = connection.Item2;
                if (!eachModelsDependsOnDict.ContainsKey(downstreamId))
                    eachModelsDependsOnDict.Add(downstreamId, new List<string>());
                if (!eachModelsDependsOnDict.ContainsKey(upstreamId))
                    eachModelsDependsOnDict.Add(upstreamId, new List<string>());

                eachModelsDependsOnDict[downstreamId] .Add(upstreamId);

                foreach (var item in eachModelsDependsOnDict)
                {
                    if (item.Value.Contains(downstreamId))
                    {
                        if (item.Key != upstreamId)
                        {
                            eachModelsDependsOnDict[item.Key].Add(upstreamId);
                        }
                    }
                }
            }

            // parse eachModelsDependsOnDict and look for computational loops

            foreach (var modelID in eachModelsDependsOnDict.Keys)
            {
                var curModelDependencies = eachModelsDependsOnDict[modelID];
                foreach (var dependencyID in curModelDependencies)
                {
                    if (!eachModelsDependsOnDict.ContainsKey(dependencyID))
                        continue;
                
                    if (eachModelsDependsOnDict[dependencyID].Contains(modelID))
                    {
                        bool addedToExistingLoop = false;
                        bool dependencyAlreadyKnow = false;
                        //  if the dependency of modelID depends on modelID, then you have a computational loop!
                        // first check if this loop is to be added to an existing loop 
                        if (compLoopDict.Count > 0)
                        {
                            int listNumber = 0;
                            foreach (var singleLoop in compLoopDict)
                            {
                                var listOfSingleLoop = singleLoop.Value;
                                if (listOfSingleLoop.Contains(modelID))
                                {
                                    if (!listOfSingleLoop.Contains(dependencyID))
                                    {
                                        compLoopDict[singleLoop.Key].Add(dependencyID);
                                        addedToExistingLoop = true;
                                    }
                                    else
                                    {
                                        dependencyAlreadyKnow = true;
                                    }
                                }
                                if (listOfSingleLoop.Contains(dependencyID))
                                {
                                    if (!listOfSingleLoop.Contains(modelID))
                                    {
                                        compLoopDict[singleLoop.Key].Add(modelID);
                                        addedToExistingLoop = true;
                                    }
                                    else
                                    {
                                        dependencyAlreadyKnow = true;
                                    }
                                }
                                listNumber++;
                            }

                        }
                        // if this loop is not part of another big looper, add a new list for it.
                        if (!addedToExistingLoop && !dependencyAlreadyKnow)
                        {
                            var uniqueLoopNumber = compLoopDict.Count();
                            var compLoopID = computationalLoopPrefix + uniqueLoopNumber;
                            var newList = new List<string>();
                            newList.Add(modelID);
                            newList.Add(dependencyID);
                            compLoopDict.Add(compLoopID,newList);
                        }
                    }
                }
            }
            return compLoopDict;
        }



        /// <summary>
        /// Determine the order in which the models must be solved
        /// </summary>
        /// <returns>returns the <see langword="string"/> of sorted model IDs, the order in which modelDict models are to be run.
        /// If the plant contains computational loops, the IDs of the computational loops are also in these lists instead of 
        /// the indivudal models. 
        /// </returns>
        public (List<string>, Dictionary<string, List<string>>) InitAndDetermineCalculationOrderOfModels(
            Dictionary<string, ISimulatableModel> modelDict)
        {
            Init(modelDict);

            List<string> unprocessedModels = modelDict.Keys.ToList();
            List<string> orderedModelAndLoopIDs = new List<string>();
            List<string> pidModels = new List<string>();

            Dictionary<string, List<string>> computationalLoopDict = FindComputationalLoops(modelDict);


            // forward-coupled models (i.e. models in series with no feedbacks and not dependant on feedbacks)
            {
                // 1.any purely forward-coupled models should be processed from left->right
                List<string> forwardModelIDs = GetModelsWithNoUpstreamConnections(modelDict);
                foreach (string forwardModelID in forwardModelIDs)
                {
                    orderedModelAndLoopIDs.Add(forwardModelID);
                    unprocessedModels.Remove(forwardModelID);
                }
                // 2 add any models downstream of the above that depend only on said upstream models
                int whileIterations = 0;
                int whileIterationsMax = 100;
                while (forwardModelIDs.Count() > 0 && whileIterations < whileIterationsMax)
                {
                    string forwardModelId = forwardModelIDs.First();
                    forwardModelIDs.Remove(forwardModelId);

                    // get the models downstream of forwardModelId, and see of any of them can be calculated
                    List<string> downstreamModelIDs = GetAllDownstreamModelIDs(forwardModelId);
                    foreach (string downstreamModelID in downstreamModelIDs)
                    {
                        if (unprocessedModels.Count == 0)
                            continue;
                        List<string> upstreamModelIDs = GetAllUpstreamModels(downstreamModelID);
                        if (DoesArrayContainAll(orderedModelAndLoopIDs, upstreamModelIDs))
                        {
                            orderedModelAndLoopIDs.Add(downstreamModelID);
                            unprocessedModels.Remove(downstreamModelID);
                            // you can have many serial models, model1->model2->model3->modle4 etc.
                            // thus add to "forwardModelIDs" recursively. 
                            forwardModelIDs.Add(downstreamModelID);
                        }
                    }
                    whileIterations++;
                }
            }

            // 3. find all the PID-controller models, these should be run first in any feedback loops, as the
            // look back to the past data point and are easy to initalize based on their setpoint.

            // Note that controllers may be in cascades, so the order in they are processed may be signficant
            // the calculation order should always be to start with the outermost pid-controllers and to 
            // work your way in.
            if(unprocessedModels.Count>0)
            {
                bool areUnprocessedPIDModelsLeft = true;
                int whileLoopIterations = 0;
                int whileLoopIterationsMax = 500;
                while (areUnprocessedPIDModelsLeft && whileLoopIterations < whileLoopIterationsMax)
                {
                    whileLoopIterations++;
                    List<string> unprocessedModelsCopy = new List<string>(unprocessedModels);
                    foreach (string modelID in unprocessedModelsCopy)
                    {
                        // look for pid-models that either a) are not connected to any pid-models or 
                        // b) are connected to a model that is already in "pidModels"
                        if (modelDict[modelID].GetProcessModelType() == ModelType.PID)
                        {
                            var upstreamModelIDs = GetAllUpstreamModels(modelDict[modelID].GetID());
                            bool modelHasUpstreamPIDNOTAlreadyProcessed = false;
                            foreach (var upstreamModelID in upstreamModelIDs)
                            {
                                if (modelDict[upstreamModelID].GetProcessModelType() == ModelType.PID)
                                {
                                    if (unprocessedModels.Contains(upstreamModelID))
                                    {
                                        modelHasUpstreamPIDNOTAlreadyProcessed = true;
                                    }
                                }
                            }
                            if (!modelHasUpstreamPIDNOTAlreadyProcessed)
                            {
                                orderedModelAndLoopIDs.Add(modelID);
                                pidModels.Add(modelID);
                                unprocessedModels.Remove(modelID);
                            }
                        }
                    }

                    // check to see if we need to do another round
                    areUnprocessedPIDModelsLeft = false;
                    foreach (string modelID in unprocessedModels)
                    {
                        // look for pid-models that either a) are not connected to any pid-models or 
                        // b) are connected to a model that is already in "pidModels"
                        if (modelDict[modelID].GetProcessModelType() == ModelType.PID)
                        {
                            areUnprocessedPIDModelsLeft = true;
                        }
                    }
                }
            }
            // 4. models that are left will be inside feedback loops. 
            // these models should also be added left->right

            // if there are multiple pid loops, then these should be added "left-to-right"
            // but "pidModels" is unordered.
            if (unprocessedModels.Count > 0)
            { 
                List<string> pidModelsLeftToParse = pidModels;

                int pidModelIdx = -1;
                int whileLoopIterations = 0;
                int whileLoopIterationsMax = 500;
                while (pidModelsLeftToParse.Count > 0 && whileLoopIterations < whileLoopIterationsMax)
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
                    bool pidLoopCompletedOk = false;
                    bool pidLoopDone = false;
                    string currentModelID = pidModelID;
                    int whileLoopSafetyCounter = 0; // fail-to-safe:avoid endless while loops.
                    int whileLoopSafetyCounterMax = 20;
                    // try to follow the entire pid loop, adding models as you go
                    HashSet<string> modelsIDLeftToParse = new HashSet<string>();
                    foreach (string ID in GetAllDownstreamModelIDs(pidModelID))
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
                        foreach (string ID in GetAllDownstreamModelIDs(currentModelID))
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
                        if (orderedModelAndLoopIDs.Contains(currentModelID))
                        {
                            continue;
                        }
                        if (DoesModelDependOnlyOnGivenModels(currentModelID, orderedModelAndLoopIDs))
                        {
                            orderedModelAndLoopIDs.Add(currentModelID);
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
            }

            // experimental:
            // add in computational-loop models 
            if (computationalLoopDict.Count > 0 && unprocessedModels.Count() > 0)
            {
                foreach (var loop in computationalLoopDict)
                {
                    var modelsInLoop = loop.Value;
                    // note that the order in which computational-loop models are added will be important if the 
                    // loop contains more than two subprocesses
                    if (modelsInLoop.Count() == 2)
                    {
                        foreach (var modelId in modelsInLoop)
                        {
                            orderedModelAndLoopIDs.Add(modelId);
                            unprocessedModels.Remove(modelId);
                        }
                    }
                    else
                    {
                        // the principle seems to be that if any 
                        // process in the comp loop takes the input of two 
                        // other processes in the loop, it should be simulated last.
                        var loopOutputs = new List<string>();
                        foreach (var modelID in modelsInLoop)
                        {
                            loopOutputs.Add(modelDict[modelID].GetOutputID());
                        }
                        var modelNumLoopedInputsDict = new Dictionary<string, int>();
                        int nMaxNumLoopedInputs = 1;
                        foreach (var modelID in modelsInLoop)
                        {
                            var inputIDs = modelDict[modelID].GetBothKindsOfInputIDs();
                            var commonItems = inputIDs.Intersect<string>(loopOutputs);
                            modelNumLoopedInputsDict.Add(modelID, commonItems.Count());
                            if (commonItems.Count() > nMaxNumLoopedInputs)
                            {
                                nMaxNumLoopedInputs = commonItems.Count();
                            }
                        }

                        // add models to ordered list sorted by the amount of looped inputs they have.
                        for (int nCurrentNumberOfInputsPerModel = 1; nCurrentNumberOfInputsPerModel <= nMaxNumLoopedInputs; 
                            nCurrentNumberOfInputsPerModel++)
                        {
                            foreach (var modelID in modelsInLoop)
                            {
                                if (modelNumLoopedInputsDict[modelID] == nCurrentNumberOfInputsPerModel)
                                {
                                    orderedModelAndLoopIDs.Add(modelID);
                                    unprocessedModels.Remove(modelID);
                                }
                            }
                        }
                    }
                }
            }

            // final sanity check
            if (unprocessedModels.Count() > 0)
            {
                Shared.GetParserObj().AddError("CalculationParser.DetermineCalculationOrderOfModels() did not parse all models."); 
            }
            return (orderedModelAndLoopIDs,computationalLoopDict);
        }


        /// <summary>
        /// Get all the models which are connected to a given model one level directly downstream of it
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        public List<string> GetAllDownstreamModelIDs(string modelID)
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
        public List<string> GetAllUpstreamModels(string modelID)
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
        /// Query if the model has an upstream PID-model.
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        public bool HasUpstreamPID(string modelID, Dictionary<string, ISimulatableModel> modelDict)
        {
            var upstreamModelIDs = GetAllUpstreamModels(modelID);

            foreach (string upstreamID in upstreamModelIDs)
            {
                if (modelDict[upstreamID].GetProcessModelType() == ModelType.PID)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the ID of the PID-controller that is upstream a given modelID
        /// </summary>
        /// <param name="modelID"></param>
        /// <returns></returns>
        public string[] GetUpstreamPIDIds(string modelID, Dictionary<string, ISimulatableModel> modelDict)
        {
            var upstreamModelIDs = GetAllUpstreamModels(modelID);

            List<string> upstreamPIDIds = new List<string>();

            foreach (string upstreamID in upstreamModelIDs)
            {
                if (modelDict[upstreamID].GetProcessModelType() == ModelType.PID)
                {
                    upstreamPIDIds.Add(upstreamID);
                }
            }
            return upstreamPIDIds.ToArray();
        }

        /// <summary>
        /// Get unit model controlled by PDI
        /// </summary>
        /// <param name="pidModelID"></param>
        /// <param name="modelDict"></param>
        /// <returns>returns null if no model is found</returns>
        public string GetUnitModelControlledByPID(string pidModelID, Dictionary<string, ISimulatableModel> modelDict)
        {
            var upstreamModelIDs = GetAllUpstreamModels(pidModelID);

            if (upstreamModelIDs.Count == 0)
                return null;


            var pidInputID = modelDict[pidModelID].GetModelInputIDs()[(int)PidModelInputsIdx.Y_meas];
            string unitModelID=null;
            foreach (var upstreamID in upstreamModelIDs)
            {
                if (modelDict[upstreamID].GetProcessModelType() != ModelType.SubProcess)
                    continue;
                if (modelDict[upstreamID].GetOutputID() == null)
                    continue;

                if (modelDict[upstreamID].GetOutputID() == pidInputID)
                {
                    unitModelID = upstreamID;
                }
            }
            return unitModelID;
        }



        internal int[] GetFreeIndices(string modelID, PlantSimulator simulator)
        {
            var inputIDs = simulator.modelDict[modelID].GetModelInputIDs();
            List<int> freeIndices = new List<int>();
            for (int idx=0; idx<inputIDs.Length; idx++)
            {
                var inputID = inputIDs[idx];
                if (!simulator.externalInputSignalIDs.Contains(inputID))
                { 
                    freeIndices.Add(idx);
                }
            }
            return freeIndices.ToArray();
        }

        /// <summary>
        /// Get the externally provided signals for a given model
        /// </summary>
        /// <param name="modelID"></param>
        /// <param name="simulator"></param>
        /// <returns></returns>
        internal string[] GetModelExternalSignals(string modelID, PlantSimulator simulator)
        {
            List<string> externalSignalsForModel = new List<string>();
            var inputIDs = simulator.modelDict[modelID].GetModelInputIDs();
            foreach(var inputID in inputIDs)
            {
                if (simulator.externalInputSignalIDs.Contains(inputID))
                    externalSignalsForModel.Add(inputID);
            }
            return externalSignalsForModel.ToArray();
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
            List<string> upstreamModelIds = GetAllUpstreamModels(modelId);
            return DoesArrayContainAll(givenModelIDs, upstreamModelIds);
        }


        /// <summary>
        /// Gets all the models that do not have any models upstream of them.
        /// (models are then either signal generators or get their input from external signals)
        /// </summary>
        /// <returns></returns>
        private List<string> GetModelsWithNoUpstreamConnections(
             Dictionary<string, ISimulatableModel> modelDict)
        {
            var modelsIDsToReturn = new List<string>(modelDict.Keys.ToArray());//GetAllModelIDs());
            foreach ((string, string) connection in connections)
            {
                modelsIDsToReturn.Remove(connection.Item2);
            }
            return modelsIDsToReturn.ToList();
        }

    }
}
