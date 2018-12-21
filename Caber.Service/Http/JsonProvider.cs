using System.IO;
using Newtonsoft.Json;

namespace Caber.Service.Http
{
    public class JsonProvider
    {
        private static JsonSerializer CreateSerialiser() => new JsonSerializer() {
            CheckAdditionalContent = true,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            DefaultValueHandling = DefaultValueHandling.Include,
            FloatFormatHandling = FloatFormatHandling.String,
            MaxDepth = 16,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        public void Serialise<T>(Stream stream, T message)
        {
            var serialiser = CreateSerialiser();
            using (var writer = new StreamWriter(stream))
            {
                serialiser.Serialize(writer, message);
            }
        }

        public void Deserialise<T>(Stream stream, T message)
        {
            var serialiser = CreateSerialiser();
            using (var reader = new StreamReader(stream))
            {
                serialiser.Populate(reader, message);
            }
        }
    }
}
