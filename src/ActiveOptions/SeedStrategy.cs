// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ActiveOptions
{
	public enum SeedStrategy
	{
		/// <summary>
		///     Do not seed any settings to the upstream provider.
		/// </summary>
		None,

		/// <summary>
		///     Seed new settings to the upstream provider.
		/// </summary>
		InsertIfNotExists,

		/// <summary>
		///     Seed all settings to the upstream provider, if none exist.
		/// </summary>
		Initialize
	}
}