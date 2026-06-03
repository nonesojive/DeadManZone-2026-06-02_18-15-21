using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DeadManZone.Core.Run
{
    public static class RunSaveSerializer
    {
        private const int CurrentSchemaVersion = 2;
        private const int LegacyDefaultManpower = 10;
        private const int LegacyDefaultMorale = 100;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string Serialize(RunState state) => ToJson(state);

        public static RunState Deserialize(string json) => FromJson(json);

        public static string ToJson(RunState state) =>
            JsonConvert.SerializeObject(state, Settings);

        public static RunState FromJson(string json)
        {
            var root = JObject.Parse(json);
            int schemaVersion = root.Value<int?>("SaveSchemaVersion") ?? 1;

            if (schemaVersion < CurrentSchemaVersion)
                json = MigrateLegacySave(root).ToString();
            else
            {
                MigrateShopLaneNames(root);
                json = root.ToString();
            }

            return JsonConvert.DeserializeObject<RunState>(json, Settings);
        }

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

        private static JObject MigrateLegacySave(JObject root)
        {
            if (root["Supplies"] == null && root["Gold"] != null)
                root["Supplies"] = root["Gold"];

            if (root["Authority"] == null && root["Requisition"] != null)
                root["Authority"] = root["Requisition"];

            if (root["Manpower"] == null)
                root["Manpower"] = LegacyDefaultManpower;

            if (root["Morale"] == null)
                root["Morale"] = LegacyDefaultMorale;

            root.Remove("Gold");
            root.Remove("Requisition");
            MigrateShopLaneNames(root);
            root["SaveSchemaVersion"] = CurrentSchemaVersion;
            return root;
        }

        private static void MigrateShopLaneNames(JObject root)
        {
            if (root.SelectToken("Shop.Offers") is not JArray offers)
                return;

            foreach (var token in offers)
            {
                if (token is not JObject offer)
                    continue;

                var lane = offer["Lane"]?.Value<string>();
                offer["Lane"] = lane switch
                {
                    "General" => "Offensive",
                    "Engineers" => "Defensive",
                    "Requisition" => "Specialty",
                    _ => lane
                };
            }
        }
    }
}
