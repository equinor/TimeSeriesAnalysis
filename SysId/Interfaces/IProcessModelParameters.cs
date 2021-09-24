using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.SysId
{
    public interface IProcessModelParameters
    {
        /*the model paramters class is intended to be a data-class that contains 
         - model parameters
         - model parameter uncertainty 
         - errors or warnings given during identification of the model parameters
         - parameters which describe the quality of the fit between data and model for the fitting dataset

        It should NOT include
        - timeBase_s, as that is more an attirbute of a particular model/simulation
        */




        /// <summary>
        /// An objective function value that describes the fit of the model to the fitting dataset
        /// (useful when ranking models, for instance when determining time delay)
        /// </summary>
        /// <returns></returns>
        double GetFittingObjFunVal();

        /// <summary>
        /// The root-mean-square of the fit of the model to the fitting dataset
        /// (useful when ranking models, for instance when determining time delay)
        /// </summary>
        /// <returns></returns>
        double GetFittingR2();





        /// <summary>
        /// Aks if model was able to be identified or not
        /// </summary>
        /// <returns>true, if the model identification was able to successfully decide on a set of parameters, otherwise false</returns>
        bool AbleToIdentify();




        void AddWarning(ProcessIdentWarnings warnings);

        List<ProcessIdentWarnings> GetWarningList();
    }
}
