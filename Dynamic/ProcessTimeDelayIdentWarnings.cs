namespace TimeSeriesAnalysis.Dynamic
{
    /// <summary>
    /// Enum of recognized warning or error states during identification of time delays
    /// </summary>
    public enum ProcessTimeDelayIdentWarnings
    {
        /// <summary>
        /// No errors or warnings
        /// </summary>
        Nothing = 0, 

        /// <summary>
        /// There are several eqvivalently good time delay estiamtes when judged by Rsquared.
        /// </summary>
        NoUniqueRsquaredMinima = 1,

        /// <summary>
        /// There are other good time delay estimates based on Rsquared that are not near the best value 
        /// This is an indication that something is wrong.
        /// </summary>
        NonConvexRsquaredSolutionSpace = 2,

        /// <summary>
        /// There are several eqvivalently good time delay estiamtes when judged by objective function value.
        /// </summary>
        NoUniqueObjectiveFunctionMinima = 3,

        /// <summary>
        /// There are other good time delay estimates based on objective function value that are not near the best value 
        /// This is an indication that something is wrong.
        /// </summary>
        NonConvexObjectiveFunctionSolutionSpace = 4,







    }
}
