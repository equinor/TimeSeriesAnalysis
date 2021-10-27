using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Handles naming of individual signals in a process simulation
    /// </summary>
    class SignalNamer
    {
        private const char separator = '-';// should not be "_"




        public static string GetSignalName(ISimulatableModel model, SignalType signalType, int idx = 0)
        {
            return GetSignalName(model.GetID(), signalType,idx);
        }


        /// <summary>
        /// Get a unique signal name for a given signal, based on the model and signal type.
        /// </summary>
        /// <param name="modelID"></param>
        /// <param name="signalType"></param>
        /// <param name="idx">models can have multiple inputs, in which case an index is needed to uniquely identify it.</param>
        /// <returns>a unique string identifier that is used to identify a signal</returns>
        public static string GetSignalName(string modelID, SignalType signalType, int idx = 0)
        {
            if (idx == 0)
                return modelID + separator + signalType.ToString();
            else
                return modelID + separator + signalType.ToString() + separator + idx.ToString();
        }

        public static int GetNumberOfSignalsOfType(string[] signals, SignalType type)
        {
            int counter = 0;
            for(int i=0;i<signals.Length; i++)
            {
                if (GetSignalType(signals[i]) == type)
                {
                    counter++;
                }
            }
            return counter;
        }

        public static SignalType GetSignalType(string signal)
        {
            string[] split = signal.Split(separator);
            if (split.Length < 2)
                return SignalType.Unset;
            string typeString = split[1];

            if (typeString.Contains("PID_U"))
            {
                return SignalType.PID_U;
            }
            if (typeString.Contains("Setpoint_Yset"))
            {
                return SignalType.Setpoint_Yset;
            }
            if (typeString.Contains("NonPIDInternal_U"))
            {
                return SignalType.NonPIDInternal_U;
            }
            if (typeString.Contains("External_U"))
            {
                return SignalType.External_U;
            }
            if (typeString.Contains("Distubance_D"))
            {
                return SignalType.Distubance_D;
            }
            if (typeString.Contains("Output_Y_sim"))
            {
                return SignalType.Output_Y_sim;
            }
            return SignalType.Unset;
        }

        public static int[] GetIndexOfSignalType(string[] signals, SignalType type)
        {
            List<int> ret = new List<int>();

            for (int i = 0; i < signals.Length; i++)
            {
                if (GetSignalType(signals[i]) == type)
                {
                    ret.Add(i);
                }
            }
            return ret.ToArray();
        }




    }
}
