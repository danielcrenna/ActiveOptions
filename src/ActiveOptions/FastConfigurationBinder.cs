// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Copyright (c) Daniel Crenna & Contributors. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Change: Fixes issue where TypeConverter is not consulted when creating an instance, which makes polymorphism impossible in configuration.
// Change: Use TypeKitchen for faster binding
// Change: Support polymorphism using type discrimination and use of TypeConverter

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using TypeKitchen;
using TypeKitchen.Creation;

namespace ActiveOptions
{
	/// <summary>
	///     Static helper class that allows binding strongly typed objects to configuration values.
	/// </summary>
	public static class FastConfigurationBinder
	{
		private static ITypeResolver _lastTypeResolver;

		/// <summary>
		///     Attempts to bind the configuration instance to a new instance of type T.
		///     If this configuration section has a value, that will be used.
		///     Otherwise binding by matching property names against configuration keys recursively.
		/// </summary>
		/// <typeparam name="T">The type of the new instance to bind.</typeparam>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <returns>The new instance of T if successful, default(T) otherwise.</returns>
		public static T Get<T>(this IConfiguration configuration)
		{
			return configuration.Get<T>(_ => { });
		}

		/// <summary>
		///     Attempts to bind the configuration instance to a new instance of type T.
		///     If this configuration section has a value, that will be used.
		///     Otherwise binding by matching property names against configuration keys recursively.
		/// </summary>
		/// <typeparam name="T">The type of the new instance to bind.</typeparam>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="configureOptions">Configures the binder options.</param>
		/// <returns>The new instance of T if successful, default(T) otherwise.</returns>
		public static T Get<T>(this IConfiguration configuration, Action<BinderOptions> configureOptions)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));
			var result = configuration.Get(typeof(T), configureOptions);
			return result == null ? default : (T) result;
		}

		/// <summary>
		///     Attempts to bind the configuration instance to a new instance of type T.
		///     If this configuration section has a value, that will be used.
		///     Otherwise binding by matching property names against configuration keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="type">The type of the new instance to bind.</param>
		/// <returns>The new instance if successful, null otherwise.</returns>
		public static object Get(this IConfiguration configuration, Type type)
		{
			return configuration.Get(type, _ => { });
		}

		/// <summary>
		///     Attempts to bind the configuration instance to a new instance of type T.
		///     If this configuration section has a value, that will be used.
		///     Otherwise binding by matching property names against configuration keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="type">The type of the new instance to bind.</param>
		/// <param name="configureOptions">Configures the binder options.</param>
		/// <returns>The new instance if successful, null otherwise.</returns>
		public static object Get(this IConfiguration configuration, Type type, Action<BinderOptions> configureOptions)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var options = new BinderOptions();
			configureOptions?.Invoke(options);
			return BindInstance(type, null, configuration, options);
		}

		/// <summary>
		///     Attempts to bind the given object instance to the configuration section specified by the key by matching property
		///     names against configuration keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="key">The key of the configuration section to bind.</param>
		/// <param name="instance">The object to bind.</param>
		public static void FastBind(this IConfiguration configuration, string key, object instance)
		{
			configuration.GetSection(key).FastBind(instance);
		}

		/// <summary>
		///     Attempts to bind the given object instance to the configuration section specified by the key by matching property
		///     names against configuration keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="key">The key of the configuration section to bind.</param>
		/// <param name="instance">The object to bind.</param>
		public static void FastBind(this IConfiguration configuration, string key, object instance,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			configuration.GetSection(key).FastBind(instance, customBinders);
		}

		/// <summary>
		///     Attempts to bind the given object instance to configuration values by matching property names against configuration
		///     keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="instance">The object to bind.</param>
		public static void FastBind(this IConfiguration configuration, object instance)
		{
			configuration.FastBind(instance, o => { });
		}

		/// <summary>
		///     Attempts to bind the given object instance to configuration values by matching property names against configuration
		///     keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="instance">The object to bind.</param>
		public static void FastBind(this IConfiguration configuration, object instance,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			configuration.FastBind(instance, o => { }, customBinders);
		}

		/// <summary>
		///     Attempts to bind the given object instance to configuration values by matching property names against configuration
		///     keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="instance">The object to bind.</param>
		/// <param name="configureOptions">Configures the binder options.</param>
		public static void FastBind(this IConfiguration configuration, object instance,
			Action<BinderOptions> configureOptions)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			if (instance == null)
				return;

			var options = new BinderOptions();
			configureOptions?.Invoke(options);
			BindInstance(instance.GetType(), instance, configuration, options);
		}

		/// <summary>
		///     Attempts to bind the given object instance to configuration values by matching property names against configuration
		///     keys recursively.
		/// </summary>
		/// <param name="configuration">The configuration instance to bind.</param>
		/// <param name="instance">The object to bind.</param>
		/// <param name="configureOptions">Configures the binder options.</param>
		public static void FastBind(this IConfiguration configuration, object instance,
			Action<BinderOptions> configureOptions, IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			if (instance == null)
				return;

			var options = new BinderOptions();
			configureOptions?.Invoke(options);
			BindInstance(instance.GetType(), instance, configuration, options, customBinders);
		}

		/// <summary>
		///     Extracts the value with the specified key and converts it to type T.
		/// </summary>
		/// <typeparam name="T">The type to convert the value to.</typeparam>
		/// <param name="configuration">The configuration.</param>
		/// <param name="key">The key of the configuration section's value to convert.</param>
		/// <returns>The converted value.</returns>
		public static T GetValue<T>(this IConfiguration configuration, string key)
		{
			return GetValue(configuration, key, default(T));
		}

		/// <summary>
		///     Extracts the value with the specified key and converts it to type T.
		/// </summary>
		/// <typeparam name="T">The type to convert the value to.</typeparam>
		/// <param name="configuration">The configuration.</param>
		/// <param name="key">The key of the configuration section's value to convert.</param>
		/// <param name="defaultValue">The default value to use if no value is found.</param>
		/// <returns>The converted value.</returns>
		public static T GetValue<T>(this IConfiguration configuration, string key, T defaultValue)
		{
			return (T) GetValue(configuration, typeof(T), key, defaultValue);
		}

		/// <summary>
		///     Extracts the value with the specified key and converts it to the specified type.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <param name="type">The type to convert the value to.</param>
		/// <param name="key">The key of the configuration section's value to convert.</param>
		/// <returns>The converted value.</returns>
		public static object GetValue(this IConfiguration configuration, Type type, string key)
		{
			return GetValue(configuration, type, key, null);
		}

		/// <summary>
		///     Extracts the value with the specified key and converts it to the specified type.
		/// </summary>
		/// <param name="configuration">The configuration.</param>
		/// <param name="type">The type to convert the value to.</param>
		/// <param name="key">The key of the configuration section's value to convert.</param>
		/// <param name="defaultValue">The default value to use if no value is found.</param>
		/// <returns>The converted value.</returns>
		public static object GetValue(this IConfiguration configuration, Type type, string key, object defaultValue)
		{
			var value = configuration.GetSection(key).Value;
			return value != null ? ConvertValue(type, value) : defaultValue;
		}

		private static void BindNonScalar(this IConfiguration configuration, ref object instance, BinderOptions options,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			if (instance == null)
				return;

			var scope = AccessorMemberScope.Public;
			if (options.BindNonPublicProperties)
				scope |= AccessorMemberScope.Private;

			var type = instance.GetType();
			var read = ReadAccessor.Create(type, AccessorMemberTypes.Properties, scope, out var members);
			var write = WriteAccessor.Create(type, AccessorMemberTypes.Properties, scope);

			if (IsTypeDiscriminated(type, out _))
			{
				// Set base properties so the converter has the right values to work with
				SetMembers(configuration, instance, options, members, read, write, customBinders);

				// Give a custom converter a chance to change what is bound
				var converter = TypeDescriptor.GetConverter(type);
				if (converter.CanConvertFrom(type))
				{
					instance = converter.ConvertFrom(instance);
					if (instance != null)
					{
						type = instance.GetType();
						read = ReadAccessor.Create(type, AccessorMemberTypes.Properties, scope, out members);
						write = WriteAccessor.Create(type, AccessorMemberTypes.Properties, scope);
					}
				}
				else
				{
					foreach (var binder in customBinders ?? Enumerable.Empty<ICustomConfigurationBinder>())
					{
						if (!binder.CanConvertFrom(type))
							continue;

						var subType = binder.GetTypeFor(instance);
						if (subType == null)
							continue;

						type = subType;
						instance = Instancing.CreateInstance(type);
						read = ReadAccessor.Create(type, AccessorMemberTypes.Properties, scope, out members);
						write = WriteAccessor.Create(type, AccessorMemberTypes.Properties, scope);
						goto setMembers;
					}
				}
			}

			setMembers:
			SetMembers(configuration, instance, options, members, read, write, customBinders);
		}

		private static void SetMembers(IConfiguration configuration, object instance, BinderOptions options,
			AccessorMembers members, ITypeReadAccessor read, ITypeWriteAccessor write,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			foreach (var member in members)
			{
				// We don't support set only, non-public, or indexer properties
				if (!member.CanRead || member.MemberInfo is MethodInfo method && method.GetParameters().Length > 0)
					continue;

				var value = read[instance, member.Name];
				if (value == null && !member.CanWrite)
				{
					// Property doesn't have a value and we cannot set it so there is no
					// point in going further down the graph
					continue;
				}

				var config = configuration.GetSection(member.Name);
				value = BindInstance(member.Type, value, config, options, customBinders);
				if (value == default || !member.CanWrite)
					continue;

				write.TrySetValue(instance, member.Name, value);
			}
		}

		private static object BindToCollection(Type typeInfo, IConfiguration config, BinderOptions options,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			var type = typeof(List<>).MakeGenericType(typeInfo.GenericTypeArguments[0]);
			var instance = CreateInstance(ref type);
			BindCollection(instance, type, config, options, customBinders);
			return instance;
		}

		// Try to create an array/dictionary instance to back various collection interfaces
		private static object AttemptBindToCollectionInterfaces(Type type, IConfiguration config, BinderOptions options,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			var typeInfo = type.GetTypeInfo();

			if (!typeInfo.IsInterface)
				return null;

			var collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyList<>), type);
			if (collectionInterface != null)
				return BindToCollection(typeInfo, config, options, customBinders);

			collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyDictionary<,>), type);
			if (collectionInterface != null)
			{
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0],
					typeInfo.GenericTypeArguments[1]);
				var instance = CreateInstance(ref dictionaryType);
				BindDictionary(instance, dictionaryType, config, options, customBinders);
				return instance;
			}

			collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
			if (collectionInterface != null)
			{
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0],
					typeInfo.GenericTypeArguments[1]);
				var instance = CreateInstance(ref dictionaryType);
				BindDictionary(instance, collectionInterface, config, options, customBinders);
				return instance;
			}

			collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyCollection<>), type);
			if (collectionInterface != null)
				return BindToCollection(typeInfo, config, options, customBinders);

			collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
			if (collectionInterface != null)
				return BindToCollection(typeInfo, config, options, customBinders);

			collectionInterface = FindOpenGenericInterface(typeof(IEnumerable<>), type);
			return collectionInterface != null ? BindToCollection(typeInfo, config, options, customBinders) : null;
		}

		private static object BindInstance(Type type, object instance, IConfiguration config, BinderOptions options,
			IEnumerable<ICustomConfigurationBinder> customBinders = null)
		{
			// if binding IConfigurationSection, break early
			if (type == typeof(IConfigurationSection))
				return config;

			var section = config as IConfigurationSection;
			var configValue = section?.Value;
			if (configValue != null && TryConvertValue(type, configValue, out var convertedValue, out var error))
			{
				if (error != null)
					throw error;

				// Leaf nodes are always reinitialized
				return convertedValue;
			}

			if (config == null || !config.GetChildren().Any())
				return instance;

			// If we don't have an instance, try to create one
			if (instance == null)
			{
				// We are already done if binding to a new collection instance worked
				instance = AttemptBindToCollectionInterfaces(type, config, options, customBinders);
				if (instance != null)
					return instance;
				instance = CreateInstance(ref type);
			}

			// See if its a Dictionary
			var collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
			if (collectionInterface != null)
			{
				BindDictionary(instance, collectionInterface, config, options, customBinders);
			}
			else if (type.IsArray)
			{
				instance = BindArray((Array) instance, config, options, customBinders);
			}
			else
			{
				// See if its an ICollection
				collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
				if (collectionInterface != null)
				{
					BindCollection(instance, collectionInterface, config, options, customBinders);
				}
				// Something else
				else
				{
					BindNonScalar(config, ref instance, options, customBinders);
				}
			}

			return instance;
		}

		private static object CreateInstance(ref Type type)
		{
			object instance;

			var typeInfo = type.GetTypeInfo();

			if (typeInfo.IsInterface || typeInfo.IsAbstract)
				throw new InvalidOperationException();

			if (type.IsArray)
			{
				if (typeInfo.GetArrayRank() > 1)
					throw new InvalidOperationException();

				instance = Array.CreateInstance(typeInfo.GetElementType() ?? throw new InvalidOperationException(), 0);
			}
			else
			{
				var hasDefaultConstructor =
					typeInfo.DeclaredConstructors.Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0);
				if (!hasDefaultConstructor)
					throw new InvalidOperationException();

				try
				{
					instance = Activator.CreateInstance(type);
				}
				catch (Exception)
				{
					throw new InvalidOperationException();
				}
			}

			if (IsTypeDiscriminated(type, out _))
				return instance;

			// Give converter a chance to change this type
			var converter = TypeDescriptor.GetConverter(type);
			if (!converter.CanConvertFrom(type))
				return instance;
			instance = converter.ConvertFrom(instance);
			if (instance != null)
				type = instance.GetType();

			return instance;
		}

		private static bool IsTypeDiscriminated(Type type, out IEnumerable<Type> subTypes)
		{
			// FIXME: perf in the degenerate case (no sub-types) is poor
			var typeResolver = _lastTypeResolver ?? new ReflectionTypeResolver();
			subTypes = typeResolver.FindByParent(type);
			var any = subTypes.Any();
			if (any)
				return true;
			_lastTypeResolver = new ReflectionTypeResolver();
			return _lastTypeResolver.FindByParent(type).Any();
		}

		private static void BindDictionary(object dictionary, Type dictionaryType, IConfiguration config,
			BinderOptions options, IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			var typeInfo = dictionaryType.GetTypeInfo();

			// IDictionary<K,V> is guaranteed to have exactly two parameters
			var keyType = typeInfo.GenericTypeArguments[0];
			var valueType = typeInfo.GenericTypeArguments[1];
			var keyTypeIsEnum = keyType.GetTypeInfo().IsEnum;

			if (keyType != typeof(string) && !keyTypeIsEnum)
			{
				// We only support string and enum keys
				return;
			}

			var setter = typeInfo.GetDeclaredProperty("Item");
			foreach (var child in config.GetChildren())
			{
				var item = BindInstance(valueType, null, child, options, customBinders);
				if (item == null)
					continue;

				if (keyType == typeof(string))
				{
					var key = child.Key;
					setter.SetValue(dictionary, item, new object[] {key});
				}
				else if (keyTypeIsEnum)
				{
					var key = Convert.ToInt32(Enum.Parse(keyType, child.Key));
					setter.SetValue(dictionary, item, new object[] {key});
				}
			}
		}

		private static void BindCollection(object collection, Type collectionType, IConfiguration config,
			BinderOptions options, IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			var typeInfo = collectionType.GetTypeInfo();

			// ICollection<T> is guaranteed to have exactly one parameter
			var itemType = typeInfo.GenericTypeArguments[0];
			var addMethod = typeInfo.GetDeclaredMethod("Add");

			foreach (var section in config.GetChildren())
			{
				try
				{
					var item = BindInstance(itemType, null, section, options, customBinders);
					if (item != null)
						addMethod.Invoke(collection, new[] {item});
				}
				catch
				{
					// ignored
				}
			}
		}

		private static Array BindArray(Array source, IConfiguration config, BinderOptions options,
			IEnumerable<ICustomConfigurationBinder> customBinders)
		{
			var children = config.GetChildren().ToArray();
			var arrayLength = source.Length;
			var elementType = source.GetType().GetElementType();
			var newArray = Array.CreateInstance(elementType ?? throw new InvalidOperationException(),
				arrayLength + children.Length);

			// binding to array has to preserve already initialized arrays with values
			if (arrayLength > 0)
			{
				Array.Copy(source, newArray, arrayLength);
			}

			for (var i = 0; i < children.Length; i++)
			{
				try
				{
					var item = BindInstance(elementType, null, children[i], options, customBinders);
					if (item != null)
						newArray.SetValue(item, arrayLength + i);
				}
				catch
				{
					// ignored
				}
			}

			return newArray;
		}

		private static bool TryConvertValue(Type type, string value, out object result, out Exception error)
		{
			while (true)
			{
				error = null;
				result = null;
				if (type == typeof(object))
				{
					result = value;
					return true;
				}

				if (type.GetTypeInfo().IsGenericType && type?.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					if (string.IsNullOrEmpty(value))
					{
						return true;
					}

					type = Nullable.GetUnderlyingType(type);
					continue;
				}

				var converter = TypeDescriptor.GetConverter(type ?? throw new ArgumentNullException(nameof(type)));
				if (!converter.CanConvertFrom(typeof(string)))
					return false;

				try
				{
					result = converter.ConvertFromInvariantString(value);
				}
				catch (Exception)
				{
					error = new InvalidOperationException();
				}

				return true;
			}
		}

		private static object ConvertValue(Type type, string value)
		{
			TryConvertValue(type, value, out var result, out var error);
			if (error != null)
			{
				throw error;
			}

			return result;
		}

		private static Type FindOpenGenericInterface(Type expected, Type actual)
		{
			var actualTypeInfo = actual.GetTypeInfo();
			if (actualTypeInfo.IsGenericType && actual.GetGenericTypeDefinition() == expected)
				return actual;

			var interfaces = actualTypeInfo.ImplementedInterfaces;
			foreach (var interfaceType in interfaces)
			{
				if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == expected)
					return interfaceType;
			}

			return null;
		}
	}
}