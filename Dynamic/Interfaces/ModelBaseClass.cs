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
        private string[] inputIDs;
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
        /// Set the stringIDs of the one or more manipulated variables <c>U</c> that enter model
        /// </summary>
        /// <param name="U_stringIDs"></param>
        /// <param name="idx">if non-null, this is the index of the element in U to set
        /// (U_stringIDs should then have just one element)</param>

        public void SetInputIDs(string[] U_stringIDs, int? idx = null)
        {
            if (idx == null)
            {
                inputIDs = U_stringIDs;
            }
            else
            {
                if (inputIDs == null)
                {
                    inputIDs = new string[GetNumberOfInputs()];
                    inputIDs[idx.Value] = U_stringIDs[0];
                }
            }
        }

        /// <summary>
        /// Get the type of the process model
        /// </summary>
        /// <returns></returns>
        public string[] GetInputIDs()
        {
            return inputIDs;
        }

        public void SetOutputID(string outputID)
        {
            this.outputID = outputID;
        }

        public string GetOutputID()
        {
            return outputID;
        }

        public abstract int GetNumberOfInputs();




    }
}
