# Scene Main Menu — 10/10/2026

Scene: `Assets/Objects/World/Scenes/Main Menu.unity`, đặt đầu tiên trong Build Settings để game build khởi động tại Menu.

## Thành phần

- **Play Game:** vào Level 1 và bắt đầu lượt chơi mới, dùng số tài nguyên mặc định đã cấu hình trong scene. Nếu có RunResources của lượt trước, owner đó được hủy để xóa checkpoint, tài nguyên và buff của lượt cũ. Các mức âm lượng đã lưu không bị reset.
- **Settings:** mở panel cùng kiểu với Settings trong game, gồm Music Volume, Ambience, Action Sounds và Combat Sounds. Back/Escape trở về panel chính.
- **Quit Game:** thoát ứng dụng; trong Unity Editor thì dừng Play Mode.
- `MenuBackGround.mp3`: nhạc nền Menu, thuộc nhóm Music; import dạng Streaming và loop qua GameAudioManager.

Settings trong gameplay đổi nhãn Quit Game thành **Menu** và gọi `ReturnToMenu`: đóng pause panel, lưu âm lượng, khôi phục Time.timeScale và chuyển scene Main Menu. Nút Quit ở Game Over giữ hành vi cũ.

## Logic code

- `MainMenuController`: các nút, mở/đóng Settings, lựa chọn bằng EventSystem và Escape để quay lại.
- `GameSceneNavigation`: đường dẫn Menu/Level 1, chặn lệnh tải scene trùng khi bấm liên tục, tải scene async và xử lý thoát ứng dụng.
- `AudioSettingsBinding`: dùng chung cho menu và GameplayPanelController, gắn listener slider, hiển thị phần trăm, cập nhật PlayerPrefs và gỡ listener khi UI bị hủy.
- `GameAudioCatalog.menuMusic` và nhánh scene Menu trong `GameAudioManager.SelectMusic`: chọn bài nhạc riêng cho Menu. Chuyển scene vẫn dùng crossfade hiện có.
- `MainMenuSceneSetup`: tạo scene bằng Unity Editor API, clone Settings Overlay từ Game HUD.prefab, dùng ItemCellFrame cho panel/nút và ghi reference qua SerializedObject.

Menu hiện là giao diện tạm với nền tối và khung cell cùng phong cách Settings. Không có chức năng Continue hoặc chọn màn; Play Game vào Level 1.

Bản trước sửa và thông tin bàn giao nằm trong `outputs/main-menu-20261010`. Không chạy test tự động trong lần triển khai này.

Unity biên dịch thành công; reference của controller, bốn slider, nhạc Menu và thứ tự Build Settings đã được đối chiếu. Đã xem panel chính và Settings trong Play Mode, lưu ảnh preview rồi trở lại Main Menu Edit Mode. Chưa chạy kiểm thử tự động cho chuỗi Play Game → gameplay → Menu hoặc standalone build.
