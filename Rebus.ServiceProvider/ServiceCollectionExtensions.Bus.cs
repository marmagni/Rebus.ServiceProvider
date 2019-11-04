﻿using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configureRebus">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configureRebus)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureRebus == null) throw new ArgumentNullException(nameof(configureRebus));
            
            return AddRebus(services, (c, p) => configureRebus(c));
        }

        /// <summary>
        /// Registers and/or modifies Rebus configuration for the current service collection.
        /// </summary>
        /// <param name="services">The current message service builder.</param>
        /// <param name="configureRebus">The optional configuration actions for Rebus.</param>
        public static IServiceCollection AddRebus(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configureRebus)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureRebus == null) throw new ArgumentNullException(nameof(configureRebus));

            var messageBusRegistration = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IBus));

            if (messageBusRegistration != null)
            {
                throw new InvalidOperationException(@"Sorry, but it seems like Rebus has already been configured in this service collection.");
            }

            services.AddTransient(s => MessageContext.Current ?? throw new InvalidOperationException("Attempted to resolve IMessageContext outside of a Rebus handler, which is not possible. If you get this error, it's probably a sign that your service provider is being used outside of Rebus, where it's simply not possible to resolve a Rebus message context. Rebus' message context is only available to code executing inside a Rebus handler."));
            services.AddTransient(s => s.GetService<IBus>().Advanced.SyncBus);

            // Register the Rebus Bus instance, to be created when it is first requested.
            services.AddSingleton(provider => new DependencyInjectionHandlerActivator(provider));
            services.AddSingleton(provider =>
            {
                var configurer = Configure.With(provider.GetRequiredService<DependencyInjectionHandlerActivator>());

                configureRebus(configurer, provider);

                return configurer.Start();
            });

            return services;
        }
    }
}
