# Cửa quay lại, trigger và key theo map

Các scene gameplay hiện tại dùng tuyến `Level 1 ↔ Level 2 ↔ Level 3 ↔ Purchasing LevelsC1`. Level 1 có cửa ra; ba scene còn lại có thêm `Entrance Return` gần điểm bắt đầu. Scene BossC1/C2/C3 hiện chưa có Player, map hay gameplay nên không nằm trong tuyến này.

## Đi qua cửa

- Bước thân player vào trigger để đi qua cửa. Nếu đã đứng trong cửa lúc nhận key hoặc lúc tiếp tục sau pause, cửa vẫn tự chuyển khi đủ điều kiện. Phím F ở gần cửa vẫn hoạt động. Collider vũ khí/skill không kích hoạt chuyển màn.
- Cửa ra chưa mở cần key của map hiện tại và chơi animation Open. Key chỉ được tiêu thụ khi player còn sống và việc chuyển màn hợp lệ.
- Cửa đã mở giữ frame cuối của Open và dùng lại được ngay, không cần thêm key. Cửa quay lại luôn hiển thị frame mở, không cần key.
- Đi tới spawn gần cửa Entrance của màn đích; quay lại spawn gần cửa Exit của màn trước. Điểm spawn nằm phía trong map và ngoài trigger để không tự quay màn. Restart giữ cửa đã dùng để vào map làm checkpoint.
- Tải lại scene tạo lại toàn bộ quái với HP đầy. Mỗi lần ghé lại có thể nhận SpiritStone từ các quái đó. Đi qua cửa ngân hàng hóa đá chưa bank; chết mất phần chưa bank theo cơ chế hiện có.
- Blessing chỉ xuất hiện khi đi tới một map mới lần đầu. Quay lại/farm không tạo thêm blessing. Purchasing giữ cơ chế hồi đầy HP khi đến.

## Inventory của map

`RunResources` giữ trạng thái theo đường dẫn scene trong lượt chơi hiện tại:

- Key đã nhặt nhưng chưa dùng thuộc về map đó. Đi sang map khác không mang key sang; quay lại sẽ khôi phục key đang giữ của map.
- Dùng key mở cửa đánh dấu key map là đã dùng. Trạng thái này giữ qua chuyển scene và chết/restart.
- Key đặt sẵn trong map đã dùng sẽ bị ẩn và hủy khi scene khởi tạo; quái hồi sinh không thể rơi key đó nữa. Chúng vẫn thưởng SpiritStone như thường.
- Chết khi chưa dùng key bỏ key đang giữ của map đang chơi; quái hồi sinh có thể rơi lại key chưa dùng. Key chưa dùng ở map khác không mất.

Đây là trạng thái trong RAM như các tài nguyên hiện có; dừng lượt chơi/thoát ứng dụng bắt đầu lại trạng thái key. Chưa thêm hệ thống lưu xuống đĩa.

## Collider

- Root collider của Slime, Bat, Rat, Mimic, DemonSlime, Bringer of Death và Golem là trigger. Child `Ground Collider` là collider đặc, chỉ va chạm layer Ground; nó giữ trọng lực/chạm nền mà không chặn player. Hitbox đánh và Hurtbox vẫn dùng logic cũ, bao gồm trạng thái Mimic chưa mở và miễn nhiễm của Golem.
- Golem giữ hurtbox là trigger khi chết; collider nền ở child xử lý rơi/chạm đất. Bat cũng giữ cơ chế rơi sau khi chết.
- Tổng cộng 112 ô trap được chuyển từ tilemap nền sang `Tilemap Spike Triggers` cùng vị trí, sprite, màu và transform. `Spike Floors` là các collider đỡ mỏng ở đáy trap, không có hình ảnh; đầu gai không còn là vật cản đặc.
- `TrapTileDamage2D` nhận tiếp xúc của thân player: rơi vào gai gây 10 damage, tiếp tục chạm gây 5 damage mỗi 0.4 giây; các giá trị vẫn chỉnh được trong Inspector. Pause dừng sát thương; rời trigger dừng damage định kỳ. Hitbox vũ khí ra khỏi trap không xóa tiếp xúc nếu thân player vẫn đứng trên gai.
- Tilemap nền Purchasing LevelsC1 được đặt về layer Ground để kiểm tra nền và điểm spawn hoạt động nhất quán.

## Thêm map hoặc chapter

Map mới cần Player, `LevelResourceFlow` với Chapter Index đúng, collider nền layer Ground, và cửa ra có Target Scene Name hợp lệ. Thêm scene vào Build Settings. Chạy `Tools/BTL/Install Chapter Return Doors and Triggers` trong Edit Mode sau khi lưu scene để cấu hình prefab/scene, tách trap và tạo cửa quay lại theo các liên kết cửa ra trong cùng chapter. Scene trống được bỏ qua.

Prefab `Assets/Objects/Items/EntranceReturn.prefab` có sẵn để đặt thủ công. `Door Id = Entrance`, `Target Door Id = Exit`, Require Key tắt và Start Open bật. Cửa ra dùng `Door Id = Exit`, `Target Door Id = Entrance`. Arrival Offset xác định điểm xuất hiện theo hệ tọa độ của cửa; giữ vị trí đó ngoài trigger và trên nền an toàn.

## Kiểm tra

Unity Editor 6000.3.18f1 đã compile thành công. 59 kiểm tra Play Mode cho chuyển màn/key/farm/restart và 48 kiểm tra Play Mode cho trigger/combat/trap đều đạt; 49 kiểm tra scene/prefab cũng đạt. Đã kiểm tra sát thương melee của cả 7 loại quái/boss, spell của Bringer, sát thương basic/skill một lần trên mỗi mục tiêu, trạng thái Mimic trước/sau khi mở, rơi/chạm nền và cleanup của Bat/Golem, và damage/pause/rời trigger của trap. Các bước chuyển màn được cô lập bằng cách tắt input và AI; bộ combat dùng AI thực trong scene kiểm tra riêng.

112 ô trap và số quái của từng scene (7/15/37/0) giữ nguyên. Scene/prefab không có missing script; GUID/meta hiện có và Build Settings giữ nguyên. Chạy lại installer không làm đổi nội dung các scene/prefab đã cấu hình. Không có lỗi compile hay gameplay mới; vòng thiết lập ban đầu có cảnh báo density và timeout của công cụ kiểm tra, đã sửa trước lần xác minh cuối.

Báo cáo `travel-report.json`, `combat-report.json`, `assets-report.json` và các script kiểm tra được lưu trong `outputs/map-travel-20261005`. Các báo cáo vòng đầu cũng được giữ lại: fixture giết quái đã được chỉnh về MaxHealth thay vì int.MaxValue; fixture Mimic phân biệt trạng thái chest chưa mở; kiểm tra Build Settings trong Edit Mode dùng EditorBuildSettings thay vì API runtime. Bản sao scene/prefab/code trước thay đổi nằm trong thư mục `before`. Chưa chạy player build.

## Sửa lỗi tự qua cửa khi đứng yên (2026-10-05)

Đã tái hiện lỗi với PlayerController và Animator vẫn hoạt động: đứng trong cửa ra Level 1 chưa có key, chờ Rigidbody2D tự sleep, rồi nhận key. Player còn sống, thân vẫn chạm trigger và CanInteract trả về true, nhưng sau 2.5 giây cửa vẫn không mở; dùng đường tương tác F thì chuyển được. Cửa trước đây chỉ thử chuyển màn trong callback Enter/Stay, nên bỏ lỡ thay đổi key khi Rigidbody2D đã sleep.

`LevelExitDoor` lưu collider thân player khi nhận tiếp xúc và kiểm tra lại trong Update khi thân còn ở trong cửa. Rời cửa, tắt collider hoặc tắt player sẽ xóa tiếp xúc đó. Cửa tiếp tục giữ điều kiện key/pause/player sống, lọc collider vũ khí và chặn quay màn ngay tại điểm spawn. Không đổi prefab, scene, inventory hay combat.

Chạy lại đúng tình huống trên sau sửa đã tự chuyển sang Level 2 trước khi gọi F. Kết quả trước/sau, kiểm tra các cửa và bản sao code nằm trong `outputs/entrance-auto-20261005`. Vòng đầu kiểm tra tuyến cửa đã chuyển đủ 10 lượt; một giả định của fixture về hitbox luôn chạm cửa ở mọi điểm spawn đã được sửa theo vị trí thực tế của cửa ra Level 3.

Lần xác minh cuối đạt 64 kiểm tra qua 10 lượt chuyển màn: đi bộ vào tất cả cửa tới/quay lại, key đã dùng, quái hồi sinh, spawn không tự quay màn dù hitbox vũ khí chạm cửa, bật/tắt Enter On Touch, pause/resume và F tùy chọn. Fixture đi bộ dùng PlayerMotor2D thực với input ngang lập trình; fixture đứng yên giữ PlayerController thực. Compile thành công, không có Console error/warning mới và Editor đã về Level 1 Edit Mode. Chưa chạy player build cho bản sửa này.
