using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>URP Lit materials for the Top Troops prototype battlefield palette.</summary>
    public static class TopTroopsMaterialLibrary
    {
        public static Material CreateGroundMaterial()
        {
            var mat = CreateLit(new Color(0.36f, 0.5f, 0.3f));
            mat.SetFloat("_Smoothness", 0.15f);
            return mat;
        }

        public static Material CreateCellMaterial(Color tint)
        {
            var mat = CreateLit(tint);
            mat.SetFloat("_Smoothness", 0.2f);
            return mat;
        }

        public static Material CreateCliffMaterial()
        {
            var mat = CreateLit(new Color(0.42f, 0.4f, 0.36f));
            mat.SetFloat("_Smoothness", 0.05f);
            return mat;
        }

        public static Material CreatePropMaterial()
        {
            var mat = CreateLit(new Color(0.48f, 0.42f, 0.3f));
            mat.SetFloat("_Smoothness", 0.1f);
            return mat;
        }

        private static Material CreateLit(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;

            return mat;
        }
    }
}
