using System.Collections.Generic;
using System.Text;

namespace DeadManZone.Core.Combat
{
    /// <summary>Formats combat event log lines for UI and dev review.</summary>
    public static class CombatLogFormatter
    {
        public static string FormatAll(IEnumerable<CombatEvent> events)
        {
            if (events == null)
                return string.Empty;

            var buffer = new StringBuilder();
            foreach (var combatEvent in events)
            {
                string line = Format(combatEvent);
                if (string.IsNullOrEmpty(line))
                    continue;

                if (buffer.Length > 0)
                    buffer.AppendLine();
                buffer.Append(line);
            }

            return buffer.Length == 0 ? string.Empty : buffer.ToString();
        }

        public static string Format(CombatEvent combatEvent)
        {
            if (combatEvent == null)
                return string.Empty;

            string phase = $"S{combatEvent.Segment}";
            int tick = combatEvent.Tick;

            return combatEvent.ActionType switch
            {
                "damage" =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}: {combatEvent.Value} dmg",
                "graze" =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}: {combatEvent.Value} graze",
                "miss" =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}: missed",
                "gas_damage" =>
                    $"[{phase} t{tick}] Gas → {Label(combatEvent.TargetId)}: {combatEvent.Value} dmg",
                "move" =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} moved to {FormatCoord(combatEvent.TargetId)}",
                "destroyed" =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} destroyed",
                "fight_end" =>
                    $"[{phase} t{tick}] Fight over — {FormatOutcome(combatEvent.TargetId)}",
                "tactic_set" =>
                    $"[{phase} t{tick}] Tactic set: {(TacticType)combatEvent.Value}",
                "shield_allies" =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} shielded {Label(combatEvent.TargetId)}",
                "checkpoint" =>
                    $"[{phase} t{tick}] Pause — {Label(combatEvent.TargetId)} forces at {combatEvent.Value}%",
                _ when combatEvent.ActionType.Contains("damage") || combatEvent.ActionType.Contains("blast") ||
                       combatEvent.ActionType.Contains("strike") || combatEvent.ActionType.Contains("mortar") =>
                    $"[{phase} t{tick}] {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}: {combatEvent.Value} ({combatEvent.ActionType})",
                _ =>
                    $"[{phase} t{tick}] {combatEvent.ActionType}: {Label(combatEvent.ActorId)} → {Label(combatEvent.TargetId)}"
            };
        }

        private static string Label(string id) =>
            string.IsNullOrEmpty(id) ? "—" : id.Replace('_', ' ');

        private static string FormatCoord(string targetId)
        {
            if (string.IsNullOrEmpty(targetId) || !targetId.Contains(','))
                return targetId ?? "?";

            var parts = targetId.Split(',');
            return parts.Length == 2 ? $"({parts[0]}, {parts[1]})" : targetId;
        }

        private static string FormatOutcome(string outcome) =>
            outcome switch
            {
                "victory" => "Victory",
                "defeat" => "Defeat",
                "draw" => "Draw",
                _ => outcome ?? "Unknown"
            };
    }
}
