using System;
using System.Collections.Generic;
using System.Linq;
using TypeKitchen;

namespace ActiveOptions
{
	/// <summary>
	///     Guesses a type binding based on the presence of a `Type` property.
	/// </summary>
	internal sealed class TypeDiscriminatorBinder : ICustomConfigurationBinder
	{
		private static readonly ITypeResolver TypeResolver = new ReflectionTypeResolver();

		public bool CanConvertFrom(Type type)
		{
			var members = AccessorMembers.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
			return members.TryGetValue("Type", out _);
		}

		public Type GetTypeFor(object instance)
		{
			var baseType = instance.GetType();
			var members = AccessorMembers.Create(baseType, AccessorMemberTypes.Properties, AccessorMemberScope.Public);

			if (!members.TryGetValue("Type", out _))
				return baseType; // no type discriminator

			if (!IsTypeDiscriminated(baseType, out var subTypes))
				return baseType; // no matching subTypes

			var read = ReadAccessor.Create(instance, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
			var typeKey = read[instance, "Type"]?.ToString();
			if (string.IsNullOrWhiteSpace(typeKey))
				return baseType; // missing type discriminant

			var subType = subTypes.SingleOrDefault(x => x.Name == typeKey) ??
			              subTypes.SingleOrDefault(x => x.Name == $"{typeKey}{baseType.Name}");

			if (subType == null)
				return baseType; // sub-type error

			return subType;
		}

		private static bool IsTypeDiscriminated(Type type, out IEnumerable<Type> subTypes)
		{
			subTypes = TypeResolver.FindByParent(type);
			return subTypes.Any();
		}
	}
}