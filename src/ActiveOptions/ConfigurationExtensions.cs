// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Primitives;
using TypeKitchen;

namespace ActiveOptions
{
	public static class ConfigurationExtensions
	{
		public static Dictionary<string, string> Unbind(this object instance, string key)
		{
			var type = instance.GetType();

			var accessor = ReadAccessor.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public,
				out var members);
			var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var member in members)
			{
				var prefix = $"{key}:{member.Name}";
				if (!accessor.TryGetValue(instance, member.Name, out var value))
					continue;

				switch (value)
				{
					case null:
						map.Add(prefix, null);
						break;
					case string s:
						map.Add(prefix, s);
						break;
					case StringValues sv:
						map.Add(prefix, sv);
						break;
					default:
					{
						if (member.Type.IsValueTypeOrNullableValueType())
						{
							map.Add(prefix, value.ToString());
						}
						else
							switch (value)
							{
								case IEnumerable<string> strings:
								{
									var index = 0;
									foreach (var child in strings)
									{
										map.Add($"{prefix}:{index}", child);
										index++;
									}

									break;
								}

								case IEnumerable enumerable:
								{
									var index = 0;
									foreach (var item in enumerable)
									{
										foreach (var kvp in item.Unbind($"{prefix}:{index}"))
											map.Add(kvp.Key, kvp.Value);
										index++;
									}

									break;
								}

								default:
								{
									foreach (var kvp in value.Unbind(prefix))
										map.Add(kvp.Key, kvp.Value);
									break;
								}
							}

						break;
					}
				}
			}

			return map;
		}

		public static bool IsValid<TOptions>(this TOptions instance, IServiceProvider serviceProvider)
		{
			var results = Pooling.ListPool<ValidationResult>.Get();
			try
			{
				// FIXME: Use TK Validator
				var context = new ValidationContext(instance, serviceProvider, null);
				Validator.TryValidateObject(instance, context, results, true);
				return results.Count == 0;
			}
			finally
			{
				Pooling.ListPool<ValidationResult>.Return(results);
			}
		}

		public static TOptions Validate<TOptions>(this TOptions instance, IServiceProvider serviceProvider)
		{
			var results = Pooling.ListPool<ValidationResult>.Get();
			try
			{
				var context = new ValidationContext(instance, serviceProvider, null);
				Validator.TryValidateObject(instance, context, results, true);
				if (results.Count == 0)
				{
					return instance;
				}

				var message = Pooling.StringBuilderPool.Scoped(sb =>
				{
					sb.Append(typeof(TOptions).Name).Append(": ");
					sb.AppendLine();

					foreach (var result in results)
					{
						sb.Append(result.ErrorMessage);
						sb.Append(" [");
						var count = 0;
						foreach (var field in result.MemberNames)
						{
							if (count != 0)
							{
								sb.Append(", ");
							}

							sb.Append(field);
							count++;
						}

						sb.AppendLine("]");
					}
				});

				throw new ValidationException(message);
			}
			finally
			{
				Pooling.ListPool<ValidationResult>.Return(results);
			}
		}
	}
}