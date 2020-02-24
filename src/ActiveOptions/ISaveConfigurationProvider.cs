// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace ActiveOptions
{
	public interface ISaveConfigurationProvider : IConfigurationProvider
	{
		bool HasChildren(string key);
		bool Save<TOptions>(string key, TOptions instance);
		bool Delete(string key);
	}
}