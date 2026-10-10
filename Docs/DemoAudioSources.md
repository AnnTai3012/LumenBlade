# Nguồn âm thanh demo

| File hiện tại | Vai trò |
|---|---|
| `Demo/Boss Battle.mp3` | Nhạc đấu boss, dài 126,264 giây; streaming và loop qua music source |
| `Demo/Shield crack.mp3` | Tiếng vỡ khiên, dài 1,056 giây; tự tính pitch khoảng 1,545 để khớp animation Break 0,683 giây |
| `Demo/BossVictory.mp3` | Tiếng thắng boss, dài 3,048 giây; phát một lần khi boss chết |

Các file này thay cho `DemonSlime Battle.wav`, `Shield Break.wav` và `Boss Victory.wav` do script từng tạo. Mình chỉ tích hợp các file mới; chưa có thông tin về tác giả, nguồn tải hoặc giấy phép của chúng. Không mô tả ba file MP3 mới là âm thanh do mình tổng hợp.

## Âm thanh do mình tạo bằng code còn được sử dụng

Nhạc Menu mới là `Assets/Components/Audio/MenuBackGround.mp3`, do người dùng cung cấp và được tích hợp vào `GameAudioCatalog.menuMusic`, import dạng Streaming. Đây cũng không phải âm thanh tổng hợp bằng Python; nguồn tác giả/giấy phép ban đầu chưa được cung cấp.

Các file WAV trong bảng dưới được tổng hợp bằng Python rồi xuất PCM 16-bit, 44.100 Hz. Không dùng microphone, không tải từ thư viện âm thanh bên ngoài và không dùng dịch vụ AI tạo nhạc. Thư mục Demo hiện chứa cả WAV tổng hợp và MP3 do người dùng cung cấp.

| File | Cách tạo và mục đích |
|---|---|
| `Sanctuary Music.wav` | Nốt nhạc/hợp âm có envelope mềm và tiếng chuông tổng hợp, loop 24 giây; dùng cho khu nghỉ hoặc cửa hàng. |
| `Cave Ambience.wav` | Ghép sóng tần số thấp có mức âm biến thiên và các âm ngắn mô phỏng giọt nước; tạo nền hang động 8 giây. Không phải bản thu thực tế trong hang. |
| `Low Health Heartbeat.wav` | Hai xung sóng sin trầm tắt dần, mô phỏng nhịp tim; phát khi HP thấp. |
| `Item Received.wav` | Hai nốt chuông tăng cao, phản hồi nhận vật phẩm. |
| `Shield Hit.wav` | Xung trầm, hai tần số tạo tiếng ngân và một lượng noise ngắn, mô phỏng khiên đỡ đòn; dài 0,18 giây. |

Script gốc:

- `outputs/audio-levels-20261009/generate-demo-audio.py`: tạo bộ WAV demo ban đầu bằng NumPy và thư viện `wave`, gồm cả ba file cũ nay đã được thay. Không chạy lại script này để ghi đè lựa chọn nhạc hiện tại.
- `outputs/audio-animation-20261010/create_shield_hit.py`: tạo tiếng đỡ đòn bằng `math`, `random`, `struct` và `wave`.

## Logic tổng hợp dễ giải thích khi báo cáo

Một file âm thanh chứa nhiều mẫu số theo thời gian. Với tần số lấy mẫu 44.100 Hz, mỗi giây có 44.100 mẫu cho mỗi kênh.

```text
sóng cơ bản = sin(2π × tần số × thời gian)
mẫu âm = (sóng cơ bản + họa âm + noise) × envelope × gain
```

- Tần số quyết định cao độ của nốt nhạc.
- Họa âm làm âm sắc phong phú hơn một sóng sin đơn.
- Noise tạo cảm giác va chạm/gõ/vỡ.
- Envelope điều khiển âm bắt đầu, ngân và tắt dần.
- Ghép các nốt ở những thời điểm khác nhau tạo giai điệu; ghép nhiều nốt cùng lúc tạo hợp âm.
- Chuẩn hóa mức âm và fade đầu/cuối giúp hạn chế clipping và chuyển tiếp đột ngột.

Các đoạn demo phục vụ tích hợp và trình bày hệ thống audio, chưa phải soundtrack được phối khí chuyên nghiệp. Cảm giác hợp theme hiện là lựa chọn thiết kế; lần gỡ rhythm này chỉ xác nhận biên dịch và dữ liệu, chưa nghe thử trên thiết bị người chơi.

