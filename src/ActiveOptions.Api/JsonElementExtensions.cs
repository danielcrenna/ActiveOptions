// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Json;

namespace ActiveOptions.Api
{
	internal static class JsonElementExtensions
	{
		public static object ToObject(this JsonElement element, Type type, JsonSerializerOptions options = null)
		{
			var ms = new MemoryStream();
			using (var writer = new Utf8JsonWriter(ms))
			{
				element.WriteTo(writer);
			}
			return JsonSerializer.Deserialize(ms.ToArray(), type, options);
		}

		public static object ToObject(this JsonDocument document, Type type, JsonSerializerOptions options = null)
		{
			var ms = new MemoryStream();
			using (var writer = new Utf8JsonWriter(ms))
			{
				document.WriteTo(writer);
			}
			return JsonSerializer.Deserialize(ms.ToArray(), type, options);
		}
	}
}