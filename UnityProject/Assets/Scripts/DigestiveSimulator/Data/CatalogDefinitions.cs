using System;

namespace DigestiveSimulator.Data
{
    [Serializable] public sealed class SpeciesCatalog { public string schemaVersion; public CatalogEntry[] species; }
    [Serializable] public sealed class FoodCatalog { public string schemaVersion; public CatalogEntry[] foods; }
    [Serializable] public sealed class CatalogEntry { public string id; public string definitionPath; }
}

