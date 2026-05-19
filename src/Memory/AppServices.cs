/*
 * @project  : ApexGate nEdit
 * @website  : https://www.apexgate.net
 * @license  : MIT
 */

using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace NEdit.Memory
{
    /// <summary>
    /// Provides a mutable application service container for dependency injection.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class AppServices
    {
        /// <summary>
        /// Gets the current service provider.
        /// </summary>
        /// <value>
        /// The provider built from the registered services.
        /// </value>
        public IServiceProvider ServiceProvider
        {
            get
            {
                lock (SyncLock)
                {
                    // Ensure we never return null to avoid crashes in ActivatorUtilities
                    return field ??= new ServiceCollection().BuildServiceProvider();
                }
            }
            private set
            {
                lock (SyncLock)
                {
                    field = value;
                }
            }
        }

        /// <summary>
        /// Gets the singleton <see cref="AppServices"/> instance.
        /// </summary>
        private static AppServices Instance => _instance ?? GetInstance();

        /// <summary>
        /// Stores the singleton <see cref="AppServices"/> instance.
        /// </summary>
        private static volatile AppServices? _instance;

        /// <summary>
        /// Synchronizes access to the service collection and provider.
        /// </summary>
        private static readonly Lock SyncLock = new();

        /// <summary>
        /// Gets or sets the current service collection.
        /// </summary>
        /// <value>
        /// The mutable service registration collection.
        /// </value>
        public static ServiceCollection ServiceCollection
        {
            get
            {
                lock (SyncLock)
                {
                    return field ??= new ServiceCollection();
                }
            }
            set
            {
                lock (SyncLock)
                {
                    field = value;
                }
            }
        }

        /// <summary>
        /// Initializes static members of the <see cref="AppServices"/> class.
        /// </summary>
        static AppServices()
        {
            // Initialize immediately to prevent null scenarios.
            ServiceCollection = new ServiceCollection();
        }

        /// <summary>
        /// Initializes the service collection with caller-provided registrations.
        /// </summary>
        /// <param name="action">The registration callback that configures services.</param>
        public static void Init(Action<ServiceCollection> action)
        {
            lock (SyncLock)
            {
                var services = new ServiceCollection();
                action.Invoke(services);

                // Replace the static collection and rebuild the provider
                ServiceCollection = services;
                Instance.ServiceProvider = services.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Registers a singleton service type.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        public static void AddSingleton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class
        {
            lock (SyncLock)
            {
                // Prevent duplicates, because this isn't an instance we'll not throw
                // an exception here on duplicates.
                if (ServiceCollection.Any(x => x.ServiceType == typeof(T)))
                {
                    return;
                }

                ServiceCollection.AddSingleton<T>();
                Instance.ServiceProvider = ServiceCollection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Registers a singleton service instance.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="instance">The service instance.</param>
        /// <exception cref="InvalidOperationException">
        /// A singleton registration already exists for <typeparamref name="T" />.
        /// </exception>
        public static void AddSingleton<T>(T instance) where T : class
        {
            lock (SyncLock)
            {
                // Prevent duplicates which can confuse the provider
                if (ServiceCollection.Any(x => x.ServiceType == typeof(T)))
                {
                    throw new InvalidOperationException($"{typeof(T).Name} already has a singleton instance registered.");
                }

                ServiceCollection.AddSingleton(instance);
                Instance.ServiceProvider = ServiceCollection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Registers a singleton service instance for the specified service type.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="implementationInstance">The service instance.</param>
        /// <exception cref="InvalidOperationException">
        /// A singleton registration already exists for <paramref name="serviceType" />.
        /// </exception>
        public static void AddSingleton(Type serviceType, object implementationInstance)
        {
            lock (SyncLock)
            {
                if (ServiceCollection.Any(x => x.ServiceType == serviceType))
                {
                    throw new InvalidOperationException($"{serviceType.GetType().Name} already has a singleton instance registered.");
                }

                ServiceCollection.AddSingleton(serviceType, implementationInstance);
                Instance.ServiceProvider = ServiceCollection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Adds service registrations to the current service collection.
        /// </summary>
        /// <param name="action">The registration callback that configures services.</param>
        public static void AddService(Action<ServiceCollection> action)
        {
            lock (SyncLock)
            {
                action.Invoke(ServiceCollection);
                Instance.ServiceProvider = ServiceCollection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>
        /// The resolved service instance, or <see langword="null" /> when the service is not registered.
        /// </returns>
        public static T? GetService<T>()
        {
            return Instance.ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Gets a service of the provided type.
        /// </summary>
        /// <param name="type">The service type to resolve.</param>
        /// <returns>
        /// The resolved service instance, or <see langword="null" /> when the service is not registered.
        /// </returns>
        public static object? GetService(Type type)
        {
            return Instance.ServiceProvider.GetService(type);
        }

        /// <summary>
        /// Gets a required service of the specified type.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <returns>
        /// The resolved service instance.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// No service is registered for <typeparamref name="T" />.
        /// </exception>
        public static T GetRequiredService<T>() where T : notnull
        {
            return Instance.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Gets a required service of the provided type.
        /// </summary>
        /// <param name="type">The service type to resolve.</param>
        /// <returns>
        /// The resolved service instance.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// No service is registered for <paramref name="type" />.
        /// </exception>
        public static object GetRequiredService(Type type)
        {
            return Instance.ServiceProvider.GetRequiredService(type);
        }

        /// <summary>
        /// Creates an instance with constructor dependencies resolved from the service provider.
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <returns>
        /// The created instance.
        /// </returns>
        public static T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        {
            return ActivatorUtilities.CreateInstance<T>(Instance.ServiceProvider);
        }

        /// <summary>
        /// Creates an instance with constructor dependencies resolved from the service provider.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <returns>
        /// The created instance.
        /// </returns>
        public static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            return ActivatorUtilities.CreateInstance(Instance.ServiceProvider, type);
        }

        /// <summary>
        /// Gets the current instance or creates a new instance.
        /// </summary>
        /// <returns>
        /// The singleton <see cref="AppServices"/> instance.
        /// </returns>
        private static AppServices GetInstance()
        {
            // Double-check locking for performance and thread safety
            if (_instance != null)
            {
                return _instance;
            }

            lock (SyncLock)
            {
                return _instance ??= new AppServices();
            }
        }

        /// <summary>
        /// Rebuilds the <see cref="ServiceProvider"/> from the current <see cref="ServiceCollection"/>.
        /// </summary>
        public static void BuildServiceProvider()
        {
            lock (SyncLock)
            {
                // We access the backing field directly or via the property, 
                // but since we are inside the lock, we ensure atomicity.
                Instance.ServiceProvider = ServiceCollection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Determines whether a service type has been registered.
        /// </summary>
        /// <param name="type">The service type to inspect.</param>
        /// <returns>
        /// <see langword="true" /> if the service type is registered; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsRegistered(Type type)
        {
            lock (SyncLock)
            {
                return ServiceCollection.Any(sd => sd.ServiceType == type);
            }
        }

        /// <summary>
        /// Determines whether a service type has been registered.
        /// </summary>
        /// <typeparam name="T">The service type to inspect.</typeparam>
        /// <returns>
        /// <see langword="true" /> if the service type is registered; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsRegistered<T>()
        {
            lock (SyncLock)
            {
                return ServiceCollection.Any(sd => sd.ServiceType == typeof(T));
            }
        }

        /// <summary>
        /// Determines whether a singleton service type has been registered.
        /// </summary>
        /// <typeparam name="T">The service type to inspect.</typeparam>
        /// <returns>
        /// <see langword="true" /> if a singleton service is registered; otherwise, <see langword="false" />.
        /// </returns>
        public static bool IsSingletonRegistered<T>()
        {
            lock (SyncLock)
            {
                return ServiceCollection.Any(sd => sd.ServiceType == typeof(T) && sd.Lifetime == ServiceLifetime.Singleton);
            }
        }
    }
}
