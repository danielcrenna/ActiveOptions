// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace ActiveOptions.Azure.Cosmos.Internal
{
	internal sealed class Disposable : IDisposable
	{
		public static IDisposable Empty = new Disposable();
		public void Dispose() { }
	}
}