// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace ActiveOptions
{
	public sealed class ValidOptionsMonitor<TOptions> : IValidOptionsMonitor<TOptions> where TOptions : class, new()
	{
		private readonly IOptionsMonitor<TOptions> _inner;
		private readonly IServiceProvider _serviceProvider;

		public ValidOptionsMonitor(IOptionsMonitor<TOptions> inner, IServiceProvider serviceProvider)
		{
			_inner = inner;
			_serviceProvider = serviceProvider;
		}

		public IDisposable OnChange(Action<TOptions, string> listener)
		{
			return _inner.OnChange(listener);
		}

		public TOptions CurrentValue => Get(Options.DefaultName);

		public TOptions Get(string name)
		{
			return _inner.Get(name).Validate(_serviceProvider);
		}
	}
}