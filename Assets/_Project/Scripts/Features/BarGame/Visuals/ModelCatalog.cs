namespace Startup.Features.BarGame
{
    // Resources paths of the imported Kenney model packs, in one place.
    // Kenney models face -Z; the yaw constants turn them where a pack differs from that.
    internal static class ModelCatalog
    {
        public const string Suburban = "Models/Suburban/";
        public const string Commercial = "Models/Commercial/";
        public const string Roads = "Models/Roads/";
        public const string Cars = "Models/Cars/";
        public const string Nature = "Models/Nature/";
        public const string Food = "Models/Food/";
        public const string Shop = "Models/Shop/";

        // Extra yaw if a pack's front turns out to be +Z instead of -Z (set to 180 to flip).
        public const float BuildingYawOffset = 0f;
        public const float CarYawOffset = 180f;

        public static readonly string[] Houses =
        {
            "building-type-a", "building-type-b", "building-type-c", "building-type-d", "building-type-e",
            "building-type-f", "building-type-g", "building-type-h", "building-type-i", "building-type-j",
            "building-type-k", "building-type-l", "building-type-m", "building-type-n", "building-type-o",
            "building-type-p", "building-type-q", "building-type-r", "building-type-s", "building-type-t",
            "building-type-u"
        };

        public static readonly string[] Shops =
        {
            "building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g",
            "building-h", "building-i", "building-j", "building-k", "building-l", "building-m", "building-n"
        };

        public static readonly string[] Skyline =
        {
            "low-detail-building-a", "low-detail-building-b", "low-detail-building-c", "low-detail-building-d",
            "low-detail-building-e", "low-detail-building-f", "low-detail-building-g", "low-detail-building-h",
            "low-detail-building-i", "low-detail-building-j", "low-detail-building-k", "low-detail-building-l",
            "low-detail-building-m", "low-detail-building-n", "low-detail-building-wide-a", "low-detail-building-wide-b"
        };

        public static readonly string[] Skyscrapers =
        {
            "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c",
            "building-skyscraper-d", "building-skyscraper-e"
        };

        public static readonly string[] Trees =
        {
            Nature + "tree_default", Nature + "tree_oak", Nature + "tree_fat", Nature + "tree_detailed",
            Nature + "tree_simple", Suburban + "tree-large"
        };

        public static readonly string[] Traffic =
        {
            "sedan", "sedan-sports", "taxi", "van", "suv", "hatchback-sports", "delivery", "police", "truck"
        };
    }
}
