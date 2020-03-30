// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace ActiveOptions.Sqlite
{
	public static class SqliteConfigurationHelper
	{
		public static void MigrateToLatest(string dataFilePath, SaveConfigurationOptions saveConfig,
			IConfiguration configSeed = null, SeedStrategy strategy = SeedStrategy.InsertIfNotExists)
		{
			try
			{
				var connectionString = $"Data Source={dataFilePath}";

				if (saveConfig.CreateIfNotExists)
					CreateIfNotExists(connectionString);

				var builder = new SqliteConnectionStringBuilder(connectionString) {Mode = SqliteOpenMode.ReadWrite};
				using (var db = new SqliteConnection(builder.ConnectionString))
				{
					db.Open();

					if (saveConfig.MigrateOnStartup)
						MigrateUp(db);

					if (configSeed != null)
					{
						db.SeedInTransaction(configSeed, strategy);
					}
				}
			}
			catch (SqliteException e)
			{
				Trace.TraceError("Error migrating configuration database: {0}", e);
				throw;
			}
		}

		private static void CreateIfNotExists(string connectionString)
		{
			var builder = new SqliteConnectionStringBuilder(connectionString) {Mode = SqliteOpenMode.ReadWriteCreate};
			if (File.Exists(builder.DataSource)) return;
			var connection = new SqliteConnection(builder.ConnectionString);
			connection.Open();
			connection.Close();
		}

		public static bool IsEmptyConfiguration(string dataFilePath)
		{
			if (!File.Exists(dataFilePath))
				return true;

			try
			{
				using (var db = new SqliteConnection($"Data Source={dataFilePath}"))
				{
					db.Open();

					MigrateUp(db);

					var count = db.ExecuteScalar<int>("SELECT COUNT(1) FROM 'Configuration'");

					return count == 0;
				}
			}
			catch (SqliteException e)
			{
				Trace.TraceError("Error migrating configuration database: {0}", e);
				throw;
			}
		}

		private static void MigrateUp(IDbConnection db)
		{
			db.Execute(@"
CREATE TABLE IF NOT EXISTS 'Configuration'
(  
    'Key' VARCHAR(128) NOT NULL,
    'Value' VARCHAR(255) NOT NULL,
    UNIQUE(Key)
);");
		}

		public static void SeedInTransaction(this SqliteConnection db, IConfiguration configSeed,
			SeedStrategy strategy = SeedStrategy.InsertIfNotExists)
		{
			var t = db.BeginTransaction();

			switch (strategy)
			{
				case SeedStrategy.InsertIfNotExists:
					InsertIfNotExists();
					break;
				case SeedStrategy.Initialize:
					var count = db.ExecuteScalar<int>("SELECT COUNT(1) FROM 'Configuration'");
					if (count == 0)
						InsertIfNotExists();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			t.Commit();

			void InsertIfNotExists()
			{
				foreach (var entry in configSeed.AsEnumerable())
				{
					db.Execute("INSERT OR IGNORE INTO 'Configuration' ('Key', 'Value') VALUES (@Key, @Value)",
						new {entry.Key, entry.Value}, t);
				}
			}
		}
	}
}