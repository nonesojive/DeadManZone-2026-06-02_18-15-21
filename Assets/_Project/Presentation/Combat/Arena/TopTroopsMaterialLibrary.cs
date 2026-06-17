using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    /// <summary>URP Lit materials for the Top Troops prototype battlefield palette.</summary>
    public static class TopTroopsMaterialLibrary
    {
        public static Material CreateGroundMaterial()
        {
            var mat = CreateLit(new Color(0.40f, 0.34f, 0.26f));
            mat.SetFloat("_Smoothness", 0.14f);
            return mat;
        }

        public static Material CreateCellMaterial(Color tint)
        {
            var mat = CreateLit(tint);
            mat.SetFloat("_Smoothness", 0.26f);
            return mat;
        }

        public static Material CreateCliffMaterial()
        {
            var mat = CreateLit(new Color(0.36f, 0.32f, 0.28f));
            mat.SetFloat("_Smoothness", 0.06f);
            return mat;
        }

        public static Material CreatePropMaterial()
        {
            var mat = CreateLit(new Color(0.46f, 0.40f, 0.32f));
            mat.SetFloat("_Smoothness", 0.10f);
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
