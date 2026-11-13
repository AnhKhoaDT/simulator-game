using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    public enum FurnitureKind
    {
        Table,
        Chair,
        Decor
    }

    // Shop tabs. Staff is not furniture; the shop adds its own rows there.
    public enum ShopCategory
    {
        Seating,
        Plants,
        Lighting,
        Rugs,
        Storage,
        Appliances,
        Electronics,
        Staff
    }

    // One thing the shop sells and the owner can place. Names are String Table keys, never display text.
    public sealed class FurnitureDef
    {
        public FurnitureDef(string id, ShopCategory category, FurnitureKind kind, int price, string modelPath,
            float height, Vector2 flatSize = default, int seatSlots = 0)
        {
            Id = id;
            Category = category;
            Kind = kind;
            Price = price;
            ModelPath = modelPath;
            Height = height;
            FlatSize = flatSize;
            SeatSlots = seatSlots;
        }

        public string Id { get; }

        public string NameKey => "shop_item_" + Id;

        public ShopCategory Category { get; }

        public FurnitureKind Kind { get; }

        public int Price { get; }

        public string ModelPath { get; }

        // Uniform fit height for standing items.
        public float Height { get; }

        // Floor size (x, z) of a rug-like item that lies flat and can be walked over; zero for everything else.
        public Vector2 FlatSize { get; }

        public bool IsFlat => FlatSize != Vector2.zero;

        // How many people can sit on it (owner and resting waiters); 0 for things you cannot sit on.
        public int SeatSlots { get; }
    }

    // Everything for sale, in shop order. Tables and chairs are built by TableBuilder; the rest become PlacedDecor.
    public static class FurnitureCatalog
    {
        public const string TableId = "table";
        public const string ChairId = "chair";

        private const string Furniture = "Models/Furniture/";
        private const string Shop = "Models/Shop/";

        private static readonly FurnitureDef[] Defs =
        {
            new FurnitureDef(TableId, ShopCategory.Seating, FurnitureKind.Table, 150, Furniture + "table", 0.75f),
            new FurnitureDef(ChairId, ShopCategory.Seating, FurnitureKind.Chair, 40, Furniture + "chair", 0.9f),
            new FurnitureDef("bench", ShopCategory.Seating, FurnitureKind.Decor, 90, Furniture + "bench", 0.8f, seatSlots: 2),
            new FurnitureDef("sofa", ShopCategory.Seating, FurnitureKind.Decor, 220, Furniture + "loungeSofa", 0.85f, seatSlots: 2),
            new FurnitureDef("armchair", ShopCategory.Seating, FurnitureKind.Decor, 130, Furniture + "loungeChair", 0.85f, seatSlots: 1),

            new FurnitureDef("plant_big", ShopCategory.Plants, FurnitureKind.Decor, 60, Furniture + "pottedPlant", 1f),
            new FurnitureDef("plant_small_1", ShopCategory.Plants, FurnitureKind.Decor, 25, Furniture + "plantSmall1", 0.45f),
            new FurnitureDef("plant_small_2", ShopCategory.Plants, FurnitureKind.Decor, 25, Furniture + "plantSmall2", 0.45f),
            new FurnitureDef("plant_small_3", ShopCategory.Plants, FurnitureKind.Decor, 25, Furniture + "plantSmall3", 0.45f),

            new FurnitureDef("lamp_round", ShopCategory.Lighting, FurnitureKind.Decor, 80, Furniture + "lampRoundFloor", 1.6f),
            new FurnitureDef("lamp_square", ShopCategory.Lighting, FurnitureKind.Decor, 80, Furniture + "lampSquareFloor", 1.5f),

            new FurnitureDef("rug_rect", ShopCategory.Rugs, FurnitureKind.Decor, 120, Furniture + "rugRectangle", 0.03f, new Vector2(3f, 2f)),
            new FurnitureDef("rug_round", ShopCategory.Rugs, FurnitureKind.Decor, 100, Furniture + "rugRound", 0.03f, new Vector2(2f, 2f)),
            new FurnitureDef("rug_square", ShopCategory.Rugs, FurnitureKind.Decor, 100, Furniture + "rugSquare", 0.03f, new Vector2(2f, 2f)),
            new FurnitureDef("doormat", ShopCategory.Rugs, FurnitureKind.Decor, 30, Furniture + "rugDoormat", 0.03f, new Vector2(1.2f, 0.7f)),

            new FurnitureDef("bookcase_open", ShopCategory.Storage, FurnitureKind.Decor, 150, Furniture + "bookcaseOpen", 1.7f),
            new FurnitureDef("bookcase_closed", ShopCategory.Storage, FurnitureKind.Decor, 170, Furniture + "bookcaseClosedWide", 1.7f),
            new FurnitureDef("side_table", ShopCategory.Storage, FurnitureKind.Decor, 60, Furniture + "sideTable", 0.55f),
            new FurnitureDef("tv_cabinet", ShopCategory.Storage, FurnitureKind.Decor, 130, Furniture + "cabinetTelevision", 0.6f),
            new FurnitureDef("coat_rack", ShopCategory.Storage, FurnitureKind.Decor, 50, Furniture + "coatRackStanding", 1.7f),
            new FurnitureDef("trashcan", ShopCategory.Storage, FurnitureKind.Decor, 20, Furniture + "trashcan", 0.5f),

            new FurnitureDef("fridge", ShopCategory.Appliances, FurnitureKind.Decor, 300, Furniture + "kitchenFridge", 1.8f),
            new FurnitureDef("freezer", ShopCategory.Appliances, FurnitureKind.Decor, 250, Shop + "freezer", 1f),
            new FurnitureDef("display_case", ShopCategory.Appliances, FurnitureKind.Decor, 200, Shop + "display-small", 1.1f),
            new FurnitureDef("stove", ShopCategory.Appliances, FurnitureKind.Decor, 200, Furniture + "kitchenStove", 0.9f),
            new FurnitureDef("sink", ShopCategory.Appliances, FurnitureKind.Decor, 180, Furniture + "kitchenSink", 0.9f),
            new FurnitureDef("kitchen_cabinet", ShopCategory.Appliances, FurnitureKind.Decor, 140, Furniture + "kitchenCabinet", 0.9f),

            new FurnitureDef("tv", ShopCategory.Electronics, FurnitureKind.Decor, 220, Furniture + "televisionModern", 0.75f),
            new FurnitureDef("speaker", ShopCategory.Electronics, FurnitureKind.Decor, 100, Furniture + "speaker", 1f)
        };

        private static readonly Dictionary<string, FurnitureDef> ById = BuildIndex();

        public static IReadOnlyList<FurnitureDef> All => Defs;

        public static FurnitureDef Find(string id)
        {
            return id != null && ById.TryGetValue(id, out var def) ? def : null;
        }

        // String Table key of a shop tab.
        public static string CategoryKey(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Seating: return "shop_cat_seating";
                case ShopCategory.Plants: return "shop_cat_plants";
                case ShopCategory.Lighting: return "shop_cat_lighting";
                case ShopCategory.Rugs: return "shop_cat_rugs";
                case ShopCategory.Storage: return "shop_cat_storage";
                case ShopCategory.Appliances: return "shop_cat_appliances";
                case ShopCategory.Electronics: return "shop_cat_electronics";
                default: return "shop_cat_staff";
            }
        }

        private static Dictionary<string, FurnitureDef> BuildIndex()
        {
            var index = new Dictionary<string, FurnitureDef>();
            foreach (var def in Defs)
            {
                index[def.Id] = def;
            }

            return index;
        }
    }
}
