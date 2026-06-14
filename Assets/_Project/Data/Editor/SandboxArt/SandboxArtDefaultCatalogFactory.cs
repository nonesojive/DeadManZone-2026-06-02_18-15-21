using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    public static class SandboxArtDefaultCatalogFactory
    {
        [MenuItem("DeadManZone/Art/Create Default Sandbox Art Catalog")]
        public static void CreateDefaultSandboxArtCatalog() =>
            SyntyArtCatalogFactory.CreateSyntySandboxArtCatalog();
    }
}
