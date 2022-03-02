using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TimeSeriesAnalysis.Utility
{

    ///<summary>
    /// Utility class to round double variables to a given nubmer of signficant digits.
    ///</summary>

    public static class SignificantDigits
    {

        ///<summary>
        ///  Returns number in scientific format with coefficient and exponential paramters
        ///</summary>
        public static void GetSciFormat(double number, out double coeff, out int exp)
        {
            coeff = 0;
            exp = 0;

            try
            {
                string numberAsSciStr = number.ToString("E6");
                string[] splitStr = numberAsSciStr.Split('E');

                if (splitStr.Length > 1)
                {
                    coeff = Double.Parse(splitStr[0]);
                    exp = Int32.Parse(splitStr[1]);
                }
            }
            catch 
            {

            }

        }

        /// <summary>
        /// Converts a scientific number on the format coeff*10^exp to a double
        /// </summary>
        /// <param name="coeff">coefficient</param>
        /// <param name="exp">exponent</param>
        /// <returns>converted double </returns>
        public static double SciToDouble(double coeff, int exp)
        {
            string format = coeff + "E" + exp;
            return Double.Parse(format);

        }

        ///<summary>
        /// Rounds down to number of significant digits (26->20 if digits=1 for instance)
        ///</summary>
        public static double Format(double number, int digits)
        {
            return Format(number, digits, out _);
        }
        ///<summary>
        /// Rounds down to number of significant digits (26->20 if digits=1 for instance)
        ///</summary>
        public static double Format(double number, int digits, out int exponent)
        {
            // scientific format : coefficient(double) times E^(exponent, an integer)
            string gridIntervalScientific = number.ToString("E" + (digits - 1));
            double parsed = Double.Parse(gridIntervalScientific);
            string[] splitStr = gridIntervalScientific.Split('E');
            if (splitStr.Length > 1)
            {
                exponent = Int32.Parse(splitStr[1].Replace("+",""));//"4e+001"->001
                return parsed;
            }
            else
            {
                exponent = 0;
                return number;
            }

        }
    }


}
