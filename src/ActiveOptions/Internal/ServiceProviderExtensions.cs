// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace ActiveOptions.Internal
{
	internal static class ServiceProviderExtensions
	{
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