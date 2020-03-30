// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using ActiveOptions.Azure.Cosmos.Internal;
using ActiveStorage.Azure.Cosmos;
using ActiveStorage.Azure.Cosmos.Configuration;
using Microsoft.Extensions.Configuration;

namespace ActiveOptions.Azure.Cosmos
{
	public static class Add
	{
		public static IConfigurationBuilder AddCosmosConfigurationProvider(this IConfigurationBuilder builder,
			string connectionString, IConfiguration configureOptions, bool reloadOnChange = false,
			IConfiguration configSeed = null)
		{
			return builder.AddCosmosConfigurationProvider(o => { DefaultStorageOptions(connectionString, o); },
				configureOptions.FastBind,
				reloadOnChange, configSeed);
		}

		public static IConfigurationBuilder AddCosmosConfigurationProvider(this IConfigurationBuilder builder,
			string connectionString, bool reloadOnChange = false, IConfiguration configSeed = null,
			Action<SaveConfigurationOptions> configureOptions = null)
		{
			return builder.AddCosmosConfigurationProvider(o => { DefaultStorageOptions(connectionString, o); },
				configureOptions,
				reloadOnChange, configSeed);
		}

		public static IConfigurationBuilder AddCosmosConfigurationProvider(this IConfigurationBuilder builder,
			Action<CosmosStorageOptions> configureDatabase, Action<SaveConfigurationOptions> configureOptions = null,
			bool reloadOnChange = false, IConfiguration configSeed = null)
		{
			var dbConfig = new CosmosStorageOptions();
			configureDatabase?.Invoke(dbConfig);

			var saveConfig = new SaveConfigurationOptions();
			configureOptions?.Invoke(saveConfig);

			var source = new CosmosConfigurationSource(dbConfig, saveConfig, configSeed)
			{
				ReloadOnChange = reloadOnChange
			};

			builder.Add(source);
			return builder;
		}

		private static void DefaultStorageOptions(string connectionString, CosmosStorageOptions o)
		{
			var connectionStringBuilder = new CosmosConnectionStringBuilder(connectionString);
			o.AccountKey ??= connectionStringBuilder.AccountKey;
			o.AccountEndpoint ??= connectionStringBuilder.AccountEndpoint;
			o.DatabaseId ??= connectionStringBuilder.Database;
			o.ContainerId ??= connectionStringBuilder.DefaultCollection ?? Constants.Options.DefaultContainer;

			o.SharedCollection = false;
			o.PartitionKeyPaths = connectionStringBuilder.PartitionKeyPaths ?? new[] {"/id"};
		}
	}
}