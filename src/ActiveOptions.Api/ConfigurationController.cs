// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using ActiveErrors;
using ActiveRoutes;
using ActiveRoutes.Meta;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TypeKitchen;
using TypeKitchen.Creation;
using TypeKitchen.Differencing;

namespace ActiveOptions.Api
{
	[DisplayName("Configuration")]
	[DynamicController(typeof(ConfigurationApiOptions))]
	[MetaCategory("Operations", "Provides diagnostic tools for server operators at runtime.")]
	[MetaDescription("Manages configuration items.")]
	public class ConfigurationController : Controller, IDynamicFeatureToggle<ConfigurationFeature>
	{
		private readonly IEnumerable<ICustomConfigurationBinder> _customBinders;
		private readonly IConfigurationRoot _root;

		private readonly ConfigurationService _service;
		private readonly IServiceProvider _serviceProvider;
		private readonly ITypeResolver _typeResolver;

		public ConfigurationController(IConfigurationRoot root, ITypeResolver typeResolver,
			IServiceProvider serviceProvider, IEnumerable<ICustomConfigurationBinder> customBinders,
			ConfigurationService service)
		{
			_root = root;
			_typeResolver = typeResolver;
			_serviceProvider = serviceProvider;
			_customBinders = customBinders;
			_service = service;
		}

		[DynamicHttpGet("")]
		[DynamicHttpGet("{section?}")]
		[MustHaveQueryParameters("type")]
		public IActionResult Get([FromQuery] string type, [FromRoute] string section = null)
		{
			if (string.IsNullOrWhiteSpace(section))
				return this.NotAcceptableError(ErrorEvents.UnsafeRequest,
					"You must specify a known configuration sub-section, to avoid exposing sensitive root-level data.");

			var prototype = ResolvePrototypeName(type);
			if (prototype == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"No configuration type found with name '{type}'.");

			return GetSerialized(prototype, section);
		}

		[DynamicHttpPatch("")]
		[DynamicHttpPatch("{section?}")]
		[MustHaveQueryParameters("type")]
		[Consumes(MediaTypeNames.Application.JsonPatch)]
		public IActionResult Patch([FromQuery] string type, [FromBody] JsonPatchDocument patch,
			[FromRoute] string section = null)
		{
			if (string.IsNullOrWhiteSpace(section))
				return this.NotAcceptableError(ErrorEvents.UnsafeRequest,
					"You must specify a known configuration sub-section, to avoid exposing sensitive root-level data.");

			var prototype = ResolvePrototypeName(type);
			if (prototype == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"No configuration type found with name '{type}'.");

			var config = _root.GetSection(section.Replace("/", ":"));
			if (config == null)
				return this.NotFoundError(ErrorEvents.InvalidParameter,
					$"Configuration sub-section path '{section}' not found.");

			var model = Instancing.CreateInstance(prototype);
			config.FastBind(model, _customBinders);
			patch.ApplyTo(model);

			return Put(type, model, section);
		}

		[DynamicHttpPatch("")]
		[DynamicHttpPatch("{section?}")]
		[MustHaveQueryParameters("type")]
		[Consumes(MediaTypeNames.Application.JsonMergePatch)]
		public IActionResult Patch([FromQuery] string type, [FromBody] object patch, [FromRoute] string section = null)
		{
			if (string.IsNullOrWhiteSpace(section))
				return this.NotAcceptableError(ErrorEvents.UnsafeRequest,
					"You must specify a known configuration sub-section, to avoid exposing sensitive root-level data.");

			var prototype = ResolvePrototypeName(type);
			if (prototype == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"No configuration type found with name '{type}'.");

			var config = _root.GetSection(section.Replace("/", ":"));
			if (config == null)
				return this.NotFoundError(ErrorEvents.InvalidParameter,
					$"Configuration sub-section path '{section}' not found.");

			if (config.Value == null)
				return Put(type, patch, section);

			var original = Instancing.CreateInstance(prototype);
			config.FastBind(original, _customBinders);

			var diff = Delta.ObjectToObject(patch, original);
			diff.ApplyTo(original);

			return Put(type, patch, section);
		}

		[DynamicHttpPut("")]
		[DynamicHttpPut("{section?}")]
		[MustHaveQueryParameters("type")]
		public IActionResult Put([FromQuery] string type, [FromBody] object model, [FromRoute] string section = null)
		{
			if (string.IsNullOrWhiteSpace(section))
				return this.NotAcceptableError(ErrorEvents.UnsafeRequest,
					"You must specify a known configuration sub-section, to avoid exposing sensitive root-level data.");

			if (model == null)
				return this.NotAcceptableError(ErrorEvents.InvalidRequest, "Missing configuration body.");

			var config = _root.GetSection(section.Replace("/", ":"));
			if (config == null)
				return this.NotFoundError(ErrorEvents.InvalidParameter,
					$"Configuration sub-section path '{section}' not found.");

			var prototype = ResolvePrototypeName(type);
			if (prototype == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"No configuration type found with name '{type}'.");

			var optionsType = typeof(IOptions<>).MakeGenericType(prototype);
			var saveOptionsType = typeof(ISaveOptions<>).MakeGenericType(prototype);
			var saveOptions = _serviceProvider.GetService(saveOptionsType);
			if (saveOptions == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"Could not resolve IOptions<{type}> for saving");

			var validOptionsType = typeof(IValidOptionsMonitor<>).MakeGenericType(prototype);
			var validOptions = _serviceProvider.GetService(validOptionsType);
			if (validOptions == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"Could not resolve IOptions<{type}> for validation");

			var valueProperty = optionsType.GetProperty(nameof(IOptions<object>.Value));
			if (valueProperty == null)
				return this.InternalServerError(ErrorEvents.FeatureError,
					$"Unexpected error: IOptions<{type}> methods failed to resolve.");

			var trySaveMethod = saveOptionsType.GetMethod(nameof(ISaveOptions<object>.TrySave),
				new[] {typeof(string), typeof(Action)});
			if (trySaveMethod == null)
				return this.InternalServerError(ErrorEvents.FeatureError,
					$"Unexpected error: IOptions<{type}> methods failed to resolve.");

			var tryAddMethod = saveOptionsType.GetMethod(nameof(ISaveOptions<object>.TryAdd),
				new[] {typeof(string), typeof(Action)});
			if (tryAddMethod == null)
				return this.InternalServerError(ErrorEvents.FeatureError,
					$"Unexpected error: IOptions<{type}> methods failed to resolve.");

			if (model is string json)
				model = JsonSerializer.Deserialize(json, prototype);

			if (model is JsonElement element)
				model = element.ToObject(prototype);

			var result = TryUpsert(section, model, trySaveMethod, tryAddMethod, saveOptions, prototype, valueProperty);
			return result;
		}

		[DynamicHttpDelete("")]
		[DynamicHttpDelete("{section?}")]
		public IActionResult Delete([FromQuery] string type, [FromRoute] string section = null)
		{
			if (string.IsNullOrWhiteSpace(section))
				return this.NotAcceptableError(ErrorEvents.UnsafeRequest,
					"You must specify a known configuration sub-section, to avoid exposing sensitive root-level data.");

			var config = _root.GetSection(section.Replace("/", ":"));
			if (config == null)
				return this.NotFoundError(ErrorEvents.InvalidParameter,
					$"Configuration sub-section path '{section}' not found.");

			var prototype = ResolvePrototypeName(type);
			if (prototype == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"No configuration type found with name '{type}'.");

			var optionsType = typeof(IOptions<>).MakeGenericType(prototype);
			var saveOptionsType = typeof(ISaveOptions<>).MakeGenericType(prototype);
			var saveOptions = _serviceProvider.GetService(saveOptionsType);
			if (saveOptions == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"Could not resolve IOptions<{type}> for saving");

			var validOptionsType = typeof(IValidOptionsMonitor<>).MakeGenericType(prototype);
			var validOptions = _serviceProvider.GetService(validOptionsType);
			if (validOptions == null)
				return this.NotAcceptableError(ErrorEvents.InvalidParameter,
					$"Could not resolve IOptions<{type}> for validation");

			var valueProperty = optionsType.GetProperty(nameof(IOptions<object>.Value));

			var tryDeleteMethod =
				saveOptionsType.GetMethod(nameof(ISaveOptions<object>.TryDelete), new[] {typeof(string)});
			if (tryDeleteMethod == null || valueProperty == null)
				return this.InternalServerError(ErrorEvents.FeatureError,
					$"Unexpected error: IOptions<{type}> methods failed to resolve.");

			var deleted = (DeleteOptionsResult) tryDeleteMethod.Invoke(saveOptions, new object[] {section});

			return deleted switch
			{
				DeleteOptionsResult.NotFound => NotFound(),
				DeleteOptionsResult.NoContent => NoContent(),
				DeleteOptionsResult.Gone => this.Gone(),
				DeleteOptionsResult.InternalServerError => this.InternalServerError(ErrorEvents.FeatureError,
					$"Could not delete section {section}"),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private IActionResult TryUpsert(string section, object model, MethodBase trySaveMethod, MethodBase tryAddMethod,
			object saveOptions, Type prototype, PropertyInfo valueProperty)
		{
			try
			{
				model.Validate(_serviceProvider);
			}
			catch (ValidationException e)
			{
				return this.UnprocessableEntityError(ErrorEvents.ValidationFailed, e.ValidationResult.ErrorMessage);
			}

			var saveResult = trySaveMethod.Invoke(saveOptions, new object[]
			{
				section, new Action(() =>
				{
					SaveOptions(prototype, saveOptions, valueProperty, model);
				})
			});

			if (saveResult is SaveOptionsResult save)
			{
				switch (save)
				{
					case SaveOptionsResult.NotFound:
					{
						var addResult = (bool) tryAddMethod.Invoke(saveOptions, new object[]
						{
							section, new Action(() =>
							{
								SaveOptions(prototype, saveOptions, valueProperty, model);
							})
						});
						if (!addResult)
							return this.InternalServerError(ErrorEvents.FeatureError,
								$"Could not add existing configuration to section '{section}'");

						break;
					}
					case SaveOptionsResult.NotModified:
						return this.NotModified();
				}
			}

			var serialized = GetSerialized(prototype, section);

			return serialized;
		}

		private IActionResult GetSerialized(Type type, string section)
		{
			var template = _service.Get(type, section);

			return template == null
				? this.NotFoundError(ErrorEvents.InvalidParameter,
					$"Configuration sub-section path '{section}' not found.")
				: Ok(template);
		}

		private static void SaveOptions(Type type, object saveOptions, PropertyInfo valueProperty, object result)
		{
			var target = valueProperty.GetValue(saveOptions);
			var writer = WriteAccessor.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);
			var reader = ReadAccessor.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);

			foreach (var member in members)
			{
				if (member.MemberType == AccessorMemberType.Property &&
				    member.CanWrite &&
				    member.CanRead &&
				    reader.TryGetValue(result, member.Name, out var value))
				{
					writer.TrySetValue(target, member.Name, value);
				}
			}
		}

		private Type ResolvePrototypeName(string type)
		{
			return _typeResolver.FindByFullName(type) ?? _typeResolver.FindFirstByName(type);
		}
	}
}