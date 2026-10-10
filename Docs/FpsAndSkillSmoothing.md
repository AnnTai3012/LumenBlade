# 60 FPS và chuyển động khi dùng chiêu

Cập nhật: 2026-10-07. Mục tiêu người dùng chọn: **60 FPS ổn định**.

`GameplayFramePacing` tự khởi tạo trước scene đầu, giữ cấu hình qua chuyển map và đọc `Assets/Components/Resources/GamePerformanceSettings.asset`. Profile hiện tại là 60 FPS, vật lý 60 Hz và tối đa 0,1 giây mô phỏng chạy bù mỗi frame. Không phải gắn thêm component vào các scene. Thoát Play Mode khôi phục cấu hình Editor, kể cả khi tắt Domain Reload.

Trong bản desktop, màn hình có thể chia chính xác về khoảng 60 Hz sẽ dùng VSync; chẳng hạn 60/120/180/240 Hz dùng lần lượt 1/2/3/4 nhịp. Màn hình 144 Hz hiện tại dùng giới hạn phần mềm 60 FPS để giữ mục tiêu đã chọn. Nhịp hiển thị vật lý trên màn hình đó chưa được đo trong bản build. Lựa chọn này tuân theo quan hệ giữa [targetFrameRate và VSync của Unity](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html).

`PlayerController.TickActions` áp dụng thay đổi khóa di chuyển ngay trong frame bắt đầu/kết thúc hành động. `CharacterAnimationDriver` vào clip chiêu ngay lập tức và trả về chuyển động khi kết thúc/hủy, giữ ưu tiên TakeHit và Death. `PlayerCombat` giữ lệnh chiêu bấm trong lúc dash đến frame dash kết thúc; chiêu được buffer gần cuối chiêu trước có thể bắt đầu ngay khi chiêu trước xong. Chi phí stamina, điều kiện mở Skill 2, thời lượng clip và các pulse sát thương vẫn do hệ thống hiện có quyết định.

Các chỉnh sửa nằm trong PlayerController, PlayerCombat, CharacterAnimationDriver, hai script cấu hình FPS mới và asset cấu hình mới. Scene, Player prefab, clip/AnimatorController, Project Settings và package trên đĩa được giữ nguyên.

## Kết quả đo

Đo Unity 6000.3.18f1 Editor ở Level 3, 37 quái trong map, 2 đang hoạt động ở vị trí camera bắt đầu. Mỗi lượt warmup 2 giây rồi lấy 360 frame; player đứng cố định và được bảo vệ. Phiên đối chiếu cuối chạy riêng, không đồng thời với fixture kiểm tra combat.

| Trường hợp | Frame trung bình | P95 frame | Bước vật lý sau khựng 250 ms |
| --- | ---: | ---: | ---: |
| Cấu hình cũ: không giới hạn FPS, vật lý 50 Hz | 2,25 ms | 3,04 ms | 13 |
| Cấu hình mới: 60 FPS, vật lý 60 Hz | 16,67 ms | 17,18 ms | 6 |
| Dùng K liên tục: 5 lần, 266 frame trong chiêu | 16,67 ms | 17,78 ms | Không áp dụng |

Đây là ổn định nhịp về mục tiêu 60 FPS; không phải kết luận tăng tốc render. Khựng 250 ms được cố ý tạo trong phép thử, không được tính vào cửa sổ frame ổn định. Giới hạn 0,1 giây giảm số bước chạy bù và lượng thời gian gameplay tăng sau một frame dài, theo [Time.maximumDeltaTime](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-maximumDeltaTime.html). Nó không loại bỏ nguyên nhân của mọi khựng CPU/GPU.

## Kiểm tra

**269 kiểm tra cuối đạt**, cùng Unity biên dịch thành công:

- 75 kiểm tra nhập lệnh, khóa/mở di chuyển, animation và dash → chiêu tại nhịp 30/60/120 FPS. Cùng fixture trước sửa có 42 kiểm tra thất bại.
- 31 kiểm tra nối chiêu, hủy chiêu, Skill 2, thiếu stamina, khóa chapter, menu, TakeHit và Death.
- 117 kiểm tra hồi quy combat với đủ 7 loại quái/boss: J/combo, tiếp cận mục tiêu, tường, dash, JumpAttack, mỗi chiêu đủ 5 pulse và miễn thương Skill 2. Fixture dùng bước vật lý 0,02 giây rõ ràng; bộ kiểm tra chuyển động mới bổ sung các nhịp khác.
- 21 kiểm tra cấu hình FPS, VSync, singleton, pause và fallback; 17 kiểm tra prefab/API; 8 kiểm tra trạng thái Editor và metadata.

59 file asset/settings/package được so hash đều giữ nguyên. Cảnh Level 3 chưa lưu vẫn dirty như ban đầu; cả 358 transform, active state và layer được giữ nguyên. Editor đã về Edit Mode, không còn runtime host. Console cuối có 0 lỗi/0 cảnh báo hiện hành; buffer vẫn giữ cảnh báo có sẵn về Input Manager/Administrator và cảnh báo trạng thái play/stop của công cụ.

Nguồn trước sửa, bản sao scene chưa lưu, fixture, báo cáo từng lần, diff và ảnh chiêu được lưu tại `outputs/fps-skill-smoothing-20261007`. Báo cáo đầu của fixture được giữ: giả định stamina 100 ở chapter 3 đã được sửa sang profile chapter 5; kiểm tra cuối đối chiếu scene parse cả hai JSON để tránh sai khác biểu diễn float. Một kiểm tra TakeHit đã phát hiện việc dọn chiêu lấn phản ứng bị đánh và dẫn tới bổ sung ưu tiên animation. Lượt đo FPS đầu bị chồng fixture đã bị dừng và không dùng cho số liệu cuối.

Chưa chạy player build, đo GPU/nhịp hiển thị của bản build hoặc đánh giá cảm giác điều khiển bằng tay. Ảnh `skill1-midcast.png` xác nhận frame chiêu hiển thị trong map; ảnh tĩnh không chứng minh độ mượt của cả animation.
