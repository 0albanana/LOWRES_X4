namespace LOWRES_X4
{
    internal class CatEntry
    {
        public string Path;
        public long OrigStreamPos;
        public long OrigSize;
        public string TimeStamp;
        public string ChkSum;
        public byte[] Data = null;
        public bool Compressed = false;
    }

    internal class LodEntry
    {
        public enum LECategory
        {
            Unset,
            Environment,
            Collectables,
            ShipInteriors,
            ShipExteriors,
            StationInteriors,
            StationExteriors
        };

        public LECategory Category = LECategory.Unset;
        public CatEntry Lod0Entry, Lod1Entry, Lod2Entry, Lod3Entry;
        public int Lod0StrIdx, Lod1StrIdx, Lod2StrIdx, Lod3StrIdx;

        static public LECategory GetCategory(string path)
        {
            if (path.StartsWith("assets/environments") ||
                path.StartsWith("assets/legacy/environments") ||
                path.StartsWith("assets/legacy/props/AdSigns") ||
                path.StartsWith("assets/legacy/props/HighwayElements") ||
                path.StartsWith("assets/legacy/props/StorageModules") ||
                path.StartsWith("assets/props/gates"))
                return LECategory.Environment;
            else if (
                path.StartsWith("assets/props/Collectable") ||
                path.StartsWith("assets/props/Crates"))
                return LECategory.Collectables;
            else if (
                path.StartsWith("assets/interiors/dockarea_corners") ||
                path.StartsWith("assets/interiors/rooms") ||
                path.StartsWith("assets/interiors/xref_parts") ||
                path.StartsWith("assets/interiors/npc_interactable_props"))
                return LECategory.StationInteriors;
            else if (
                path.StartsWith("assets/structures"))
                return LECategory.StationExteriors;
            else if (
                path.StartsWith("assets/legacy/props/Cockpit_Bridge") ||
                path.StartsWith("assets/interiors/bridges"))
                return LECategory.ShipInteriors;
            else if (
                path.StartsWith("assets/props/Engines") ||
                path.StartsWith("assets/props/SurfaceElements") ||
                path.StartsWith("assets/props/WeaponSystems") ||
                path.StartsWith("assets/units"))
                return LECategory.ShipExteriors;
            else
                return LECategory.Unset;
        }
    }

    internal static class TextureEntry
    {
        public enum TECategory
        {
            Unset,
            Fonts,
            GUI,
            NPCs,
            FX, 
            Environments,
            StationExteriors,
            StationInteriors,
            Ships,
            Misc
        };

        static public TECategory GetCategory(string path)
        {
            if (!path.EndsWith(".gz") && !path.EndsWith(".dds"))
                return TECategory.Unset;

            if (
                path.StartsWith("assets/fx/gui/fonts/textures/"))
                return TECategory.Fonts;
            else if (
                path.StartsWith("assets/fx/gui/textures/"))
                return TECategory.GUI;
            else if (
                path.StartsWith("assets/characters/") &&
                path.Contains("/textures/"))
                return TECategory.NPCs;
            else if (
                path.StartsWith("assets/legacy/fx/textures/") ||
	            path.StartsWith("assets/textures/fx/") ||
	            path.StartsWith("assets/legacy/fx/weaponfx/textures/"))
                return TECategory.FX;
            else if (
                path.StartsWith("assets/legacy/props/HighwayElements/textures/") ||
                path.StartsWith("assets/textures/ad_signs/") ||
                path.StartsWith("assets/legacy/textures/environments/") ||
                path.StartsWith("assets/textures/environments/"))
                return TECategory.Environments;
            else if (
                path.StartsWith("assets/legacy/textures/"))
                return TECategory.StationExteriors;
            else if (
                path.StartsWith("assets/legacy/textures/generic/"))
                return TECategory.Misc;
            else if (
                path.StartsWith("assets/legacy/textures/interiors/") ||
                path.StartsWith("assets/textures/interiors/") ||
                path.StartsWith("assets/textures/natural/") ||
                path.StartsWith("assets/legacy/textures/") && // "catch all" (mostly station interiors of different fractions)
                !path.Contains("textures/player/"))
                return TECategory.StationInteriors;
            else if (
                path.StartsWith("assets/legacy/textures/player/") ||
                (path.EndsWith(".gz") && path.StartsWith("assets/textures/"))) // "catch all" (mostly ship textures of different fractions)
                return TECategory.Ships;
            else
                return TECategory.Unset;
        }
    }
}
