namespace DeadManZone.Core.Board
{
    public enum AttackSpeedTier
    {
        Slow,
        Medium,
        Fast
    }

    public enum AttackRangeTier
    {
        Melee,
        Short,
        Medium,
        Long
    }

    public enum MovementSpeedTier
    {
        None,
        Low,
        Medium,
        High
    }

    public enum ArmorType
    {
        None,
        Light,
        Medium,
        Heavy
    }

    public enum AttackType
    {
        None,
        Ballistic,
        Explosive,
        Piercing,
        Shredding,
        Fire,
        Melee,
        Gas
    }

    public enum GrantedAbility
    {
        None,
        MortarShot,
        ShieldAllies,
        CannonBlast,
        /// <summary>2026-07-15 faction-roster-v1 §2.2: Grand Battery's Rolling Barrage — a
        /// bigger area strike than MortarShot that scales with the army's artillery-tag count.</summary>
        RollingBarrage,
        /// <summary>2026-07-15 faction-roster-v1 §2.6 Resonance Coil: repeat the last ability
        /// issued this fight, free (CommandProcessor.TryApplyBatch). Not itself repeatable by
        /// Doctor Recursion into further Echoes — see the border rule note there.</summary>
        Echo
    }
}
