using System.Configuration;

namespace Caber.Configuration
{
    internal static class ConfigurationExtensions
    {
        /// <summary>
        /// Returns true if the specified property was entirely omitted from the element.
        /// </summary>
        /// <remarks>
        /// Useful for forcing string properties to return null when omitted, since they will
        /// always default to empty-string and this cannot be overridden because someone thought
        /// magic-empty-string-overriding-specified-null-default was a good idea when the more
        /// sensible approach is *always* less magic and more explicit.
        ///
        /// Yes, I am annoyed.
        /// </remarks>
        public static bool IsPropertyOmitted(this ConfigurationElement element, string propertyName)
        {
            return element.ElementInformation.Properties[propertyName]?.ValueOrigin == PropertyValueOrigin.Default;
        }
    }
}
