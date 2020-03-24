// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace ActiveOptions.Azure.Cosmos.Internal
{
	internal sealed class OptionsMonitorShim<T> : IOptionsMonitor<T>
	{
		public OptionsMonitorShim(T options) => CurrentValue = options;

		public T Get(string name)
		{
			return CurrentValue;
		}

		public IDisposable OnChange(Action<T, string> listener)
		{
			return Disposable.Empty;
		}

		public T CurrentValue
		{
			get;
		}
	}
}