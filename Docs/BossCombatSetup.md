# Thiết lập boss và thanh máu

## Prefab có sẵn

- `Assets/Objects/Bosses/Golem/Golem 1.prefab`: boss C1 đang đặt trong BossC1; 1.841 HP, hồi 92 HP hai lượt; melee/đá 13, Magic/Laser 20.
- `Assets/Objects/Bosses/DemonSlime/DemonSlime (1).prefab`: chuẩn bị C2; 2.363 HP, melee 14; chưa có hitbox đòn thứ hai, field damage 21 là dự phòng. Đã bỏ thanh máu nổi trên đầu.
- `Assets/Objects/Bosses/BringerOfDeath/Bringer of Death.prefab`: chuẩn bị C3; 2.723 HP, melee 16, Spell 23; đã nối Animator, AI, collider cơ thể, hitbox cận chiến và Spell.
- `Assets/Components/UI/Gameplay/Game HUD.prefab`: có thanh máu boss ở giữa phía trên màn hình, dùng khung và thanh đỏ trong `Assets/Components/UI/Bosses`, tên boss ở giữa.

Bộ chỉ số và công thức C1–C3 đã áp dụng ngày 06/10/2026: [ChapterBalance.md](ChapterBalance.md). Các lượt đo nhịp/animation ở phần dưới là bằng chứng của các đợt trước; thông số HP/damage hiện tại lấy theo bảng trên.

## Nhịp ra chiêu của Golem

Golem xen kẽ một Melee với một chiêu tầm xa khi player đứng gần. Các chiêu gây sát thương luân phiên Ném đá → Magic → Laser; sau 6 lượt chiêu tầm xa mới dùng Shield. Boss vẫn ưu tiên Immune khi giảm qua mốc 50% và 10% HP.

Trong `GolemBossController`: `Attack Cooldown` mặc định 0,9 giây, tính giữa hai thời điểm bắt đầu tấn công; boss luôn hoàn thành động tác hiện tại trước khi bắt đầu chiêu tiếp theo. `Melee Attacks Before Special` mặc định 1; `Offensive Attacks Before Shield` mặc định 6. Thời gian niệm, nạp đạn, cảnh báo Laser và thời điểm sát thương của các animation vẫn giữ nguyên.

Lượt thử Play Mode 16 giây với prefab trước/sau: ở gần, từ 11 Melee chuyển thành 8 Melee, 3 Ném đá, 2 Magic, 2 Laser và 1 Shield; ở xa, số chiêu gây sát thương tăng từ 9 lên 12, Shield giảm từ 2 xuống 1. Báo cáo: `outputs/golem-attack-cadence-20261005/cadence-before.json` và `cadence-after.json`.

Đã đạt 52/52 kiểm tra cho nhịp chiêu, chiến đấu trong lúc hồi máu, giữ đủ phần hồi của hai mốc khi kích hoạt liên tiếp, animation, bất tử, HUD và chết. Unity biên dịch thành công, không có lỗi gameplay mới. Bảy scene đã lưu và nội dung scene Level 2 đang sửa giữ nguyên; Play Mode đã dừng. Chưa chạy player build.

## Immune và Melee của Golem

`Immune` và `Golem_immune` dùng `Golem_immune.anim`: Golem co lại và giữ tư thế này trong 2 giây bất tử. Khi máu giảm qua mốc 50% hoặc 10%, mỗi mốc kích hoạt một lần. Trong thời gian này, cả đòn thường và skill đều không làm mất máu boss. Sau đó boss tiếp tục chiến đấu và hồi 5% máu tối đa trong nền theo thiết lập hồi máu hiện tại (mặc định 5 giây). Với 1.800 HP, mỗi lần hồi 90 HP; hai mốc tương ứng 900 HP và 180 HP. Nếu mốc tiếp theo kích hoạt khi đang hồi, phần máu chưa hồi được giữ lại và hồi tiếp sau Immune mới.

`Melee` và `Golem_melee` dùng `Golem_Melee.anim`, clip vung tay 7 frame trước đây có tên `Golem_.anim`. Clip đã được đổi tên qua Unity để giữ GUID và các tham chiếu. Khi player ở gần, Golem dùng Melee, gây 30 sát thương một lần ở giây 0,625 và kết thúc động tác ở giây 0,875. Các thông số nằm trong phần Melee của `GolemBossController`. Cả hai clip đều tắt Loop: Immune giữ frame cuối đến hết 2 giây; Melee đánh một lượt rồi controller đưa boss về Idle.

Đã đạt 32/32 kiểm tra Play Mode cho đòn Melee, cả hai mốc máu, bất tử 2 giây, hồi đúng 90 HP mỗi mốc, Pause, HUD và chết. Báo cáo cuối: `outputs/golem-immune-melee-20261005/playmode-report.json`.

## Golem khi chết

Golem tắt chiến đấu, chạy Death và rơi xuống sàn bằng trọng lực. Collider vẫn bật trong lúc rơi để chạm Ground; xác đi xuyên qua player và không gây sát thương. Va vào cạnh tường không tính là tiếp đất. Khi chạm sàn, xác dừng physics và được căn đáy sprite theo mặt sàn qua các frame Death để tránh bị lơ lửng do pivot khác nhau.

Trong phần Death của `GolemBossController`: `Death Gravity Scale` mặc định 3; `Death Destroy Delay` mặc định 2 giây, tính từ lúc tiếp đất và chờ clip Death hoàn tất; `Death Fall Timeout` mặc định 10 giây nếu boss rơi ra ngoài map không chạm được Ground. `Ground Layers` dùng chung với phần Appear, mặc định layer 6.

Thay đổi này đã biên dịch và đạt 27/27 kiểm tra Play Mode, gồm rơi từ trên cao, chết khi đã đứng trên sàn, va cạnh tường, Pause, nằm đúng mặt đất qua các frame Death, ẩn HUD và xóa xác. Báo cáo: `outputs/golem-death-fall-20261005/playmode-report.json`.

## Đặt boss vào map

1. Kéo prefab boss vào vị trí sàn trong scene gameplay. Bringer dùng pivot chân, scale mặc định 3.
2. Chọn component `BossEncounter`, sửa `Boss Name`, `Arena Size` và `Arena Offset`. Bật Gizmos để thấy hình chữ nhật khu vực giao chiến. Kích thước đo bằng đơn vị thế giới, không nhân theo scale của boss.
3. Scene cần Player có `Health` và shared `Game HUD`; các scene Level 1, Level 2, Level 3 và Purchasing Levels đã có HUD này. Để trống trường Player trên prefab để tự tìm player của scene.
4. Đảm bảo sàn có collider và layer Ground (layer 6). Bringer dùng Ground để dừng trước mép vực và đặt Spell đúng mặt sàn.
5. Play: vào khu vực sẽ hiện tên và máu boss; ra ngoài khu vực hoặc hạ boss sẽ ẩn thanh máu. Bringer bắt đầu chiến đấu khi player còn sống trong khu vực.

Chưa chọn vị trí spawn cho boss trong bốn map hiện tại; prefab đã hoàn chỉnh và được thử trong scene riêng.

## Chỉ số Bringer of Death

| Chỉ số | Mặc định | Component chỉnh |
| --- | ---: | --- |
| Máu tối đa | 2.200 | MonsterHealth |
| Tốc độ đuổi | 2,6 | MonsterMotor2D |
| Tầm bắt đầu cận chiến | 3,7 | BringerOfDeathController |
| Sát thương cận chiến | 40 | MonsterCombat |
| Hồi cận chiến | 1,8 giây | MonsterCombat |
| Thời điểm chạm đòn cận chiến | 0,55 giây | MonsterCombat |
| Tầm Cast | 10 | BringerOfDeathController |
| Sát thương Spell | 35 | BringerOfDeathController |
| Hồi Cast | 4,5 giây | BringerOfDeathController |
| Chờ Cast đầu tiên | 1,5 giây | BringerOfDeathController |
| Chiêu Spell tồn tại | 1,6 giây | BringerSpell2D trên Spell prefab |
| Khoảng Spell gây sát thương | 0,5–1,3 giây | BringerSpell2D |
| Xóa boss sau khi chết | 1,2 giây | BringerOfDeathController |

Các clip dùng 10 frame/giây: Idle/Walk 0,8 giây, Attack/Death 1 giây, Cast 0,9 giây, Hurt 0,3 giây, Spell 1,6 giây. Nếu đổi tốc độ clip, chỉnh thời gian tương ứng ở controller, MonsterCombat hoặc Spell để giữ đúng thời điểm sát thương.

## Animator và chiêu thức

Animator chính nối Idle ↔ Walk qua Bool `IsMoving`. Trigger `Attack`, `Cast`, `Hurt` vào các trạng thái tương ứng rồi tự quay về Idle. Bool `Dead` ưu tiên chuyển vào Death và không quay lại.

Attack dừng di chuyển, bật hitbox phía trước và kiểm tra trúng đúng thời điểm vung lưỡi hái. Mỗi lượt đánh chỉ gây sát thương một lần, kèm knockback theo hệ thống combat hiện tại.

Cast khóa vị trí player khi bắt đầu niệm. Sau khoảng 0,5 giây, boss tạo một Spell tại vị trí đã khóa; Spell có Animator và BoxCollider2D riêng. Những frame đầu báo trước, chưa gây sát thương. Khi hitbox bật, mỗi player nhận tối đa một lần sát thương từ Spell đó. Người chơi có thể rời vị trí đã khóa để né. Pause dừng cả clip và thời gian sát thương.

Boss có phản ứng Hurt khi đứng hoặc đuổi. Attack/Cast vẫn tiếp tục khi nhận thêm đòn để tránh bị khóa liên tục; hiệu ứng trúng đòn vẫn hoạt động. Khi chết, boss tắt hitbox, hủy Spell, ẩn HUD, chạy Death rồi xóa sau khi trình diễn xong. Khi rời khu vực, hành động đang thực hiện bị hủy và boss về Idle; Spell đã tạo tiếp tục cho đến khi hết thời gian hoặc boss chết/bị tắt.

## Kết quả kiểm tra

Unity Editor biên dịch thành công. Kịch bản Play Mode có 30/30 kiểm tra đạt cho HUD ba boss, di chuyển, Attack, Hurt, Cast gọi Spell, né Spell, Pause, sát thương một lần, chết và thưởng SpiritStone. Prefab không có script thiếu và không phát sinh lỗi runtime mới. Bốn scene sản xuất không bị sửa trong quá trình thử. Chưa chạy player build.

Ảnh và báo cáo: `outputs/boss-system-20261005`. Báo cáo cuối: `playmode-report.json`. Bản đầu có một kiểm tra thưởng thất bại do scene thử chưa khởi tạo level trong RunResources; bản cuối khởi tạo đúng như LevelResourceFlow và kiểm tra thưởng đạt, không sửa code thưởng sản xuất.
