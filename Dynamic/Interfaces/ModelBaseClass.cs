﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Abstract base class that contains common functionality across all models which are to be implemented.
    /// <seealso cref="ISimulatableModel"/>
    /// </summary>
    public abstract class ModelBaseClass
    {
        /// <summary>
        /// A unique ID string that is used to identify the model uniquely in a PlantSimulation.
        /// </summary>
        public string ID { get; set; } = "not_named";

        /// <summary>
        /// Unique signal IDs that are mapped to the non-additive model inputs.
        /// </summary>
        public string[] ModelInputIDs;


        /// <summary>
        /// Unique signal IDs that are added to the output of the model (typically a disturbance).
        /// </summary>
        public List<string> additiveInputIDs;

        /// <summary>
        /// Unique signal ID that defines the name of output signal of the model.
        /// </summary>
        public string outputID=null;

        /// <summary>
        /// Optional unique optional signal ID to be used for fitting/identification, if this is different from outputID.
        /// This tag is mainly for modeling estimated disturbance signals.
        /// </summary>
        public string outputIdentID = null;

        /// <summary>
        /// The type of the model.
        /// </summary>
        public ModelType processModelType = ModelType.UnTyped;

        /// <summary>
        /// Optional comment.
        /// </summary>
        public string comment;

        /// <summary>
        /// Optional visual position of model when displayed as graph: x-axis position (origo is top left).
        /// </summary>
        public double? x;

        /// <summary>
        /// Optional visual position of model when displayed as graph: y-axis position (origo is top left).
        /// </summary>
        public double? y;


        /// <summary>
        /// Optional color string when model is displayed as a graph.
        /// </summary>
        public string color;

        /// <summary>
        /// Get the ID of the model.
        /// </summary>
        /// <returns></returns>
        public string GetID()
        {
            return ID;
        }

        /// <summary>
        /// Set the ID of the model.
        /// </summary>
        /// <param name="ID"></param>
        public void SetID(string ID)
        {
            this.ID = ID ;
        }



        /// <summary>
        /// Set the type of the process model.
        /// </summary>
        /// <returns></returns>
        public void SetProcessModelType(ModelType newType)
        {
            processModelType = newType;
        }

        /// <summary>
        /// Get the type of the process model.
        /// </summary>
        /// <returns></returns>
        public ModelType GetProcessModelType()
        {
            return processModelType;
        }

        /// <summary>
        /// Set the stringIDs of one or more of the manipulated variables <c>U</c> that enter model.
        /// This method may append/lengthen the inputIDs.
        /// </summary>
        /// <param name="U_stringIDs"></param>
        /// <param name="idx">if non-null, this is the index of the element in U to set
        /// (U_stringIDs should then have just one element)</param>

        public bool  SetInputIDs(string[] U_stringIDs, int? idx = null)
        {
            if (idx == null)
            {
                ModelInputIDs = U_stringIDs;
            }
            else
            {
                if (ModelInputIDs == null)
                {
                    ModelInputIDs = new string[GetLengthOfInputVector()];
                }
                if (idx.Value < GetLengthOfInputVector())
                {
                    if (ModelInputIDs == null)
                    {
                        ModelInputIDs = new string[GetLengthOfInputVector()];
                    }
                    else if (idx.Value > ModelInputIDs.Length-1)
                    {
                        var ModelInputIDsOld = ModelInputIDs;
                        ModelInputIDs = new string[GetLengthOfInputVector()];
                        for (int i = 0; i < ModelInputIDsOld.Length; i++)
                        {
                            ModelInputIDs[i] = ModelInputIDsOld[i];
                        }
                    }
                    ModelInputIDs[idx.Value] = U_stringIDs[0];
                }
                else // append the inputIDs string()
                {
                    var oldInputIds = ModelInputIDs;
                    ModelInputIDs = new string[idx.Value + 1];
                    int k = 0;
                    foreach (string oldId in oldInputIds)
                    {
                        ModelInputIDs[k] = oldId;
                        k++;
                    }
                    ModelInputIDs[idx.Value] = U_stringIDs[0];
                    if (U_stringIDs.Length == 1)
                        return true;
                    else
                        return false;//unsupported
                }
            }
            return true;
        }

        /// <summary>
        /// Add an additive signal to the output.
        /// </summary>
        /// <param name="additiveInputID">ID of signal to add</param>
        public void AddSignalToOutput(string additiveInputID)
        {
            if (additiveInputIDs == null)
            {
                additiveInputIDs = new List<string>();
                additiveInputIDs.Add(additiveInputID);
            }
            else
            {
                if (!additiveInputIDs.Contains(outputID))
                {
                    additiveInputIDs.Add(additiveInputID);
                } 
            } 
        }

        /// <summary>
        /// Get the type of the process model.
        /// </summary>
        /// <returns></returns>
        public string[] GetModelInputIDs()
        {
            return ModelInputIDs;
        }

        /// <summary>
        /// Get the IDs of any additive inputs that are included in model.
        /// </summary>
        /// <returns>returns <c>null</c> if no additive inputs are defined.</returns>
        public string[] GetAdditiveInputIDs()
        {
            if (additiveInputIDs != null)
            {
                return additiveInputIDs.ToArray();
            }
           return null;
        }

        /// <summary>
        /// Get the IDs of both the model inputs and the additive model inputs.
        /// </summary>
        /// <returns></returns>
        public string[] GetBothKindsOfInputIDs()
        {
            List<string> ret = new List<string>();
            if (ModelInputIDs != null)
            {
                ret.AddRange(ModelInputIDs);
            }
            if (additiveInputIDs!=null)
            {
                ret.AddRange(additiveInputIDs);
            }
            return ret.ToArray();
        }


        /// <summary>
        /// Set the ID of the output.
        /// </summary>
        /// <param name="outputID"></param>
        public void SetOutputID(string outputID)
        {
            this.outputID = outputID;
        }

        /// <summary>
        /// Return the output ID.
        /// </summary>
        /// <returns> may return <c>null</c> if output is not set.</returns>
        public virtual string GetOutputID()
        {
             if (this.outputID == null)
                return SignalNamer.GetSignalName(GetID(), GetOutputSignalType());
             else
                return this.outputID;
        }


        /// <summary>
        /// Return the ID of the signal the output is identified against.
        /// </summary>
        /// <returns> may return <c>null</c> if output is not set.</returns>
        public virtual string GetOutputIdentID()
        {
            return this.outputIdentID;
        }



        /// <summary>
        /// Get the length of the output vector.
        /// </summary>
        /// <returns></returns>
        virtual public int GetLengthOfInputVector()
        {
            return GetBothKindsOfInputIDs().Length;

        }

        /// <summary>
        /// Get the type of the output signal.
        /// </summary>
        /// <returns></returns>
        public abstract SignalType GetOutputSignalType();



    }
}
