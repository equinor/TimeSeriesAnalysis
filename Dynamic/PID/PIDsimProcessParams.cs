using TimeSeriesAnalysis;

namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Class that contains the paramters that determine the process behavior
    /// of the process to be simulated in ProcessPIDsim
    /// </summary>
    public class PIDsimProcessParams
    {
        double yNoiseAmp_abs;
        double processGainYChangeWhenUChangesOnePercent;
        double processTimeConstant_s = 0;
        int processTimeDelay_samples = 0;
        double ymeasBias = 0;
        bool isPIDoutputDelayOneSample = true;
        double y0 = 0;
        double u0 = 0;


        public PIDsimProcessParams(double yNoiseAmp_abs,
            double processGainYChangeWhenUChangesOnePercent,
            double processTimeConstant_s = 0,
            int processTimeDelay_samples = 0,
            double ymeasBias = 0,
            bool isPIDoutputDelayOneSample = true,
            double y0 = 0, 
            double u0 = 0)
        {
            this.yNoiseAmp_abs = yNoiseAmp_abs;
            this.processGainYChangeWhenUChangesOnePercent = processGainYChangeWhenUChangesOnePercent;
            this.processTimeConstant_s = processTimeConstant_s;
            this.processTimeDelay_samples = processTimeDelay_samples;
            this.ymeasBias = ymeasBias;
            this.isPIDoutputDelayOneSample= isPIDoutputDelayOneSample;
            this.y0 = y0;
            this.u0 = u0;
        }

        //
        // Setters
        //

        /// <summary>
        /// Sets the "y0" the absolute, constant value to be added to y that is independent of u
        /// </summary>

        public void SetY0(double y0)
        {
            this.y0 = y0;
        }


        /// <summary>
        /// Sets the "y0" the absolute, constant value to be added to y that is independent of u
        /// </summary>

        public void SetU0(double u0)
        {
            this.u0 = u0;
        }

        /// <summary>
        /// Sets a bias to be added to the process measurement in absolute values.
        /// </summary>
        public void SetMeasBias(double ymeasBias)
        {
            this.ymeasBias = ymeasBias;
        }

        /// <summary>
        /// Sets the amplitude of noise to be added to the process measurement
        /// </summary>
        public void SetNoiseAmplitude(double yNoiseAmp_abs)
        {
            this.yNoiseAmp_abs = yNoiseAmp_abs;
        }

        /// <summary>
        /// Sets the (first-order) process time constants in seconds of the process.
        /// </summary>
        public void SetTimeConstant_s(double processTimeConstant_s)
        {
            this.processTimeConstant_s = processTimeConstant_s;
        }

        /// <summary>
        /// Sets the time delay in samples of the process
        /// </summary>
        public void SetTimeDelay(int processTimeDelay_samples)
        { 
            this.processTimeDelay_samples = processTimeDelay_samples;
        }

        /// <summary>
        /// Determines if the pid controller output to a given y[k] will be implemented at u[k](false) or u[k+1](true)
        /// </summary>
        public void SetIsPIDoutputDelayedOneSample(bool isPIDoutputDelayOneSample)
        {
            this.isPIDoutputDelayOneSample = isPIDoutputDelayOneSample;
        }

        //
        // Getters
        //

        /// <summary>
        ///Get y0, the constant value to be added to y that is independent of u
        /// </summary>

        public double GetY0()
        {
            return y0;
        }


        /// <summary>
        ///Get u0, the value of u at wich y = y0
        /// </summary>

        public double GetU0()
        {
            return u0;
        }


        /// <summary>
        /// Gets the process time constant in seconds, by definition the time it takes the process to get 67% of the way towards
        /// its new steady steady following a step change.
        /// </summary>
        public double GetTimeConstant_s()
        {
            return processTimeConstant_s;
        }

        /// <summary>
        /// The number of time steps the process is delayed(in samples).
        /// </summary>
        public int GetTimeDelay_samples()
        {
            return processTimeDelay_samples;
        }

        /// <summary>,
        /// Typically the PID input based on y at time k will be implemented at time k+1 (i.e delayed one sample, corresponds to "true")
        /// but if analyzing a dataset with a lower sampling rate than the clock frequency of the controller, the 
        /// controller input will sometimes appear to be occur for the time sample k (corrsponds to "false")
        /// </summary>

        public bool IsPIDinputDelayedOneSample()
        {
            return isPIDoutputDelayOneSample;
        }

        /// <summary>
        /// Gets the measurement noise amplitude  in absolute values
        /// </summary>
        public double GetMeasurementBias()
        {
            return ymeasBias;
        }

        /// <summary>
        /// Gets the noise in absolute values
        /// </summary>

        public double GetNoiseAmplitude()
        {
            return yNoiseAmp_abs;
        }

        /// <summary>
        /// Gets the process gain, in terms of how much y will increase when u changes 1%.
        /// </summary>
        public double GetProcessGain()
        {
            return processGainYChangeWhenUChangesOnePercent;
        }


    }
}
