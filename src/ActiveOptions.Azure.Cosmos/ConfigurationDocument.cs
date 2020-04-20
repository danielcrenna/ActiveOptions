// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ActiveStorage.Azure.Cosmos;

namespace ActiveOptions.Azure.Cosmos
{
	public class ConfigurationDocument : DocumentEntityBase<ConfigurationDocument>
	{
		public string Key { get; set; }
		public string Value { get; set; }
	}
}