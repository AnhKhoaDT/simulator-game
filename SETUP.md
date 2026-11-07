# SETUP — Startup (Giai đoạn 1)

> Các bước dưới đây là **thao tác thủ công trong Unity Editor**. Chúng đụng tới
> Player Settings, Package Manager, scene (`.unity`) và pipeline — những thứ mà
> code **không được** tự sinh ra. Làm theo đúng thứ tự.

Dự án: Unity `6000.6.7f1` (Unity 6) + URP. Mở thư mục `simulator-game` (dấu gạch ngang,
**không** có dấu cách) bằng Unity Hub: `Add > Add project from disk`.

> Nếu Unity báo "Active Input Handling is invalid", gần như chắc chắn là đang mở nhầm
> thư mục project. Kiểm tra tiêu đề cửa sổ và đường dẫn project trong Unity Hub.

---

## 1. Player Settings

`Edit > Project Settings > Player`:

1. **Company Name** = `Startup`.
2. **Product Name** = `Startup`.
3. Khóa màn hình ngang (chỉ cần khi build mobile):
   - Mục **Default Orientation** chỉ hiện ở tab **Android/iOS**. Nếu Player Settings chỉ có
     tab PC và WebGL thì **chưa cài module Android**: Unity Hub > Installs > bánh răng bản
     `6000.6.7f1` > Add modules > **Android Build Support** (kèm OpenJDK, Android SDK & NDK).
   - Có thể **bỏ qua** bước này ở Giai đoạn 1 nếu chỉ chạy thử trên PC (Play trong Editor).
   - Khi đã có tab Android: `Resolution and Presentation` > **Default Orientation** =
     `Auto Rotation`, rồi trong **Allowed Orientations for Auto Rotation**:
     - Bật `Landscape Left`, bật `Landscape Right`.
     - Tắt `Portrait`, tắt `Portrait Upside Down`.

---

## 2. Package Manager

`Window > Package Manager`:

1. Cài **Localization** (`com.unity.localization`): bấm `+ > Install package by name...`,
   nhập `com.unity.localization`, bấm `Install`. Đây là nền cho đa ngôn ngữ.
2. Xác nhận **Input System** (`com.unity.inputsystem`) đã được cài (dự án đã có sẵn).
   Nếu Unity hỏi bật backend Input System mới và tắt backend cũ, chọn bật cái mới rồi
   để Editor khởi động lại.
3. Ghi chú về Locale (chi tiết đầy đủ ở **Giai đoạn 4**):
   - Chưa cần tạo Locale hay String Table ở bước này.
   - Giai đoạn 4 sẽ tạo hai Locale: **Tiếng Việt `vi` (mặc định)** và **English `en`**,
     cùng String Tables (key → giá trị theo từng locale) và Smart Strings.

---

## 3. External Tools (trình soạn code) — tùy chọn

`Edit > Preferences > External Tools`:

1. **External Script Editor**: chọn trình soạn code đang dùng.
   - Không có trong danh sách thì chọn `Browse...`. Trên Linux, trong hộp thoại bấm
     `Ctrl+L` rồi gõ đường dẫn, ví dụ Kiro: `/opt/kiro/bin/kiro`.
2. Nút **Regenerate project files** chỉ hiện với các editor Unity hỗ trợ sẵn (Visual Studio,
   VS Code, Rider). Với Kiro nút này **không hiện** — bỏ qua được, game vẫn chạy bình thường
   (chỉ ảnh hưởng gợi ý code). Các file `.csproj`/`.sln` không commit (đã nằm trong `.gitignore`).

---

## 4. URP — Mobile Renderer

Dự án đã có sẵn trong `Assets/Settings/`: `Mobile_RPAsset` + `Mobile_Renderer` (cho mobile)
và `PC_RPAsset` + `PC_Renderer` (cho PC).

1. `Edit > Project Settings > Quality`: với mức chất lượng dùng cho **Android/mobile**
   (ví dụ `Mobile`), đặt **Render Pipeline Asset** = `Mobile_RPAsset`.
2. Với mức chất lượng dùng cho **PC** (ví dụ `PC`), đặt = `PC_RPAsset`.
3. Kiểm tra `Edit > Project Settings > Graphics` đang trỏ tới RP Asset hợp lý làm mặc định
   (`PC_RPAsset` là ổn).

---

## 5. Scene và Build Settings

Ba scene `Splash`, `MainMenu`, `BarGame` nằm trong `Assets/_Project/Scenes/`.
Nếu thiếu, tạo tay (code **không** tạo file `.unity`):

1. `File > New Scene`, chọn template **Empty** hoặc **Basic**, bấm `Create`.
2. `File > Save As...` (hoặc `Ctrl+S`), lưu vào đúng `Assets/_Project/Scenes/` với tên
   `Splash`, rồi lặp lại cho `MainMenu` và `BarGame`.
   - Scene mới tạo mang tên `Untitled` là **chưa lưu**, chưa có file `.unity` trên đĩa.
3. Thêm vào Build: `File > Build Profiles` (Unity 6; bản cũ là `Build Settings`), mục
   **Scene List**. Mở từng scene (bấm đúp trong tab Project) rồi bấm **Add Open Scenes**.
   Thứ tự (index 0 trước):
   1. `Splash`
   2. `MainMenu`
   3. `BarGame`
4. Bỏ tick `Assets/Scenes/SampleScene.unity` khỏi danh sách (không bắt buộc xóa file).
5. `Ctrl+S` để Unity ghi `ProjectSettings/EditorBuildSettings.asset` xuống đĩa.

> Tên scene phải khớp đúng các hằng trong `SceneNames.cs`
> (`"Splash"`, `"MainMenu"`, `"BarGame"`) để `SceneLoader` tải đúng.

---

## 6. Kiểm tra cuối

1. Tab **Console**: bấm `Clear`, đợi biên dịch xong. Không có dòng **đỏ** là đạt
   (cảnh báo vàng tạm bỏ qua). Unity không hiện thông báo gì khi biên dịch thành công.
2. Tab **Project**, ô tìm kiếm gõ `t:AssemblyDefinitionAsset` (tìm theo tên file thì
   gõ `asmdef` sẽ **không** ra). Phải thấy đủ 6 file `Startup.*`:
   `Core`, `Data`, `Shared`, `Features.Splash`, `Features.MainMenu`, `Features.BarGame`.
3. Hiện mới có `Startup.Core` và `Startup.Data` biên dịch ra `.dll` vì chỉ hai assembly này
   có file `.cs`. Các assembly còn lại trống nên chưa sinh `.dll`; đó là bình thường.
4. Mở scene `Splash` rồi bấm **Play**: Console không có lỗi đỏ là đạt.

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

> Unity tự sinh file `.meta` cho mỗi file/thư mục mới khi import (lần đầu mở project, hoặc
> khi focus lại Editor sau khi tạo file từ bên ngoài). **Không** tạo tay file `.meta` và
> **không** thêm `*.meta` vào `.gitignore`; khi commit nhớ commit `.meta` kèm asset của nó.
