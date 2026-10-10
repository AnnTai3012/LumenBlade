# Hệ thống âm thanh — Mức 1 và Mức 2

Cập nhật 09/10/2026. Phạm vi demo boss: **Demon Slime**.

## Cách quản lý

`Assets/Components/Resources/GameAudioCatalog.asset` là nơi gán clip và chỉnh âm lượng, pitch, cooldown, số tiếng được phát chồng và độ ưu tiên cho từng sự kiện. Những giá trị enum cũ được giữ nguyên; các sự kiện mới được thêm cuối danh sách.

`GameAudioManager` tự khởi tạo một lần, tồn tại qua chuyển màn và có 25 AudioSource trên các GameObject con: Music A/B để chuyển nhạc dần, UI SFX, Environment, Low health heartbeat và 20 voice gameplay tái sử dụng. UI, phần thưởng, tiếng chuyển màn và restart không bị filter của gameplay. Music/SFX dùng hai slider đang có và lưu PlayerPrefs. SFX bao gồm UI, ambience và heartbeat.

Gameplay quyết định khi nào xảy ra sự kiện; audio chỉ đọc sức khỏe, trạng thái pause và vùng đấu boss. Break khiên dùng event `Health.OnShieldBroken`. Âm quái giảm dần từ khoảng cách 4 đến 24 đơn vị và có hướng trái/phải nhẹ; ngoài 24 đơn vị không phát. Cooldown và giới hạn voice giúp tránh chồng quá nhiều sound khi dùng kỹ năng nhiều hit.

## Mức 1

Đã nối 43 sự kiện SFX: UI/menu, đánh thường, nhảy/đánh trên không, dash, bước chân, kỹ năng 1–3, trúng đòn, chết, hồi sinh/restart, heal, shield/break, quái thường, Slime, Mimic, rương, vật phẩm, chìa khóa, phước lành, cửa, chuyển màn và Demon Slime. Các file đổi từ MP3 sang OGG được nối lại theo asset thực tế.

Demon Slime dùng `DemonSlimeAttack.ogg` cho cả hai đòn liên tiếp, `BossTakeHit.mp3` khi bị đánh, `Death demon slime.mp3` khi chết, `BossAppear.mp3` lúc người chơi vào đấu trường lần đầu và cue chiến thắng khi boss bị hạ. Golem/Bringer chưa được hoàn thiện: năm slot BossShootAttack, BossMagicAttack, BossLaserAttack, BossMeleeAttack và BossShield để trống theo phạm vi yêu cầu.

Pitch của sound Skill1–3 vẫn theo Match Animation và Speed Multiplier. Mỗi lần phát giữ pitch trên voice của chính nó, nên không thay đổi tốc độ các SFX khác.

## Mức 2

| Điều kiện | Âm thanh |
|---|---|
| Level 1/2/3 | Theme background, ambience hang; reverb tăng lần lượt 0.16/0.23/0.28 |
| Purchasing LevelsC1 | Nhạc Sanctuary riêng; ambience hang tắt dần |
| Người chơi vào vùng boss | Nhạc DemonSlime Battle; reverb đấu trường |
| Boss còn ≤35% HP | Nhạc boss tăng pitch dần đến 1.08 |
| Người chơi còn ≤25% HP | Heartbeat; giảm nhạc, low-pass đến 4200 Hz |
| Mở panel/pause | Voice gameplay giữ vị trí phát; nhạc giảm và low-pass 1900 Hz; UI tiếp tục |
| Người chơi chết | Nhạc giảm và low-pass 900 Hz; cue chết/hồi sinh vẫn rõ |
| Heal/revive/đóng panel/rời boss | Khôi phục mix theo tình trạng thật |
| Hạ boss | Cue chiến thắng một lần, chuyển về nhạc khám phá |
| Chuyển scene | Crossfade nhạc 1.25 giây; cue chuyển màn tiếp tục qua load |

Hiệu ứng sử dụng `AudioLowPassFilter` và `AudioReverbFilter` trên nguồn riêng. Đây là xử lý âm thanh runtime; dự án không cần thao tác tạo AudioMixer hoặc nối dây Inspector để sử dụng.

`AudioEnvironmentZone` là tùy chọn cho một khu vực nhỏ trong cùng scene: thêm lên Collider2D trigger, chọn Environment/Priority; vùng ưu tiên cao nhất đang có người chơi sẽ được dùng. Các profile theo scene và vùng đấu boss đã hoạt động tự động.

Nhạc boss, nhạc sanctuary, ambience, heartbeat, nhận vật phẩm, break khiên và chiến thắng là âm thanh demo được tổng hợp mới. Sound nhân vật/quái vẫn dùng thư viện người dùng cung cấp. Ba nhạc dùng Streaming; clip demo ngắn dùng DecompressOnLoad. Script tạo âm và thông số peak/RMS lưu trong `outputs/audio-levels-20261009`.

## Demo trong Editor

Mở **Assets/Objects/World/Scenes/Level BossC2.unity**, rồi Play: scene hiện có Demon Slime. Đi vào vùng đấu để nghe đổi nhạc; đánh boss xuống dưới 35% HP; thử ít máu, mở settings, heal/revive và hạ boss. Scene này chưa có trong Build Settings hiện tại, nên phần demo này chạy trực tiếp trong Editor.

Mở catalog để thay nhạc/clip, chỉnh Cue Settings và Scene Profiles. Ngưỡng ít máu, ngưỡng boss và thời gian crossfade nằm ở Dynamic Mix.

## Kiểm tra

Kết quả compile và Play Mode được ghi tại `outputs/audio-levels-20261009`. Kiểm tra dùng player và Demon Slime thật, các panel hiện có, chuyển sang scene mua đồ, restart và Level 2. Runtime chỉnh health/vị trí để tạo tình huống kiểm tra; không lưu những thay đổi đó vào scene/prefab. Các slider âm lượng được trả về giá trị người dùng đã lưu sau kiểm tra.

Unity 6000.3.18f1 biên dịch thành công. `runtime-report.json` đạt **135/135**: catalog/playback, mix theo boss/HP, filter, UI, pause/resume, mute, shield break, chết/hồi sinh, hạ boss, chuyển scene và restart. `regression-report.json` đạt **8/8**: gọi kỹ năng từ PlayerCombat thật, trigger 2D với nhiều collider người chơi, ra khỏi zone và giới hạn nguồn phát khi có 200 yêu cầu. Console không có lỗi runtime mới. Báo cáo thử đầu tiên được giữ lại, gồm ngưỡng ambience kiểm tra chưa tính theo âm lượng người dùng và giá trị gain còn lưu trên nguồn đã dừng; bản cuối đã sửa.

Chưa chạy standalone build hoặc đánh giá bằng tai người dùng. Có thể cần chỉnh lại gain theo loa/tai nghe khi trình bày bài.
