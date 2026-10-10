# Căn collider và animation của quái — 10/10/2026

## Phạm vi và nguyên nhân

Đã đo 392 sprite keyframe của 44 clip trên 7 prefab: Slime, Rat, Bat, Mimic, Demon Slime, Golem và Bringer of Death. Với sprite sheet bị Unity giảm kích thước khi import, phép đo dùng đúng kích thước texture sau import, pivot và PPU của Unity.

Slime/Rat/Bat trước đây dùng pivot giữa từng sprite có chiều cao khác nhau. Khi animation đổi frame, điểm thấp nhất của hình ảnh thay đổi dù Rigidbody và collider đứng yên. Collider thân/chạm đất của các quái này cũng nhỏ hoặc có offset nằm cao hơn chân. Golem có một số frame shield/melee/death thấp hơn đường chân Idle. Golem đã có xử lý căn rubble sau khi tiếp đất trong `LateUpdate`; xử lý đó được giữ nguyên.

## Thay đổi

| Loại | Collider thân/chạm đất (đơn vị local) | Căn sprite |
|---|---|---|
| Slime | Capsule ngang, size 0,70 × 0,40; offset Y −0,01875; đáy −0,21875 | Chung đường chân −0,1979167 qua đi/đánh/hurt/death |
| Rat | Thay hai Circle nhỏ bằng Capsule ngang, size 0,64 × 0,38; offset Y −0,0391667; đáy −0,2291667 | Chung đường chân −0,2083333 qua idle/run/attack/hurt/death |
| Bat | Capsule ngang, size 0,44 × 0,40; offset Y −0,30; đáy −0,50 | Chung đường chân −0,4791667 qua fly/attack/hurt/fly-to-fall/fall |
| Golem | Giữ Box hiện có | Chỉ nâng frame thấp hơn đường chân Idle −0,359375; giữ các frame vỡ/bay lên vốn đã ở trên ground |
| Mimic, Demon Slime, Bringer | Giữ collider/pivot hiện có | Dữ liệu đã giữ hình ảnh trên đáy collider qua các clip được quét |

103 pivot trên 14 texture được chỉnh qua Unity Sprite Editor Data Provider, sau khi kiểm tra capability `EditPivot`. Giữ tên sprite, sprite IDs, rectangles, PPU, scale và các key/event của animation. Một số sprite corpse rất thấp cần custom pivot nằm ngoài rectangle để vẫn giữ cùng đường chân; Unity đã chấp nhận và phép đo sau import xác nhận vị trí mới.

Riêng `attack 1.png` của Slime có kích thước gốc 2964 × 156 nhưng trước đây bị giới hạn 2048 và nén texture, tạo tọa độ sprite lẻ cùng các pixel alpha mờ ngoài đường chân. Importer đã đổi Max Size thành 4096 và Compression thành Uncompressed cho đúng texture này, giữ Point filtering và PPU cấu hình 48. Sau đó căn lại 19 frame theo pixel GPU. PNG gốc không đổi; texture GPU giờ giữ đúng 2964 × 156, đổi lại tốn thêm bộ nhớ so với bản giảm kích thước/nén.

Collider nhận đòn ở root vẫn là trigger; collider `Ground Collider` vẫn là solid chỉ tương tác với Ground. Các attack hitbox riêng, tầm đánh và thông số sát thương không thay đổi. Collider đất giữ ổn định qua animation; không thêm code thay size collider mỗi frame, tránh Rigidbody bị đẩy lên/xuống khi đổi đòn.

## Kiểm tra đã thực hiện

- Đo lại toàn bộ 392 frame/44 clip và xem contact sheet trước/sau. Các pixel có alpha từ 20/255 trở lên không còn nằm dưới đáy collider đất theo phép đo.
- Đọc pixel thực tế sau import qua GPU cho toàn bộ frame: cả pixel có alpha lớn hơn 0 cũng không còn nằm dưới đáy collider đất (`gpu-pixel-validation.json`, 7/7 loại đạt).
- Kiểm tra 57 instance trong cả 7 scene ở `Assets/Objects/World/Scenes`: không có missing script; Slime/Rat/Bat nhận đúng collider mới, không thấy override hình học cũ khác prefab.
- Trong Play Mode, tạo physics scene 2D riêng và ground Box phẳng. Cho từng prefab rơi, sau đó sample các frame và mô phỏng PhysicsScene2D để kiểm tra vị trí collider và pixel chân. Cả 7 prefab đạt; mức chân thấp nhất vẫn trên ground. `physics-validation.json` ghi số liệu cụ thể.
- Fixture vật lý đã được gỡ, Editor trở lại Level 1 Edit Mode. Scene của người dùng đã dirty trước khi làm; không lưu hoặc thay thế các thay đổi chưa lưu đó.

Phép kiểm tra vật lý dùng mặt đất phẳng và sample clip trực tiếp; chưa chạy toàn bộ chuỗi AI/combat ở mọi mép bậc hoặc địa hình dốc. Các file PNG gốc và animation clip không bị chỉnh; thay đổi nằm ở metadata pivot của sprite sheet và collider trong 3 prefab.

## Tài liệu kiểm chứng và khôi phục

Thư mục `outputs/monster-collider-20261010` có:

- `before.json`, `after.json`: snapshot prefab/frame.
- `before-sheet.png`, `after-sheet.png`: ảnh sprite với đường đỏ là đáy collider đất.
- `scene-audit.json`: instance trong 7 scene.
- `physics-validation.json`: kết quả ground phẳng trong Play Mode.
- `gpu-pixel-validation.json`: kiểm tra pixel có alpha khác 0 sau import.
- `before-assets`: bản trước sửa của các prefab và `.meta` liên quan.
- Các script audit, đo pixel, chỉnh pivot/collider và kiểm tra vật lý để giải thích hoặc chạy lại.
