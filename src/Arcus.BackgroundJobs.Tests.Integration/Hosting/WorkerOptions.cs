using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arcus.Testing.Logging;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.BackgroundJobs.Tests.Integration.Hosting
{
    /// <summary>
    /// Represents the configurable options to influence the test <see cref="Worker"/>.
    /// </summary>
    public class WorkerOptions
    {
        private readonly ICollection<Action<IServiceCollection>> _additionalServices = new Collection<Action<IServiceCollection>>();
        private readonly ICollection<Action<IHostBuilder>> _additionalHostOptions = new Collection<Action<IHostBuilder>>();
        
        /// <summary>
        /// Gets the services that will be included in the test <see cref="Worker"/>.
        /// </summary>
        //public IServiceCollection Services { get; } = new ServiceCollection();

        /// <summary>
        /// Gets the configuration instance that will be included in the test <see cref="Worker"/> and which will result in an <see cref="IConfiguration"/> instance.
        /// </summary>
        public IDictionary<string, string> Configuration { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Adds an additional configuration option on the to-be-created <see cref="IHostBuilder"/>.
        /// </summary>
        /// <param name="additionalHostOption">The action that configures the additional option.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="additionalHostOption"/> is <c>null</c>.</exception>
        public WorkerOptions Configure(Action<IHostBuilder> additionalHostOption)
        {
            Guard.NotNull(additionalHostOption, nameof(additionalHostOption), "Requires an custom action that will add the additional hosting option");
            _additionalHostOptions.Add(additionalHostOption);

            return this;
        }

        /// <summary>
        /// Adds additional service(s) to the available registered services on the to-be-created <see cref="Worker"/>.
        /// </summary>
        /// <param name="configureServices">The function to register the service(s).</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureServices"/> is <c>null</c>.</exception>
        public WorkerOptions ConfigureServices(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices), "Requires a function to register the available application services");
            _additionalServices.Add(configureServices);
            
            return this;
        }

        /// <summary>
        /// Adds a <paramref name="logger"/> to the test worker instance to write diagnostic trace messages to the test output.
        /// </summary>
        /// <param name="logger">The test logger to write the diagnostic trace messages to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        public WorkerOptions ConfigureLogging(ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic trace messages to the test output");

            _additionalHostOptions.Add(hostBuilder =>
            {
                hostBuilder.ConfigureLogging(logging =>
                {
                    logging.AddProvider(new CustomLoggerProvider(logger))
                           .SetMinimumLevel(LogLevel.Trace);
                });
            });
            
            return this;
        }

        /// <summary>
        /// Applies the previously configured additional host options to the given <paramref name="hostBuilder"/>.
        /// </summary>
        /// <param name="hostBuilder">The builder instance to apply the additional host options to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="hostBuilder"/> is <c>null</c>.</exception>
        internal void ApplyOptions(IHostBuilder hostBuilder)
        {
            Guard.NotNull(hostBuilder, nameof(hostBuilder), "Requires a host builder instance to apply the worker options to");
            
            hostBuilder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(Configuration))
                       .ConfigureServices(services =>
                       {
                           foreach (Action<IServiceCollection> configureServices in _additionalServices)
                           {
                               configureServices(services);
                           }
                       });

            hostBuilder.ConfigureLogging(logging => logging.ClearProviders());
            
            foreach (Action<IHostBuilder> additionalHostOption in _additionalHostOptions)
            {
                additionalHostOption(hostBuilder);
            }
        }
    }
}
