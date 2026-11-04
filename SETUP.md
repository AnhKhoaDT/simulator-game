# SETUP — Startup (Giai đoạn 1)

> Các bước dưới đây là **thao tác thủ công trong Unity Editor**. Chúng đụng tới
> Player Settings, Package Manager, scene (`.unity`) và pipeline — những thứ mà
> code **không được** tự sinh ra. Làm theo đúng thứ tự.

Dự án: Unity `6000.6.7f1` (Unity 6) + URP. Mở thư mục `simulator game` bằng Unity Hub.

---

## 1. Player Settings

`Edit > Project Settings > Player`:

1. **Company Name** = `Startup`.
2. **Product Name** = `Startup`.
3. Mở mục **Resolution and Presentation**:
   - **Default Orientation** = `Landscape Left` (hoặc `Auto Rotation` nếu muốn cho xoay cả hai chiều ngang).
   - Trong **Allowed Orientations for Auto Rotation**:
     - Bật `Landscape Left`.
     - Bật `Landscape Right`.
     - Tắt `Portrait`.
     - Tắt `Portrait Upside Down`.
   - Mục tiêu: game chỉ chạy ngang (mobile landscape trước), PC vẫn chạy bình thường.

---

## 2. Package Manager

`Window > Package Manager`:

1. Cài **Localization** (`com.unity.localization`): bấm `+ > Add package by name...`,
   nhập `com.unity.localization`, bấm `Add`. Đây là nền cho đa ngôn ngữ.
2. Xác nhận **Input System** (`com.unity.inputsystem`) đã được cài (dự án đã có sẵn).
   Nếu Unity hỏi bật backend Input System mới và tắt backend cũ, chọn bật cái mới rồi
   để Editor khởi động lại.
3. Ghi chú về Locale (chi tiết đầy đủ ở **Giai đoạn 4**):
   - Sau khi cài Localization, vào `Edit > Project Settings > Localization` để tạo
     **Localization Settings** nếu chưa có.
   - Tạo hai Locale: **Tiếng Việt `vi` (mặc định)** và **English `en`**.
   - String Tables (key → giá trị theo từng locale) và Smart Strings sẽ dựng ở Giai đoạn 4.

---

## 3. External Tools (trình soạn code)

`Edit > Preferences > External Tools`:

1. **External Script Editor** = VS Code (hoặc Kiro).
2. Bấm **Regenerate project files** để Unity tạo lại file `.csproj`/`.sln`
   (các file này không commit — đã nằm trong `.gitignore`).

---

## 4. URP — Mobile Renderer

Dự án đã có sẵn trong `Assets/Settings/`: `Mobile_RPAsset` + `Mobile_Renderer` (cho mobile)
và `PC_RPAsset` + `PC_Renderer` (cho PC).

1. `Edit > Project Settings > Quality`: với mức chất lượng dùng cho **Android/mobile**,
   đặt **Render Pipeline Asset** = `Mobile_RPAsset`.
2. Với mức chất lượng dùng cho **PC**, đặt = `PC_RPAsset`.
3. Kiểm tra `Edit > Project Settings > Graphics` đang trỏ tới RP Asset hợp lý làm mặc định.

---

## 5. Tạo Scene và thêm vào Build Settings

Code **không** tạo file `.unity`, nên tạo tay ba scene sau:

1. Tạo scene mới (`File > New Scene`) rồi `File > Save As...` lần lượt, lưu vào
   `Assets/_Project/Scenes/`:
   - `Splash.unity`
   - `MainMenu.unity`
   - `BarGame.unity`
   (Để scene trống lúc này cũng được; nội dung dựng ở các giai đoạn sau.)
2. `File > Build Settings...`, bấm **Add Open Scenes** để thêm từng scene.
   Thứ tự mong muốn (index 0 trước):
   1. `Splash`
   2. `MainMenu`
   3. `BarGame`
3. Có thể bỏ `Assets/Scenes/SampleScene.unity` khỏi danh sách Build Settings
   (không bắt buộc xóa file).

> Tên scene phải khớp đúng các hằng trong `SceneNames.cs`
> (`"Splash"`, `"MainMenu"`, `"BarGame"`) để `SceneLoader` tải đúng.

---

## Kiến trúc Assembly Definition (asmdef) — chiều phụ thuộc

Các assembly đã tạo trong Giai đoạn 1 và quan hệ phụ thuộc:

- `Startup.Shared` — module lá, **không** phụ thuộc ai.
- `Startup.Core` → phụ thuộc `Startup.Shared`.
- `Startup.Data` → phụ thuộc `Startup.Shared`.
- `Startup.Features.Splash` / `.MainMenu` / `.BarGame`
  → phụ thuộc `Startup.Core`, `Startup.Data`, `Startup.Shared`.

Nguyên tắc: **Core và Data độc lập nhau** (không tham chiếu lẫn nhau). Feature chỉ
được phụ thuộc Core / Data / Shared, **không bao giờ** phụ thuộc feature khác. Khi hai
feature cần trao đổi, đi qua Core hoặc một event channel.

> Lần đầu mở Editor, Unity sẽ tự sinh file `.meta` cho các thư mục/asset mới.
> **Không** tạo tay file `.meta`; khi commit nhớ commit `.meta` kèm asset của nó.
