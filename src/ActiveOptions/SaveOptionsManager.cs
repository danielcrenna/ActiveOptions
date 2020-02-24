// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ActiveOptions
{
	public sealed class SaveOptionsManager<TOptions> : ISaveOptions<TOptions>
		where TOptions : class, new()
	{
		private readonly IConfigurationRoot _configuration;
		private readonly IOptionsMonitor<TOptions> _monitor;
		private readonly IServiceProvider _serviceProvider;

		public SaveOptionsManager(
			IConfigurationRoot configuration,
			IServiceProvider serviceProvider,
			IOptionsMonitor<TOptions> monitor)
		{
			_configuration = configuration;
			_serviceProvider = serviceProvider;
			_monitor = monitor;
		}

		public TOptions Value => _monitor.CurrentValue;

		public SaveOptionsResult TrySave(string key, Action<TOptions> mutator = null)
		{
			var found = false;
			var saved = false;

			foreach (var provider in _configuration.Providers.Reverse())
			{
				if (!(provider is ISaveConfigurationProvider saveProvider))
					continue; // this provider does not support saving

				if (!saveProvider.HasChildren(key))
					continue; // key not found in this provider

				var current = _monitor.CurrentValue;

				mutator?.Invoke(current);
				found = true;

				if (!current.IsValid(_serviceProvider))
					continue; // don't allow saving invalid options

				if (saveProvider.Save(key, current))
					saved = true;
			}

			if (saved)
				_configuration.Reload();
			return !found ? SaveOptionsResult.NotFound : saved ? SaveOptionsResult.Ok : SaveOptionsResult.NotModified;
		}

		public SaveOptionsResult TrySave(string key, Action mutator = null)
		{
			var found = false;
			var saved = false;

			foreach (var provider in _configuration.Providers.Reverse())
			{
				if (!(provider is ISaveConfigurationProvider saveProvider))
					continue; // this provider does not support saving

				if (!saveProvider.HasChildren(key))
					continue; // key not found in this provider

				var current = _monitor.CurrentValue;

				mutator?.Invoke();
				found = true;

				if (!current.IsValid(_serviceProvider))
					continue; // don't allow saving invalid options

				if (saveProvider.Save(key, current))
					saved = true;
			}

			if (saved)
				_configuration.Reload();

			return !found ? SaveOptionsResult.NotFound : saved ? SaveOptionsResult.Ok : SaveOptionsResult.NotModified;
		}

		public bool TryAdd(string key, Action mutator = null)
		{
			var saved = false;

			foreach (var provider in _configuration.Providers.Reverse())
			{
				if (!(provider is ISaveConfigurationProvider saveProvider))
					continue; // this provider does not support saving

				var current = _monitor.CurrentValue;

				mutator?.Invoke();

				if (!current.IsValid(_serviceProvider))
					continue; // don't allow saving invalid options

				if (saveProvider.Save(key, current))
					saved = true;
			}

			if (saved)
				_configuration.Reload();

			return saved;
		}

		public DeleteOptionsResult TryDelete(string key)
		{
			foreach (var provider in _configuration.Providers.Reverse())
			{
				if (!(provider is ISaveConfigurationProvider saveProvider))
					continue; // this provider does not support saving

				if (!saveProvider.HasChildren(key))
					continue; // key not found in this provider

				if (saveProvider.Delete(key))
				{
					_configuration.Reload();
					return DeleteOptionsResult.NoContent;
				}
			}

			return DeleteOptionsResult.NotFound;
		}

		public TOptions Get(string name)
		{
			return _monitor.Get(name);
		}
	}
}