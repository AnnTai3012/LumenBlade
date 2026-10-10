# Vật phẩm, SpiritStone và UI

Ngày cập nhật phần thiết lập gốc: 2026-10-04. Đối chiếu 08/10/2026: giá runtime hiện hành CryHeal/CryShield/Cross là 6/5/30; kho tối đa 5/5/2. GDD và mô hình kinh tế đến C3 đã đồng bộ tại [GDD](../GDD.md) và [Mô hình độ khó](../Mo_hinh_do_kho.md). Các ghi chú quá trình phía dưới cần đọc theo mốc ngày triển khai.

Cập nhật 2026-10-07: cả ba Chúc Phúc chỉ số tăng 5% chỉ số gốc, tối đa +100% sau 20 lần chọn từng loại. Lựa chọn số 2 là **Thể lực**, tăng dung lượng stamina. Stamina gốc C1 là 100. Skill 3 dùng U, damage gốc 24, chi phí stamina gốc/6, chờ animation hoàn thành trước đòn kế tiếp. Cấu hình mới và kết quả kiểm chứng nằm trong `Docs/Skill3AndBlessingCaps.md`; các ghi chú bên dưới mô tả quá trình thiết lập trước lần cập nhật này.

## Cách dùng

- `1`: dùng CryHeal, hồi 30% HP tối đa, làm tròn tới HP nguyên gần nhất. Máu đầy hoặc hết vật phẩm thì không trừ đá.
- `2`: dùng CryShield, hấp thụ 25% HP tối đa trong 8 giây. Khiên đang chạy thì không tiêu thêm đá. Sát thương vượt lượng khiên còn lại tiếp tục trừ HP.
- `E`: nhặt đá/chìa khóa gần nhân vật, dùng đường tương tác hiện có.
- `F`: mở cửa khi có chìa khóa; tương tác với tượng ở Purchasing Levels để mở cửa hàng.
- Cửa hàng: chọn nút mua; `Esc` hoặc nút đóng để tiếp tục chơi.
- `Esc` khi đang chơi: bật/tắt Settings và tạm dừng game. Hai thanh trượt chỉnh nhạc nền và âm thanh UI/SFX từ 0–100%, có phần trăm hiển thị. `RESUME GAME` tiếp tục chơi; `QUIT GAME` thoát ứng dụng (dừng Play Mode trong Editor).
- Khi player chết: sau animation chết sẽ hiện `GAME OVER` với `RESTART` để tải lại màn hiện tại và `QUIT GAME` để thoát. `Esc` không đóng bảng này.
- Chúc phúc: chọn một trong bốn panel bằng chuột, phím 1/2/3/4 (hoặc numpad) hay điều hướng UI rồi Enter.
- Rương: đứng gần và nhấn `F`. Animation mở xong sẽ nhận mặc định 1 CryHeal và 1 CryShield; HUD cập nhật kho ngay.

HUD góc dưới trái có hai ô liền nhau phía trên thanh HP đỏ và stamina. Dấu vàng chỉ ô được chọn khi dùng vật phẩm. Thanh HP trên HUD và trên đầu player cùng đọc `Health.OnHealthChanged`. Lớp khiên có thanh xanh và thời gian còn lại. SpiritStone và chìa khóa hiển thị ở góc trên phải; chìa khóa bắt đầu bằng 0 ở mỗi màn.

## Tài nguyên và chuyển màn

`RunResources` giữ kho SpiritStone đã tích lũy, số đá tiêu hao và chúc phúc trong cùng lần chạy game. SpiritStone do quái trả trong map được giữ ở phần chờ; mở cửa sang màn tiếp theo mới nhập vào kho đã tích lũy. Chết sẽ xóa phần chờ và sổ quái của lượt đó, để quái được thưởng lại sau khi map được tải lại. Cửa hàng chỉ tiêu SpiritStone đã tích lũy. Hệ thống vẫn giữ tài nguyên trong lần chạy hiện tại, chưa ghi save xuống đĩa.

Sau khi chết, chọn `RESTART` trong bảng `GAME OVER` để tải lại scene hiện tại về vị trí bắt đầu của map (checkpoint đầu màn). Scene được dựng lại nên quái, chìa khóa, rương và vật phẩm nhặt trên đất xuất hiện lại. Kho CryHeal/CryShield chưa dùng và Chúc Phúc được giữ; vì rương xuất hiện lại, người chơi có thể thử nhiều lần để tích trữ vật phẩm tới giới hạn 5 viên mỗi loại. Chìa khóa đang giữ bị mất khi chết.

Giá CryHeal/CryShield hiện hành: 6/5 SpiritStone. Kho tối đa 5 viên mỗi loại. Giao dịch kiểm tra số tiền và chỗ trống trước khi cùng lúc trừ tiền/cộng món. Không có SpiritStone trong rương. Thánh giá bán ở catalog đặc biệt, giá 30 và kho tối đa 2.

Cửa thoát dùng chung `Assets/Objects/Items/EntranceForward.prefab`; mỗi instance chọn Target Scene Name trong Inspector. Tuyến hiện tại là Level 1 → Level 2 → Level 3 → Purchasing Levels. Cửa Level 3 nằm ở đoạn cuối bên dưới và Bat (15) giữ chìa khóa. Sau khi cửa xác nhận chuyển màn, SpiritStone đang chờ được nhập vào kho rồi HP/stamina/khiên còn lại được chuyển sang scene đích; chìa khóa thuộc player mới nên reset. Khi đến map mua sắm, HP được hồi đầy và không hiện chúc phúc theo GDD. Bốn lựa chọn hiện ngay khi chuyển từ cửa sang màn chiến đấu; gameplay dừng đến khi lựa chọn xong.

Chúc phúc mặc định theo thứ tự ô 1–4: +5% HP nền, +5% tốc độ hồi stamina, tăng sát thương mọi đòn đánh, và nhận đồng thời một CryHeal và một CryShield. Phím 3 chọn Sức mạnh; phím 4 chọn Vật phẩm. Chúc phúc sát thương mặc định tăng 5% mỗi lần chọn, tối đa 30% trong Chapter; giá trị này sửa được trong asset cấu hình. Buff được giữ qua màn và reset khi đổi Chapter.

Sát thương được tăng tại `CombatHitbox2D`, áp dụng một lần cho đánh thường/combo, đánh trên không và từng pulse của Skill 1/Skill 2. Không thay đổi các giá trị sát thương nền trong prefab; sát thương nguyên được làm tròn lên. Sát thương của quái giữ nguyên.

Chúc phúc vật phẩm cộng một viên mỗi loại nếu loại đó còn chỗ, giữ giới hạn kho. Nếu một kho đầy, mô tả panel ghi rõ chỉ nhận được loại còn chỗ. Khi một chỉ số đã đạt trần, lựa chọn chuyển thành phần thưởng vật phẩm; nếu cả hai kho đầy thì đổi thành hồi máu ngay để tiếp tục màn chơi.

## Rương và thông báo nhận đá

`Assets/Objects/Items/Chest.prefab` dùng `TreasureChestInteractable` với animation mở có sẵn. Rương trao mỗi viên qua `RunResources.TryAddConsumable`, nên cùng dùng giới hạn kho và sự kiện HUD. Vật phẩm chỉ được nhận một lần từ lượng còn lại của rương. Nếu một kho đầy, phần chưa nhận vẫn trong rương đã mở; dùng bớt đá rồi nhấn F để lấy phần còn lại. Nếu cả hai kho đầy, rương giữ nguyên và hiện “Kho đá đã đầy”. Rương không trao SpiritStone.

`RunResources.OnConsumableReceived` chỉ phát khi tăng kho do nhận đá. `PlayerItemReceiptUI` trên Game HUD lắng nghe sự kiện này, hiện một dòng icon + “Nhận CryHeal +1” hoặc “Nhận CryShield +1” phía trên player, đi theo nhân vật/camera, nổi lên rồi mờ và biến mất sau mặc định 2.2 giây. Nhận liên tiếp cùng loại trong thời gian hiển thị sẽ cộng số lượng trên dòng đó. Hai loại có hai dòng riêng. Nhặt đá, mở rương, mua đá và chúc phúc đều dùng chung thông báo; sửa số trong Inspector không tạo thông báo nhận vật phẩm.

## Chỉnh tiếp trong Inspector

- Trong Hierarchy của mỗi scene gameplay, chọn GameObject `Run Resources`. Component `Run Resources` có ba ô sửa trực tiếp: `Spirit Stone` đã tích lũy, `Cry Heal Count`, `Cry Shield Count`.
- Sửa trước khi Play để đặt số tài nguyên ban đầu khi bắt đầu game từ scene đó. Ví dụ chơi thử cửa hàng: Spirit Stone = 100, Cry Heal Count = 2, Cry Shield Count = 2. Các scene mặc định vẫn bắt đầu bằng 0.
- Khi đang Play, `Run Resources` nằm trong mục `DontDestroyOnLoad` của Hierarchy. Sửa ba số ở đây sẽ cập nhật HUD và cửa hàng trong frame tiếp theo, kể cả khi menu đang mở. Số đá bị giới hạn theo `Capacity Per Kind` (mặc định 5); SpiritStone không nhận số âm.
- Số tài nguyên cấu hình ở scene tiếp theo chỉ dùng khi bắt đầu game trực tiếp từ scene đó. Chuyển màn qua cửa tiếp tục giữ kho của lần chơi hiện tại. Thay đổi trong Play Mode chỉ dùng cho lần chơi đó; sửa ở Edit Mode và lưu scene để giữ cấu hình ban đầu.
- `Assets/Components/UI/Gameplay/Resources and UI Settings.asset`: giá, giới hạn, tỉ lệ hồi/khiên, thời gian khiên, icon và nội dung/loại thưởng của bốn chúc phúc. Mục `Damage Blessing` có `Damage Bonus Per Blessing` (mặc định 0.05) và `Max Damage Bonus` (0.3).
- Mô tả chúc phúc sát thương hỗ trợ `{damagePercent}` và `{damageCap}` để tự hiển thị đúng phần trăm theo cấu hình và buff đang có.
- `Assets/Components/UI/Gameplay/Game HUD.prefab`: bố cục HUD, cửa hàng hai ô và bốn panel chúc phúc. Panel vật phẩm có hai icon đỏ/xanh. Có thể sửa Sprite, vị trí và kích thước tại `Blessing Icon` trong scene hoặc prefab; khi mở menu, script giữ hình ảnh đã đặt cho phần thưởng tương ứng. Icon trong Settings là dự phòng khi Image chưa có Sprite. Khi kho đầy hoặc buff đạt trần khiến loại thưởng đổi, UI hiển thị icon/nội dung của phần thưởng thay thế; mở lại với loại thưởng gốc sẽ khôi phục hình ảnh đã đặt.
- `GameplayPanelController.onBlessingSelected`: sự kiện có chỉ số panel 0/1/2/3 để nối xử lý bổ sung.
- `Game HUD.prefab > Settings Overlay / Restart Overlay`: sửa bố cục Settings và Game Over; hai bảng dùng cùng khung store. Thanh máu HUD dùng khung hoa văn, thanh đỏ và thanh stamina xanh trong tilesheet đã gửi.
- `Assets/Components/Resources/GameAudioCatalog.asset`: gán clip tại `Background Music`, `Ui Button Click`, `Settings Open/Close` và `Restart Panel Open` (cùng các cue khác nếu có). Hiện chưa có clip âm thanh trong Assets; thanh trượt và các nguồn phát đã được nối nhưng cần clip để nghe âm thanh. Âm lượng được lưu bằng PlayerPrefs khi Resume/Quit.
- `Level Resources` trong mỗi scene: đánh dấu Purchasing Level và đặt Chapter Index khi thêm Chapter.
- `LevelExitDoor`: Target Scene Name, Require Key và Offer Blessing On Arrival khi thêm màn mới.
- `Assets/Objects/Items/CryHeal.prefab` và `CryShield.prefab`: kéo vào map để đặt đá nhặt được.
- `Chest` trong scene hoặc `Assets/Objects/Items/Chest.prefab`: chỉnh `Cry Heal Amount`, `Cry Shield Amount`, `Opening Duration` (mặc định 0.6 giây theo clip Open), và `Prompt Range` trong `TreasureChestInteractable`.
- `Game HUD > Player Item Receipt UI`: chỉnh `World Offset`, `Duration`, `Fade Out Duration`, `Rise Distance` và `Row Spacing`. Hai dòng UI nằm trong `Item Receipt Notifications`; không chặn chuột trên các panel khác.
- `Tools > BTL > Set Up Chest Loot and Item Notifications`: nối prompt rương và hai dòng thông báo vào prefab đang có, không tạo lại HUD.
- `Tools > BTL > Install Resources, HUD and Store`: nối lại ba scene đã liệt kê; giữ asset cấu hình và sử dụng prefab UI hiện có.
- `Tools > BTL > Install Shared Gameplay UI and Respawn in Current Scene`: cài HUD prefab dùng chung, EventSystem, `Run Resources`, `LevelResourceFlow`, checkpoint hồi sinh và đưa scene đang mở vào Build Settings. Dùng mục này khi thêm một scene gameplay mới.
- `Tools > BTL > Install Pause, Restart and Audio UI`: cài/cập nhật các bảng Settings/Game Over và sprite thanh trượt/thanh máu vào HUD dùng chung; giữ các object hiện có khi chạy lại.

Sprite cell/panel lấy từ các prefab có sẵn trong `Assets/Components/UI/Gameplay`, icon đá và SpiritStone lấy từ `Assets/Objects/Items`. Hiệu ứng heal/shield dùng Animator đã có trong `Assets/Objects/Player/Ui`. Hai effect demo trong Purchasing Levels được giữ trong hierarchy ở trạng thái tắt; effect thực chạy theo việc dùng vật phẩm trên player.

Tilemap của Purchasing Levels ban đầu chưa có collider; đã thêm TilemapCollider2D để player đứng và đi tới tượng. Khung UI/đá/SpiritStone dùng lọc Point, không nén và không mipmap để giữ nét pixel. Chỉ số HP/stamina có sẵn trên player được giữ; hiệu ứng và chúc phúc tính theo phần trăm.

Bản sao trước thay đổi nằm trong `outputs/item-ui-20261004/before`, gồm scene Purchasing với các chỉnh sửa trước đó chưa lưu.

## Kết quả kiểm tra trong Editor

### Cập nhật checkpoint và Level 3 (2026-10-04)

`SharedGameplaySceneSetup` đã cài `Game HUD.prefab`, EventSystem, `Run Resources` và `LevelResourceFlow` vào Level 3. Player prefab dùng chung có `PlayerCheckpointRespawn`; Level 3 được thêm vào Build Settings, cùng cửa Entrance ở đoạn cuối map và chìa khóa do Bat (15) giữ. Level 2 dẫn sang Level 3, sau đó cửa Level 3 dẫn tới Purchasing Levels. Unity Editor recompile hoàn tất với `compilationFailed=false` và không có lỗi biên dịch. Cấu trúc scene và các override của cửa/chìa khóa đã được đọc lại trong Editor. Chưa chạy Play Mode hoặc test suite cho lượt cập nhật này.

Đã chạy các cơ chế trong Play Mode: nhặt đá, nhận SpiritStone khi quái chết, mua/thiếu tiền/kho đầy, hồi máu, khiên hấp thụ/hết hạn/tạm dừng theo menu, chuyển màn, reset chìa khóa, nhận chúc phúc và giữ tài nguyên khi reload cùng map. Phiên xem trước cuối xác nhận HP 200 → 100 sau sát thương → 160 sau CryHeal, khiên 50 HP, mua hai loại trừ đúng 14 SpiritStone. Các nút mua và đóng nhận được sự kiện chuột qua raycast UI.

Ba scene đều có một HUD và một EventSystem, không có script bị mất. Phiên xem trước cuối không có lỗi biên dịch hoặc cảnh báo/lỗi Console mới. Unity đã trở về Edit Mode ở Purchasing Levels; dữ liệu minh họa chỉ tồn tại trong Play Mode.

Ảnh HUD, cửa hàng và chúc phúc, cùng nhật ký kiểm tra, nằm trong `outputs/item-ui-20261004`. Chưa tạo player build hoặc đo hiệu năng.

Lần cập nhật bốn lựa chọn nằm trong `outputs/four-blessings-20261004`: đã xem lại ảnh UI, raycast/chọn panel thứ tư và panel vật phẩm; chọn vật phẩm tăng kho từ 2/2 lên 3/3. Bonus sát thương 5% cho các giá trị nền 5/10/15/25/40 cho ra 6/11/16/27/42 sau làm tròn. Phiên xem trước không có cảnh báo/lỗi Console mới. Số vật phẩm 5/5 mà người dùng đã đặt ở scene Level 1 được giữ lại sau phiên xem trước.

Sửa lỗi icon khi vào Play Mode: `OpenBlessings` trước đây ghi đè Sprite và RectTransform từ cấu hình, làm mất hiệu lực chỉnh Image trong scene. Giờ hình ảnh gốc được nhớ ở Awake để sử dụng và khôi phục khi mở menu; ba icon Heal/Shield/Dame do người dùng đặt được cập nhật vào HUD chung và Settings. Ô 4 hiển thị “Hồi máu” khi cả hai kho đang 5/5 là phần thưởng thay thế của hệ thống; giảm số vật phẩm trong `Run Resources` ở Edit Mode để xem phần thưởng nhận hai đá. Bản sao trước thay đổi nằm trong `outputs/blessing-visuals-20261004/before`.

Bản sao trước khi thêm phần thưởng rương và thông báo nhận đá nằm trong `outputs/chest-loot-20261004/before`.

Đã xem trước rương bằng đường tương tác F (`PlayerInteraction.TryInteract`): kho 2/2 tăng lên 3/3 sau animation mở, lượng còn lại của rương về 0/0 và hai dòng có đúng sprite đỏ/xanh. Sau khi hết thời gian, cả hai dòng tự tắt. Ảnh `outputs/chest-loot-20261004/received-items.png` và các JSON ghi trạng thái này. Unity trở về Edit Mode ở Level 1 với số tài nguyên 0 SpiritStone, 5 CryHeal, 5 CryShield như trước; scene không thay đổi. Không có lỗi biên dịch/lỗi Console của gameplay. Chưa tạo player build.

Để tự xem trong scene: đặt hai số đá của `Run Resources` dưới 5, Play, đứng gần Chest và nhấn F. Xem hai dòng nhận đá và HUD; chờ khoảng 2.2 giây để thông báo biến mất. Số lượng mỗi rương chỉnh được tại component `TreasureChestInteractable`.
