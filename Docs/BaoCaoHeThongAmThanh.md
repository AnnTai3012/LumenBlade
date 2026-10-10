# Báo cáo hệ thống âm thanh trong trò chơi 2D

**Báo cáo cuối môn và tài liệu giải thích khi thuyết trình**

Dự án BTL • Unity 6000.3.18f1 • Phạm vi demo boss Demon Slime • Ngày 09/10/2026

**Ghi chú phiên bản 10/10/2026:** Phạm vi hiện tại là tích hợp âm thanh phù hợp với theme game và hiệu ứng âm thanh Mức 1–2. Cơ chế rhythm combat đã được gỡ theo yêu cầu, gồm HUD nhịp, chấm Perfect/Good/Miss và thưởng sát thương. Nguồn âm demo được giải thích trong `Docs/DemoAudioSources.md`; đồng bộ dash, đánh thường và khiên được mô tả trong `Docs/AudioAnimationTiming.md`.

## 1 Mục tiêu và phạm vi thực hiện

Đề bài yêu cầu lập trình hệ thống âm thanh cho một trò chơi, từ việc tích hợp nhạc và hiệu ứng đến xử lý âm thanh theo trạng thái của game. Hệ thống trong dự án được xây dựng để âm thanh phản hồi đúng hành động, có thể điều chỉnh trong settings và thay đổi theo môi trường, scene, sức khỏe người chơi và tiến trình trận boss.

Bản triển khai hiện tại tập trung vào Mức 1 và Mức 2. Demon Slime là boss dùng để trình diễn hoàn chỉnh. Các đòn bắn, phép, laser, cận chiến và tạo khiên dành cho những boss khác còn năm ô chưa gán clip. Đây là giới hạn phạm vi demo đã xác định, nên khi báo cáo cần trình bày rõ thay vì khẳng định tất cả boss đều đã hoàn thiện âm thanh.

Catalog có 48 loại sự kiện SFX, trong đó 43 sự kiện đã gán clip và năm sự kiện của boss khác còn để trống. Ngoài các sự kiện SFX còn có năm trường dành cho nhạc khám phá, nhạc boss, nhạc khu mua đồ, ambience và heartbeat. **43 sự kiện không có nghĩa là 43 file âm thanh khác nhau** vì một số sự kiện dùng chung clip.

### 1.1 Đối chiếu với yêu cầu môn học

| Yêu cầu | Tính năng đã triển khai | Cách chứng minh |
|---|---|---|
| Mức 1 có background | Nhạc khám phá, nhạc boss và nhạc khu mua đồ phát lặp | Chơi ở các bối cảnh tương ứng |
| Mức 1 có SFX gameplay | Âm thanh di chuyển, chiến đấu, vật phẩm, cửa, hồi sinh và Demon Slime | Thực hiện hành động và nghe phản hồi |
| Mức 1 có SFX UI | Click, mở đóng panel, phước lành, nhận vật phẩm | Bấm nút và mở settings |
| Mức 1 có settings | Slider Music và SFX; lưu bằng PlayerPrefs | Kéo slider, mute, chạy lại game |
| Mức 2 có filter | AudioLowPassFilter và AudioReverbFilter | So sánh khi ít máu, pause và ở hang |
| Mức 2 theo môi trường và scene | Scene Profiles, ambience hang, nhạc khu mua đồ và vùng môi trường | Đi qua các scene; thử trigger khu vực |
| Mức 2 theo gameplay | Boss dưới 35% HP, người chơi dưới 25% HP, chết và hồi sinh | Tạo các trạng thái trong demo |
| Mức 3 gameplay gắn với xử lý âm thanh | Chưa triển khai cơ chế chơi phụ thuộc kết quả xử lý âm thanh | Trình bày là hướng phát triển |

Trong Mức 2, gameplay cung cấp trạng thái để hệ thống thay đổi âm thanh. Mức 3 cần cơ chế chơi sử dụng âm thanh làm dữ liệu hoặc quy tắc, chẳng hạn phân tích nhịp để chấm đòn đánh, phân tích cường độ để điều khiển nhân vật hoặc giải đố bằng âm thanh. Việc đổi nhạc khi HP giảm là minh chứng Mức 2; báo cáo hiện tại chưa nhận là hoàn thành Mức 3.

## 2 Kiến trúc và luồng xử lý chung

Hệ thống tách dữ liệu âm thanh, quyết định gameplay và việc phát âm. Ví dụ PlayerCombat xác nhận kỹ năng đã dùng thành công; GameAudioCatalog lưu clip của kỹ năng; GameAudioManager lấy clip, chọn nguồn phát và áp dụng âm lượng cùng hiệu ứng. Khi thay file sound trong catalog, phần tính sát thương không cần sửa.

### 2.1 Những khái niệm cần hiểu

**AudioClip** là dữ liệu âm thanh đã được Unity import từ WAV, MP3 hoặc OGG. **AudioSource** là component phát clip. **GameAudioCue** là mã sự kiện, ví dụ PlayerAttack hoặc DemonSlimeDeath. **Voice** trong code là một lượt phát gameplay được quản lý cùng nguồn phát, cue, gain, độ ưu tiên và thời điểm kết thúc.

**Gain** là hệ số âm lượng. **Pitch** trong cách dùng AudioSource này thay đổi tốc độ phát và đồng thời đổi cao độ. **Cutoff** là tần số cắt của bộ lọc low-pass. **Reverb** tạo cảm giác tiếng vang. **Crossfade** là giảm dần nhạc cũ đồng thời tăng dần nhạc mới.

### 2.2 Vai trò của các script

| Thành phần | Trách nhiệm chính |
|---|---|
| GameAudioCatalog.cs và GameAudioCatalog.asset | Định nghĩa cue, lưu clip, cấu hình từng cue và scene |
| GameAudioManager.cs | Phát sound, tái sử dụng nguồn phát, chuyển nhạc và cập nhật mix |
| UiButtonAudio.cs | Nghe sự kiện click của Button rồi yêu cầu phát âm |
| AudioEnvironmentZone.cs | Phát hiện người chơi vào ra vùng môi trường bằng trigger 2D |
| PlayerCombat và PlayerMotor2D | Gọi sound khi kỹ năng hoặc hành động di chuyển thực sự bắt đầu |
| PlayerController và MonsterHealth | Gọi cue theo trúng đòn, chết và hồi sinh |
| BossEncounter.cs | Cung cấp vùng đấu, HP và việc người chơi đang ở trong vùng boss |
| GameplayPanelController.cs | Nối slider với audio manager và phát âm của panel |

Luồng phát SFX gồm sáu bước: gameplay xác nhận sự kiện; gửi GameAudioCue; catalog tìm AudioClip; manager đọc cấu hình và kiểm tra cooldown; manager chọn nguồn UI hoặc voice gameplay; AudioSource phát với gain, pitch và filter phù hợp.

Luồng âm thanh thay đổi liên tục có ba bước: manager đọc HP, pause, scene và vùng boss trong Update; xác định các giá trị âm lượng và filter đích; nội suy dần các tham số của những nguồn phát đang hoạt động.

### 2.3 Khởi tạo một hệ thống cho toàn bộ game

GameAudioManager được tạo trước khi scene đầu tiên tải. Trong Awake, nếu đã có một manager khác thì đối tượng mới bị loại bỏ. Instance trỏ tới manager đang dùng, còn DontDestroyOnLoad giữ manager khi chuyển màn. Cách này tránh mỗi scene phát thêm một bài nhạc nền trùng nhau.

Đoạn code trích từ GameAudioManager.cs:

```csharp
if (Instance != null && Instance != this)
{
    Destroy(gameObject);
    return;
}
Instance = this;
DontDestroyOnLoad(gameObject);
catalog ??= Resources.Load<GameAudioCatalog>("GameAudioCatalog");
```

Catalog nằm trong thư mục Resources để manager có thể tải theo tên. Mã vẫn có bước ResetStatics khi bắt đầu phiên chạy để tránh giữ Instance cũ trong cấu hình Editor không reload domain.

## 3 Mức 1 tích hợp âm thanh và settings

### 3.1 Dữ liệu âm thanh tập trung trong catalog

GameAudioCatalog là ScriptableObject. Mỗi trường AudioClip trong asset có thể được kéo thả từ Project vào Inspector. Resolve nhận GameAudioCue và trả về clip tương ứng. Cách này dùng mã sự kiện có kiểu rõ ràng thay cho việc nhập chuỗi tên file ở nhiều script.

```csharp
GameAudioManager.PlayCue(GameAudioCue.PlayerAttack);
```

Lệnh trên có nghĩa là “phát sound đánh thường”. PlayerCombat không cần biết đường dẫn file, âm lượng cuối cùng hoặc nguồn phát nào còn trống. Nếu clip chưa gán, manager trả về mà không phát; các trường hợp này phải được kiểm tra khi hoàn thiện nội dung.

Âm thanh đang được sử dụng gồm các nhóm sau:

| Nhóm | Ví dụ trong dự án |
|---|---|
| Di chuyển | Footstep, Jump, Dash, FallImpact |
| Chiến đấu của người chơi | Atttack1, Jumpattack, Skill1, Skill2, Skill3, TakeHit |
| Sinh tồn | HealUseItem, UseShield, Shield Break, Death, Revive, restart |
| Quái thường | Monster attack, Monsterdeath, Slime Attk, Slime take hit, Mimic skill2 |
| Vật phẩm và tiến trình | ChestOpen, KeyPickUp, Item Received, After bless, OpenExitGate, Pass Level |
| Giao diện | Click, mở đóng settings và cửa hàng, Bless open |

Các clip di chuyển, nhân vật và quái chủ yếu dùng thư viện đã cung cấp. Nhạc boss, nhạc sanctuary, ambience, heartbeat, nhận vật phẩm, break khiên và chiến thắng là các clip demo được tổng hợp thêm. Không trình bày chúng là âm thanh thu trực tiếp từ một không gian thực.

### 3.2 Phân nhóm nguồn phát

Manager tạo 25 AudioSource trên các GameObject con để mỗi nhóm có cách xử lý riêng.

| Nhóm nguồn phát | Số lượng | Cách sử dụng |
|---|---|---|
| Music A và Music B | 2 | Hai nguồn phát lặp để crossfade |
| UI SFX | 1 | PlayOneShot cho UI, phần thưởng, chuyển màn và restart |
| Environment | 1 | Ambience hang phát lặp, thay gain theo môi trường |
| Low health heartbeat | 1 | Nhịp tim phát lặp, có gain khi ít máu |
| Gameplay voice | 20 | Phát SFX có clip, pitch và filter độc lập |

Con số 20 là giới hạn voice gameplay do code quản lý. Nhóm UI sử dụng PlayOneShot và không đi qua giới hạn này; 25 AudioSource component không có nghĩa là toàn bộ hệ thống Unity chỉ được phát đúng 25 tiếng cùng lúc.

Music được điều khiển bởi slider Music. Slider SFX điều khiển SFX gameplay, UI, ambience và heartbeat. Đây là cách để mute SFX thực sự tắt toàn bộ hiệu ứng liên quan.

### 3.3 SFX khi tương tác UI

UiButtonAudio đăng ký PlayClick với Button.onClick trong OnEnable và gỡ listener trong OnDisable. Việc gỡ listener tránh một nút gọi sound nhiều lần sau nhiều lần bật tắt giao diện.

```csharp
private void OnEnable()
{
    if (button == null) button = GetComponent<Button>();
    if (button != null) button.onClick.AddListener(PlayClick);
}
private static void PlayClick() =>
    GameAudioManager.PlayCue(GameAudioCue.UiButtonClick);
```

UI SFX được đặt trên đối tượng riêng, không có low-pass hoặc reverb của gameplay. Khi người chơi mở settings, tiếng gameplay được pause nhưng tiếng click tiếp tục phát. Nếu một nút và panel cùng yêu cầu một clip click trong khoảng 0,05 giây, manager bỏ lần trùng để tránh nghe thành hai tiếng.

### 3.4 Settings âm thanh và lưu lựa chọn

Slider gọi SetMusicVolume hoặc SetSfxVolume. Giá trị được giới hạn từ 0 đến 1, lưu trong PlayerPrefs, áp dụng lên nguồn phát rồi ghi bằng Save. Giá trị mặc định là Music 0,7 và SFX 0,8, nhưng lựa chọn đã lưu của người dùng được ưu tiên.

```csharp
public static void SetMusicVolume(float volume)
{
    PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
    Instance?.ApplyVolumes();
    PlayerPrefs.Save();
}
```

Đối với một voice gameplay, âm lượng cuối cùng được tính bằng dòng code:

```csharp
voice.source.volume = SfxVolume * voice.gain * gameplayGain;
```

SfxVolume là lựa chọn của người chơi; voice.gain là mức riêng của cue; gameplayGain là hệ số theo trạng thái. Ví dụ SFX bằng 0,8, gain cue bằng 0,5 và gameplayGain bằng 1 thì nguồn phát có volume 0,4. Khi SFX bằng 0, mọi voice gameplay đều có volume 0.

### 3.5 Demon Slime làm boss demo

Boss có các sound cho toàn bộ chuỗi xuất hiện, tấn công, bị đánh và bị hạ. MonsterCombat nhận diện Demon Slime từ cấu hình component đang dùng và gán cả primaryAudioCue lẫn secondaryAudioCue thành DemonSlimeAttack. Vì vậy hai đòn liên tiếp đều có đúng sound riêng của boss.

| Sự kiện | Cue hoặc clip | Nơi quyết định |
|---|---|---|
| Người chơi vào đấu trường lần đầu | BossAppear.mp3 | BossEncounter.Update |
| Hai đòn tấn công | DemonSlimeAttack.ogg | MonsterCombat |
| Trúng đòn còn sống | BossTakeHit.mp3 qua DemonSlimeTakeHit | MonsterHealth |
| HP về 0 | Death demon slime.mp3 qua DemonSlimeDeath | MonsterHealth |
| Boss đã theo dõi bị hạ | Boss Victory.wav | GameAudioManager.Update |

Cue bị đánh và chết gọi PlayCueAt với vị trí boss để manager có thể giảm âm theo khoảng cách. Sound chiến thắng có cờ victoryPlayed để phát một lần. HP và sát thương vẫn do gameplay quyết định; audio đọc kết quả đó.

## 4 Mức 2 xử lý âm thanh theo bối cảnh

### 4.1 Scene Profiles và ambience

AudioSceneProfile lưu tên scene, môi trường, nhạc, gain ambience và mức điều khiển reverb. Khi SceneManager báo scene đã tải, ConfigureScene tìm profile theo đúng tên scene, bỏ tham chiếu player cũ, dọn voice gameplay và chọn nhạc của bối cảnh mới.

| Scene hoặc điều kiện | Nhạc | Ambience | Mức điều khiển reverb |
|---|---|---|---|
| Level 1 | Theme background | 0,10 | 0,16 |
| Level 2 | Theme background | 0,13 | 0,23 |
| Level 3 | Theme background | 0,16 | 0,28 |
| Purchasing LevelsC1 | Sanctuary Music | 0 | 0,03 |
| Đấu trường boss đang hoạt động | DemonSlime Battle | 0,07 | 0,32 |

Các số trong cột reverb là tham số điều khiển nội bộ, không phải tỉ lệ tiếng vang được đo từ vật lý. Khi rời hang sang khu mua đồ, ambience được giảm dần về 0; nhạc chuyển sang Sanctuary. Trong scene boss, nhạc chiến đấu bắt đầu khi người chơi thực sự nằm trong vùng BossEncounter, không chỉ vì tên scene có chữ Boss.

### 4.2 Low-pass để tạo trạng thái nghe khác nhau

AudioLowPassFilter làm suy giảm thành phần tần số cao hơn cutoff, khiến âm thanh nghe trầm và ít sáng hơn. Đây là thay đổi phổ tần, khác với việc chỉ giảm volume. Chức năng này được mô tả trong tài liệu Unity [1].

Đoạn code xác định cutoff đích:

```csharp
float targetCutoff = dead ? 900f :
    paused ? 1900f : lowHealth ? 4200f : 22000f;
cutoff = Mathf.Lerp(cutoff, targetCutoff, blend);
musicLowPass.cutoffFrequency = cutoff;
nextMusicLowPass.cutoffFrequency = cutoff;
```

| Trạng thái | Cutoff đích | Cảm giác và mục đích |
|---|---|---|
| Bình thường | 22.000 Hz | Âm thanh ít bị lọc |
| Người chơi ít máu | 4.200 Hz | Không gian nghe bị thu hẹp, kết hợp heartbeat |
| Pause | 1.900 Hz | Nhạc dịu và xa hơn, tập trung vào menu |
| Chết | 900 Hz | Nhạc trầm xuống, thể hiện trạng thái thất bại |

Các voice gameplay cũng được cập nhật low-pass theo trạng thái. Riêng cue PlayerDeath và PlayerRevive giữ cutoff 22.000 Hz để người chơi nhận biết rõ sự kiện quan trọng; chúng vẫn có thể nhận reverb theo môi trường. UI được tách riêng nên không nhận các filter này.

### 4.3 Reverb để mô tả môi trường

AudioReverbFilter tạo hiệu ứng tiếng vang [2]. Trong dự án, mỗi voice gameplay có filter riêng với preset User. Cấu hình ban đầu có decayTime 0,85 giây, room −1.200 mB và reverbDelay 0,025 giây. Khi môi trường đổi, code cập nhật mức reverb của voice đang phát.

```csharp
voice.reverb.enabled = reverbAmount > 0.01f;
voice.reverb.reverbLevel = Mathf.Lerp(
    -2800f, 0f, Mathf.Clamp01(reverbAmount * 2f));
```

reverbAmount là tham số điều khiển nội bộ; reverbLevel là tham số của Unity theo đơn vị millibel. Giá trị reverb lớn hơn làm phần vang rõ hơn trong cách ánh xạ hiện tại. Reverb được nội suy khi chuyển môi trường, nên tiếng đánh không đổi chất đột ngột.

Reverb áp dụng vào voice gameplay. Nhạc Music A/B có low-pass riêng, còn UI không có hai filter trên. Vì filter là component trên GameObject, việc đặt các nguồn phát ở những đối tượng con khác nhau giúp tránh một filter vô tình tác động cả tiếng click menu.

### 4.4 Âm thanh thay đổi theo HP và tiến trình boss

Manager tính lowHealth từ HP hiện tại và HP tối đa của người chơi. Ngưỡng trong catalog là 0,25. Nếu nhân vật có 100 HP tối đa, hiệu ứng bắt đầu khi HP còn 25 hoặc thấp hơn; nhân vật có 200 HP thì ngưỡng là 50.

```csharp
bool lowHealth = player != null && !dead &&
    player.CurrentHealth <=
    player.MaxHealth * catalog.lowHealthThreshold;
```

Heartbeat đã được phát lặp từ lúc khởi tạo ở gain 0. Khi lowHealth và không pause, gain đích tăng lên 0,42 rồi được nhân với slider SFX. Nhạc giảm về hệ số 0,55 và low-pass xuống 4.200 Hz. Khi hồi máu qua ngưỡng, những tham số này trở về trạng thái bình thường.

Đối với boss, pressure đúng khi đang trong trận và HP boss còn 35% trở xuống. Nhạc chiến đấu tăng dần pitch đến 1,08 khi boss yếu máu, tạo nhịp căng hơn; vì dùng pitch nên cao độ cũng thay đổi. Đây là biểu đạt âm thanh của giai đoạn trận đấu, không thay đổi sát thương hay tốc độ AI.

### 4.5 Thứ tự ưu tiên khi có nhiều trạng thái

Một người chơi có thể vừa ít máu vừa đang đấu boss. Manager chọn nhãn CurrentState theo thứ tự: Dead, Paused, LowHealth, BossPressure, BossCombat, Exploration. Các cờ bossFight, pressure và lowHealth vẫn được giữ để quyết định riêng từng tham số.

```csharp
CurrentState = dead ? GameAudioState.Dead :
    paused ? GameAudioState.Paused :
    lowHealth ? GameAudioState.LowHealth :
    pressure ? GameAudioState.BossPressure :
    bossFight ? GameAudioState.BossCombat :
    GameAudioState.Exploration;
```

Ví dụ khi cả người chơi và boss đều ít máu, trạng thái được ghi là LowHealth: heartbeat và low-pass được bật, nhạc boss vẫn được giữ, đồng thời cờ pressure còn có thể nâng pitch của nhạc. Khi pause, mix ưu tiên menu: heartbeat giảm về 0, voice gameplay dừng và nhạc dịu xuống. Khi chết, mix ưu tiên trạng thái Dead.

### 4.6 Crossfade và nội suy tham số

Crossfade dùng Music A và Music B. SelectMusic chọn clip mới; nguồn mới bắt đầu ở volume 0. Update tăng crossfade từ 0 đến 1 theo thời gian 1,25 giây; nhạc cũ giảm dần, nhạc mới tăng dần. Khi hoàn tất, nguồn cũ dừng.

```csharp
crossfade = Mathf.MoveTowards(crossfade, 1f,
    dt / Mathf.Max(0.05f, catalog.transitionSeconds));
musicSource.volume = MusicVolume * musicGain *
    outgoingGain * (1f - crossfade);
nextMusicSource.volume = MusicVolume * musicGain * crossfade;
```

outgoingGain giữ mức của nguồn nhạc cũ khi một lần chuyển nhạc bị ngắt giữa chừng. Nếu clip mong muốn vẫn là clip đang được chọn thì SelectMusic không gọi Play lại, giúp tránh nhạc bị khởi động lại mỗi frame.

Gain, cutoff, reverb và pitch còn dùng Mathf.Lerp với hệ số blend tính từ Time.unscaledDeltaTime. Thời gian không bị scale giúp menu vẫn chuyển mix mượt ngay cả khi Time.timeScale bằng 0. Crossfade là thay đổi mức phát; low-pass và reverb là các bước xử lý tín hiệu cụ thể.

### 4.7 Vùng môi trường trong cùng một scene

AudioEnvironmentZone là phần mở rộng tùy chọn. Gắn component lên Collider2D trigger, chọn Environment và Priority. Khi collider thuộc người chơi đi vào, zone đăng ký với manager. Khi ra hết hoặc zone bị tắt, đăng ký được gỡ.

HashSet lưu từng collider để một người chơi có nhiều collider không làm zone thoát sớm. Nếu nhiều zone cùng có người chơi, manager chọn zone có Priority lớn nhất. Priority của zone càng lớn càng được ưu tiên; đây là quy tắc khác với priority của voice âm thanh.

Các profile scene và vùng BossEncounter đang tự hoạt động. Zone cục bộ đã có code và được kiểm tra bằng trigger 2D; không trình bày rằng mọi map hiện đã được bố trí nhiều zone nếu chưa đặt chúng vào scene.

## 5 Logic phát sound ổn định và khớp gameplay

### 5.1 Tái sử dụng voice và hạn chế âm phát chồng

Manager tạo sẵn 20 voice gameplay. Khi phát sound, nó tìm voice rảnh thay vì tạo AudioSource mới. Voice kết thúc được Stop, bỏ clip, đưa volume về 0 rồi đánh dấu rảnh. Điều này giữ số component ổn định trong lúc chiến đấu.

AudioCueSettings có volume, minPitch, maxPitch, cooldown, maxVoices và priority. Cooldown ngăn một cue được gọi quá dày. maxVoices giới hạn số lượt cùng cue đang phát trong pool gameplay. priority càng nhỏ thì voice càng quan trọng; tiếng chết có priority 15, kỹ năng 45, bước chân 220.

Khi pool đầy, một cue mới có thể thay voice yếu hơn hoặc ngang ưu tiên; nếu tất cả voice đều quan trọng hơn, yêu cầu mới bị bỏ. Không thể cam kết mọi yêu cầu sound đều phát trong mọi tình huống, nhưng các sound quan trọng được ưu tiên trước tiếng nền ít quan trọng.

### 5.2 Khoảng cách và hướng của tiếng quái trong game 2D

PlayCueAt nhận vị trí nguồn âm. Trong cách làm hiện tại, AudioSource có spatialBlend bằng 0; manager tự tính volume và panStereo từ vị trí nguồn và người chơi. Đây là giảm âm và định hướng stereo do code, không phải hệ thống âm thanh 3D có che khuất vật cản.

```csharp
float distance = Vector2.Distance(position, player.transform.position);
if (distance >= 24f) return;
gain *= Mathf.Clamp01(1f -
    Mathf.Max(0f, distance - 4f) / 20f);
pan = Mathf.Clamp((position.x - player.transform.position.x)
    / 20f, -0.35f, 0.35f);
```

Trong 4 đơn vị đầu, gain không giảm vì khoảng cách. Từ 4 đến 24 đơn vị, gain giảm tuyến tính. Ở 12 đơn vị, hệ số khoảng cách là 0,6. Ví dụ slider SFX 0,8 và gain cue 0,4 thì volume còn 0,192 khi gameplayGain bằng 1. pan âm là lệch trái, pan dương là lệch phải và được giới hạn để âm không bị lệch quá mạnh.

### 5.3 Sound kỹ năng khớp thời lượng animation

PlayerCombat chỉ gọi sound sau khi đã kiểm tra cấu hình kỹ năng và tiêu hao stamina thành công. PlayCueForDuration nhận thời lượng thực tế của kỹ năng; catalog tính pitch bằng độ dài clip âm thanh chia thời lượng animation, sau đó nhân Speed Multiplier và giới hạn từ 0,01 đến 3.

```csharp
float pitch = clip != null && animationDuration > 0f &&
    (timing == null || timing.matchAnimation)
    ? clip.length / animationDuration : 1f;
return Mathf.Clamp(pitch *
    (timing != null ? timing.speedMultiplier : 1f), 0.01f, 3f);
```

Ví dụ Skill1 dài 1,368 giây còn animation dài khoảng 1,01667 giây. Pitch tính được khoảng 1,34557. Thời gian nghe sau điều chỉnh là 1,368 chia 1,34557, gần bằng 1,01667 giây. Mỗi voice giữ pitch riêng nên tiếng bước chân và nhạc không bị đổi tốc độ theo kỹ năng.

Match Animation có thể tắt để dùng Speed Multiplier thủ công. Nếu multiplier khác 1 hoặc pitch chạm giới hạn cho phép, âm thanh có thể kết thúc sớm hoặc muộn hơn animation. Đây là đổi tốc độ bằng pitch, nên báo cáo không gọi nó là thuật toán kéo giãn thời gian giữ nguyên cao độ.

Âm cửa có cách đồng bộ khác: LevelExitDoor đọc độ dài OpenExitGate và điều chỉnh tốc độ Animator mở cửa để chờ đủ đoạn âm. Animator được trả về tốc độ trước đó khi hủy hoặc tắt đối tượng.

### 5.4 Pause resume và chuyển màn

Time.timeScale bằng 0 dừng thời gian gameplay nhưng code audio vẫn cần xử lý. Manager gọi AudioSource.Pause trên voice đang hoạt động, đánh dấu paused và cộng thời gian chờ vào endTime. Nếu không cộng phần thời gian pause, voice có thể bị coi là đã hết hạn ngay khi tiếp tục chơi.

Khi đóng menu, UnPause tiếp tục từ vị trí đã dừng [3]. Dùng Play lại có thể làm sound bắt đầu từ đầu. UI SFX tiếp tục phát riêng; nhạc không dừng hoàn toàn mà giảm gain và cutoff để thể hiện màn hình pause.

Khi chuyển scene, manager được giữ lại và ConfigureScene dọn các voice gameplay của scene cũ. LevelTransition đi qua nhóm UI SFX nên không bị dọn cùng pool gameplay. Trước khi restart, PlayerCheckpointRespawn gọi NotifyRestart; khi scene mới được cấu hình, manager phát PlayerRestart rồi xóa cờ. Cue restart được phát sau load để tránh bị cắt mất.

### 5.5 Sự kiện khiên và hồi sinh

Health phát OnShieldBroken khi damage làm khiên về 0. Manager đăng ký với Health đang sử dụng và phát ShieldBreak khi nhận event, đồng thời gỡ đăng ký khi đổi player. Audio không tự trừ khiên hay quyết định HP.

Khi Health.Revive thành công, OnRevived thông báo cho các thành phần. PlayerController khôi phục trạng thái nhân vật và phát PlayerRevive; PlayerRecoveryEffectUI chạy hiệu ứng hình ảnh. Lỗi hình Revive không hiện đã được sửa bằng cách bật GameObject chứa Image khi bắt đầu hiệu ứng. Âm thanh và hình ảnh cùng phản hồi sự kiện hồi sinh nhưng có luồng trình bày riêng.

## 6 Kiểm tra kết quả và giới hạn

Việc kiểm tra dùng Unity 6000.3.18f1, player và Demon Slime thật trong scene, panel settings hiện có, chuyển scene mua đồ, restart và Level 2. Những thay đổi HP hoặc vị trí để tạo tình huống chỉ diễn ra trong Play Mode và không được lưu vào scene.

| Nhóm bằng chứng | Kết quả | Nội dung chính |
|---|---|---|
| Biên dịch Unity | Thành công | Runtime code được Unity compile |
| Kiểm tra tích hợp audio | 135/135 đạt | Cue, playback, filter, HP, boss, pause, mute, chuyển màn và restart |
| Kiểm tra bổ sung audio | 8/8 đạt | PlayerCombat thật, trigger 2D, nhiều collider và pool khi nhận 200 yêu cầu |
| Kiểm tra hình Revive | 20/20 đạt | Hiển thị, phát lại, pause, bật tắt HUD và Heal |

Hai nhóm audio có tổng cộng 143 kiểm tra; 20 kiểm tra hình Revive là nhóm riêng. Những con số này là kiểm tra tích hợp do script thăm dò trong Play Mode, không được trình bày như một bộ NUnit Test Runner hoặc kiểm thử toàn bộ trò chơi.

Bằng chứng nằm ở outputs/audio-levels-20261009 và outputs/revive-fix-20261009. Chưa chạy standalone build hoặc đánh giá âm thanh bằng tai người dùng. AudioSource đang phát và giá trị filter đúng chứng minh luồng xử lý hoạt động, nhưng không thay thế việc nghe trên loa hoặc tai nghe trước khi thuyết trình.

Giới hạn cần công bố gồm năm cue đòn của boss khác còn trống, nhạc bổ sung ở mức demo, Level BossC2 chưa có trong Build Settings hiện tại và chưa có cơ chế thuộc Mức 3. Sinh viên nên demo Demon Slime bằng cách mở scene trực tiếp trong Editor.

## 7 Kịch bản demo trước giảng viên

Trước buổi báo cáo, kiểm tra loa hoặc tai nghe, đặt Music và SFX đủ nghe, chọn một bản sao hoặc phiên Play Mode để tạo tình huống HP. Không sửa và lưu HP thấp vào prefab chỉ để demo. Các mã dưới đây dành cho giải thích; việc ép trạng thái trong kiểm tra đã có script riêng trong thư mục outputs.

| Bước | Thao tác | Điều cần chỉ ra |
|---|---|---|
| 1 | Play Level 1, di chuyển và dùng kỹ năng | Nhạc, footsteps, SFX và thời điểm phát đúng hành động |
| 2 | Mở settings, kéo Music và SFX về 0 rồi tăng lại | Hai nhóm volume, lưu lựa chọn, click vẫn nghe khi pause |
| 3 | Vào Purchasing LevelsC1 | Nhạc khu mua đồ; ambience hang giảm dần |
| 4 | Mở Level BossC2 trực tiếp trong Editor và Play | Scene có Demon Slime; đi vào vùng boss để đổi nhạc |
| 5 | Đánh và cho boss đánh | Sound xuất hiện, hai đòn, hit và khoảng cách |
| 6 | Tạo boss còn ≤35% HP và người chơi còn ≤25% HP | Nhạc boss tăng pitch; heartbeat và low-pass ít máu |
| 7 | Mở settings giữa một sound dài rồi tiếp tục | Gameplay sound pause và tiếp tục; nhạc dịu xuống |
| 8 | Heal, dùng thánh giá hồi sinh và hạ boss | Mix được khôi phục, sound hồi sinh và chiến thắng |

Nếu HP giảm quá nhanh khi demo, dùng các fixture đã có trong outputs để tạo tình huống có kiểm soát, hoặc chuẩn bị giá trị trong Play Mode. Nói rõ đây là tạo trạng thái để kiểm tra phản ứng audio, không phải một cơ chế cheat đã đưa vào sản phẩm.

### 7.1 Các câu có thể nói khi trình bày

“Em chia hệ thống thành ba phần: gameplay phát sinh sự kiện, catalog lưu dữ liệu và manager phụ trách phát cùng xử lý âm. Ví dụ khi kỹ năng được xác nhận và đã trừ stamina, PlayerCombat gửi một cue; manager tìm clip và lấy một voice rảnh.”

“Ở Mức 1, em có nhạc nền, sound trong game, sound UI và hai slider Music SFX có lưu thiết lập. Em tách UI khỏi voice gameplay để khi pause vẫn dùng được menu.”

“Ở Mức 2, em dùng low-pass và reverb. Low-pass đổi cảm giác nghe khi ít máu, pause hoặc chết; reverb thay theo hang và đấu trường. Nhạc cùng ambience còn thay theo scene và trạng thái trận Demon Slime.”

“Khi chuyển nhạc, hai nguồn Music A B giảm và tăng volume trong 1,25 giây. Khi sound của kỹ năng dài hơn animation, em chỉnh pitch trên chính voice đó để không đổi tốc độ của các sound khác.”

“Phạm vi boss demo của em là Demon Slime. Những boss khác còn thiếu các sound đòn đặc thù. Hiện em trình bày Mức 1 và Mức 2; Mức 3 như gameplay dựa trên phân tích nhịp là hướng phát triển tiếp.”

## 8 Câu hỏi thường gặp khi bảo vệ

### Vì sao không đặt toàn bộ AudioSource trên một GameObject

Các nguồn cần nhận xử lý khác nhau. Đặt trên các đối tượng con giúp gắn low-pass và reverb riêng cho gameplay, gắn low-pass cho nhạc và giữ UI rõ ràng. Nếu dùng chung vị trí component filter, việc kiểm soát phạm vi tác động sẽ khó hơn.

### Vì sao dùng ScriptableObject cho catalog

Catalog là dữ liệu dùng chung có thể chỉnh trong Inspector. Thay clip hoặc gain không cần sửa từng script chiến đấu. Cue vẫn có mã rõ ràng, còn asset quyết định mã đó trỏ đến clip nào.

### Đồ án có dùng AudioMixer và snapshot không

Phiên bản hiện tại dùng AudioSource theo nhóm và các AudioFilter component, chưa có AudioMixer asset hay snapshot. Hai slider được áp dụng bằng code. Low-pass và reverb đang hoạt động thật trên nguồn phát nên vẫn có minh chứng xử lý filter của Mức 2. Có thể chuyển sang AudioMixer khi cần quản lý thêm nhiều nhóm hoặc hiệu ứng.

### Giảm volume có được gọi là low-pass không

Giảm volume làm toàn bộ âm nhỏ hơn. Low-pass giảm thành phần tần số cao hơn cutoff, làm màu âm thay đổi. Dự án áp dụng cả hai: giảm gain để nhạc nhẹ hơn và giảm cutoff để nghe trầm hơn.

### Vì sao phải dùng thời gian không bị scale

Khi pause, Time.timeScale bằng 0. Update của audio vẫn cần nội suy gain và filter cho menu, nên dùng unscaledDeltaTime. Voice gameplay được Pause riêng để không phát tiếp trong lúc nhân vật đứng yên.

### Priority của zone và priority của voice có giống nhau không

Không. Zone có số lớn hơn thì được ưu tiên khi các vùng chồng nhau. Voice có số nhỏ hơn thì quan trọng hơn, như chết hoặc kỹ năng. Hai giá trị có tên giống nhau nhưng điều khiển hai quyết định riêng.

### Đổi pitch có giữ nguyên cao độ của sound không

Không trong cách làm hiện tại. AudioSource pitch làm thay đổi tốc độ và cao độ. Đây là lựa chọn đơn giản để khớp thời lượng hoặc tăng căng thẳng. Thuật toán kéo giãn thời gian giữ nguyên cao độ là một hướng khác, chưa được triển khai.

### Nếu nhiều quái tấn công cùng lúc thì có sound cho từng con không

Manager giới hạn theo cue và số voice để tránh quá nhiều sound chồng. Các con ở xa còn bị giảm hoặc bỏ sound. Vì vậy không phải mọi lời gọi đều được phát; hệ thống ưu tiên những sound quan trọng và giữ hỗn hợp dễ nghe hơn.

### Khi boss và người chơi đều ít máu thì hiệu ứng nào được chọn

Nhãn CurrentState ưu tiên LowHealth nên heartbeat và low-pass ít máu xuất hiện. Nhạc vẫn là nhạc boss; nếu cờ pressure còn đúng và không pause hoặc chết, pitch có thể vẫn tăng. Đây là kết hợp bối cảnh trận đấu với cảnh báo sinh tồn.

### Cần làm gì để được trình bày Mức 3

Cần bổ sung một cơ chế chơi mà quy tắc sử dụng kết quả xử lý âm thanh, ví dụ phân tích nhịp để xác định cửa sổ đánh trúng hoặc phân tích cường độ để giải đố. Sau đó phải demo tác động của dữ liệu âm thanh lên kết quả gameplay. Những ví dụ này là đề xuất phát triển, không phải tính năng có sẵn trong bản báo cáo.

## 9 Hướng dẫn đọc code và nguồn tham khảo

Để hiểu nhanh, đọc GameAudioCatalog trước để biết cue và cấu hình; tiếp theo đọc Play trong GameAudioManager để hiểu một lượt SFX; sau đó đọc Update, SelectMusic và ConfigureScene để hiểu Mức 2. Cuối cùng đọc các điểm gọi trong PlayerCombat, MonsterCombat, MonsterHealth và GameplayPanelController.

Các nhóm Audio, Player và Monster trong bảng nằm dưới Assets/Components/Scripts/Runtime. Asset catalog nằm dưới Assets/Components/Resources.

| Nơi đọc trong project | Nội dung cần tìm |
|---|---|
| Audio / GameAudioCatalog.cs | ResolveSkillPitch dòng 143; Resolve dòng 153; ResolveScene dòng 209 |
| Audio / GameAudioManager.cs | ConfigureScene dòng 112; Update dòng 132; SelectMusic dòng 202 |
| Audio / GameAudioManager.cs | SetMusicVolume dòng 220; Play dòng 255; ApplyVoiceMix dòng 313 |
| Player / PlayerCombat.cs | Lời gọi PlayCueForDuration dòng 595 |
| Monster / MonsterCombat.cs | Routing hai đòn dòng 75; PlayCueAt dòng 125 |
| Monster / MonsterHeal.cs | Cue hit dòng 125; cue chết dòng 134 |
| Audio / AudioEnvironmentZone.cs | Trigger vào ra vùng và HashSet collider |
| GameAudioCatalog.asset | Clip, Cue Settings, ngưỡng HP và Scene Profiles |

Các số dòng là vị trí của code tại thời điểm viết báo cáo; có thể đổi nếu chỉnh script sau này. Các đoạn code trong tài liệu là trích đoạn hoặc được ngắt dòng cho dễ đọc, không thay thế toàn bộ phương thức.

[1] Unity Scripting API AudioLowPassFilter: https://docs.unity3d.com/ScriptReference/AudioLowPassFilter.html

[2] Unity Scripting API AudioReverbFilter: https://docs.unity3d.com/ScriptReference/AudioReverbFilter.html

[3] Unity Scripting API AudioSource UnPause: https://docs.unity3d.com/ScriptReference/AudioSource.UnPause.html

Nguồn kiểm chứng của dự án: Docs/AudioLevels1And2.md; Docs/ReviveVisibilityFix.md; outputs/audio-levels-20261009/runtime-report.json; outputs/audio-levels-20261009/regression-report.json; outputs/revive-fix-20261009/regression-report.json.
