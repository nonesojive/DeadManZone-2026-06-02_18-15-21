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
        Piercing
    }

    public enum GrantedAbility
    {
        None,
        GrenadeLob,
        ShieldAllies,
        CannonBlast
    }
}
