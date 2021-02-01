using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis
{
    public static class VecExtensionMethods
    {
        public static string ToString(this double[] array,int nSignificantDigits)
        {
            StringBuilder sb = new StringBuilder();
            if (array.Length > 0)
            {
                sb.Append("[");
                sb.Append(SignificantDigits.Format(array[0], nSignificantDigits));
                for (int i = 1; i < array.Length; i++)
                {
                    sb.Append(";");
                    sb.Append(SignificantDigits.Format(array[i], nSignificantDigits) );
                }
                sb.Append("]");
            }
            return sb.ToString();
        }
    }
}
