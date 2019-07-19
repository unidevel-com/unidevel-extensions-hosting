using System;

namespace Unidevel.Extensions.Hosting
{
    public class SafeBackgroundServiceInternalLoopNotStableException : BackgroundServiceNotStableException
    {
    }

    public class SafeBackgroundServiceExternalLoopNotStableException : BackgroundServiceNotStableException
    {
    }

    public class BackgroundServiceNotStableException : Exception
    {
    }

    public class UnrecoverableBackgroundServiceException : Exception
    {
        public UnrecoverableBackgroundServiceException() : base()
        {
        }

        public UnrecoverableBackgroundServiceException(string message) : base(message)
        {
        }
    }
}
