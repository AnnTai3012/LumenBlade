# BÁO CÁO HỆ THỐNG ÂM THANH TRONG GAME 2D

## 1. Mục tiêu

Xây dựng hệ thống âm thanh phù hợp với không khí của game, phản hồi các thao tác của người chơi và thay đổi theo môi trường, scene, HP hoặc tiến trình trận đấu. Hệ thống cho phép thay file sound và điều chỉnh thông số trong catalog mà không phải sửa phần tính sát thương.

## 2. Các thành phần đã làm cho SFX

**Phần thể hiện:** Đã tích hợp nhạc nền, SFX gameplay, SFX giao diện và Settings âm thanh. Phần âm thanh boss tập trung demo Demon Slime; chưa nhận là hoàn thiện toàn bộ sound của các boss khác.

| Thành phần | Cách ứng dụng trong game |
|---|---|
| **Nhạc nền** | Có nhạc Menu, nhạc khi chơi, nhạc cửa hàng và nhạc chiến đấu boss. |
| **Âm thanh hành động** | Tiếng bước chân, nhảy, dash và va chạm khi rơi. |
| **Âm thanh chiến đấu** | Tiếng đánh thường, đánh trên không, dùng kỹ năng, nhận đòn, chết và hồi sinh. |
| **Âm thanh quái/boss** | Tiếng tấn công, nhận đòn và chết; Demon Slime có các cue riêng. |
| **Âm thanh vật phẩm và khiên** | Tiếng nhận đồ, nhặt chìa khóa, mở rương, hồi máu, bật khiên, đỡ đòn và vỡ khiên. |
| **Âm thanh giao diện** | Tiếng click, mở/đóng menu và các thông báo trong game. |
| **Settings âm thanh** | Bốn slider Music, Ambience, Action và Combat. Âm giao diện dùng chung nhóm Ambience. Điều chỉnh có hiệu lực ngay và lưu bằng `PlayerPrefs`. |
| **Quản lý tập trung** | `GameAudioCatalog` lưu clip/thông số; `GameAudioManager` quản lý phát âm, phân nhóm và tái sử dụng nguồn SFX. |

### 2.1. Công dụng các nhóm âm lượng

| Nhóm | Nội dung điều khiển |
|---|---|
| **Music Volume** | Nhạc Menu, nhạc khi chơi, cửa hàng và chiến đấu boss. |
| **Ambience** | Âm nền môi trường cùng click/menu/thông báo, nhận đồ, nhặt chìa khóa và báo thắng boss. |
| **Action Sounds** | Bước chân, nhảy, dash, va chạm khi rơi, mở rương và mở cổng. |
| **Combat Sounds** | Đánh, kỹ năng, nhận đòn, chết/hồi sinh, hồi máu, khiên, âm quái/boss và nhịp tim cảnh báo HP thấp. |

Không còn slider SFX tổng hoặc UI Sounds riêng. Mỗi hiệu ứng được tính âm lượng theo nhóm của nó:

```text
Âm lượng hiệu ứng = âm lượng nhóm × gain riêng của cue × mức mix trạng thái × fade
```

### 2.2. Menu và Settings

- **Play Game:** bắt đầu lượt chơi mới tại Level 1.
- **Settings:** dùng cùng bốn slider và giá trị đã lưu với Settings trong gameplay.
- **Quit Game:** thoát ứng dụng từ Menu; trong Unity Editor thì dừng Play Mode.
- Settings trong gameplay có **Resume Game** để tiếp tục chơi và **Menu** để chuyển về scene Main Menu.

Scene Main Menu được đặt đầu tiên trong Build Settings. Khi mở Settings trong gameplay, game tạm dừng; âm giao diện vẫn được phép phát để phản hồi thao tác.

## 3. Các thành phần đã làm cho Filter xử lý âm thanh

**Phần thể hiện:** Có filter xử lý âm thanh và âm thanh thay đổi theo **môi trường, scene, trạng thái người chơi và tiến trình gameplay**. Crossfade, pitch, fade và khoảng cách là các kỹ thuật xử lý bổ sung; không phải tất cả đều là filter.

| Thành phần                          | Cách ứng dụng trong game                                                                                                                                     |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Low-pass Filter**                 | Giảm âm cao, tạo cảm giác âm trầm/nghẹt khi HP thấp, pause hoặc chết. Cutoff mục tiêu lần lượt khoảng **4.200 / 1.900 / 900 Hz**; bình thường **22.000 Hz**. |
| **Reverb Filter**                   | Tạo tiếng vang cho SFX gameplay. Mức vang thay đổi giữa hang động, đấu trường và khu nghỉ.                                                                   |
| **Chuyển nhạc theo scene/gameplay** | Chọn nhạc riêng cho Menu, khám phá, cửa hàng; khi người chơi vào vùng boss thì chuyển sang nhạc chiến đấu.                                                   |
| **Crossfade**                       | Dùng hai AudioSource nhạc: giảm âm lượng bài cũ và tăng âm lượng bài mới để chuyển nhạc mượt.                                                                |
| **Âm thanh theo HP người chơi**     | Khi HP còn không quá ngưỡng cấu hình, mặc định **25%**, giảm nhạc nền, áp dụng low-pass và phát nhịp tim.                                                    |
| **Âm thanh theo tiến trình boss**   | Khi HP boss còn không quá ngưỡng cấu hình, mặc định **35%**, tăng pitch nhạc tới **1,08** để nhạc nhanh hơn và cao hơn.                                      |
| **Khoảng cách và hướng**            | Với cue phát theo vị trí, tiếng quái giảm dần từ khoảng cách 4 đến 24 đơn vị; lệch trái/phải theo vị trí so với người chơi.                                  |
| **Fade và đồng bộ animation**       | Sound dash, đánh và khiên được chọn đoạn âm, chỉnh tốc độ theo thời lượng hành động và có fade cuối. Dừng cue khi hành động bị hủy.                          |
| **Âm thanh sau chiến thắng**        | Dừng nhạc chiến đấu, phát BossVictory một lần; sau **10 giây gameplay** bắt đầu lại Theme background bằng fade-in. Pause tạm dừng thời gian chờ.             |

### 3.1. Filter và cách điều khiển

`GameAudioManager` tạo các component xử lý âm thanh có sẵn của Unity bằng code:

```csharp
source.gameObject.AddComponent<AudioLowPassFilter>();
source.gameObject.AddComponent<AudioReverbFilter>();
```

- `AudioLowPassFilter` được gắn trên các nguồn nhạc và nguồn SFX gameplay.
- `AudioReverbFilter` được gắn trên các nguồn SFX gameplay.
- Nguồn UI được tách riêng để tiếng click/menu không bị áp low-pass như SFX gameplay.
- Cue chết và hồi sinh của người chơi giữ cutoff 22.000 Hz để phản hồi này rõ hơn.

Mỗi frame, manager đọc scene, môi trường, HP, trạng thái boss và pause/chết. Từ dữ liệu đó, hệ thống chọn cutoff, mức reverb, volume và pitch mục tiêu. Các thông số được chuyển dần bằng `Mathf.Lerp`; tiến độ crossfade dùng `Mathf.MoveTowards`, tránh đổi âm đột ngột.

**Ví dụ:** Khi vào trận Demon Slime, nhạc chuyển sang Boss Battle và SFX dùng mức vang của đấu trường. Nếu HP người chơi thấp, âm thanh bị lọc trầm hơn và nhịp tim xuất hiện. Khi boss chết, nhạc chiến đấu dừng, tiếng thắng boss phát, rồi nhạc nền trở lại sau thời gian chờ.

### 3.2. Ví dụ đồng bộ sound và animation

- **Dash:** dùng 0,6 giây đầu của sound và thời lượng `dashClip`; fade nhẹ ở cuối, dừng khi dash kết thúc/bị hủy.
- **Đánh thường:** truyền thời lượng từng đòn để tự tính tốc độ phát đoạn âm.
- **Bật khiên:** phát sound theo vòng animation đầu tiên của khiên.
- **Đỡ đòn:** chỉ phát Shield Hit khi khiên hấp thụ sát thương và vẫn còn HP.
- **Vỡ khiên:** phát Shield Crack cùng lúc animation `Break` bắt đầu; file dài 1,056 giây được chỉnh pitch khoảng 1,545 để khớp animation khoảng 0,683 giây. Khiên tự hết thời gian không phát tiếng vỡ.

```text
Pitch = độ dài đoạn âm / thời lượng hành động × hệ số tốc độ
```

Pitch của AudioSource cũng thay đổi cao độ. Vì vậy, hệ thống kết hợp chọn đoạn âm phù hợp và fade, thay vì chỉ tăng tốc toàn bộ một file dài.

## 4. Kiến trúc và luồng xử lý code

| Thành phần code | Trách nhiệm |
|---|---|
| `GameAudioCue` | Định danh sự kiện âm thanh, ví dụ PlayerDash, ShieldBreak, BossVictory. |
| `GameAudioCatalog` | ScriptableObject lưu clip, thông số cue, profile scene, ngưỡng HP và thời gian chuyển nhạc. |
| `GameAudioManager` | Chọn nguồn phát, áp volume/filter/pitch, chuyển nhạc và quản lý pool 20 nguồn SFX gameplay. |
| `BossEncounter` | Xác định người chơi có ở trong vùng boss hay không để audio manager chọn trạng thái chiến đấu. |
| `AudioEnvironmentZone` | Hỗ trợ ghi đè môi trường âm thanh khi người chơi đi vào/ra một vùng trigger được cấu hình. |
| `AudioSettingsBinding` | Dùng chung cho Menu và gameplay: gắn slider, cập nhật phần trăm, lưu âm lượng và gỡ listener khi UI bị hủy. |
| Các script gameplay | Phát cue khi hành động xảy ra; ví dụ PlayerMotor2D cho dash, PlayerCombat cho đánh/kỹ năng, PlayerConsumables cho khiên. |

Luồng xử lý chung:

```text
Hành động/trạng thái game
        ↓
Chọn GameAudioCue hoặc trạng thái nhạc
        ↓
Lấy clip và thông số từ GameAudioCatalog
        ↓
GameAudioManager chọn AudioSource và áp xử lý âm thanh
        ↓
Người chơi nghe phản hồi phù hợp
```

Hệ thống âm thanh quan sát gameplay để thay đổi cách phát âm. Việc phát sound không quyết định sát thương hoặc thay đổi luật chiến đấu.

