# Settings âm thanh theo nhóm — 10/10/2026

Settings có bốn slider: Music Volume, Ambience, Action Sounds và Combat Sounds. Đã bỏ SFX tổng và gộp UI Sounds vào Ambience theo yêu cầu.

Khung Settings, bốn ô slider và hai nút dùng `ItemCellFrame` (`32x32-Tilesheet_0`) theo mẫu ô cell nhỏ của Shop. Track/tay kéo của slider vẫn dùng sprite cũ.

| Nhóm | Âm thanh hiện được điều khiển |
|---|---|
| Ambience | Nền hang động/bản đồ; click, mở/đóng menu, thông báo nhận đồ/phước lành, nhặt chìa khóa, chuyển màn/restart và báo thắng boss |
| Action Sounds | Bước chân, nhảy, dash, va chạm khi rơi, mở rương và mở cổng |
| Combat Sounds | Đánh thường/đánh trên không, kỹ năng, nhận đòn/chết/hồi sinh, hồi máu, bật/đỡ/vỡ khiên, âm quái/boss và cảnh báo nhịp tim khi HP thấp |

```text
âm lượng hiệu ứng = âm lượng nhóm × gain của cue × mix trạng thái × fade
```

Ambience bằng 0 tắt cả nền môi trường lẫn âm giao diện. Action/Combat/Music điều chỉnh độc lập. Mức SFX tổng và UI riêng đã lưu trước đây không còn tham gia tính âm lượng. Vì bỏ hệ số tổng, các hiệu ứng có thể nghe lớn hơn nếu trước đây SFX tổng thấp hơn 100%; chỉnh trực tiếp từng nhóm để chọn mức mong muốn.

`GameAudioManager.GetCueCategory` phân loại cue. Environment source và UI source cùng dùng Ambience; heartbeat source dùng Combat. UI source vẫn phát được khi menu pause, còn nền môi trường vẫn giảm theo trạng thái pause như trước. Khi thay file trong catalog, nhóm của cue giữ nguyên.

`GameplayPanelController` đăng ký Music và ba slider hiệu ứng, hiển thị phần trăm, cập nhật khi mở Settings và gỡ listener khi bị destroy. Ba mức hiệu ứng giữ các khóa PlayerPrefs `BTL.Audio.AmbienceVolume`, `BTL.Audio.ActionVolume`, `BTL.Audio.CombatVolume`, mặc định 100%. Music giữ khóa đã có.

`GameplayPauseAudioUiSetup.InstallSfxCategoryControls` xóa hai ô cũ, thu gọn panel và nối navigation bàn phím qua bốn slider. Menu Tools/BTL/Install SFX Category Controls có thể dùng lại cho shared prefab. Installer đầy đủ cũng gọi bước này.

Không thêm sound mới trong lần chỉnh này. Unity compilation và reference của prefab được kiểm tra; chưa chạy test tự động hoặc nghe thử trong Play Mode.
