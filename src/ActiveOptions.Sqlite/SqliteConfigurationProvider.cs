// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace ActiveOptions.Sqlite
{
	public class SqliteConfigurationProvider : ConfigurationProvider, ISaveConfigurationProvider
	{
		private readonly SqliteConfigurationSource _source;

		public SqliteConfigurationProvider(SqliteConfigurationSource source) => _source = source;

		public bool HasChildren(string key)
		{
			foreach (var entry in Data)
				if (entry.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}

		public bool Save<TOptions>(string key, TOptions instance)
		{
			var map = instance.Unbind(key);

			var changed = false;
			using (var db = new SqliteConnection($"Data Source={_source.DataFilePath}"))
			{
				db.Open();

				var t = db.BeginTransaction();

				foreach (var (k, v) in map)
				{
					if (v == default)
						continue;

					Data.TryGetValue(k, out var value);

					var before = value;
					if (before != null && before.Equals(v, StringComparison.Ordinal))
						continue; // no change

					var count = db.Execute(UpdateValue, new { Key = k, Value = v}, t);
					if (count == 0)
						count = db.Execute(InsertValue, new {Key = k, Value = v}, t);
					if (count > 0)
						changed = true;
				}

				t.Commit();
			}

			return changed;
		}

		public bool Delete(string key)
		{
			using var db = new SqliteConnection($"Data Source={_source.DataFilePath}");

			db.Open();
			var t = db.BeginTransaction();
			var count = db.Execute(DeleteByKey, new {Key = key}, t);
			t.Commit();

			return count > 0;
		}

		public override void Set(string key, string value)
		{
			if (TryGet(key, out var previousValue) && value == previousValue)
				return;
			using (var db = new SqliteConnection($"Data Source={_source.DataFilePath}"))
			{
				db.Open();
				var t = db.BeginTransaction();
				var count = db.Execute(UpdateValue, new {Key = key, Value = value}, t);
				if (count == 0)
					db.Execute(InsertValue, new {Key = key, Value = value}, t);
				t.Commit();
			}

			Data[key] = value;
			if (_source.ReloadOnChange)
				OnReload();
		}

		public override void Load()
		{
			var onChange = Data.Count > 0;
			Data.Clear();
			using (var db = new SqliteConnection($"Data Source={_source.DataFilePath}"))
			{
				db.Open();
				var data = db.Query<ConfigurationRow>(GetAll);
				foreach (var item in data)
				{
					Data[item.Key] = item.Value;
					onChange = true;
				}
			}

			if (onChange && _source.ReloadOnChange)
				OnReload();
		}

		[DebuggerDisplay("{Key} = {Value}")]
		private struct ConfigurationRow
		{
#pragma warning disable 649
			public string Key;
			public string Value;
#pragma warning restore 649
		}

		#region SQL

		private const string GetAll = "SELECT * FROM Configuration";
		private const string UpdateValue = "UPDATE Configuration SET Value = :Value WHERE Key = :Key";
		private const string InsertValue = "INSERT INTO Configuration (Key, Value) VALUES (:Key, :Value)";
		private const string DeleteByKey = "DELETE FROM Configuration WHERE Key = :Key";

		#endregion
	}
}