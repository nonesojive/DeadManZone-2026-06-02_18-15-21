using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>Procedural capsule soldiers adapted from TopTroopsCombat MilitarySquadVisualFactory.</summary>
    public static class TopTroopsSquadVisualFactory
    {
        private static readonly Vector3[] FormationOffsets =
        {
            new(-0.25f, 0f, -0.2f),
            new(0.25f, 0f, -0.2f),
            new(-0.25f, 0f, 0.2f),
            new(0.25f, 0f, 0.2f),
            new(0f, 0f, 0f)
        };

        public static void BuildSquad(Transform root, PieceDefinitionSO piece, CombatSide side)
        {
            if (root == null || piece == null)
                return;

            ResolveColors(piece, side, out Color primary, out Color accent);
            int squadSize = ResolveSquadSize(piece);
            bool isArtillery = IsArtillery(piece);

            for (int i = 0; i < squadSize; i++)
            {
                bool isLeader = i == 0 && IsLeaderRole(piece);
                CreateSoldier(root, primary, accent, FormationOffsets[i], isLeader, piece);
            }

            if (isArtillery)
                CreateArtillery(root, primary, accent);
        }

        private static int ResolveSquadSize(PieceDefinitionSO piece)
        {
            if (IsArtillery(piece) || CombatAttackProfileResolver.IsVehicle(piece))
                return 1;

            if (piece.combatRole == GameTagIds.Sniper)
                return 2;

            return Mathf.Clamp(piece.manpowerCost, 1, FormationOffsets.Length);
        }

        private static void ResolveColors(PieceDefinitionSO piece, CombatSide side, out Color primary, out Color accent)
        {
            if (side == CombatSide.Player)
            {
                primary = piece.categoryTint != Color.white
                    ? piece.categoryTint
                    : new Color(0.38f, 0.32f, 0.26f);
                accent = new Color(0.30f, 0.26f, 0.20f);
                return;
            }

            primary = new Color(0.30f, 0.26f, 0.22f);
            accent = new Color(0.42f, 0.36f, 0.28f);
        }

        private static void CreateSoldier(
            Transform root,
            Color primary,
            Color accent,
            Vector3 offset,
            bool isLeader,
            PieceDefinitionSO piece)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = isLeader ? "SquadLeader" : "Soldier";
            body.transform.SetParent(root, false);
            body.transform.localPosition = offset + Vector3.up * 0.35f;
            body.transform.localScale = isLeader
                ? new Vector3(0.35f, 0.45f, 0.35f)
                : new Vector3(0.28f, 0.38f, 0.28f);
            ApplyMaterial(body, primary);

            var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmet.name = "Helmet";
            helmet.transform.SetParent(body.transform, false);
            helmet.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            helmet.transform.localScale = new Vector3(0.7f, 0.45f, 0.7f);
            ApplyMaterial(helmet, accent);
            DestroyCollider(helmet);

            if (IsRangedRole(piece))
                CreateWeapon(body.transform, accent, piece.combatRole == GameTagIds.Sniper ? 0.9f : 0.55f);
            else if (piece.combatRole == GameTagIds.Assault)
                CreateShield(body.transform, accent);

            DestroyCollider(body);
        }

        private static void CreateArtillery(Transform root, Color primary, Color accent)
        {
            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlate.name = "ArtilleryBase";
            basePlate.transform.SetParent(root, false);
            basePlate.transform.localPosition = new Vector3(0f, 0.2f, -0.35f);
            basePlate.transform.localScale = new Vector3(0.8f, 0.2f, 0.6f);
            ApplyMaterial(basePlate, accent);

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(basePlate.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0.35f, 0.2f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.12f, 0.45f, 0.12f);
            ApplyMaterial(barrel, primary);
            DestroyCollider(barrel);
            DestroyCollider(basePlate);
        }

        private static void CreateWeapon(Transform parent, Color color, float length)
        {
            var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = "Weapon";
            weapon.transform.SetParent(parent, false);
            weapon.transform.localPosition = new Vector3(0.2f, 0.1f, 0.25f);
            weapon.transform.localScale = new Vector3(0.06f, 0.06f, length);
            ApplyMaterial(weapon, color);
            DestroyCollider(weapon);
        }

        private static void CreateShield(Transform parent, Color color)
        {
            var shield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shield.name = "RiotShield";
            shield.transform.SetParent(parent, false);
            shield.transform.localPosition = new Vector3(-0.22f, 0f, 0.2f);
            shield.transform.localScale = new Vector3(0.08f, 0.45f, 0.3f);
            ApplyMaterial(shield, color);
            DestroyCollider(shield);
        }

        private static bool IsArtillery(PieceDefinitionSO piece) =>
            piece.combatRole == GameTagIds.Artillery;

        private static bool IsRangedRole(PieceDefinitionSO piece) =>
            piece.combatRole == GameTagIds.Sniper
            || piece.combatRole == GameTagIds.Support
            || piece.attackRange == AttackRangeTier.Long;

        private static bool IsLeaderRole(PieceDefinitionSO piece) =>
            piece.combatRole == GameTagIds.Assault
            || IsArtillery(piece)
            || CombatAttackProfileResolver.IsVehicle(piece);

        private static void ApplyMaterial(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.sharedMaterial = TopTroopsMaterialLibrary.CreateCellMaterial(color);
        }

        private static void DestroyCollider(GameObject obj)
        {
            var collider = obj != null ? obj.GetComponent<Collider>() : null;
            if (collider == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }
    }
}
