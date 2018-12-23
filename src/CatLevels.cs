namespace LOWRES_X4
{
    public class LODLevels
    {
        public int LvlEnvironment;
        public int LvlCollectables;
        public int LvlShipInteriors;
        public int LvlShipExteriors;
        public int LvlStationInteriors;
        public int LvlStationExteriors;

        public bool AllZero()
        {
            return
                LvlEnvironment == 0 &&
                LvlCollectables == 0 &&
                LvlShipInteriors == 0 &&
                LvlShipExteriors == 0 &&
                LvlStationInteriors == 0 &&
                LvlStationExteriors == 0;
        }
    }

    public class TextureLevels
    {
        public int MinTextureSize;
        public int LvlFonts;
        public int LvlGUI;
        public int LvlNPCs;
        public int LvlFX;
        public int LvlEnvironments;
        public int LvlStationExteriors;
        public int LvlStationInteriors;
        public int LvlShips;
        public int LvlMisc;

        public bool AllZero()
        {
            return
                LvlFonts == 0 &&
                LvlGUI == 0 &&
                LvlNPCs == 0 &&
                LvlFX == 0 &&
                LvlEnvironments == 0 &&
                LvlStationExteriors == 0 &&
                LvlStationInteriors == 0 &&
                LvlShips == 0 &&
                LvlMisc == 0;
        }
    }
}
