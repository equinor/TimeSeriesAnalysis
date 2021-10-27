using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    public abstract class ModelBaseClass
    {
        private string ID = "not_named";
        private string[] modelInputIDs;
        private List<string> additiveInputIDs;
        private string outputID;

        internal ProcessModelType processModelType = ProcessModelType.UnTyped;


        public string GetID()
        {
            return ID;
        }

        public void  SetID(string ID)
        {
            this.ID = ID;
        }

        /// <summary>
        /// Set the type of the process model
        /// </summary>
        /// <returns></returns>
        public void SetProcessModelType(ProcessModelType newType)
        {
            processModelType = newType;
        }

        /// <summary>
        /// Get the type of the process model
        /// </summary>
        /// <returns></returns>
        public ProcessModelType GetProcessModelType()
        {
            return processModelType;
        }

        /// <summary>
        /// Set the stringIDs of the one or more manipulated variables <c>U</c> that enter model.
        /// This method may append/lengthen the inputIDs
        /// </summary>
        /// <param name="U_stringIDs"></param>
        /// <param name="idx">if non-null, this is the index of the element in U to set
        /// (U_stringIDs should then have just one element)</param>

        public bool  SetInputIDs(string[] U_stringIDs, int? idx = null)
        {
            if (idx == null)
            {
                modelInputIDs = U_stringIDs;
            }
            else
            {
                if (modelInputIDs == null)
                {
                    modelInputIDs = new string[GetLengthOfInputVector()];
                }
                if (idx.Value < GetLengthOfInputVector())
                {
                    if (modelInputIDs == null)
                    {
                        modelInputIDs = new string[GetLengthOfInputVector()];
                    }
                    modelInputIDs[idx.Value] = U_stringIDs[0];
                }
                else // append the inputIDs string()
                {
                    var oldInputIds = modelInputIDs;
                    modelInputIDs = new string[idx.Value + 1];
                    int k = 0;
                    foreach (string oldId in oldInputIds)
                    {
                        modelInputIDs[k] = oldId;
                        k++;
                    }
                    modelInputIDs[idx.Value] = U_stringIDs[0];
                    if (U_stringIDs.Length == 1)
                        return true;
                    else
                        return false;//unsupported
                }
            }
            return true;
        }

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
        /// Get the type of the process model
        /// </summary>
        /// <returns></returns>
        public string[] GetModelInputIDs()
        {
            return modelInputIDs;
        }

        /// <summary>
        /// Get the DIs of any additive inputs that are included in model
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

        public string[] GetBothKindsOfInputIDs()
        {
            List<string> ret = new List<string>();
            if (modelInputIDs != null)
            {
                ret.AddRange(modelInputIDs);
            }
            if (additiveInputIDs!=null)
            {
                ret.AddRange(additiveInputIDs);
            }
            return ret.ToArray();
        }


        public void SetOutputID(string outputID)
        {
            this.outputID = outputID;
        }

        /// <summary>
        /// returns the output ID
        /// </summary>
        /// <returns> may return <c>null</c> if output is not set</returns>
        public string GetOutputID()
        {
             return SignalNamer.GetSignalName(GetID(), GetOutputSignalType());
        }

        virtual public int GetLengthOfInputVector()
        {
            return GetBothKindsOfInputIDs().Length;

        }

        public abstract SignalType GetOutputSignalType();



    }
}
