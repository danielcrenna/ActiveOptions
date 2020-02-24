// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace ActiveOptions
{
	public sealed class ValidOptionsManager<TOptions> : IValidOptions<TOptions>, IValidOptionsSnapshot<TOptions>
		where TOptions : class, new()
	{
		private readonly OptionsCache<TOptions> _cache = new OptionsCache<TOptions>();
		private readonly IOptionsFactory<TOptions> _factory;
		private readonly IServiceProvider _serviceProvider;

		public ValidOptionsManager(IOptionsFactory<TOptions> factory, IServiceProvider serviceProvider)
		{
			_factory = factory;
			_serviceProvider = serviceProvider;
		}

		public TOptions Value => Get(Options.DefaultName);

		public TOptions Get(string name)
		{
			return _cache.GetOrAdd(name, () => _factory.Create(name)).Validate(_serviceProvider);
		}
	}
}