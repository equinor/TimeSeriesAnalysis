using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    /// <summary>
    /// To enable logging of errors and warnings throughout the codebase with a single line of code, 
    /// this class holds a "ParserFeedback" object.
    /// <example>
    /// Shared.GetParserObj().AddWarning("This is a warning that can be shownn on the console, written to file, or both");
    /// </example>
    /// </summary>
    public class Shared
    {
        private static ParserFeedback parserObj = new ParserFeedback();
        /// <summary>
        /// Returns the ParserFeedback object, which can be used to add loglines for info,warnings or errors
        /// </summary>
        /// <returns></returns>
        public static ParserFeedback GetParserObj()
        {
            return parserObj;
        }
    }
}