# Skill 3 và trần Chúc Phúc

Cập nhật 07/10/2026 cho Unity 6000.3.18f1.

## Chỉ số

Mỗi Chúc Phúc cộng 5% chỉ số gốc của Chapter. HP, dung lượng stamina và damage có bộ đếm riêng, giới hạn 20 lần (+100%). C1: HP 100 → 200; stamina 100 → 200; J 12 → 24 và U 24 → 48. Chọn một loại 20 lần sẽ max loại đó; với một phần thưởng mỗi level, cần 60 lựa chọn chỉ số để max cả ba. Khi đã max, hệ thống dùng phần thưởng vật phẩm/hồi máu thay thế như trước.

Stamina gốc C1–C5 là 100 / 115 / 130 / 145 / 160; HP gốc vẫn 100 / 110 / 120 / 130 / 140. HP quái/boss và damage của chúng giữ bộ cân chỉ số hiện có. Chúc Phúc giữ qua map trong cùng Chapter, reset khi đổi Chapter. Sang map/restart không nhân buff lần nữa. Không thêm lưu game xuống đĩa.

Lựa chọn số 2 hiện là **Thể lực**, tăng dung lượng stamina; tốc độ hồi giữ 12/giây. Enum cũ `StaminaRecovery = 1` vẫn tương thích với dữ liệu scene, alias cho `MaxStamina = 1`.

## Đánh xa bằng U

- Skill mở ngay C1. U bắn luồng chém ngang theo hướng nhân vật; damage gốc 24, cao hơn J 12 và jump attack 18.
- Một lượt tiêu `stamina gốc / 6`: khoảng 16,67 ở C1. Thanh 100 dùng liên tiếp đúng 6 lần; thanh đã max 200 dùng 12 lần trước khi hồi. Chi phí không tăng theo Chúc Phúc dung lượng.
- Clip `Skill_3` dùng chín sprite theo thứ tự trái → phải của sheet, 12 frame/giây, dài 0,75 giây, không loop. Pivot theo thân nhân vật và dùng pixel nguyên/48 PPU.
- Luồng chém có bảy mẫu collider đi từ trước mặt đến khoảng 7,2 world unit ở scale prefab. Quét đoạn giữa các mẫu để không bỏ sót khi frame bị chậm. Mỗi quái nhận damage tối đa một lần mỗi lượt. Collider dưới chân/weapon của quái không thay vùng nhận damage của thân. Terrain chặn damage.
- Đòn mới chỉ bắt đầu sau khi Animator thực sự kết thúc, kể cả khi animation bị giảm tốc. U nhấn gần cuối có thể được buffer; U trong dash chờ dash kết thúc. Giữ U không tự bắn vì Input dùng KeyDown.
- Di chuyển/nhảy/dash bị khóa trong cast theo luật combat chung. Cast dùng được khi đang trên không. Trúng đòn, chết, disable hoặc mở panel sẽ hủy cast/collider. Pause theo time scale dừng cả animation và damage.
- Stamina ngừng hồi trong cast U, trở lại sau độ trễ 0,5 giây lúc nghỉ. Skill dùng cue âm thanh Skill 1 hiện có.

## Inspector và tài sản

`Assets/Objects/Player/Player.prefab` đã có child **Skill3Hitbox** với BoxCollider2D trigger, được tắt ngoài cast. `PlayerCombat` đã gắn clip và component Skill 3. `PlayerInputReader` đã gắn U. `Player.controller` có Trigger **Skill3**, Bool **IsUsingSkill**, dây Any State → Skill_3 và dây ra Fall/Run/idle ở exit time 1; Jump không ngắt cast.

Thanh HP world-space giữ mốc đầu nhân vật trong skill 3; các animation khác vẫn dùng vị trí theo sprite như trước, tránh thanh HP chạy theo vùng ảnh của luồng chém.

Sửa damage tại `SkillDamageController > Skill 3 Damage Per Hit` (phần tử đầu). Sửa số lượt trên thanh gốc tại `PlayerStamina > Skill 3 Casts Per Base Bar`. Các mẫu offset/size theo animation nằm ở `Skill3Hitbox > Frames`.

Menu `Tools/BTL/Configure Ranged Skill 3 and Blessing Caps` cung cấp lại cấu hình mặc định cho clip/controller/prefab/settings; nó đặt damage U về 24, số lượt về 6 và cập nhật các mô tả Chúc Phúc. Việc sửa pivot được thực hiện bằng Sprite Editor data provider trong script riêng ở outputs, không nằm trong menu này.

## Kiểm chứng

Unity biên dịch thành công. Toàn bộ **324 kiểm tra** đạt: 38 kiểm tra chỉ số/C1–C3 và C5, 105 kiểm tra đánh xa trên bảy prefab quái/boss theo hai hướng, 117 kiểm tra combat cũ, 22 kiểm tra thanh HP qua chín frame/hai hướng, 20 kiểm tra prefab/Animator/metadata và 22 kiểm tra năm scene/chuyển tiếp prefab. Các test dùng input frame của PlayerController; chưa chơi thử bằng bàn phím thật. Chưa chạy player build.

Các bản sao trước sửa, script và báo cáo nằm trong `outputs/skill3-blessing-cap-20261007`. Báo cáo đầu tiên được giữ: lỗi bỏ sót body collider đã được sửa; hai kỳ vọng stamina trong regression cũ được đổi từ số dư 20 của thanh gốc 40 sang kiểm tra chi phí dash 20 trên thanh gốc mới 100. Kiểm tra thanh HP dùng physics tĩnh để loại trừ vị trí nội suy ở hai frame đầu. Hai timeout của Pipeline lúc import và các thông báo global light khi mở map additively trong Edit Mode được ghi lại, không phải lỗi trong lượt chơi một scene bình thường.

Các scene đã lưu và ProjectSettings/Packages giữ hash gốc. Scene Level 1 ban đầu vẫn sạch, mọi transform/active state cũ được giữ; chỉ có child Skill3Hitbox mới được kế thừa từ prefab.
