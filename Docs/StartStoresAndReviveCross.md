# Tượng tiếp tế và thánh giá

Tượng `Stutue_0` ở đầu Level 1, Level 2, Level 3 và Level BossC1 mở cửa hàng CryHeal/CryShield bằng F. Tượng người dùng đã đặt ở Level 1 giữ nguyên vị trí và tỷ lệ. Tượng Level 2 nằm ngoài vùng tương tác của cửa quay về để F không chọn nhầm cửa. Tượng `Bless Statue( store)` ở Purchasing LevelsC1 chỉ bán vật phẩm đặc biệt; vật phẩm hiện có là thánh giá với sprite `Assets/Objects/Items/ThanhGia.png`.

| Vật phẩm | Giá linh thạch | Kho tối đa | Tác dụng |
| --- | ---: | ---: | --- |
| CryHeal | 6 | 5 | Hồi 30% HP tối đa; phím 1 |
| CryShield | 5 | 5 | Khiên 25% HP tối đa trong 8 giây; phím 2 |
| Thánh giá | 30 | 2 | Tự tiêu hao khi hết HP, hồi sinh tại chỗ với 50% HP và đầy stamina; bảo vệ 3 giây |

Giá và giới hạn nằm trong `Assets/Components/UI/Gameplay/Resources and UI Settings.asset`. Giá, sức hồi sinh và thời gian bảo vệ có thể chỉnh trong Inspector. Thánh giá có ô AUTO và số lượng riêng trên HUD, đồng thời dùng thông báo nhận vật phẩm hiện có.

Đối chiếu trên đĩa ngày 08/10/2026: mỗi quái vẫn rơi 1 linh thạch. Level 1/2/3 có 7/15/33 quái trả thưởng, tổng 55. Đổ đầy cả ba loại từ kho trống tốn 115 linh thạch: 55 cho CryHeal/CryShield và 60 cho hai thánh giá. Hai lượt dọn ba màn cho 110, còn thiếu 5 nếu chưa tính vật phẩm miễn phí và vật phẩm đã dùng trong lúc farm. Quay lại map tạo lại quái đầy HP và cho phép nhận thưởng mới. Cửa hàng dùng linh thạch đã ngân hàng khi qua cửa; linh thạch đang mang trong lượt chơi chưa thể chi tiêu. Phân tích kinh tế và curve C1–C3 hiện hành nằm trong [GDD](../GDD.md) và [Mô hình độ khó](../Mo_hinh_do_kho.md).

Khi hết HP, `PlayerCheckpointRespawn` lấy đúng một thánh giá trước khi xử lý Game Over. Sau khoảng thời gian animation chết hiện có (1,25 giây), `Health.Revive` phục hồi nhân vật tại nguyên vị trí. Rigidbody, trạng thái bật/tắt từng collider và Animator được phục hồi; combat và input tạm được xóa. Cảnh và quái đang đánh không tải lại. Chìa khóa, linh thạch chưa ngân hàng, linh thạch đã ngân hàng và các vật phẩm còn lại được giữ. Trong khoảng bảo vệ sau hồi sinh, sát thương quái/bẫy/rơi không làm mất HP. Hết thánh giá thì dùng lại luồng chết, mất tài nguyên chưa ngân hàng và Game Over/Restart trước đây; chết ở Boss C1 rồi Restart vẫn về Purchasing.

`PlayerRecoveryEffectUI` trên Game HUD phát các sprite và mốc thời gian chính xác lấy từ `AnimHeal/Heal.anim` và `AnimRevive/Revive.anim` trong hai khung UI phía trên nhân vật. Khung theo vị trí camera, không bị lật theo hướng nhìn của Player, không bắt raycast và tự ẩn sau một lượt animation. Pause dừng thời gian animation. Effect heal SpriteRenderer cũ không còn được tạo bên trong Player để tránh hiển thị hai hiệu ứng cùng lúc. Object xem thử `HoiSinh_0` ở Level 1 được giữ nhưng tắt để không tự lặp hiệu ứng trong thế giới. Khi nhận hồi máu từ blessing, cùng khung heal cũng hiển thị.

`Tools/BTL/Install Start Stores and Revive Cross` là bộ cài lặp lại được, giữ prefab/scene hiện có và cập nhật các tham chiếu. `Install Shared Gameplay UI and Respawn in Current Scene` cũng gắn tượng tiếp tế cho map gameplay mới đã có Player và LevelResourceFlow. Boss C2/C3 hiện chưa có hệ thống gameplay và không bị thay đổi.

Bản trước của code/assets và bản sao Level 1 chưa lưu nằm trong `outputs/statue-revive-20261006/before` và `unsaved-Level 1.unity`. Script kiểm tra và báo cáo nằm trong cùng thư mục output. Console khi mở nhiều scene URP đồng thời để authoring có thông báo trùng global light; runtime được kiểm tra mỗi lần một scene, đúng luồng chơi thực tế. Chưa chạy player build hoặc đánh giá độ khó bằng một lượt chơi đầy đủ của người chơi.

Unity 6000.3.18f1 đã biên dịch thành công. 82 kiểm tra scene/prefab, 73 kiểm tra Play Mode về kho/shop/heal/khiên/hồi sinh/di chuyển/combat/farm và 11 kiểm tra Boss C1/Restart/Purchasing đều đạt. Đã xem ảnh của shop tiếp tế, shop đặc biệt, khung heal và khung hồi sinh. Các lần thử đầu được giữ lại: fixture sát thương quái dùng số vượt phạm vi float-to-int đã được sửa về giá trị đủ lớn hợp lệ; fixture pause đợi LateUpdate trước khi lấy sprite; cửa ở Level 2 đã được tách khỏi vùng tương tác shop.

Clip Revive đã có 9 keyframe trong bộ nhớ Editor lúc bắt đầu, trong khi bản trên đĩa chưa có curve. Việc lưu asset đã lưu đúng animation người dùng đang soạn; các sprite và mốc thời gian được giữ như báo cáo baseline. Bản animation này cũng được giữ tại `outputs/statue-revive-20261006/authored-Revive.anim`.
