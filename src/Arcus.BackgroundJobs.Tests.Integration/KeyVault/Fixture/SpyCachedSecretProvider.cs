using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Core.Caching.Configuration;
using GuardNet;

namespace Arcus.BackgroundJobs.Tests.Integration.KeyVault.Fixture
{
    /// <summary>
    /// Represents a stubbed version of an <see cref="ICachedSecretProvider"/> that spies on invalidating secrets.
    /// </summary>
    public class SpyCachedSecretProvider : ICachedSecretProvider
    {
        private readonly string _staticSecretValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpyCachedSecretProvider" /> class.
        /// </summary>
        public SpyCachedSecretProvider(string staticSecretValue)
        {
            Guard.NotNull(staticSecretValue, nameof(staticSecretValue));
            _staticSecretValue = staticSecretValue;
        }

        /// <summary>Gets the cache-configuration for this instance.</summary>
        public ICacheConfiguration Configuration { get; }

        /// <summary>
        /// Gets the value indicating whether or not the secret name is invalidated.
        /// </summary>
        public bool IsSecretInvalidated { get; private set; }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns the secret key.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<string> GetRawSecretAsync(string secretName)
        {
            return Task.FromResult(_staticSecretValue);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="T:Arcus.Security.Core.Secret" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<Secret> GetSecretAsync(string secretName)
        {
            return Task.FromResult(new Secret(_staticSecretValue));
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="T:System.Threading.Tasks.Task`1" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The name must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The name must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<string> GetRawSecretAsync(string secretName, bool ignoreCache)
        {
            return Task.FromResult(_staticSecretValue);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="T:System.Threading.Tasks.Task`1" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The name must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The name must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<Secret> GetSecretAsync(string secretName, bool ignoreCache)
        {
            return Task.FromResult(new Secret(_staticSecretValue));
        }

        /// <summary>
        /// Removes the secret with the given <paramref name="secretName" /> from the cache;
        /// so the next time <see cref="M:Arcus.Security.Core.Caching.CachedSecretProvider.GetSecretAsync(System.String)" /> is called, a new version of the secret will be added back to the cache.
        /// </summary>
        /// <param name="secretName">The name of the secret that should be removed from the cache.</param>
        public Task InvalidateSecretAsync(string secretName)
        {
            IsSecretInvalidated = secretName == _staticSecretValue;
            return Task.CompletedTask;
        }
    }
}
