# Nhạc nền sau khi thắng boss

Khi audio manager ghi nhận boss đã chết:

1. Dừng cả hai music source để nhạc chiến đấu và nhạc exploration không đè lên đoạn kết trận.
2. Phát `BossVictory` một lần. Nếu đang pause hoặc player chết, giữ cue chờ tới khi có thể phát.
3. Chờ `bossBackgroundDelay`, mặc định 10 giây gameplay. Pause giữ nguyên thời gian chờ.
4. Bắt đầu `backgroundMusic` (hiện là `Theme background.wav`) bằng fade-in theo `transitionSeconds` đang có. Nhạc bắt đầu tại mốc hết 10 giây và đạt âm lượng đích sau fade-in.

Timer độc lập với GameObject boss nên vẫn còn khi corpse bị destroy. Chuyển scene/restart hoặc bắt đầu một encounter mới xóa timer/cue cũ. Không có coroutine chuyển nhạc chạy trễ sang scene khác.

Muốn chỉnh thời gian: mở `Assets/Components/Resources/GameAudioCatalog.asset` → Dynamic mix → **Boss Background Delay**. Các nhóm âm lượng và sound gameplay vẫn giữ cách xử lý hiện có.

Unity compilation được kiểm tra; chưa chạy test tự động hoặc nghe thử Play Mode cho lần chỉnh này. Bản trước sửa nằm ở `outputs/boss-background-delay-20261010/before`.
