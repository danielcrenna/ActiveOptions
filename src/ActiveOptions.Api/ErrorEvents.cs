// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace ActiveOptions.Api
{
	internal static class ErrorEvents
	{
		/// <summary>
		///     The request was improperly structured, to the point it could not be validated.
		/// </summary>
		public const long InvalidRequest = 1001;

		/// <summary>
		///     The request was evaluated, but failed validation
		/// </summary>
		public const long ValidationFailed = 1001;

		/// <summary>
		///     The request is valid, but could expose sensitive data.
		/// </summary>
		public const long UnsafeRequest = 1002;

		/// <summary>
		///     The request is invalid, because one of its parameters is invalid.
		/// </summary>
		public const long InvalidParameter = 1003;

		/// <summary>
		///     The request failed because this feature has an issue.
		/// </summary>
		public const long FeatureError = 1004;
	}
}