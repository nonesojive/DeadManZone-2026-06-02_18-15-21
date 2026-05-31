using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DeadManZone.Core.Run
{
    public static class RunSaveSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string ToJson(RunState state) =>
            JsonConvert.SerializeObject(state, Settings);

        public static RunState FromJson(string json) =>
            JsonConvert.DeserializeObject<RunState>(json, Settings);

        public static bool TryFromJson(string json, out RunState state)
        {
            try
            {
                state = FromJson(json);
                return state != null;
            }
            catch
            {
                state = null;
                return false;
            }
        }
    }
}
