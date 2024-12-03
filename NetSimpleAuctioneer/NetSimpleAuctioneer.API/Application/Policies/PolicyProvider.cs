using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;

namespace NetSimpleAuctioneer.API.Application.Policies
{
    public interface IPolicyProvider
    {
        /// <summary>
        /// Gets the retry policy for handling database update exceptions..
        /// Retries up to 3 times with exponential backoff (2, 4, 8 seconds).
        /// </summary>
        /// <returns>A retry policy that retries up to 3 times with exponential backoff.</returns>
        IAsyncPolicy GetRetryPolicy();

        /// <summary>
        /// Gets the circuit breaker policy for handling database update exceptions. 
        /// Breaks the circuit after 2 consecutive failures and keeps it broken for 20 seconds.
        /// </summary>
        /// <returns>A circuit breaker policy that breaks after 2 consecutive failures for 1 minute.</returns>
        IAsyncPolicy GetCircuitBreakerPolicy();

        /// <summary>
        /// Gets the retry policy specifically designed to handle concurrency exceptions.
        /// Retries up to 3 times with exponential backoff (2, 4, 8 seconds).
        /// </summary>
        /// <returns>A retry policy that retries up to 3 times with exponential backoff.</returns>
        IAsyncPolicy GetRetryPolicyWithConcurrencyException();
    }

    public class PolicyProvider(ILogger<PolicyProvider> logger) : IPolicyProvider
    {
        public IAsyncPolicy GetRetryPolicy()
        {
            return Policy.Handle<DbUpdateException>()
                         .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                                            // Log each retry attempt and the backoff time
                                            (exception, timeSpan, attempt, context) =>
                                            {
                                                logger.LogWarning("Attempt {Attempt} failed due to exception: {Exception}. Retrying in {TimeSpan} seconds.",
                                                                    attempt, exception.Message, timeSpan.TotalSeconds);
                                            });
        }

        public IAsyncPolicy GetCircuitBreakerPolicy()
        {
            return Policy.Handle<DbUpdateException>()
                         .CircuitBreakerAsync(2, TimeSpan.FromSeconds(20),
                                              // Log when the circuit is broken due to multiple failures
                                              onBreak: (exception, timespan) =>
                                              {
                                                  logger.LogError("Circuit breaker triggered due to multiple failures: {Exception}. The system will be paused for {TimeSpan} seconds.",
                                                                   exception.Message, timespan.TotalSeconds);
                                              },
                                              // Log when the circuit breaker is reset and operations can be retried
                                              onReset: () =>
                                              {
                                                  logger.LogInformation("Circuit breaker reset. Retrying operations after 20 seconds.");
                                              });
        }

        public IAsyncPolicy GetRetryPolicyWithConcurrencyException()
        {
            return Policy.Handle<DbUpdateException>(ex => !(ex.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505"))
                         .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                                            // Log each retry attempt and the backoff time
                                            (exception, timeSpan, attempt, context) =>
                                            {
                                                logger.LogWarning("Attempt {Attempt} failed due to exception: {Exception}. Retrying in {TimeSpan} seconds.",
                                                                    attempt, exception.Message, timeSpan.TotalSeconds);
                                            });
        }
    }
}