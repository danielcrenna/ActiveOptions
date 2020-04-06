// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ActiveOptions
{
	public static class Add
	{
		public static IServiceCollection AddValidOptions(this IServiceCollection services)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			services.AddOptions();

			services.TryAdd(ServiceDescriptor.Singleton(typeof(IValidOptions<>), typeof(ValidOptionsManager<>)));
			services.TryAdd(ServiceDescriptor.Scoped(typeof(IValidOptionsSnapshot<>), typeof(ValidOptionsManager<>)));
			services.TryAdd(ServiceDescriptor.Singleton(typeof(IValidOptionsMonitor<>), typeof(ValidOptionsMonitor<>)));

			services.TryAddEnumerable(
				ServiceDescriptor.Singleton<ICustomConfigurationBinder, TypeDiscriminatorBinder>());

			return services;
		}

		public static IServiceCollection AddSaveOptions(this IServiceCollection services)
		{
			services.AddOptions();
			services.TryAdd(ServiceDescriptor.Singleton(typeof(ISaveOptions<>), typeof(SaveOptionsManager<>)));
			return services;
		}

		public static IServiceCollection FastConfigure<TOptions>(this IServiceCollection services,
			IConfiguration config)
			where TOptions : class
		{
			return services.FastConfigure<TOptions>(Options.DefaultName, config);
		}

		public static IServiceCollection FastConfigure<TOptions>(this IServiceCollection services, string name,
			IConfiguration config) where TOptions : class
		{
			return services.FastConfigure<TOptions>(name, config, _ => { });
		}

		public static IServiceCollection FastConfigure<TOptions>(this IServiceCollection services,
			IConfiguration config,
			Action<BinderOptions> configureBinder)
			where TOptions : class
		{
			return services.FastConfigure<TOptions>(Options.DefaultName, config,
				configureBinder);
		}

		public static IServiceCollection FastConfigure<TOptions>(this IServiceCollection services, string name,
			IConfiguration config, Action<BinderOptions> configureBinder)
			where TOptions : class
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			if (config == null)
				throw new ArgumentNullException(nameof(config));

			services.AddOptions();
			services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(
				new ConfigurationChangeTokenSource<TOptions>(name, config));
			return services.AddSingleton<IConfigureOptions<TOptions>>(r =>
				new FastNamedConfigureFromConfigurationOptions<TOptions>(name, config, configureBinder,
					r.GetServices<ICustomConfigurationBinder>()));
		}

		public static IConfigurationBuilder AddSqlServerConfigurationProvider(this IConfigurationBuilder builder,
			string connectionString,
			IConfiguration configSeed = null)
		{
			return AddSqlServerConfigurationProvider(builder, connectionString, true, configSeed);
		}

		public static IConfigurationBuilder AddSqlServerConfigurationProvider(this IConfigurationBuilder builder,
			string connectionString, bool reloadOnChange, IConfiguration configSeed, Action<SaveConfigurationOptions> configureAction = null)
		{
			var saveConfig = new SaveConfigurationOptions();
			configureAction?.Invoke(saveConfig);
			return builder;
		}
	}
}