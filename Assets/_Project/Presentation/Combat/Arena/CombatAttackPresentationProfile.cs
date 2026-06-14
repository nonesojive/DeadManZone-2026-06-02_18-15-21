namespace DeadManZone.Presentation.Combat.Arena
{
    public readonly struct CombatAttackPresentationProfile
    {
        public CombatAttackPresentationKind Kind { get; }
        public bool UseForwardStep { get; }
        public float MuzzleDelaySeconds { get; }
        public float ImpactDelaySeconds { get; }
        public float TotalDurationSeconds { get; }

        public CombatAttackPresentationProfile(
            CombatAttackPresentationKind kind,
            bool useForwardStep,
            float muzzleDelaySeconds,
            float impactDelaySeconds,
            float totalDurationSeconds)
        {
            Kind = kind;
            UseForwardStep = useForwardStep;
            MuzzleDelaySeconds = muzzleDelaySeconds;
            ImpactDelaySeconds = impactDelaySeconds;
            TotalDurationSeconds = totalDurationSeconds;
        }

        public static CombatAttackPresentationProfile InfantryRifle => new(
            CombatAttackPresentationKind.InfantryRifle,
            useForwardStep: false,
            muzzleDelaySeconds: 0.08f,
            impactDelaySeconds: 0.20f,
            totalDurationSeconds: 0.55f);

        public static CombatAttackPresentationProfile InfantryGrenade => new(
            CombatAttackPresentationKind.InfantryGrenade,
            useForwardStep: false,
            muzzleDelaySeconds: 0.35f,
            impactDelaySeconds: 0.50f,
            totalDurationSeconds: 0.80f);

        public static CombatAttackPresentationProfile InfantryMelee => new(
            CombatAttackPresentationKind.InfantryMelee,
            useForwardStep: true,
            muzzleDelaySeconds: 0.10f,
            impactDelaySeconds: 0.18f,
            totalDurationSeconds: 0.45f);

        public static CombatAttackPresentationProfile VehicleCannon => new(
            CombatAttackPresentationKind.VehicleCannon,
            useForwardStep: false,
            muzzleDelaySeconds: 0.05f,
            impactDelaySeconds: 0.25f,
            totalDurationSeconds: 0.50f);

        public static CombatAttackPresentationProfile BuildingArtillery => new(
            CombatAttackPresentationKind.BuildingArtillery,
            useForwardStep: false,
            muzzleDelaySeconds: 0.05f,
            impactDelaySeconds: 0.30f,
            totalDurationSeconds: 0.55f);
    }
}
