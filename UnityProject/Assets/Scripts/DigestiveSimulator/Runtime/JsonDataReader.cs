using Newtonsoft.Json;

namespace DigestiveSimulator.Runtime
{
    public static class JsonDataReader
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include
        };

        public static T Read<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }
}

