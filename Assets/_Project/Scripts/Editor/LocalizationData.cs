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
            new Row("hud_money", "Ví quán: {0}", "Shop wallet: {0}"),
            new Row("settings_title", "Cài đặt", "Settings"),
            new Row("settings_owner_name", "Tên chủ quán", "Owner name"),
            new Row("settings_tutorial", "Hướng dẫn chơi", "How to play"),
            new Row("common_owner_default", "Chủ quán", "Owner"),
            new Row("staff_waiter_default", "Bồi bàn", "Waiter"),
            new Row("tut_title", "Hướng dẫn chơi", "How to play"),
            new Row("tut_controls_title", "Điều khiển", "Controls"),
            new Row("tut_controls_body",
                "Máy tính: WASD để đi, rê chuột để nhìn quanh. Bấm vào màn hình để khóa chuột, Tab để hiện chuột khi cần bấm nút. Điện thoại: kéo nửa trái màn hình để đi, kéo nửa phải để nhìn.",
                "PC: WASD to walk, move the mouse to look around. Click the game to lock the cursor, Tab to free it for buttons. Phone: drag the left half to walk, the right half to look."),
            new Row("tut_shift_title", "Mở quán", "Opening the bar"),
            new Row("tut_shift_body",
                "Bấm Mở quán (phím O) để bắt đầu ca. Người đi đường sẽ rẽ vào quán. Từ 17 giờ đến 23 giờ khách đông nhất, đêm khuya vắng khách. Đóng quán khi muốn nghỉ, khách đang ngồi vẫn ăn xong rồi mới về.",
                "Press Open shop (O) to start a shift and passers-by will come in. Evenings from 17:00 to 23:00 are busiest, late night is quiet. Close whenever you like; seated guests still finish."),
            new Row("tut_serve_title", "Phục vụ khách", "Serving guests"),
            new Row("tut_serve_body",
                "Khách ngồi vào bàn sẽ hiện bong bóng hình món họ muốn. Đi tới gần khách là phục vụ, món sẽ hiện trên bàn. Thanh dưới bong bóng là độ kiên nhẫn: chuyển đỏ là sắp bỏ về.",
                "Seated guests show a bubble with the item they want. Walk up to them to serve it and it appears on the table. The bar under the bubble is their patience: red means they are about to leave."),
            new Row("tut_cashier_title", "Thu tiền", "Taking payment"),
            new Row("tut_cashier_body",
                "Ăn xong, khách hiện đồng xu và ra quầy thu ngân xếp hàng. Đứng lên vòng tròn sau quầy (chuyển xanh) để thu tiền. Không ai thu thì khách bỏ đi không trả tiền.",
                "After eating, guests show a coin and queue at the cashier. Stand on the circle behind the counter (it turns green) to take payment. If nobody comes, they leave without paying."),
            new Row("tut_shop_title", "Cửa hàng", "Shop"),
            new Row("tut_shop_body",
                "Dùng tiền trong ví quán (phím B) để mua thêm bàn và thuê bồi bàn tự phục vụ. Món mới tự mở khi tổng doanh thu đủ lớn.",
                "Spend the shop wallet (B) on more tables and a waiter who serves for you. New menu items unlock as total revenue grows."),
            new Row("tut_view_title", "Góc nhìn", "Camera"),
            new Row("tut_view_body",
                "Phím V đổi giữa góc nhìn thứ nhất và góc nhìn sau lưng. Esc để tạm dừng.",
                "V switches between first person and the over-the-shoulder view. Esc pauses the game."),
            new Row("hud_customers", "Khách đã phục vụ: {0}", "Customers served: {0}"),
            new Row("hud_time", "Giờ: {0}", "Time: {0}"),
            new Row("hud_status_open", "Đang mở cửa", "Open"),
            new Row("hud_status_closed", "Đã đóng cửa", "Closed"),
            new Row("hud_btn_open", "Mở quán", "Open shop"),
            new Row("hud_btn_close", "Đóng quán", "Close shop"),
            new Row("hud_hint_pc",
                "WASD: đi  •  Chuột: nhìn  •  V: đổi góc nhìn  •  Tab: hiện chuột  •  O: mở/đóng quán  •  B: cửa hàng  •  Esc: tạm dừng",
                "WASD: walk  •  Mouse: look  •  V: switch view  •  Tab: show cursor  •  O: open/close  •  B: shop  •  Esc: pause"),
            new Row("hud_hint_touch", "Kéo bên trái: đi  •  Kéo bên phải: nhìn quanh",
                "Drag left side: walk  •  Drag right side: look around"),
            new Row("hud_btn_pause", "Tạm dừng", "Pause"),
            new Row("hud_paused", "Tạm dừng", "Paused"),
            new Row("common_resume", "Tiếp tục", "Resume"),
            new Row("common_home", "Về trang chủ", "Main menu"),
            new Row("bargame_served", "Thu tiền {0}  +{1}", "Paid for {0}  +{1}"),
            new Row("bargame_item_beer", "Bia", "Beer"),
            new Row("bargame_item_snack", "Mồi nhậu", "Snack"),
            new Row("bargame_item_soda", "Nước ngọt", "Soda"),
            new Row("bargame_item_grill", "Đồ nướng", "Grilled meat"),
            new Row("bargame_unlocked", "Món mới: {0}", "New item: {0}"),
        };
    }
}
