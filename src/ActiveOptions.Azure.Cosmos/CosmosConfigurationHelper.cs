// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using ActiveOptions.Azure.Cosmos.Internal;
using ActiveStorage.Azure.Cosmos;
using ActiveStorage.Azure.Cosmos.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace ActiveOptions.Azure.Cosmos
{
	public static class CosmosConfigurationHelper
	{
		public static Container MigrateToLatest(string slot, CosmosStorageOptions options,
			IConfiguration configSeed = null, SeedStrategy strategy = SeedStrategy.InsertIfNotExists)
		{
			var optionsMonitor = new OptionsMonitorShim<CosmosStorageOptions>(options);

			var runner = new CosmosMigrationRunner(slot, optionsMonitor);

			var container = runner.CreateContainerIfNotExistsAsync().GetAwaiter().GetResult();

			if (configSeed != null && strategy != SeedStrategy.None)
			{
				var repository = new CosmosRepository(slot, container, optionsMonitor, null);

				switch (strategy)
				{
					case SeedStrategy.InsertIfNotExists:
					{
						InsertIfNotExists(repository);
						break;
					}

					case SeedStrategy.Initialize:
					{
						var count = repository.CountAsync<ConfigurationDocument>().GetAwaiter().GetResult();
						if (count == 0)
						{
							InsertIfNotExists(repository);
						}

						break;
					}

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			void InsertIfNotExists(ICosmosRepository repository)
			{
				var manifest = repository.RetrieveAsync<ConfigurationDocument>()
					.GetAwaiter().GetResult().Select(x => x.Key).ToImmutableHashSet();

				var changedKeys = new HashSet<string>();
				foreach (var (k, v) in configSeed.AsEnumerable())
				{
					if (manifest.Contains(k))
						continue;

					repository.CreateAsync(new ConfigurationDocument {Key = k, Value = v})
						.GetAwaiter().GetResult();

					changedKeys.Add(k);
				}

				Trace.TraceInformation(changedKeys.Count > 0
					? $"Configuration updated the following keys: {string.Join(",", changedKeys)}"
					: "Configuration is up to date.");
			}

			return container;
		}
	}
}