# Quy ước thư mục Assets

Tách nội dung theo hai nhóm chính để dễ tìm và không lẫn đối tượng game với thành phần dùng lại:

- `Assets/Objects/` chứa prefab và tài nguyên riêng của đối tượng trong game: `Player`, `Enemies`, `Bosses`, `Items`, `Environment`; scene nằm trong `World/Scenes`.
- `Assets/Components/` chứa logic và thành phần dùng chung: script runtime trong `Scripts/Runtime`, script Boss riêng trong `Scripts/Bosses`, công cụ Editor trong `Editor`, UI trong `UI`, cùng `Resources`, `Settings` và `Physics`.
- UI theo phạm vi sử dụng: `UI/Gameplay`, `UI/Bosses`, `UI/Enemies`. Hiệu ứng chỉ dùng riêng cho Player vẫn ở `Objects/Player/Ui`.
- `TextMesh Pro`, `_Recovery`, `Temp` và `outputs` được giữ riêng làm package/tài liệu phục hồi/tạm/archive.

Hai tên thư mục đặc biệt cần giữ nguyên: `Editor` để Unity nhận diện script Editor, và `Resources` để `Resources.Load` tiếp tục tìm được asset. Khi tạo thêm asset, đặt prefab và hình/animation riêng của đối tượng dưới `Objects`; đặt script, cấu hình, UI và tài nguyên dùng chung dưới `Components`.
