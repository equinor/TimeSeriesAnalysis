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

        /*
        ///<summary>
        /// Plot one vector X, where the sampling time interval is dT_s. 
        ///</summary>
        public void One(double[] X, int dT_s, string tagName = "Var1", string comment = null, DateTime t0 = new DateTime())
        {
            if (DetermineIfToPlot())
                Plot.One(X, dT_s, tagName, comment, t0);
        }
        ///<summary>
        /// Plot two vectors V1 and V2, where the sampling time interval is dT_s. 
        ///</summary>
        public void Two(double[] V1, double[] V2, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2",
            bool plotAllVarsOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (DetermineIfToPlot())
                Plot.Two(V1, V2, dT_s, tagNameV1, tagNameV2, plotAllVarsOnLeftYaxis, useSubplots, comment, t0);
        }

        ///<summary>
        /// Plot three vectors V1, V2, V3 where the sampling time interval is dT_s. 
        ///</summary>
        public void Three(double[] V1, double[] V2, double[] V3, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (DetermineIfToPlot())
                Plot.Three(V1, V2, V3, dT_s, tagNameV1, tagNameV2, tagNameV3, plotAllOnLeftYaxis, useSubplots, comment, t0);
        }

        ///<summary>
        /// Plot three vectors V1,V2,V3,V4 where the sampling time interval is dT_s. 
        ///</summary>
        public void Four(double[] V1, double[] V2, double[] V3, double[] V4, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4",
             bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (DetermineIfToPlot())
                Plot.Four(V1, V2, V3, V4, dT_s, tagNameV1, tagNameV2, tagNameV3, tagNameV4, plotAllOnLeftYaxis,
                useSubplots, comment, t0);
        }

        ///<summary>
        /// Plot five vectors V1,V2,V3,V4,V5 where the sampling time interval is dT_s. 
        ///</summary>
        public void Five(double[] V1, double[] V2, double[] V3, double[] V4, double[] V5, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4", string tagNameV5 = "Var5",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (DetermineIfToPlot())
                Plot.Five(V1, V2, V3, V4, V5, dT_s, tagNameV1, tagNameV2, tagNameV3, tagNameV4, tagNameV5,
                plotAllOnLeftYaxis, useSubplots, comment, t0);
        }

        ///<summary>
        /// Plot six vectors V1,V2,V3,V4 where the sampling time interval is dT_s. 
        ///</summary>
        public void Six(double[] V1, double[] V2, double[] V3, double[] V4, double[] V5, double[] V6, int dT_s,
            string tagNameV1 = "Var1", string tagNameV2 = "Var2", string tagNameV3 = "Var3", string tagNameV4 = "Var4", string tagNameV5 = "Var5",
            string tagNameV6 = "Var6",
            bool plotAllOnLeftYaxis = true, bool useSubplots = false, string comment = null, DateTime t0 = new DateTime())
        {
            if (DetermineIfToPlot())
                Plot.Six(V1, V2, V3, V4, V5, V6, dT_s, tagNameV1, tagNameV2, tagNameV3, tagNameV4, tagNameV5, tagNameV6,
                plotAllOnLeftYaxis, useSubplots, comment, t0);
        }
*/
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
