// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace ActiveOptions.Api
{
	public static class Use
	{
		public static IApplicationBuilder UseConfigurationApi(this IApplicationBuilder app)
		{
			app.UseRouting();
			app.UseEndpoints(endpoints => endpoints.MapControllers());
			return app;
		}
	}
}