// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using ActiveRoutes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TypeKitchen;

namespace ActiveOptions.Api
{
	public static class Add
	{
		public static IServiceCollection AddConfigurationApi(this IServiceCollection services,
			IConfigurationRoot configurationRoot, IConfiguration config)
		{
			return AddConfigurationApi(services, configurationRoot, config.FastBind);
		}

		public static IServiceCollection AddConfigurationApi(this IServiceCollection services,
			IConfigurationRoot configurationRoot, Action<ConfigurationApiOptions> configureAction = null)
		{
			services.AddSingleton(configurationRoot);
			services.AddActiveRouting(mvcBuilder =>
			{
				mvcBuilder.AddConfigurationApi(configureAction);
			});
			return services;
		}

		public static IMvcCoreBuilder AddConfigurationApi(this IMvcCoreBuilder mvcBuilder,
			IConfigurationRoot configurationRoot, IConfiguration config)
		{
			mvcBuilder.Services.AddSingleton(configurationRoot);
			return AddConfigurationApi(mvcBuilder, config.FastBind);
		}

		public static IMvcCoreBuilder AddConfigurationApi(this IMvcCoreBuilder mvcBuilder,
			Action<ConfigurationApiOptions> configureAction = null)
		{
			if (configureAction != null)
				mvcBuilder.Services.Configure(configureAction);

			mvcBuilder.Services.AddValidOptions();
			mvcBuilder.Services.AddSaveOptions();

			mvcBuilder.Services.TryAddSingleton<ITypeResolver, ReflectionTypeResolver>();
			mvcBuilder.Services.AddSingleton<ConfigurationService>();

			mvcBuilder.AddActiveRoute<ConfigurationController, ConfigurationFeature, ConfigurationApiOptions>();

			return mvcBuilder;
		}
	}
}