using System;

namespace Unidevel.Extensions.Hosting
{
    public class SafeBackgroundServiceErrorHandlingOptions
    {
        public TimeSpan ErrorTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan FailureTimeout { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan ErrorSleep { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan FailureSleep { get; set; } = TimeSpan.FromMinutes(1);
        public int MaximumErrorCountBeforeFailure { get; set; } = 25;
        public int MaximumFailureCountBeforePanic { get; set; } = 15;
    }
}
