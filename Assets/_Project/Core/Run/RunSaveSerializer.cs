using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace DeadManZone.Core.Run
{
    public static class RunSaveSerializer
    {
        private const int CurrentSchemaVersion = 8;
        private const int MinimumSupportedSchemaVersion = 8;
        private const int LegacyMigrationTargetVersion = 2;
        private const int LegacyDefaultManpower = 100;
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

            if (schemaVersion < MinimumSupportedSchemaVersion)
                throw new System.InvalidOperationException(
                    $"Save schema v{schemaVersion} is no longer supported. Start a new run.");

            if (schemaVersion < LegacyMigrationTargetVersion)
                json = MigrateLegacySave(root).ToString();
            else
            {
                MigrateShopLaneNames(root);
                MigrateLockedOffers(root);
                MigrateCombatSave(root);
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
            MigrateCombatSave(root);
            MigrateShopLaneNames(root);
            root["SaveSchemaVersion"] = LegacyMigrationTargetVersion;
            return root;
        }

        private static void MigrateLockedOffers(JObject root)
        {
            if (root["LockedOffers"] is JArray)
                return;

            if (root["LockedOffer"] is not JObject locked || locked.Type == JTokenType.Null)
                return;

            root["LockedOffers"] = new JArray { locked };
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

        private static void MigrateCombatSave(JObject root)
        {
            if (root["Combat"] is not JObject combat)
                return;

            if (combat["Authority"] == null && combat["Requisition"] != null)
                combat["Authority"] = combat["Requisition"];

            MigrateGrenadeLobRename(combat);
        }

        /// <summary>GrenadeLob was renamed to MortarShot (2026-07-11); saves serialize enums
        /// as strings and replay event ActionType strings, so rewrite both in-place.</summary>
        private static void MigrateGrenadeLobRename(JObject combat)
        {
            if (combat["PendingSelectedAbilities"] is JArray pending)
                foreach (var token in pending)
                    if (token is JValue { Value: "GrenadeLob" } ability)
                        ability.Value = "MortarShot";

            if (combat["SubmittedCommands"] is JArray commands)
                foreach (var token in commands)
                    if (token is JObject command && command["Ability"]?.Value<string>() == "GrenadeLob")
                        command["Ability"] = "MortarShot";

            if (combat["EventLog"] is JArray events)
                foreach (var token in events)
                    if (token is JObject record && record["ActionType"]?.Value<string>() == "grenade_lob")
                        record["ActionType"] = "mortar_shot";
        }
    }
}
