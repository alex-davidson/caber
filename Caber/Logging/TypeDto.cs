using System;

namespace Caber.Logging
{
    public class TypeDto
    {
        public string FullName { get; set; }
        public string AssemblyQualifiedName { get; set; }

        public static TypeDto MapFrom(Type type)
        {
            if (type == null) return null;
            return new TypeDto {
                FullName = type.FullName,
                AssemblyQualifiedName = type.AssemblyQualifiedName
            };
        }
    }
}
