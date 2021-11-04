using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Utility
{
    ///<summary>
    /// Version of Plot class where plots code can be Enabled()/Disabled() programatically.
    ///  This allows you to keep all your "Plot" calls in your unit tests and turn them on as needed to 
    ///  debug a single test, while avoid being overwhelmed with plots if for instance re-running all unit tests. 
    ///</summary>
    public class Plot4Test
    {
        private bool isEnabled;
        private int plotCounter;
        private int maxNplots;

        ///<summary>
        /// Determine wheter plots are to be enabled or disabled on. 
        /// To further prevent thousands of plots from accidentally being created by this object
        /// a maximum number of plots is set, override as needed.
        ///</summary>

        public Plot4Test(bool enableByDefault = true, int maxNplots = 6)
        {
            this.plotCounter = 0;
            this.maxNplots = maxNplots;
            if (enableByDefault)
                Enable();
            else
                Disable();
        }
        ///<summary>
        /// Disable all subsequent calls to plot using the same instance of this class.
        ///</summary>
        public void Disable()
        {
            isEnabled = false;
        }

        ///<summary>
        /// Enable all subsequent calls to plot using the same instance of this class.
        ///</summary>
        public void Enable()
        {
            isEnabled = true;
        }

        /// <summary>
        /// Gets the number of plots that have been written
        /// </summary>
        /// <returns></returns>
        public int GetNumberOfPlotsMade()
        {
            return plotCounter;
        }


        private bool DetermineIfToPlot()
        {
            plotCounter++;
            if (isEnabled && plotCounter <= maxNplots)
                return true;
            else
                return false;
        }


        /// <summary>
        /// Wrapper for Plot.FromList that 
        /// </summary>
        /// <returns></returns>
        public string FromList(List<double[]> dataList, List<string> plotNames,
            int dT_s, string comment = null, DateTime t0 = new DateTime(),
            string caseName = "", bool doStartChrome = true)
        {
            if (DetermineIfToPlot())
            {
                return Plot.FromList(dataList, plotNames, dT_s, comment, t0, caseName, doStartChrome          );
            }
            else
                return null;

        }
    }
}
