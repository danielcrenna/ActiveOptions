using System;

namespace ActiveOptions
{
	public interface ICustomConfigurationBinder
	{
		bool CanConvertFrom(Type type);
		Type GetTypeFor(object instance);
	}
}