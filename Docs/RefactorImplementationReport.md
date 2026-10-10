# Báo cáo triển khai refactor gameplay

Ngày rà soát: 2026-09-23  
Phạm vi: gameplay Player, combat, health/feedback, Monster và migration `SampleScene`.

## Trạng thái

**Đã triển khai các thay đổi mã và migration scene. Nghiệm thu đầy đủ còn bị chặn ở đo hiệu năng và kiểm thử Play Mode.** Không có số liệu FPS, p95/p99, GC hay CPU before/after để kết luận refactor giảm lag hoặc đạt mục tiêu `0 B` allocation.

## Kết quả theo thứ tự kế hoạch

| Bước | Trạng thái | Kết quả và bằng chứng |
|---|---|---|
| 1. Đo nền và thêm marker | **Đo nền bị chặn; marker đã thêm** | Tài liệu kế hoạch nêu kết quả capture cũ theo định tính, nhưng không có capture hay số liệu gốc trong workspace. Thư mục `ProfilerCaptures` không có tệp. Đã thêm marker `Gameplay.Player.Movement`, `Gameplay.Combat.Query`, `Gameplay.Skill.Pulse`, `Gameplay.Damage.Apply` để dùng ở lượt đo sau. |
| 2. Chuẩn hóa combat query | **Đã triển khai** | `CombatHitbox2D` dùng buffer overlap tái sử dụng, `ContactFilter2D`, dedupe theo `Hurtbox2D` thay vì collider và tăng buffer có giới hạn cùng cảnh báo. `Skill1Hitbox` và basic attack dùng chung đường query. Không còn `OverlapCircleAll`, `OverlapBoxAll` hoặc `OnTriggerEnter2D` trong các script Player/Monster đã rà. |
| 3. Dùng chung skill nhiều pulse | **Đã triển khai** | `SkillExecution` phục vụ Skill 1 và Skill 2, dựng danh sách pulse một lần, bắt kịp các pulse đến hạn theo thứ tự khi frame trễ và chặn phát lại giữa timer với Animation Event. Khi hủy hành động, execution được cancel và hitbox tắt. Scene hiện có bốn giá trị Skill 2 `[5, 5, 5, 25]`; bốn thời điểm pulse được lưu riêng. |
| 4. Tách Player | **Đã triển khai** | `PlayerController` điều phối thứ tự chạy; `PlayerInputReader`, `PlayerMotor2D`, `PlayerCombat`, `CharacterAnimationDriver` sở hữu input, vận động, combat và Animator tương ứng. `PlayerMotor2D` sở hữu ghi `Rigidbody2D`; đường disable/death xóa input buffer và hủy dash đang chạy. Các tên hàm bridge Animation Event cũ được giữ. |
| 5. Tách health và Monster | **Đã triển khai** | `Health`/`MonsterHealth` giữ máu, i-frame và event; `HitFeedback` sở hữu flash. `MonsterController` giữ AI state; `MonsterMotor2D` ghi vật lý; `MonsterCombat` giữ cooldown và impact một lần. `MonsterController` không còn ghi Rigidbody2D trực tiếp. |
| 6. Migration và kiểm chứng | **Migration tĩnh đạt; runtime/performance chưa xác nhận** | `SampleScene` đã nối component mới và giữ các tham chiếu/config chính. Scene có trong Build Settings và Editor log ghi nhận đã tải scene. Kiểm tra tĩnh không thấy ID scene trùng, GUID trùng hoặc thiếu `.meta` cho script. 19 script gameplay biên dịch bằng compiler đi kèm Unity với 0 lỗi. Chưa chạy Play Mode, test suite, build hoặc đo Profiler. |

## Thay đổi chính

### Combat

- [CombatHitbox2D](../Assets/Components/Scripts/Runtime/Player/CombatHitbox2D.cs) có buffer collider và buffer receiver tái sử dụng. Khi kết quả lấp đầy buffer, buffer tăng theo cấp số nhân tới giới hạn cấu hình; đạt giới hạn sẽ báo lỗi thay vì âm thầm bỏ qua tình trạng.
- [Hurtbox2D](../Assets/Components/Scripts/Runtime/Player/Hurtbox2D.cs) cache `Health` hoặc `MonsterHealth` ở parent. Một receiver có nhiều collider vẫn chỉ bị đánh một lần trong mỗi pulse.
- `WeaponHitbox` giữ các method bridge tương thích nhưng không còn trigger gây damage riêng. Hai field `damage` và `targetTag` đã bỏ vì không còn được sử dụng; hiện `SampleScene` không gắn component này.
- Basic attack dùng chung `CombatHitbox2D`; timer và Animation Event đi qua cùng one-shot guard trong combat.

### Skill

- [SkillExecution](../Assets/Components/Scripts/Runtime/Player/SkillExecution.cs) dùng cho cả hai skill. Pulse được dựng lúc khởi tạo, không tạo mảng overlap mới trong mỗi đòn.
- Skill 1 có năm thời điểm mặc định `.28, .39, .50, .61, .72`; Skill 2 có bốn thời điểm `.33, .443, .547, .66`. Timing Skill 2 được đặt riêng theo các đổi frame trong clip.
- Scene serialize bốn lượng sát thương Skill 2 là `[5, 5, 5, 25]`. Không đổi các giá trị đang lưu khi chuyển cấu hình sang `SkillDamageController`.
- File animation Player hiện có `m_Events: []`; vì vậy trong scene này đường timer là đường chạy. Các bridge vẫn còn để tương thích khi clip/prefab có Animation Event.

### Player, Health và Monster

- `PlayerController` gọi lần lượt đọc input, cập nhật state motor, dash, combat, jump và animation. `PlayerMotor2D.FixedTick` là nơi cập nhật movement vật lý.
- `Health` và `MonsterHealth` không còn chứa `Animator`, `SpriteRenderer` hoặc coroutine. I-frame dùng mốc thời gian; feedback flash có timer riêng.
- `MonsterController` giữ state Patrol/Chase/Attack/Hurt/Dead. `MonsterMotor2D` xử lý Rigidbody2D; `MonsterCombat` xử lý cooldown và impact. Vùng impact kiểm tra quanh `attackPoint` trong `attackHitboxRadius`; khoảng cách `attackRange` quyết định khi AI bắt đầu attack.
- `HealthBarUI` không bị thay đổi.

## Migration `SampleScene`

- Đã thêm `Hurtbox2D`, `CombatHitbox2D`, `PlayerInputReader`, `PlayerMotor2D`, `PlayerCombat`, `CharacterAnimationDriver`, `HitFeedback`, `MonsterMotor2D` và `MonsterCombat` vào đúng root object.
- Các reference từ `PlayerController`, `Health`, `MonsterController`, `MonsterHealth`, skill clip/hitbox, ground check/layer, attack point, health bar sprite và player target được rà trong YAML. `playerTarget` trỏ đến Player root; `SampleScene` có tag `Player`.
- Player config được chuyển sang motor/combat; `PlayerController` giữ health và các reference điều phối. GUID các script component cũ được giữ nguyên. GUID script mới có `.meta`.
- `SampleScene` là scene duy nhất được liệt kê trong `ProjectSettings/EditorBuildSettings.asset` và cũng là scene được ghi nhận trong Editor log.

## Kiểm tra đã chạy

1. **Compile phạm vi gameplay:** Unity C# compiler `3.7.0` đi kèm Unity `6000.3.18f1`, chạy bằng Mono và tham chiếu UnityEngine assemblies cùng `netstandard` facade. Compile 19 script trong `Assets/Components/Scripts/Runtime/Player` và `Assets/Components/Scripts/Runtime/Monster` thành `C:\Users\OS\AppData\Local\Temp\BTLGameplayAudit.dll`: **0 lỗi, 8 cảnh báo `CS0649`**. Các cảnh báo này là các field được gán qua Unity serialization nên compiler standalone không nhìn thấy; các reference tương ứng trong `SampleScene` đã được kiểm tra. Không còn cảnh báo `CS0414` về field damage/tag bỏ lại trong `WeaponHitbox`.
2. **Unity Editor log:** log ghi `Tundra build success`, thời gian script compilation khoảng 2.08 giây và `Loaded scene 'Assets/Objects/World/Scenes/SampleScene.unity'`. Đây là lượt Editor trước các chỉnh sửa cuối ở Monster range và buffer hủy input; trạng thái mã cuối được compile lại bằng compiler Unity ở mục 1.
3. **Serialization tĩnh:** 69 YAML object ID của `SampleScene` không trùng nhau; không có GUID trùng trong meta dưới `Assets`; tất cả `.cs` trong `Assets/Components/Scripts/Runtime` có `.meta`; các GUID script/package của scene resolve được qua `Assets` hoặc `Library/PackageCache`.
4. **Tìm đường query cũ:** không tìm thấy `OverlapCircleAll`, `OverlapBoxAll`, `OnTriggerEnter2D` hoặc `StartCoroutine` trong script Player/Monster.
5. **Ownership vật lý:** không tìm thấy `Rigidbody2D`, `linearVelocity`, `MovePosition` hoặc `AddForce` trong `MonsterController`; ghi vật lý nằm trong `MonsterMotor2D`.

Compiler standalone báo các field `[SerializeField]` như `groundCheck`, clip và `attackPoint` là chưa gán trong mã C#. Đây là giới hạn của compile ngoài Unity; YAML scene có các reference tương ứng. Một cảnh báo khác về `playerTarget` được xử lý bằng reference đã gán trong scene và fallback tìm object tag `Player`.

## Chưa xác nhận và blocker

- **Không có baseline hiệu năng dùng để so sánh.** Capture trước được nhắc đến trong kế hoạch không có tệp/raw values trong workspace; không thể tính FPS, p95/p99, GC, CPU hay tách Editor/render/wait bằng số liệu.
- **Không chạy Play Mode hoặc build.** CUA trả danh sách app trống, dù Unity process đang mở; không có cửa sổ Unity để điều khiển trong phiên này. Không mở thêm Editor thứ hai trên cùng project.
- Các trường hợp cần chạy trước khi nghiệm thu gameplay: nhiều collider cùng receiver; frame trễ làm nhiều pulse đến hạn; chết giữa skill; disable/enable khi đang dash hoặc skill; combo liên tiếp; dash trên không; hit kết liễu; Monster attack/Hurt/death/despawn.
- Không thêm hoặc chạy test suite. Package Unity Test Framework có trong project, nhưng không tìm thấy test source/assembly dưới `Assets`.
- Workspace không phải Git repository; không thể cung cấp diff hoặc đối chiếu chính xác mọi serialized value với commit gốc. Việc giữ config được kiểm tra từ scene hiện tại và các giá trị được chuyển trong quá trình migration.
- Log khởi động có cảnh báo Legacy Input Manager deprecated, Unity chạy elevated và Cinemachine sample `.asmref` không có assembly đích. Không có baseline Console trước refactor nên không thể đối chiếu toàn bộ warning cũ/mới; không thấy lỗi C# trong lượt compile ghi ở log.

## Việc cần làm để đóng nghiệm thu

Mở `SampleScene` trong Unity Editor, chạy các trường hợp gameplay ở trên và xem Console. Sau đó thu Profiler trong cùng workload/configuration cho bản gốc và bản refactor. Chỉ khi có capture tương đương mới kết luận mục tiêu allocation hoặc frame time.
