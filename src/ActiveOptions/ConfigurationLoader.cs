// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ActiveOptions
{
	public static class ConfigurationLoader
	{
		public static IConfiguration FromEmbeddedJsonFile(string fileName)
		{
			return LoadFromEmbeddedResource(fileName, AppDomain.CurrentDomain.GetAssemblies());
		}

		public static IConfiguration FromEmbeddedJsonFile(string fileName, params Assembly[] assemblies)
		{
			return LoadFromEmbeddedResource(fileName, assemblies);
		}

		private static IConfiguration LoadFromEmbeddedResource(string fileName, Assembly[] assemblies)
		{
			var builder = new ConfigurationBuilder();
			foreach (var assembly in assemblies)
			{
				if (assembly.IsDynamic)
					continue;
				try
				{
					foreach (var resourceName in assembly.GetManifestResourceNames())
					{
						if (!resourceName.EndsWith(fileName))
							continue;
						var baseNamespace = resourceName.Replace($".{fileName}", string.Empty);
						var provider = new EmbeddedFileProvider(assembly, baseNamespace);
						builder.AddJsonFile(o =>
						{
							o.FileProvider = provider;
							o.Path = fileName;
							o.Optional = false;
							o.ReloadOnChange = false;
						});
					}
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception);
					throw;
				}
			}

			IConfiguration configSeed = null;
			if (builder.Sources.Count > 0)
				configSeed = builder.Build();
			return configSeed;
		}
	}
}