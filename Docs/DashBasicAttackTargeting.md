# Dash, đánh thường và tự bám mục tiêu

Triển khai ngày 2026-10-07, Unity 6000.3.18f1.

## Cách chơi

- Bấm **J trong khi dash**: giữ một lệnh đánh và thực hiện ngay ở update kết thúc dash. Dash vẫn dùng clip 0,6 giây và stamina hiện tại; lệnh J không mất sau bộ đệm cũ 0,18 giây.
- Đánh thường dưới đất tự chọn **quái sống gần nhất trong 3,5 world units**, tính tới bề mặt collider. Mục tiêu phải có Hurtbox đang hoạt động, nằm trong chiều cao của vùng đánh và không bị địa hình chắn đường. Quái chết, Mimic chưa mở và mục tiêu quá cao không được chọn.
- Player quay về mục tiêu, lướt ngang một đoạn ngắn tới khi `AttackHitbox` chạm collider của nó, rồi gây sát thương bằng **chính collider AttackHitbox**. Player đã ở tầm đánh sẽ đứng tại chỗ. Mỗi cú combo chọn lại mục tiêu; mỗi cú chỉ đánh một mục tiêu đã chọn, một lần.
- Không có mục tiêu phù hợp vẫn vung đòn. Không dịch chuyển tức thời, kéo player lên không hoặc đi xuyên tường. Mục tiêu ra khỏi vùng đánh trước lúc va chạm thì đòn có thể trượt.
- Đánh trên không giữ JumpAttack; hai skill giữ vùng đánh diện rộng, pulse, stamina và miễn sát thương sẵn có. Cú lướt của đánh thường không tiêu hao thêm stamina dash và không cấp miễn sát thương dash.

## Thiết lập

Không cần sửa hoặc gắn thêm component cho scene/prefab hiện có. `PlayerCombat.Awake` tự liên kết BoxCollider2D của child `AttackHitbox`. Nếu một biến thể player không có box này, hệ thống dùng circle của `attackRange` như trước.

Trong Inspector của PlayerCombat, nhóm **Basic Attack Targeting** có:

| Field | Mặc định | Ý nghĩa |
| --- | ---: | --- |
| Auto Target Basic Attacks | true | Bật chọn mục tiêu và lướt tới tầm đánh |
| Basic Attack Hitbox | tự tìm AttackHitbox | Có thể gắn collider khác cho biến thể player |
| Basic Attack Target Range | 3,5 | Phạm vi tìm collider mục tiêu |
| Basic Attack Approach Speed | 28 | Tốc độ lướt ngang |
| Basic Attack Approach Duration | 0,15 giây | Giới hạn thời gian lướt, còn được giới hạn bởi windup của từng cú |

Sát thương cơ bản 12, combo 0,2/0,6 giây, clip, tốc độ dash và chi phí stamina hiện tại được giữ. Damage của đánh thường dùng box được tác giả đặt sẵn thay cho circle khi box tồn tại. Pulse box/circle cũ vẫn có nguyên chữ ký API; skill và JumpAttack tiếp tục mặc định đánh tất cả receiver trong vùng.

## Tích hợp và kiểm tra

`PlayerCombat` sở hữu lệnh đánh sau dash, target lock và thời điểm gây damage. `PlayerMotor2D` sở hữu quay mặt và vận động bằng Rigidbody2D trong FixedTick; các collider thân thật được cast trước mỗi bước để chặn tường mỏng. Knockback, `SuspendForPanel`, death và disable hủy chuyển động/lệnh chờ. Damage vẫn được kiểm tra bằng physics overlap ở impact; khóa mục tiêu không tự gây damage ngoài collider. `CharacterAnimationDriver` chuyển trực tiếp từ Dash sang Atk1 để không mất trigger ở frame kết thúc dash. Target selection dùng lại các buffer của CombatHitbox2D; không quét toàn scene hay tạo collection mỗi frame.

Unity biên dịch không có lỗi/warning gameplay mới. **134 kiểm tra đạt**: 117 runtime và 17 prefab/API. Runtime dùng prefab thật, Animator thật và mô phỏng Physics2D bước 0,02 giây, đưa PlayerInputFrame qua cùng thứ tự xử lý của PlayerController. Bao gồm 7 loại quái/boss ở cả hai phía, mục tiêu gần/xa/cao/chết/Mimic ngủ, sát thương đúng 12 và không trùng, collider contact, tường có sẵn/tường mỏng xuất hiện giữa đường, knockback/menu/death/disable, J đầu dash, frame kết thúc dash bị chậm, không tự đánh nếu chưa bấm J, đổi mục tiêu ở cú combo thứ hai, JumpAttack, fallback circle và pulse diện rộng của cả hai skill. Boss trong fixture được kích hoạt và giữ đứng yên để kiểm tra hình học/damage.

Không chạy player build hoặc thử cảm giác điều khiển bằng tay; các kiểm tra input dùng PlayerInputFrame và luồng gameplay hiện có. Bản đầu có một kiểm tra combo quan sát sớm hơn impact của Atk2; báo cáo đó được giữ, và kiểm tra cuối chờ đủ windup 0,3 giây.

Cảnh Boss C1 đang mở và chưa lưu của người dùng được sao lưu trước khi thử. Editor đã trở về chính cảnh đó ở Edit Mode, vẫn dirty như ban đầu; cả 184 object giữ nguyên path/active/layer/local transform. Hash của 7 scene trên đĩa, Player prefab và Player AnimatorController giống trước sửa. Không chỉnh package, Build Settings hay asset gameplay khác. Bản sao source, scene chưa lưu, script kiểm tra, kết quả và diff: `outputs/dash-basic-targeting-20261007`.
