# Cửa Purchasing sang màn boss

Ngày cập nhật: 2026-10-06.

`Assets/Objects/World/Scenes/Purchasing LevelsC1.unity` có cửa **Entrance Boss C1** bên phải cửa hàng, tại `(-82.5, 16.3233)`. Cửa dùng prefab EntranceForward hiện có, không cần hoặc tiêu thụ chìa khóa. Chạm bằng thân Player hoặc tương tác F sẽ mở cửa và đi tới `Level BossC1`. Cửa quay về Level 3 vẫn được giữ nguyên.

Khi nhân vật hết HP ở `Level BossC1`, nút Restart đưa về Purchasing C1 tại điểm `(-83.85, 16.2701)`, bên cạnh cửa vừa dùng để vào boss. Nhân vật hồi đầy HP/stamina và đứng ngoài trigger để tránh tự đi vào lại. Có thể nhấn F để vào lại ngay, hoặc bước ra xa cửa rồi đi vào theo cơ chế chống chuyển màn ngược hiện có.

SpiritStone đã ngân hàng, vật phẩm chưa dùng và blessing của Chapter vẫn được giữ. SpiritStone chưa ngân hàng của lượt đánh boss bị mất khi chết. Cửa không đánh dấu chìa khóa Purchasing là đã dùng và không xóa chìa khóa đang giữ ở map đó. Restart ở màn thường vẫn tải lại màn đó tại cửa checkpoint trước đây.

Theo lựa chọn của người dùng, **chưa đặt boss**. Level BossC1 đã có Player, nền Tilemap trên layer Ground, camera theo Player, ánh sáng URP, HUD, EventSystem, RunResources và LevelResourceFlow. Có cửa quay về Purchasing bằng F. Scene được bật trong Build Settings sau Purchasing C1; BossC2 và BossC3 được giữ nguyên. Người dùng có thể đặt prefab boss lên nền arena khi tiếp tục thiết kế màn.

## Cấu hình

- Cửa Purchasing: `doorId = BossExit`, `targetSceneName = Level BossC1`, `targetDoorId = Entrance`, `requireKey = false`, `returnHereOnTargetRestart = true`, `offerBlessingOnArrival = false`.
- Cửa boss quay về: `doorId = Entrance`, `targetSceneName = Purchasing LevelsC1`, `targetDoorId = BossExit`, `requireKey = false`, `enterOnTouch = false`.
- LevelResourceFlow trong boss: `restartScenePath = Assets/Objects/World/Scenes/Purchasing LevelsC1.unity`, `restartDoorId = BossExit`. Đây là checkpoint dự phòng khi chạy trực tiếp scene boss từ Editor. Nếu đã đi qua cửa Purchasing, checkpoint thực tế được lưu theo scene đích trong RunResources và được ưu tiên.
- PlayerCheckpointRespawn dùng chung đường Restart cho nút Game Over và trường hợp không có HUD. Trạng thái HP mang theo từ lượt trước được xóa trước khi tải checkpoint.

## Xác minh

Unity 6000.3.18f1 biên dịch thành công. 69 kiểm tra Play Mode đạt: vào không có key, pause/resume khi chạm cửa, giữ HP khi vào boss, ngân hàng tài nguyên, death/Game Over/Restart, hồi đúng Purchasing, không chuyển ngược tự động, mở shop sau chết, đi bộ vào lần nữa, giữ chìa khóa Purchasing, fallback khi không có HUD, quay về bằng cửa boss, chạy boss trực tiếp và regression checkpoint/cửa khóa/quái hồi sinh ở Level 3. Vì chưa đặt boss, death được kích hoạt qua Health.TakeDamage để kiểm tra toàn bộ luồng hết HP và Restart.

31 kiểm tra scene đạt: reference Player/HUD/resource owner, camera theo đúng Player, nền solid, cửa và checkpoint đúng hướng, không missing script, scene metadata và Build Settings hợp lệ. Chưa chạy player build và chưa kiểm tra một trận đánh boss thực tế.

Purchasing đang có thay đổi chưa lưu khi bắt đầu; bản sao toàn bộ trạng thái đó nằm ở `outputs/purchasing-boss-door-20261006/unsaved-Purchasing LevelsC1.unity`. Bản scene đã lưu giữ nguyên nội dung này và thêm 88 dòng cho cửa/cấu hình mới. Bản trước của code, scene và Build Settings nằm trong `before` của cùng thư mục.

Các script cài đặt và xác minh nằm ngoài Assets trong `outputs/purchasing-boss-door-20261006`. Báo cáo chính: `playmode-flow.json`, `playmode-direct-restart.json`, `assets-report.json`. Báo cáo vòng thử đầu được giữ lại: fixture vào lại cửa đã được sửa để bước ra xa theo arrival guard; fixture màn thường đã được sửa để giữ nhân vật/quái trước Start, tránh dịch vị trí do physics/AI trước khi đo checkpoint. Các lỗi ánh sáng khi mở đồng thời scene shop và boss để authoring không xảy ra trong luồng tải một scene ở Play Mode.
