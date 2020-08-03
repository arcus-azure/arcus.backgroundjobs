using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.Tests.Unit.Databricks
{
    /// <summary>
    /// Spy (stub) <see cref="ILogger{TCategoryName}"/> implementation to track the logged messages.
    /// </summary>
    /// <typeparam name="T">The type who's name is used for the logger category name.</typeparam>
    public class SpyLogger<T> : ILogger<T>
    {
        private readonly ICollection<string> _messages = new Collection<string>();

        /// <summary>
        /// Gets the current logged messages.
        /// </summary>
        public IEnumerable<string> Messages => _messages.AsEnumerable();

        /// <summary>Writes a log entry.</summary>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">Id of the event.</param>
        /// <param name="state">The entry to be written. Can be also an object.</param>
        /// <param name="exception">The exception related to this entry.</param>
        /// <param name="formatter">Function to create a <see cref="T:System.String" /> message of the <paramref name="state" /> and <paramref name="exception" />.</param>
        /// <typeparam name="TState">The type of the object to be written.</typeparam>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Guard.NotNull(formatter, nameof(formatter));

            string message = formatter(state, exception);
            _messages.Add(message);
        }

        /// <summary>
        /// Checks if the given <paramref name="logLevel" /> is enabled.
        /// </summary>
        /// <param name="logLevel">level to be checked.</param>
        /// <returns><c>true</c> if enabled.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>Begins a logical operation scope.</summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
        /// <returns>An <see cref="T:System.IDisposable" /> that ends the logical operation scope on dispose.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
