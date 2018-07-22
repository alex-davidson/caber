using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace Caber.Configuration
{
    public class CaseInsensitiveEnumConfigurationConverter<T> : ConfigurationConverterBase
    {
        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data) => Enum.Parse(typeof(T), (string)data, true);
    }
}
