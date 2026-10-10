# Chỉ số chiến đấu và thứ tự boss C1–C3

Cập nhật 08/10/2026. Dữ liệu thiết kế hiện hành nằm ở [GDD](../GDD.md) và [Mô hình độ khó](../Mo_hinh_do_kho.md). Workbook: [bảng C1–C3](../outputs/balance-chapter3-20261008/Level_Deconstruction_Updated.xlsx).

**Thứ tự mới đã chốt:** C1 DemonSlime, C2 Golem, C3 Bringer of Death. Chỉ số target:

| Chapter | Boss đã chốt | HP thanh | Hồi | Tổng HP phải gây | Thường | Nặng | TTK phần HP ở 22,5 DPS |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | DemonSlime | 2025 | Không | 2025 | 13 | 20 dự phòng | 90,00 s |
| 2 | Golem | 2149 | 107 × 2 | 2363 | 14 | 21 | 105,02 s |
| 3 | Bringer of Death | 2723 | Không | 2723 | 16 | 23 | 121,02 s |

**Snapshot trên đĩa 08/10:** Level 1/2/3 thuộc C1, có 7/15/33 quái; BossC1 đang đặt Bringer. Prefab Demon/Golem vẫn giữ bộ chỉ số theo thứ tự trước, chưa được áp bảng target mới. Scene C2/C3 vẫn chưa có gameplay đầy đủ. Kho khởi chạy Level 1 là 5 Heal/5 Shield/2 Cross; giá hiện hành 6/5/30. Các báo cáo Play Mode ở phần lịch sử mô tả trạng thái cũ, không chứng minh bộ bố trí mới.

Lần cập nhật này sửa GDD, mô hình độ khó và sheet, giữ nguyên code/scene/prefab. Trước khi triển khai boss mới cần cập nhật prefab/scene, ChapterIndex và chạy lại combat validation tương ứng.

## Lịch sử triển khai đến 07/10/2026

Phần dưới được giữ để truy vết các lần cân/test trước. Thứ tự boss, số quái Level 3 và target workbook trong phần này đã được thay thế bởi snapshot và tài liệu hiện hành phía trên.

﻿# Chỉ số chiến đấu chap 1–3

Ngày cập nhật: 07/10/2026. Chúc Phúc HP, dung lượng stamina và tấn công hiện cộng 5% chỉ số gốc mỗi lần, tối đa +100% sau 20 lần chọn **từng loại**. Skill 3 dùng U để đánh xa. Bốn quái khởi đầu theo thứ tự **slime xanh < dơi < chuột < Mimic**, dùng sức Player chưa có Chúc Phúc làm nền. Công thức runtime nằm trong `Assets/Components/Scripts/Runtime/Monster/ChapterBalance.cs`; hướng dẫn skill mới ở `Docs/Skill3AndBlessingCaps.md`.

`Level 1`, `Level 2`, `Level 3`, `Purchasing LevelsC1` và `Level BossC1` hiện **đều thuộc chap 1**. Số Level không phải số Chapter. Scene BossC1 đang đặt Golem, vì vậy thứ tự mặc định là **Golem C1 → DemonSlime C2 → Bringer of Death C3**. Chỉ số prefab đã được chuẩn bị cho thứ tự này; không tạo hoặc di chuyển boss trong scene.

## Công thức và quy tắc làm tròn

- `HP nền player(c) = 100 + 10 × (c−1)`.
- `HP tối đa player(c) = HP nền × (1 + 0,05 × số Chúc Phúc HP)`, với số lần tối đa 20.
- `HP thiết kế player(c) = HP nền × 1,30` vẫn là mốc cân damage quái/boss cũ; không phải trần HP hiện tại.
- `Stamina gốc(c) = 100 + 15 × (c−1)`; stamina tối đa = stamina gốc × `(1 + 0,05 × số Chúc Phúc stamina)`, tối đa 20 lần.
- Combo runtime có **hai hit**, mỗi hit 12 damage; cửa sổ hit 1 là 0,20 giây, clip hit 2 dài khoảng 0,60 giây. DPS lý thuyết tham chiếu `24/0,80 = 30`; DPS thiết kế `30 × 0,75 = 22,5`. Hiệu quả 75% vẫn là giả định cần đo; animation transition, input và né có thể làm DPS thực thấp hơn.
- `ChapterScale(c) = 1 + 0,05 × (c−1)`.
- Bốn quái khởi đầu: `HP = round(12 × số hit J nền × ChapterScale)`, với số hit nền 2 / 3 / 4 / 6. `PlayerReferenceBasicDamage = 12` khớp damage J hiện tại.
- Damage bốn quái khởi đầu: `round(HP nền player × 4% / 5% / 7% / 10%)`, không lấy HP đã có đủ Chúc Phúc.
- Quái mở rộng: `HP = round(22,5 × TTK archetype × ChapterScale)` và `damage = round(HP thiết kế player / số hit tới KO)` theo bảng riêng hiện có.
- `Ngân sách HP boss = round(22,5 × [90 + 10 × (c−1)] × ChapterScale)`.
- `Damage boss thường = round(HP thiết kế × 0,10)`; đòn nặng `round(HP thiết kế × 0,15)`.
- HP và damage runtime là số nguyên, làm tròn gần nhất và `.5` lên trên. Damage được làm tròn trực tiếp từ công thức, không làm tròn qua một bảng số thập phân trung gian.

HP boss theo công thức là 2.025 / 2.363 / 2.723. TTK của phần HP tại DPS tham chiếu lần lượt khoảng 90 / 105 / 121 giây, trước miễn nhiễm, khiên và thời gian đọc đòn. Không xem các số này là thời gian đã chơi thử.

## Player

| Chỉ số | C1 | C2 | C3 |
|---|---:|---:|---:|
| HP nền | 100 | 110 | 120 |
| HP tối đa với +100% Chúc Phúc | 200 | 220 | 240 |
| Stamina gốc | 100 | 115 | 130 |
| Stamina tối đa với +100% Chúc Phúc | 200 | 230 | 260 |
| Dash từ thanh gốc, 20 STM/lần, trước khi hồi | 5 | 5 | 6 |
| K từ thanh gốc, 40 STM/lần, trước khi hồi | 2 | 2 | 3 |
| I / Skill 2 từ thanh gốc, 67 STM/lần, trước khi hồi | Chưa mở | Chưa mở | 1 |
| U / Skill 3 từ thanh gốc, chi phí stamina gốc/6 | 6 | 6 | 6 |
| U / Skill 3 từ thanh tối đa +100% | 12 | 12 | 12 |

| Hành động/chỉ số chung | Giá trị đã áp dụng |
|---|---|
| J trên đất | 12 damage/hit, combo hai hit = 24 |
| Jump attack | 18 damage |
| K / Skill 1 | 5 pulse × 8 = 40 damage/mục tiêu; 40 STM |
| I / Skill 2 | 5 pulse × 6 = 30 damage/mục tiêu; 67 STM; mở từ C3 |
| U / Skill 3 | 24 damage/mục tiêu/lượt; luồng chém theo collider đi xa khoảng 7,2 world unit; stamina gốc/6 |
| Hồi stamina | 12 STM/giây, bắt đầu sau 0,5 giây từ lần tiêu gần nhất |
| Chúc Phúc stamina | Tăng dung lượng 5% stamina gốc/lần, tối đa +100%; tốc độ hồi giữ 12 STM/giây |
| Chạy ngang | 4,5 world unit/giây |

Thời gian skill, jump, dash, cửa sổ pulse, hitbox và i-frame giữ theo clip/controller hiện có. Dash hiện dùng clip khoảng 0,60 giây và tốc độ prefab 10, không phải mốc 0,25 giây của tài liệu prototype. K dài khoảng 1,017 giây; L dài khoảng 1,767 giây và có miễn nhiễm trong hành động. Đây là các điểm phải đo khi playtest; không thay animation hoặc quãng nhảy trong đợt cân chỉ số này.

Chúc Phúc tấn công tăng 5%/lần, tối đa +100% sau 20 lần, áp dụng ở `CombatHitbox2D`. Ở trần: J 12 → 24, jump 18 → 36, tổng K 40 → 80, tổng I 30 → 60 và U 24 → 48 nếu mọi pulse trúng. Damage từng pulse vẫn làm tròn lên; ở các mức giữa, hiệu quả tăng có thể nhỉnh hơn tỷ lệ do làm tròn riêng.

Skill 3 dùng chín frame của `skill3.png` trong clip 0,75 giây, chờ Animator kết thúc trước khi bắt đầu đòn tiếp theo. Collider quét theo luồng chém, mỗi mục tiêu nhận một lần, không gây damage qua terrain. Stamina ngừng hồi trong lúc dùng U và hồi lại sau độ trễ 0,5 giây khi nghỉ. Chi phí theo stamina gốc, không theo thanh đã được buff, nên tăng dung lượng cho phép dùng nhiều lần hơn.

`RunResources.RestoreArrival` lấy lại HP nền theo chapter rồi áp Chúc Phúc đúng một lần. Chuyển chapter đặt lại các stack như luật hiện có; đổi map trong cùng chapter giữ HP/stamina và stack. Khởi chạy trực tiếp map C2/C3 với `LevelResourceFlow.ChapterIndex` đúng sẽ có HP và stamina đầy theo chapter.

## Quái thường

Mỗi ô chỉ số ghi **HP / damage một hit**. Bộ khởi đầu lấy Player C1 có 100 HP, J gây 12 damage/hit và K gây tổng 40 damage làm chuẩn. Trước chỉnh sửa, Mimic có 158 HP / 22 damage và Rat có ít HP hơn cả Slime; bảng mới giảm sức bốn quái và đặt đúng thứ tự đã yêu cầu.

| Quái khởi đầu | C1 | C2 | C3 | Hit J để hạ ở C1 | Hit quái để hạ Player 100 HP | Prefab |
|---|---:|---:|---:|---:|---:|---|
| Slime xanh lá | 24 / 4 | 25 / 4 | 26 / 5 | 2 | 25 | `Slime.prefab` |
| Dơi | 36 / 5 | 38 / 6 | 40 / 6 | 3 | 20 | `Bat.prefab` |
| Chuột | 48 / 7 | 50 / 8 | 53 / 8 | 4 | 15 | `Rat.prefab` |
| Minic/Mimic | 72 / 10 | 76 / 11 | 79 / 12 | 6 | 10 | `Minic.prefab` |

Cột hit là phép tính với mọi đòn trúng, chưa tính né, khiên, heal, hồi sinh hoặc Chúc Phúc. K trúng đủ năm pulse có thể hạ Slime/Dơi; ở C1, Chuột còn 8 HP và Mimic còn 32 HP. Bốn archetype giữ thứ tự tăng HP và damage từ C1 tới C5. Các con số chưa phải thời gian clear đo với người chơi thật.

| Quái mở rộng hiện có | TP tham chiếu | TTK nền | Hit tới KO ở HP thiết kế | C1 | C2 | C3 |
|---|---:|---:|---:|---:|---:|---:|
| Slime xanh dương | 1,50 | 5 giây | 8 | 113 / 16 | 118 / 18 | 124 / 20 |
| Slime đỏ | 1,80 | 6 giây | 7 | 135 / 19 | 142 / 20 | 149 / 22 |
| Siêu quái generic | 4,00 | 9 giây | 8 | 203 / 16 | 213 / 18 | 223 / 20 |

HP/damage của phần mở rộng dùng công thức riêng hiện có để người dùng cân tiếp sau. Ngân sách HP siêu quái vẫn là `round(22,5 × 9 × scale)`, độc lập với HP Slime khởi đầu mới; không tự nhân 24 HP thành một bộ siêu quái mới.

| Quái có sẵn | Tuần tra | Đuổi player | Cooldown giữa các lần bắt đầu đánh |
|---|---:|---:|---:|
| Slime xanh | 0,9 | 1,4 | 1,80 giây |
| Bat | 1,4 | 2,0 | 1,60 giây |
| Rat | 1,6 | 2,8 | 1,40 giây |
| Minic | 0,5 | 0,9 | 1,80 giây |

Tốc độ tính bằng world unit/giây. Cooldown Slime/Dơi/Chuột tăng từ 1,50/1,25/0,90 lên 1,80/1,60/1,40 giây để giảm áp lực đầu game; Mimic giữ 1,80 giây. Thời lượng clip/impact và cơ chế dormant, counter, bay, chết/rơi được giữ nguyên. Hai biến thể đòn dùng cùng damage theo archetype. Không tăng tốc độ/cooldown mỗi chapter; HP và damage đã tạo bước tiến triển. Các biến thể siêu quái mượn prefab Slime/Rat cũng kế thừa cooldown của prefab gốc, trong khi HP/damage vẫn theo `SuperMonster`.

`MonsterChapterStats` trên bốn prefab tự đọc `LevelResourceFlow.ChapterIndex` trong scene khi spawn, trước `MonsterHealth.Awake`. Nó khởi tạo HP đầy và damage từ công thức một lần. Cả bốn prefab có fallback C1 để thống nhất bộ quái nền trong Inspector và các scene chưa có flow. Khi có scene flow, chapter của scene luôn được ưu tiên. Các instance trong map dùng bảng mới qua prefab/công thức hiện có; không cần đặt lại quái.

Ba biến thể siêu quái đã có trước đợt cân bằng được giữ đúng vị trí/scale/sprite, với override `Archetype = SuperMonster`: `L1_Z1_SuperSlime` ở Level 1, Rat lớn ở Level 2 và `Slime (8)` ở Level 3. Cả ba map là C1 nên chúng có 203 HP / 16 damage. Không biến các instance này thành quái thường khi loại bỏ ghi đè chỉ số cũ. Khi bổ sung prefab siêu quái riêng, có thể thay hình thể sau mà giữ cùng loại chỉ số.

## Boss

| Chap | Boss | HP thanh máu | Tổng HP cần đánh nếu hồi đủ | Đòn thường | Đòn nặng |
|---|---|---:|---:|---:|---:|
| C1 | Golem | 1.841 | 2.025 | Melee/đá 13 | Magic/Laser 20 |
| C2 | DemonSlime | 2.363 | 2.363 | 14 | 21 dự phòng, chưa có hitbox đòn này |
| C3 | Bringer of Death | 2.723 | 2.723 | Melee 16 | Spell 23 |

Golem giữ hồi 5% ở hai mốc 50% và 10%. HP nền `ceil(2025 / 1,10) = 1841`; mỗi lượt hồi `round(1841 × 5%) = 92`, tổng 1.841 + 92 + 92 = 2.025. Giữ hai lượt miễn nhiễm 2 giây, mỗi lượt hồi 5 giây, Shield 50% damage nhận vào và cooldown 0,9 giây. Khiên/miễn nhiễm vẫn kéo dài trận ngoài ngân sách HP; không gọi 2.025 là EHP cuối cùng có tính Shield.

Các prefab Golem, DemonSlime và Bringer giữ AI, animation và khả năng hiện có. DemonSlime đang tắt `Use Secondary Attack`, chưa có collider/point cho đòn thứ hai; 21 damage chỉ được lưu trong field dự phòng. Yêu cầu variant 2 sẽ dùng lại đòn thường 14 theo logic hiện có. Không thêm phase mới chỉ để khớp mô hình hai phase. Scene BossC1 vẫn giữ Golem người dùng đã đặt. BossC2/C3 còn cần dựng map/đặt boss và cấu hình chapter khi làm các chapter sau; đợt này chỉ chuẩn bị chỉ số prefab.

## Quái chưa có prefab: ghi lại để thêm sau

| Loại | C1 HP/damage | C2 HP/damage | C3 HP/damage | Tốc độ tuần tra/đuổi đề xuất | Cooldown đề xuất | Mở từ |
|---|---:|---:|---:|---|---:|---|
| Slime xanh dương | 113 / 16 | 118 / 18 | **124 / 20** | 0,7 / 1,25 | 1,60 giây | C3; cột C1/C2 chỉ là tham chiếu |
| Siêu quái generic | 203 / 16 | 213 / 18 | 223 / 20 | 0,8 / 1,3 | 1,80 giây | C1 |
| Slime đỏ | 135 / 19 | 142 / 20 | 149 / 22 | 0,8 / 1,6 | 1,45 giây | C4; không đặt trong C1–C3 |

Slime đỏ mở ở C4 có mốc 155 HP / 24 damage theo cùng công thức. Tốc độ/cooldown của các prefab chưa có là đề xuất chưa được thử animation. Quái mới cần tín hiệu báo đòn tối thiểu khoảng 0,4–0,6 giây; siêu quái nên khoảng 0,6–0,8 giây và chỉ một con gây áp lực chính cùng lúc.

Khi tạo prefab mới, nối `MonsterHealth`, `MonsterCombat`, `MonsterMotor2D`, `MonsterController`, Hurtbox/collider/animation như prefab đang có. Thêm `MonsterChapterStats`, chọn `BlueSlime`, `RedSlime` hoặc `SuperMonster`, bật `Use Scene Chapter` và đặt fallback tương ứng. Cấu hình `LevelResourceFlow.ChapterIndex` cho map. Không cần sao chép công thức vào từng prefab hoặc nhân damage siêu quái thủ công.

## Công thức difficulty ở cấp map

Tiếp tục dùng `D = 0,35C + 0,15M + 0,10K + 0,15S + 0,15R + 0,10T` của mô hình mới. Phần S tính từ TTK và damage so với EHP; phần C tính TP theo zone và số attacker đồng thời. Target boss C1/C2/C3 là 51,2 / 53,8 / 56,4 điểm trong workbook, **không phải điểm đo được của scene hiện tại**.

Chỉ số đơn vị không thể tự bảo đảm cả map đạt target D. Đợt này không thay số lượng quái, cửa, tilemap, bẫy, đường đi hoặc phần thưởng. Level 1/2/3 hiện có 7/15/37 địch, trong đó mỗi map có một siêu quái (quái thường là 6/14/36). Muốn áp quota 25+ quái hoặc mở đúng chủng loại cần một đợt bố trí map riêng.

## Kiểm tra bộ quái khởi đầu mới

Bản trước, baseline, script áp dụng prefab và script kiểm tra nằm trong `outputs/starter-monsters-20261006`. Kiểm tra bao gồm compile Unity, HP/damage trong prefab và runtime C1–C3, sát thương melee/đòn thứ hai, cooldown, số hit J thực qua hurtbox và năm pulse K. Bộ fixture đầu đã được sửa để hỗ trợ Slime dùng attack point thay vì giả định mọi quái đều có attack collider; không sửa gameplay để làm fixture đạt. Kết quả và phạm vi chính xác được lưu trong `assets-report.json`, `runtime-report.json` và `scene-runtime-report.json` của thư mục này. Chưa chạy player build hoặc full playtest độ khó với người chơi thật.

Unity 6000.3.18f1 biên dịch thành công. 30 kiểm tra prefab/công thức, 109 kiểm tra combat Play Mode C1–C3 và 122 kiểm tra 59 quái trên ba map thật đều đạt. Hai đòn melee/fallback gây đúng damage, cooldown chặn spam và cho phép đánh lại sau thời gian cấu hình, số hit J và hiệu quả K đúng bảng. Các scene vẫn giữ 7/15/37 quái và mỗi map một archetype SuperMonster. Console cuối có 0 lỗi/0 cảnh báo. Hash xác nhận cả 7 scene, Player, HUD, settings tài nguyên và 3 prefab boss không đổi; thay đổi asset chỉ gồm 4 prefab quái khởi đầu và nguồn ChapterBalance.

## Lịch sử kiểm tra trước khi giảm bộ quái khởi đầu

- Unity 6000.3.18f1 đã compile các script thay đổi.
- 77 kiểm tra prefab/scene đạt: chỉ số, đủ component, không missing script, đúng chapter hiện tại, không giữ ghi đè chỉ số cũ của player và giữ đủ ba biến thể siêu quái.
- 82 kiểm tra Play Mode đạt cho C1–C3: HP đầy khi spawn, melee thực nhận của bốn loại, guard một hit, damage qua hitbox chung, cost/capacity stamina, L mở từ C3, Chúc Phúc và pause/recovery.
- 27 kiểm tra boss Play Mode đạt: Golem thật ở BossC1 kích hoạt/đánh/hồi đúng hai lần 92 HP, miễn nhiễm/pause, đá/Magic/Laser gây damage, DemonSlime một đòn và Bringer melee/spell. Kết quả tại `outputs/chapter-balance-20261006/boss-runtime-validation.json`.
- 65 kiểm tra Play Mode của ba scene thật đạt: 59 enemy instance có đúng HP đầy/damage C1 và mỗi map còn đúng một siêu quái. Kết quả tại `outputs/chapter-balance-20261006/scene-runtime-validation.json`.
- Scene và prefab được sửa qua Unity Editor API. Bản trước sửa, script áp dụng, các lượt kiểm tra và Console nằm trong `outputs/chapter-balance-20261006`.
- Lượt test boss đầu tiên giả định DemonSlime có đòn thứ hai; prefab thực tế chưa nối đòn đó. Báo cáo thất bại được giữ, expectation đã sửa để kiểm tra chính xác fallback hiện có, không bật một kỹ năng mới để làm test đạt. Lượt đồng bộ đầu cũng được sửa để bảo toàn ba biến thể siêu quái; kiểm tra cả ba scene đã đạt sau sửa.
- Lượt test scene mở cả ba map cùng lúc tạo thông báo trùng Global Light/EventSystem. Đã giữ log và chạy lại mỗi map trong một phiên Play Mode riêng: cả Level 1/2/3 đều có 0 lỗi và 0 cảnh báo Console. Chạy lại installer không đổi byte của 15 prefab/scene đã kiểm tra.
- Chưa build player và chưa đo full clear time, tỷ lệ chết, khả năng vượt mọi khe nhảy ở tốc độ chạy mới hoặc độ khó với người chơi thật. Cần playtest để chốt DPS hiệu dụng và nhịp encounter.
