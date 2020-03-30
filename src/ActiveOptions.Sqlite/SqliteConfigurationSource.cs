// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ActiveOptions.Sqlite
{
	public class SqliteConfigurationSource : IConfigurationSource
	{
		public SqliteConfigurationSource(string dataFilePath, SaveConfigurationOptions saveConfig,
			IConfiguration configSeed = null, SeedStrategy strategy = SeedStrategy.Initialize)
		{
			SaveConfig = saveConfig;
			ConfigSeed = configSeed;
			DataFilePath = dataFilePath;
			DataDirectoryPath = new FileInfo(DataFilePath).Directory?.FullName;
			DataFileName = Path.GetFileName(DataFilePath);
			SeedStrategy = strategy;
		}

		public SaveConfigurationOptions SaveConfig { get; }
		public string DataFilePath { get; }
		public string DataDirectoryPath { get; }
		public string DataFileName { get; }
		public bool ReloadOnChange { get; set; }

		public IConfiguration ConfigSeed { get; set; }
		public SeedStrategy SeedStrategy { get; set; }
		public IFileProvider FileProvider { get; set; }

		public IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			if (DataDirectoryPath != null)
				Directory.CreateDirectory(DataDirectoryPath);

			SqliteConfigurationHelper.MigrateToLatest(DataFilePath, SaveConfig, ConfigSeed, SeedStrategy);
			return new SqliteConfigurationProvider(this);
		}
	}
}