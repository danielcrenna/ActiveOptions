// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ActiveOptions.Azure.Cosmos.Internal;
using ActiveStorage.Azure.Cosmos.Configuration;
using Microsoft.Extensions.Configuration;

namespace ActiveOptions.Azure.Cosmos
{
	public class CosmosConfigurationSource : IConfigurationSource
	{
		public CosmosConfigurationSource(CosmosStorageOptions options, SaveConfigurationOptions saveConfig,
			IConfiguration configSeed = null)
		{
			SaveConfig = saveConfig;
			Options = options;
			ConfigSeed = configSeed;
		}

		public CosmosStorageOptions Options { get; }

		public SaveConfigurationOptions SaveConfig { get; set; }

		public bool ReloadOnChange { get; set; }

		public IConfiguration ConfigSeed { get; set; }

		public IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			var slot = Options.ContainerId ?? Constants.Options.DefaultContainer;

			var container = CosmosConfigurationHelper.MigrateToLatest(slot, Options, ConfigSeed,
				SaveConfig?.SeedStrategy ?? SeedStrategy.None);

			return new CosmosConfigurationProvider(this, slot, container);
		}
	}
}