using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extensions on the <see cref="IConfiguration"/> for a more test dev-friendly experience.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// Extracts the value with the specified <paramref name="key"/> and converts it to type <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to convert the configuration value to.</typeparam>
        /// <param name="config">The configuration instance where the <paramref name="key"/> should be present.</param>
        /// <param name="key">The key of the configuration section's value to convert.</param>
        /// <exception cref="KeyNotFoundException">Thrown when no configuration value can be found for the given <paramref name="key"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the configuration value for the given <paramref name="key"/> is still a token and is not yet replaced.</exception>
        public static TValue GetRequiredValue<TValue>(this IConfiguration config, string key)
        {
            var value = config.GetValue<TValue>(key);
            if (value is null)
            {
                throw new KeyNotFoundException($"Cannot find configuration value for key '{key}' in test configuration settings");
            }

            if (value is string str && str.StartsWith("#{") && str.EndsWith("}#"))
            {
                throw new InvalidOperationException(
                    $"Cannot find configuration value for key '{key}' in test configuration settings because the token wasn't replaced yet");
            }

            return value;
        }
    }
}
