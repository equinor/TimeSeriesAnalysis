using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeSeriesAnalysis.Utility;

namespace TimeSeriesAnalysis
{
    class Shared
    {
        private static ParserFeedback parserObj = new ParserFeedback();
        public static ParserFeedback GetParserObj()
        {
            return parserObj;
        }
    }
}