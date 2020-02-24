// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ActiveOptions
{
	public class SaveConfigurationOptions
	{
		public static SaveConfigurationOptions Default = new SaveConfigurationOptions();
		public bool CreateIfNotExists { get; set; } = true;
		public bool MigrateOnStartup { get; set; } = true;
		public SeedStrategy SeedStrategy { get; set; } = SeedStrategy.None;
	}
}