# Tầm nhìn quái và tối ưu mô phỏng

Ngày triển khai: 2026-10-07. Unity 6000.3.18f1, URP 2D, camera theo player.

## Hành vi

Tầm nhìn là vùng hiển thị của `Camera.main` trong Game View. Khi toàn bộ sprite nằm ngoài vùng này, quái và thanh máu riêng của nó được ẩn. Sprite còn chạm mép màn hình vẫn được hiển thị. Scene View và camera phụ không quyết định tầm nhìn của player. Nếu chưa có MainCamera, hệ thống khôi phục hình ảnh và mô phỏng cho tới khi camera xuất hiện.

Quái thường ở ngoài vùng camera mở rộng được tạm nghỉ: AI và motor bỏ qua tick, Animator tạm dừng giữ nguyên state, Rigidbody2D tạm ngừng simulated. Chúng giữ HP, vị trí, hướng và trạng thái hiện tại. Vùng thức dậy rộng hơn màn hình 3 world units; vùng bắt đầu ngủ rộng thêm 1 unit để tránh bật/tắt liên tục ở mép. Vật lý được khôi phục trước khi quái vào vùng hiển thị.

Collider gắn với Rigidbody2D đang ngủ cũng tạm rời mô phỏng và query vật lý. Sát thương gọi trực tiếp qua MonsterHealth vẫn đánh thức quái trước khi xử lý.

Đòn đánh đang chạy, Hurt, Mimic đang biến hình, quái đang rơi và quá trình chết tiếp tục hoạt động. Quái nhận damage được đánh thức trước khi phát sự kiện health, để reaction/death vẫn quyết định vật lý và Animator. Boss chỉ được ẩn hình ảnh; cơ chế boss luôn tiếp tục mô phỏng, gồm Appear, miễn sát thương, đạn, hồi máu và xác rơi.

## Tích hợp

`MonsterHealth.Awake` tự thêm `MonsterVisibilityCulling` khi chưa có, nên toàn bộ prefab, quái đặt trong scene và quái spawn bằng Instantiate đều được áp dụng mà không sửa scene/prefab. `MonsterVisibilitySystem` tạo một runtime object qua các scene, tính frustum một lần sau camera LateUpdate và duyệt registry quái. Registry được reset khi vào Play Mode, kể cả khi tắt Domain Reload. Runtime host dùng vòng đời thông thường và được huỷ khi thoát Play Mode.

Camera orthographic không xoay dùng thể tích AABB lấy từ viewport, gồm cả near/far clip. Camera xoay hoặc perspective dùng sáu frustum plane. Không tạo mảng, LINQ, tìm component/hierarchy hay instance vật liệu trong vòng cập nhật tầm nhìn.

`HealthBarUI.SetCulled` xử lý thanh máu world-space được tạo ngoài hierarchy của quái; khi hiện lại, vị trí thanh máu được cập nhật ngay. Các Renderer, Animator và body vốn bị disable bởi gameplay được giữ nguyên trạng thái khi hide/show. Không thay đổi `GameObject.activeSelf` của quái để cull.

Mặc định không cần thiết lập thêm. Camera gameplay phải giữ tag `MainCamera` như các map hiện tại. Muốn cấu hình lâu dài từng loại quái, có thể thêm `MonsterVisibilityCulling` vào prefab rồi chỉnh `Cull Outside Player View`, `Suspend When Far Away` và `Simulation Padding`. Bỏ chọn `Cull Outside Player View` tắt cả ẩn và ngủ cho quái đó. Quái thường kiểm tra layer `Ground` trước khi ngủ; quái bay và body không dùng gravity có thể ngủ trực tiếp khi đủ xa.

## Kiểm tra và bằng chứng

Bản sao source trước sửa, trạng thái Editor, hash scene, script kiểm tra, kết quả runtime và dữ liệu đo được lưu tại `outputs/monster-visibility-20261007`. Hai báo cáo đầu được giữ để ghi nhận điều chỉnh fixture Golem (cần kích hoạt Appear trước khi thử damage), cách tìm runtime host và việc bỏ DontSave để tránh native object tồn dư qua các phiên Play Mode.

Unity Editor biên dịch thành công, Console không có lỗi/warning gameplay mới. Cả **213 kiểm tra** đạt: 153 runtime trên đủ 7 prefab, 25 kiểm tra asset/script/meta và 35 kiểm tra trên Level 1, Level 2, Level 3, Purchasing C1, Boss C1. Runtime kiểm tra sprite chạm mép, ẩn thanh máu, ngủ/thức, giữ HP/Animator/body, component vốn disable, pause, camera phụ/mất/thay camera, camera xoay/perspective/clip xa, quái đang rơi/đã chạm Ground, damage/death, Mimic Transform, Golem dormant/Appear và vòng đời Play Mode. Số quái 7/15/37/0/1 được giữ; toàn bộ 7 file scene có hash giống trước sửa. Không sửa prefab, package, project settings hoặc Build Settings. Editor được trả về Purchasing C1 ở Edit Mode, sạch; runtime host đã được huỷ.

Đo đối chiếu trên **Level 3** gồm 6 lượt xen kẽ tắt/bật culling, 720 frame mỗi lượt, warmup 2 giây; player đứng ở vị trí bắt đầu, vị trí quái reset giữa lượt. VSync 0, target frame rate -1, quality 0 giống nhau. Dữ liệu thô và từng lượt: `measure-paired.json`; tổng hợp: `performance-summary.json`.

| Chỉ số Editor trung bình mỗi frame | Tắt culling | Bật culling |
| --- | ---: | ---: |
| Quái chạy AI / Animator / body | 37 | 8 |
| Quái tạm nghỉ | 0 | 29 |
| Update của các script | 0,0982 ms | 0,0817 ms |
| Physics2D.Simulate | 0,0290 ms | 0,0104 ms |
| DirectorUpdateAnimationBegin | 0,1172 ms | 0,0862 ms |
| LateUpdate của các script, gồm culling | 0,0972 ms | 0,1289 ms |
| Riêng kiểm tra tầm nhìn mới | 0 ms | 0,0552 ms |
| Main Thread | 2,4288 ms | 2,3845 ms |

Physics giảm khoảng **64%**, animation giảm khoảng **26%** trong workload này. Culling thêm chi phí LateUpdate; Main Thread thay đổi nhỏ và kết quả từng lượt biến động, nên chưa thể kết luận mức tăng FPS ổn định. GC tổng Editor khoảng 11,9 KB/frame ở cả hai chế độ, bao gồm Editor/diagnostics; đây không phải đo allocation riêng của tính năng. Không chạy player build hoặc đo GPU/máy mục tiêu. Số liệu Editor không thay thế việc đo FPS trong bản build. Unity vốn đã cull phần vẽ ngoài camera; tối ưu này chủ yếu giảm chi phí mô phỏng quái và cập nhật thanh máu.

Tham khảo API: [Renderer.forceRenderingOff](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Renderer-forceRenderingOff.html), [Rigidbody2D.simulated](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody2D-simulated.html).
