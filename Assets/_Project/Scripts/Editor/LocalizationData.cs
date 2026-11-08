namespace Startup.EditorTools
{
    // All String Table content in one place. Add a row for a new key, then rerun Startup/Setup Localization.
    public static class LocalizationData
    {
        public readonly struct Row
        {
            public readonly string Key;
            public readonly string Vietnamese;
            public readonly string English;

            public Row(string key, string vietnamese, string english)
            {
                Key = key;
                Vietnamese = vietnamese;
                English = english;
            }
        }

        public static readonly Row[] Rows =
        {
            new Row("splash_title", "Startup", "Startup"),
            new Row("splash_tagline", "Mô phỏng kinh doanh", "Business Simulation"),
            new Row("splash_loading", "Đang tải...", "Loading..."),
            new Row("menu_title", "Startup", "Startup"),
            new Row("menu_subtitle", "Chọn loại quán", "Choose a venue"),
            new Row("menu_venue_bar", "Quán Nhậu", "Pub"),
            new Row("menu_venue_cafe", "Quán Cafe", "Cafe"),
            new Row("menu_venue_drinks", "Quán Nước", "Drink Shop"),
            new Row("menu_venue_locked", "Khóa", "Locked"),
            new Row("menu_btn_language", "Ngôn ngữ: Tiếng Việt", "Language: English"),
            new Row("menu_btn_settings", "Cài đặt", "Settings"),
            new Row("menu_btn_quit", "Thoát", "Quit"),
            new Row("hud_btn_shop", "Cửa hàng", "Shop"),
            new Row("shop_title", "Cửa hàng", "Shop"),
            new Row("shop_upgrade_table", "Mua thêm bàn", "Buy a table"),
            new Row("shop_upgrade_waiter", "Thuê bồi bàn", "Hire a waiter"),
            new Row("shop_info_table", "(Đã có {0}/{1} bàn - Giá {2})", "(Tables {0}/{1} - Price {2})"),
            new Row("shop_info_waiter", "(Cấp {0}/{1} - Giá {2})", "(Level {0}/{1} - Price {2})"),
            new Row("shop_maxed", "(Đã tối đa)", "(Maxed out)"),
            new Row("shop_btn_buy", "Mua", "Buy"),
            new Row("common_close", "Đóng", "Close"),
            new Row("hud_money", "Tiền: {0}", "Money: {0}"),
            new Row("hud_customers", "Khách đã phục vụ: {0}", "Customers served: {0}"),
            new Row("hud_time", "Thời gian: {0}", "Time: {0}"),
            new Row("hud_btn_pause", "Tạm dừng", "Pause"),
            new Row("hud_paused", "Tạm dừng", "Paused"),
            new Row("common_resume", "Tiếp tục", "Resume"),
            new Row("common_home", "Về trang chủ", "Main menu"),
            new Row("bargame_served", "Đã phục vụ {0}  +{1}", "Served {0}  +{1}"),
            new Row("bargame_item_beer", "Bia", "Beer"),
            new Row("bargame_item_snack", "Mồi nhậu", "Snack"),
            new Row("bargame_item_soda", "Nước ngọt", "Soda"),
            new Row("bargame_item_grill", "Đồ nướng", "Grilled meat"),
            new Row("bargame_unlocked", "Món mới: {0}", "New item: {0}"),
        };
    }
}
