// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ActiveOptions.Sqlite
{
	public static class Add
	{
		public static IConfigurationBuilder AddSqliteConfigurationProvider(this IConfigurationBuilder builder,
			string connectionString,
			IConfiguration configSeed = null)
		{
			return AddSqliteConfigurationProvider(builder, null, connectionString, false, configSeed);
		}

		public static IConfigurationBuilder AddSqliteConfigurationProvider(this IConfigurationBuilder builder,
			SqliteConnectionStringBuilder connectionStringBuilder,
			IConfiguration configSeed = null)
		{
			var path = connectionStringBuilder.DataSource;

			return AddSqliteConfigurationProvider(builder, null, path, false, configSeed);
		}

		public static IConfigurationBuilder AddSqliteConfigurationProvider(this IConfigurationBuilder builder,
			string connectionString,
			bool reloadOnChange, IConfiguration configSeed = null,
			Action<SaveConfigurationOptions> configureAction = null)
		{
			return AddSqliteConfigurationProvider(builder, null, connectionString, reloadOnChange, configSeed,
				configureAction);
		}

		public static IConfigurationBuilder AddSqliteConfigurationProvider(this IConfigurationBuilder builder,
			IFileProvider provider,
			string path, bool reloadOnChange, IConfiguration configSeed = null,
			Action<SaveConfigurationOptions> configureAction = null)
		{
			var saveConfig = new SaveConfigurationOptions();
			configureAction?.Invoke(saveConfig);

			if (provider == null && Path.IsPathRooted(path))
			{
				provider = new PhysicalFileProvider(Path.GetDirectoryName(path));
				path = Path.GetFileName(path);
			}

			var source = new SqliteConfigurationSource(path, saveConfig)
			{
				ReloadOnChange = reloadOnChange,
				ConfigSeed = configSeed,
				SeedStrategy = SeedStrategy.Initialize,
				FileProvider = provider
			};

			builder.Add(source);
			return builder;
		}
	}
}