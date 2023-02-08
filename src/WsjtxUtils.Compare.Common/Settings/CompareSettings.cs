namespace WsjtxUtils.Compare.Common.Settings
{
    public class CompareSettings
    {
        /// <summary>
        /// Compare utility settings constructor
        /// </summary>
        public CompareSettings() : this(new ServerSettings()) { }

        /// <summary>
        /// Compare utility settings constructor
        /// </summary>
        /// <param name="server"></param>
        public CompareSettings(ServerSettings server)
        {
            Server = server;
        }

        /// <summary>
        /// Server settings
        /// </summary>
        public ServerSettings Server { get; set; }

        /// <summary>
        /// The time in second that periodically used to check for decodes
        /// </summary>
        public double PeriodicTimerSeconds { get; set; } = 1.0;

        /// <summary>
        /// The amount of time a message spends in the buffer without a match before being considered uncorrelated
        /// </summary>
        public double AmountOfSecondsInBufferWithNoMatchIsUncorrelated { get; set; } = 30.0;

        /// <summary>
        /// The amount of time in seconds, based on the decode time, that a message is considered correlated
        /// </summary>
        public double IsCorrelatedDecodeWiggleTimeInSeconds { get; set; } = 15.0;

        /// <summary>
        /// Filename and path to correlated decode file
        /// </summary>
        public string CorrelatedDecodesFile { get; set; } = "correlated-decodes-ab.csv";

        /// <summary>
        /// Filename and path to correlated decode file
        /// </summary>
        public string UncorrelatedDecodesFileSourceA { get; set; } = "uncorrelated-decodes-a.csv";

        /// <summary>
        /// Filename and path to correlated decode file
        /// </summary>
        public string UncorrelatedDecodesFileSourceB { get; set; } = "uncorrelated-decodes-b.csv";
    }
}
