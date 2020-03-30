// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace ActiveOptions
{
	public interface IValidOptionsSnapshot<out T> : IOptionsSnapshot<T> where T : class, new()
	{
	}
}