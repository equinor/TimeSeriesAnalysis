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
        public static string DecimalSeparator;

        static SignificantDigits()
        {
        }

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
            catch (Exception e)
            {

            }

        }

        public static double sciToDouble(double coeff, int exp)
        {
            string format = coeff + "E" + exp;
            return Double.Parse(format);

        }

        ///<summary>
        /// Rounds down to number of significant digits (26->20 if digits=1 for instance)
        ///</summary>
        public static double Format(double number, int digits)
        {
            int exp;
            return Format(number, digits, out exp);
        }
        ///<summary>
        /// Rounds down to number of significant digits (26->20 if digits=1 for instance)
        ///</summary>
        public static double Format(double number, int digits, out int exponent)
        {

            string gridIntervalScientific = number.ToString("E" + (digits - 1));
            double parsed = Double.Parse(gridIntervalScientific);
            string[] splitStr = gridIntervalScientific.Split('E');
            if (splitStr.Length > 1)
            {
                exponent = Int32.Parse(splitStr[1]);//"4e+001"->001

                if (number > 0)
                {
                    if (number < parsed)
                        parsed = parsed - Math.Pow(10, exponent - digits + 1);
                }
                else if (number < 0)
                {
                    if (number > parsed)
                        parsed = parsed + Math.Pow(10, exponent - digits + 1);
                }
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
