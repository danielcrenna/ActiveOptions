// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace ActiveOptions
{
	public interface ISaveOptions<out T> : IOptions<T> where T : class, new()
	{
		SaveOptionsResult TrySave(string key, Action<T> mutator = null);
		SaveOptionsResult TrySave(string key, Action mutator = null);

		bool TryAdd(string key, Action mutator = null);
		DeleteOptionsResult TryDelete(string key);
	}
}