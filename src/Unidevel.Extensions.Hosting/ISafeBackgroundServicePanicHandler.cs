using System;

namespace Unidevel.Extensions.Hosting
{
    public interface ISafeBackgroundServicePanicHandler
    {
        void HandlePanic(Exception reasonException);
    }
}
