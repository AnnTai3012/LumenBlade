# Đồng bộ âm thanh với animation — 10/10/2026

## Những phần đã chỉnh

| Hành động | Đoạn âm được dùng | Thời lượng mục tiêu | Cách xử lý |
|---|---:|---:|---|
| Dash | 0–0,6 giây đầu của `Dash.ogg` (file gốc 3,24 giây) | Theo `dashClip.length`, hiện 0,6 giây | Pitch gần 1; fade cuối 0,08 giây; dừng khi dash kết thúc hoặc bị hủy |
| Đánh thường | 0–0,36 giây đầu của `Atttack1.ogg` | Theo cửa sổ đòn 1/đòn 2, hiện 0,2/0,6 giây | Pitch lần lượt 1,8/0,6; mỗi đòn thay voice cũ |
| Đánh trên không | 0–0,75 giây đầu của `Jumpattack.ogg` | Theo `jumpAttackClip.length`, hiện 0,75 giây | Pitch gần 1; phần giữ tư thế cuối đòn không kéo dài tiếng vung vũ khí |
| Bật khiên | `UseShield.ogg`, 1,0115 giây | Một vòng đầu của `Shiled`, hiện 1,0167 giây | Pitch khoảng 0,995; phát sau khi kích hoạt khiên và khởi động hình ảnh |
| Khiên đỡ đòn | `Demo/Shield Hit.wav`, 0,18 giây | Âm va chạm ngắn | Chỉ phát nếu có sát thương được hấp thụ và khiên vẫn còn HP |
| Vỡ khiên | `Demo/Shield crack.mp3`, 1,056 giây | Animation `Break`, hiện 0,6833 giây | Pitch khoảng 1,545; dừng tiếng bật/đỡ đòn trước khi phát tiếng vỡ |

Các thông số của kỹ năng 1–3 vẫn dùng `SkillSoundTiming` đã có. Thay đổi này tập trung vào các hành động trong bảng; các cue còn lại giữ cách gọi hiện tại.

## Luồng xử lý và trách nhiệm code

1. `PlayerMotor2D.StartDash` gọi `PlayCueForDuration(PlayerDash, dashDuration)` đúng lúc dash được chấp nhận. Nhấn dash khi không đủ stamina hoặc đang cooldown không phát sound. `EndDash` và nhánh hủy dash gọi `StopCue`.
2. `PlayerCombat.StartAttackWindow` truyền thời lượng từng đòn. `CancelActions` dừng sound đánh thường, đánh trên không và kỹ năng để tiếng của hành động cũ không tiếp tục khi hành động bị hủy.
3. `Health.TakeDamage` tính lượng khiên hấp thụ và phát `OnShieldDamaged(absorbed, remaining)`. `Health` chỉ quản lý dữ liệu, không phát audio hoặc điều khiển Animator.
4. `PlayerConsumables` quản lý hình ảnh và audio của khiên. Khi `remaining > 0`, phát tiếng đỡ đòn. Khi khiên bị phá, `ShowShieldBreak` phát tiếng vỡ cùng lượt xử lý với `Animator.Play(Break)`.
5. `GameAudioManager` không còn phát tiếng vỡ qua subscription riêng, tránh hai nơi cùng phát một sự kiện. Khiên hết thời gian tự biến mất không phát tiếng vỡ. Chết hoặc disable component dọn sound khiên.

Tiếng khiên đỡ đòn là âm demo tự tạo, có mã tạo lại tại `outputs/audio-animation-20261010/create_shield_hit.py`.

## Công thức đồng bộ

```text
độ dài đoạn âm = min(segmentSeconds, clip.length − startSeconds)
pitch = độ dài đoạn âm / thời lượng hành động × speedMultiplier
```

`segmentSeconds = 0` dùng phần còn lại của file. Pitch giới hạn 0,01–3 theo AudioSource. Khi bật `matchAnimation`, voice còn được giới hạn bởi thời lượng hành động, nên file quá dài không tiếp tục phát sau đòn. Nếu pitch bị giới hạn hoặc đổi speedMultiplier, âm có thể kết thúc sớm hoặc bị cắt ở cuối cửa sổ; nên chọn đoạn âm phù hợp trước khi chỉnh tốc độ.

Fade điều chỉnh volume của voice trước khi dừng. Pause menu giữ AudioSource cùng mốc bắt đầu/kết thúc của envelope; khi resume, phần còn lại tiếp tục. Xử lý kết thúc/fade chạy theo frame của Unity, nên không phải cơ chế đồng bộ chính xác từng audio sample. Pitch cũng thay đổi cao độ; đây là cách điều chỉnh tốc độ thông thường của AudioSource.

## Tự điều chỉnh trong Inspector

Mở `Assets/Components/Resources/GameAudioCatalog.asset`, tìm phần `Cue Settings`, chọn cue muốn chỉnh:

- `Volume`: độ lớn tương đối, trước slider SFX của người chơi.
- `Start Seconds`: vị trí bắt đầu trong file âm thanh.
- `Segment Seconds`: độ dài đoạn âm sử dụng.
- `Match Animation`: tự tính tốc độ khi code gọi `PlayCueForDuration`.
- `Speed Multiplier`: mặc định 1; tăng để phát nhanh hơn, giảm để chậm hơn.
- `Fade In Seconds` / `Fade Out Seconds`: giảm đột ngột tại đầu/cuối âm.
- `Cooldown`, `Max Voices`, `Priority`: kiểm soát phát lặp và số âm chồng nhau.

Các tham số đoạn âm/fade áp dụng cho gameplay voice. Cue UI dùng `PlayOneShot` riêng. `Match Animation` cần được gọi qua `PlayCueForDuration`; bật checkbox không tự tìm animation cho những cue dùng `PlayCue`.

## Trạng thái bàn giao

Unity đã biên dịch thành công sau thay đổi. Catalog đã lưu bằng Unity AssetDatabase, có reference hợp lệ tới `Shield Hit.wav`. Không thay đổi scene/prefab/animation hoặc thông số gameplay. Editor giữ Level 1 ở Edit Mode. Chưa chạy test tự động hoặc nghe thử trong Play Mode cho lần chỉnh này; cần nghe trên thiết bị thực tế để tinh chỉnh cảm giác mix nếu muốn.

Các bản trước sửa nằm trong `outputs/audio-animation-20261010/before`.
