using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using TimeSeriesAnalysis.Dynamic;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Calculates a percentage score of maximum 100% that indicates the match between measurement and simulation for a plant that 
    /// may consist of several unit models and several siginals to be matched, and may include closed-loops.
    /// </summary>
    public class FitScoreCalculator
    {
        /// <summary>
        /// Calculate a "fit score" between two signals. 
        /// </summary>
        /// <param name="meas"></param>
        /// <param name="sim"></param>
        /// <param name="indToIgnore"></param>
        /// <returns> a fit score that is maximum 100 percent, but can also go negative if fit is poor</returns>
        public static double Calc(double[] meas, double[] sim,  List<int> indToIgnore = null)
        {
            if (meas == null)
                return double.NaN;
            if (sim == null)
                return double.NaN;

            double dev = Deviation(meas, sim, indToIgnore);
            double devFromSelf = DeviationFromAvg(meas, indToIgnore);

            double fitScore = 0;
            if ( !Double.IsNaN(dev))
            {
                fitScore = (double)(1 - dev / devFromSelf) * 100;
            }
            else
            {
                fitScore = double.NaN;
            }
            return fitScore;
        }

        /// <summary>
        /// Determines a score for how well a simulation fits with inputData. This can 
        /// be compared over time to see any signs of degredation of the plant over time
        /// 
        /// </summary>
        /// <param name="plantSimObj"></param>
        /// <param name="inputData"></param>
        /// <param name="simData"></param>
        /// <param name="indToIgnore">optional list of indices to be ignored</param>
        /// <returns></returns>
        static public double GetPlantWideSimulated(PlantSimulator plantSimObj, TimeSeriesDataSet inputData, 
            TimeSeriesDataSet simData,, List<int> indToIgnore = null)
        {
            const string disturbanceSignalPrefix = "_D";
            List<double> fitScores = new List<double>();
            foreach (var modelName in plantSimObj.modelDict.Keys)
            {
                double[] measY = null;
                double[] simY = null;

                var modelObj = plantSimObj.modelDict[modelName];
                var outputName = modelObj.GetOutputID();
                var outputIdentName = modelObj.GetOutputIdentID();
                if (outputName == "" || outputName == null)
                    continue;

                if (plantSimObj.modelDict[modelName].GetProcessModelType() == ModelType.PID)
                {
                    if (inputData.ContainsSignal(outputName))
                    {
                        measY = inputData.GetValues(outputName);
                    }

                }
                else //if (plantSimObj.modelDict[modelName].GetProcessModelType() == ModelType.SubProcess)
                {
                    if (outputIdentName != null)
                    {
                        if (inputData.ContainsSignal(outputIdentName))
                        {
                            measY = inputData.GetValues(outputIdentName);
                        }
                        else if (simData.ContainsSignal(outputIdentName))
                        {
                            measY = simData.GetValues(outputIdentName);
                        }
                    }
                    // add in fit of process output, but only if "additive output signal" is not a
                    // locally identified "_D_" signal, as then output matches 100%  always (any model error is put into disturbance signal as well)
                    else if (modelObj.GetAdditiveInputIDs() != null)
                    {
                        if (!modelObj.GetAdditiveInputIDs()[0].StartsWith(disturbanceSignalPrefix))
                        {
                            if (inputData.ContainsSignal(outputName))
                            {
                                measY = inputData.GetValues(outputName);
                            }
                        }
                    }
                    else if (inputData.ContainsSignal(outputName))
                    {
                        measY = inputData.GetValues(outputName);
                    }
                }
                if (simData.ContainsSignal(outputName))
                { 
                    simY = simData.GetValues(outputName);
                }

                if (measY != null && simY != null)
                {
                    var curFitScore = FitScoreCalculator.Calc(measY, simY, indToIgnore);

                    if (curFitScore != double.NaN)
                    {
                        fitScores.Add(curFitScore);
                    }
                }

            }
            if (!fitScores.Any())
                return double.NaN;
            else
            {
                return fitScores.Average();
            }

        }



        /// <summary>
        /// Get the average stored fit-scores in the "paramters.Fitting" object, 
        /// indicating how well the plant on average described/aligned with the dataset when it was fitted. 
        /// </summary>
        /// <param name="plantSimObj"></param>
        /// <returns></returns>
        static public double GetPlantWideStored(PlantSimulator plantSimObj)
        {
            List<double> fitScores = new List<double>();
            foreach (var modelName in plantSimObj.modelDict.Keys)
            {
                double curFitScore = 0;
                if (plantSimObj.modelDict[modelName].GetProcessModelType() == ModelType.PID)
                {
                    var modelObj = (PidModel)plantSimObj.modelDict[modelName];
                    if (modelObj.pidParameters.Fitting != null)
                    {
                        curFitScore = modelObj.pidParameters.Fitting.FitScorePrc;
                    }
                }
                else if (plantSimObj.modelDict[modelName].GetProcessModelType() == ModelType.SubProcess)
                {
                    var modelObj = (UnitModel)plantSimObj.modelDict[modelName];
                    if (modelObj.modelParameters.Fitting != null)
                    {
                        curFitScore = modelObj.modelParameters.Fitting.FitScorePrc;
                    }
                }
                if (curFitScore != 0)
                {
                    fitScores.Add(curFitScore);
                }

            }
            if (!fitScores.Any())
                return double.NaN;
            else
            {
                return fitScores.Average();
            }
        }


        /// <summary>
        /// Get the absolute average deviation between the referene and model signals
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="model"></param>
        /// <param name="indToIgnore"></param>
        /// <returns></returns>
        private static double Deviation(double[] reference, double[] model, List<int> indToIgnore = null)
        {
            if (reference == null)
                return double.NaN;
            if (model == null)
                return double.NaN;
            if (reference.Length == 0)
                return double.NaN;
            if (model.Length == 0)
                return double.NaN;
            double ret = 0;
            double N = 0;
            indToIgnore?.Sort();
            int indToIgnoreIndex = 0;
            for (var i = 0; i < Math.Min(reference.Length, model.Length); i++)
            {
                if (!((indToIgnore == null) || (indToIgnore.Count == 0)))
                {
                    if (i == indToIgnore[indToIgnoreIndex])
                    {
                        if (indToIgnoreIndex + 1 < indToIgnore.Count)
                        {
                            indToIgnoreIndex++;
                        }
                        continue;
                    }
                }
                ret += Math.Abs(reference[i] - model[i]);
                N++;
            }
            if (N == 0)
                return 0;
            ret = ret / N;
            return ret;
        }

        private static double DeviationFromAvg(double[] signal, List<int> indToIgnore = null)
        {
            if (signal == null) return double.NaN;
            if (signal.Length == 0) return double.NaN;

            var vec = new Vec();
            var avg = vec.Mean(signal);

            if (!avg.HasValue)
                return double.NaN;

            var N = 0;
            double ret = 0;
            indToIgnore?.Sort();
            int indToIgnoreIndex = 0;
            for (var i = 0; i < signal.Length; i++)
            {
                if (!((indToIgnore == null) || (indToIgnore.Count == 0)))
                {
                    if (i == indToIgnore[indToIgnoreIndex])
                    {
                        if (indToIgnoreIndex + 1 < indToIgnore.Count)
                        {
                            indToIgnoreIndex++;
                        }
                        continue;
                    }
                }
                ret += Math.Abs(signal[i] - avg.Value);
                N++;
            }
            return ret / N;
        }


    }
}
