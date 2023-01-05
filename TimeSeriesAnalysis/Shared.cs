using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// Globals 
    /// <para>
    /// Should only be used for logging, setting/getting configurations.
    /// </para>
    /// </summary>
    public class Shared
    {
        private static ParserFeedback parserObj = new ParserFeedback();

        private static bool isPlottingEnabled = false;
        /// <summary>
        /// Returns the ParserFeedback object, which can be used to add loglines for info,warnings or errors
        /// </summary>
        /// <example>Example usage:
        /// <code>
        /// Shared.GetParserObj().AddWarning("This is a warning that can be shown on the console, written to file, or both");
        /// </code></example>
        /// <returns>the Parserfeedback object</returns>
        public static ParserFeedback GetParserObj()
        {
            return parserObj;
        }

        /// <summary>
        /// Disables all plotting 
        /// </summary>
        public static void DisablePlots()
        {
            isPlottingEnabled = false;
        }

        /// <summary>
        /// Enables all plotting 
        /// </summary>
        public static void EnablePlots()
        {
            isPlottingEnabled = true;
        }

        /// <summary>
        /// Queries if plotting is enabled or not 
        /// </summary>
        public static bool IsPlottingEnabled()
        {
            return isPlottingEnabled;
        }


    }
}