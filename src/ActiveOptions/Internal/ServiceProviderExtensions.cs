// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace ActiveOptions.Internal
{
	public static class ServiceProviderExtensions
	{
		public static bool TryBindOptions(this IServiceProvider serviceProvider, Type optionsWrapperType, bool validate,
			out object options)
		{
			// IOptions<T>
			var arguments = optionsWrapperType.GetGenericArguments();
			var resolved = arguments[0];
			while (resolved != null && resolved.IsGenericType)
			{
				resolved = resolved.IsGenericTypeDefinition
					? resolved.MakeGenericType(arguments) // IOptions<TService<T1,...TN>>
					: resolved.BaseType; // HubOptions<THub> -> HubOptions
			}

			var testingType = validate ? typeof(IValidOptions<>) : typeof(IOptions<>);

			var targetType = testingType.MakeGenericType(resolved);

			try
			{
				var instance = serviceProvider.GetService(targetType);
				var property = targetType.GetProperty(nameof(IOptions<object>.Value));
				options = property?.GetValue(instance);
				return options != null;
			}
			catch (Exception e)
			{
				options = new
				{
					Type = optionsWrapperType.GetInnerGenericTypeName(),
					ErrorType = e.GetType().Name,
					e.Message,
					e.StackTrace
				};
				return false;
			}
		}

		public static string GetInnerGenericTypeName(this Type optionsWrapperType)
		{
			if (!optionsWrapperType.IsGenericParameter)
				return FallbackToGenericTypeName(optionsWrapperType);

			var declaringMethod = optionsWrapperType.DeclaringMethod;
			if (declaringMethod == null)
				return FallbackToGenericTypeName(optionsWrapperType);

			return declaringMethod.IsGenericMethod
				? declaringMethod.GetGenericArguments()[0].Name
				: declaringMethod.Name;
		}

		private static string FallbackToGenericTypeName(Type optionsWrapperType)
		{
			return optionsWrapperType.IsGenericType
				? optionsWrapperType.GetGenericArguments()[0].Name
				: optionsWrapperType.Name;
		}
	}
}