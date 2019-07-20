using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unidevel.Extensions.Hosting
{
    public abstract class SafeBackgroundService : BackgroundService, IDisposable
    {
        public bool IsAlive { get; private set; }
        public SafeBackgroundServiceErrorHandlingOptions Options { get; } = new SafeBackgroundServiceErrorHandlingOptions();

        public SafeBackgroundService(ISafeBackgroundServicePanicHandler safeBackgroundServicePanicHandler, ILogger<SafeBackgroundService> logger = null)
        {
            _safeBackgroundServicePanicHandler = safeBackgroundServicePanicHandler ?? throw new ArgumentNullException(nameof(safeBackgroundServicePanicHandler));
            _logger = logger;
        }

        protected abstract Task DisconnectAsync();
        protected abstract Task ExecuteIterationAsync(CancellationToken cancellationToken);
        protected abstract Task ConnectAsync(CancellationToken cancellationToken);

        public sealed override Task StartAsync(CancellationToken startCancelledToken)
        {
            lock (stateChangeLock)
            {
                if (IsAlive)
                {
                    _logger?.LogError("Start failed: Already running.");
                    throw new InvalidOperationException("Already running.");
                }

                IsAlive = true;
            }

            return base.StartAsync(startCancelledToken);
        }

        protected sealed async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            IsAlive = true;

            try
            {
                _logger?.LogInformation("Loop started.");

                var disconnectRequired = false;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger?.LogTrace("Call to service's Connect started.");
                        disconnectRequired = true;
                        await ConnectAsync(cancellationToken);
                        _logger?.LogInformation("Call to service's Connect completed.");

                        try
                        {
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    _logger?.LogTrace("Call to service's Execute started.");
                                    await ExecuteIterationAsync(cancellationToken);
                                    _logger?.LogTrace("Call to service's Execute completed."); // LogTrace, because it may be often
                                    await Task.Yield(); // we do not use no-work sleeps or deep sleeps, but yield is here just to handle mistakes better
                                }
                                catch (OperationCanceledException)
                                {
                                    break;
                                }
                                catch (UnrecoverableBackgroundServiceException) { throw; }
                                catch (Exception errorReasonException)
                                {
                                    await Task.Delay(Options.ErrorSleep, cancellationToken);

                                    clearObsolete(nonObsoleteErrors, Options.ErrorTimeout);
                                    nonObsoleteErrors.Add(new Tuple<DateTime, Exception>(DateTime.UtcNow, errorReasonException));

                                    if (nonObsoleteErrors.Count >= Options.MaximumErrorCountBeforeFailure) throw new SafeBackgroundServiceInternalLoopNotStableException();
                                }
                            }
                        }
                        finally
                        {
                            // Disconnect phase which will be executed if completion or failure
                            // occured in Execute() method 

                            if (disconnectRequired)
                            {
                                _logger?.LogTrace("Call to service's Disconnect started.");
                                try
                                {
                                    disconnectRequired = false; // before, because we do not want to repeat it even if it fails
                                    await DisconnectAsync();
                                    _logger?.LogInformation("Call to service's Disconnect completed.");
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogCritical(ex, "Call to service's Disconnect failed. This is not allowed. Rethrowing.");
                                    throw;
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogInformation("Cancellation token set. Leaving loop.");
                        break;
                    }
                    catch (UnrecoverableBackgroundServiceException) { throw; }
                    catch (Exception failureReasonException)
                    {
                        await Task.Delay(Options.FailureSleep, cancellationToken);

                        clearObsolete(nonObsoleteFailures, Options.FailureTimeout);
                        nonObsoleteFailures.Add(new Tuple<DateTime, Exception>(DateTime.UtcNow, failureReasonException));

                        if (nonObsoleteFailures.Count >= Options.MaximumFailureCountBeforePanic) throw new SafeBackgroundServiceExternalLoopNotStableException();

                        nonObsoleteErrors.Clear(); // otherwise we wouldn't reliably continue after restart
                    }
                    finally
                    {
                        // Disconnect phase which will be executed if completion failure
                        // occured in Connect() method.

                        if (disconnectRequired)
                        {
                            _logger?.LogTrace("Call to service's Disconnect started.");
                            try
                            {
                                disconnectRequired = false; // before, because we do not want to repeat it even if it fails
                                await DisconnectAsync();
                                _logger?.LogInformation("Call to service's Disconnect completed.");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogCritical(ex, "Call to service's Disconnect failed. This is not allowed. Rethrowing.");
                                throw;
                            }
                        }
                    }
                }
                _logger?.LogInformation("Loop completed.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Loop failed.");
                _safeBackgroundServicePanicHandler?.HandlePanic(ex);
            }
            finally
            {
                IsAlive = false;
            }
        }

        private readonly ILogger<SafeBackgroundService> _logger;
        private readonly ISafeBackgroundServicePanicHandler _safeBackgroundServicePanicHandler;
        private readonly object stateChangeLock = new object();
        private List<Tuple<DateTime, Exception>> nonObsoleteErrors = new List<Tuple<DateTime, Exception>>();
        private List<Tuple<DateTime, Exception>> nonObsoleteFailures = new List<Tuple<DateTime, Exception>>();

        private void clearObsolete(List<Tuple<DateTime, Exception>> listToClear, TimeSpan obsoleteTime)
        {
            var obsoleteHorizon = DateTime.UtcNow.Subtract(obsoleteTime);
            listToClear.RemoveAll(p => p.Item1 <= obsoleteHorizon);
        }
    }
}
