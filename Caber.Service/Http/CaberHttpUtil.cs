using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Caber.Service.Http
{
    public static class CaberHttpUtil
    {
        public static bool ReadUuid(IHeaderDictionary requestHeaders, string header, out Guid uuid)
        {
            if (requestHeaders.TryGetValue(header, out var uuids))
            {
                return ParseUuids(uuids, out uuid);
            }
            uuid = default;
            return false;
        }

        public static bool ReadUuid(HttpHeaders requestHeaders, string header, out Guid uuid)
        {
            if (requestHeaders.TryGetValues(header, out var uuids))
            {
                return ParseUuids(uuids, out uuid);
            }
            uuid = default;
            return false;
        }

        private static bool ParseUuids(IEnumerable<string> uuids, out Guid uuid)
        {
            var firstUuid = uuids.FirstOrDefault();
            return Guid.TryParse(firstUuid, out uuid);
        }
    }
}
