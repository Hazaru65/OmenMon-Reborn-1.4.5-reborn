# OmenMon Fan Safety Diagnosis

**Kapsam:** Constant fan mode'dan güvenli Auto moda geçişin zaman zaman gerçekleşmemesi  
**Durum:** Kod değişikliği yapılmadan tamamlanan statik teşhis  
**İlgili sürüm:** OmenMon-Reborn 1.4.5  
**İnceleme tarihi:** 2026-08-20

## Kısa Sonuç

Kodda gerçek bir state-detection problemi var. Constant → Auto güvenlik kontrolü, fiziksel fan durumundan bağımsız olarak GUI state'ine ve manual EC register'ına bağlanmış.

Tray-only çalışırken ana form yoktur. Bu durumda safety kontrolü Constant modu şu şekilde tespit eder:

```csharp
Platform.Fans.GetManual()
```

Ancak çalışan yapılandırmada:

```xml
<FanLevelNeedManual>false</FanLevelNeedManual>
```

olduğu için Constant fan seviyeleri BIOS üzerinden yazılabilir ve manual EC register'ı aktif olmayabilir. Fiziksel olarak fanlar Constant seviyesinde olsa bile `GetManual()` false dönebilir. Böylece sıcaklık 94 °C ve threshold 90 °C olsa dahi safety kontrolü çağrılmaz.

GUI açıldığında form state'i yeniden oluşturulur/güncellenir. Sonraki timer tick'inde GUI radio state'i Constant olarak görülürse gecikmiş safety kontrolü çalışabilir. Bu, fanların GUI açıldıktan sonra çalışmaya başlaması izlenimini açıklıyor.

## Ana Kanıtlar

### 1. Safety gate GUI state'ine bağlı

**Dosya:** `App/Gui/GuiTray.cs`  
**Sembol:** `GuiTray.Update()`  
**İlgili satırlar:** yaklaşık 376-403

```csharp
bool needsSafetyCheck = Config.FanConstSafetyEnabled
    && (this.FormMain != null
        ? this.FormMain.IsConstMode
        : this.Op.Platform.Fans.GetManual());
```

Safety kontrolünün çalışması için iki koşul gerekir:

```text
FanConstSafetyEnabled == true
AND
(
    FormMain varsa: FormMain.IsConstMode == true
    FormMain yoksa: Fans.GetManual() == true
)
```

`needsSafetyCheck` false ise `CheckFanConstSafety()` çağrılmaz.

### 2. Constant mode tespiti gerçek hardware state değildir

**Dosya:** `App/Gui/GuiFormMain.cs`  
**Sembol:** `IsConstMode`  
**İlgili satırlar:** yaklaşık 1115-1116

```csharp
public bool IsConstMode => this.RdoFanConst.Checked;
```

Bu değer yalnızca GUI'deki radio button state'idir. EC/BIOS'un gerçek Constant latch veya level state'i değildir.

`UpdateFanCtl()` radio button'ı şu sırayla yeniden belirler:

1. Aktif fan programı varsa `Auto` veya `Prog`
2. Fan kapalıysa `Off`
3. Fan max ise `Max`
4. Trackbar'lar açıksa `Const`
5. Aksi halde `Auto`

Bu nedenle hardware Constant durumda iken GUI `Auto` state'inde kalabilir.

### 3. Tray fallback'i `FanLevelNeedManual=false` ile uyumsuz

**Dosya:** `Hardware/FanArray.cs`  
**İlgili semboller:** `SetLevels()`, `GetManual()`  
**İlgili satırlar:** yaklaşık 136-193

Constant branch'te `SetLevels()` yalnızca `FanLevelNeedManual=true` ise manual state'i açar:

```csharp
if(Config.FanLevelNeedManual)
    this.SetManual(true);
```

Çalışan config'te bu değer false:

```xml
<FanLevelNeedManual>false</FanLevelNeedManual>
```

Aynı anda tray safety fallback'i manual register'a bakar:

```csharp
public bool GetManual() {
    return this.Manual.GetValue() == this.ManualValueOn;
}
```

Bu iki karar birbiriyle uyumsuzdur: Constant mode BIOS seviyeleriyle uygulanabilir, fakat `GetManual()` false kalabilir.

### 4. `GetManual()` cache'i güncellemiyor

`GetMode()` ve `GetOff()` okumadan önce component'i günceller:

```csharp
this.Mode.Update();
this.Switch.Update();
```

`GetManual()` ise `Manual.Update()` çağırmadan cached `GetValue()` sonucunu kullanıyor. Manual register uygulama dışında değişmişse veya başlangıçta cache doldurulmamışsa yanlış state dönebilir.

### 5. Safety yalnızca tray icon update path'inde çalışıyor

**Dosya:** `App/Gui/GuiTray.cs`  
**Sembol:** `Update()`

Timer yaklaşık 1 saniyede bir tick alıyor. Safety branch'i `UpdateIconTick` üzerinden yaklaşık 3 saniyede bir çalışıyor:

```text
GuiTray.Timer
  → EventTimerTick
  → GuiTray.Update()
  → UpdateIconTick branch
  → CheckFanConstSafety()
```

`FanProgram.Update()` sıcaklığı fan curve seçmek için okuyor; Constant safety kontrolünü yapmıyor. Fan safety herhangi bir hardware write path'inde merkezi olarak uygulanmıyor.

## Çalışan Config Bulguları

İncelenen runtime dosyası: `Bin/OmenMon.xml`

```xml
<FanConstReapplyEnabled>true</FanConstReapplyEnabled>
<FanLevelNeedManual>false</FanLevelNeedManual>
<GuiDynamicIcon>true</GuiDynamicIcon>
<FanConstSafetyEnabled>true</FanConstSafetyEnabled>
<FanConstSafetyTemp>90</FanConstSafetyTemp>
```

Dolayısıyla çalışan binary için safety feature açık ve threshold 90 °C.

Ayrı bir özellik olan Thermal Panic ise kapalı:

```xml
<ThermalPanicEnabled>false</ThermalPanicEnabled>
<ThermalPanicTemperature>90</ThermalPanicTemperature>
```

Bu iki threshold karıştırılmamalıdır:

| Özellik | Threshold | Davranış |
|---|---:|---|
| `FanConstSafety` | `FanConstSafetyTemp` | Constant → Auto revert |
| `ThermalPanic` | `ThermalPanicTemperature` | Fanları MAX yapar |

## Gözlenen Davranışın Muhtemel Akışı

```text
1. Uygulama tray-only başlar; FormMain == null.
2. Fanlar fiziksel olarak Constant seviyesinde olabilir.
3. Fan seviyeleri BIOS üzerinden yazılmıştır; FanLevelNeedManual=false.
4. Fans.GetManual() false döner.
5. needsSafetyCheck false olur.
6. Temperature 94 °C olsa bile CheckFanConstSafety çağrılmaz.
7. Kullanıcı tray ikonuna tıklayıp ana formu açar.
8. GuiFormMain.UpdateAll() GUI state'ini yeniden senkronize eder.
9. Bir sonraki icon tick'inde FormMain.IsConstMode true görülürse safety çalışır.
10. RevertToAuto() çağrılır ve fanlar hareket etmeye başlar.
```

Son iki adım donanım/model state'ine bağlıdır. GUI açılması doğrudan fanları çalıştıran tekil bir çağrı değildir; GUI state'in safety gate'i yeniden etkinleştirmesi veya aynı anda BIOS thermal curve'ünün devreye girmesi söz konusu olabilir.

## İkincil Model-Dependent Risk

**Dosya:** `App/Gui/GuiOp.cs`  
**Sembol:** `RevertToAuto()`  
**İlgili satırlar:** yaklaşık 376-445

Revert sonrası OmenMon'un level-only `Auto` programı yalnızca şu koşulda başlatılır:

```csharp
if(Config.FanProgram.ContainsKey(Config.FanProgramAuto)
    && this.Platform.RequiresAutoDrive)
{
    this.Program.Run(Config.FanProgramAuto);
}
```

`RequiresAutoDrive`, `PlatformPreset.FanLevelReleaseViaEc` değerine bağlıdır. Mevcut model database'inde bu flag özellikle 8BD4 için aktiftir.

Diğer modellerde revert işlemi BIOS'un kendi Auto curve'üne güvenir:

```text
Program.Terminate()
Fan off/max latch temizle
Fan level'ları 0xFF ile serbest bırak
Gerekirse manual state'i kapat
Countdown = 0
Mevcut BIOS fan mode'una dön
```

Modelin BIOS/EC Auto curve'ü çalışmıyorsa fanlar release edilmiş fakat sürülmüyor olabilir.

## Hata Sıralaması

### 1. Yüksek güven: Tray safety gate yanlış state kaynağı kullanıyor

`FanLevelNeedManual=false` ile BIOS tabanlı Constant mode, tray tarafındaki `GetManual()` fallback'i tarafından tespit edilemiyor.

### 2. Yüksek güven: GUI radio state ile hardware state ayrışıyor

`IsConstMode` gerçek hardware durumu değil. Program, Max/Off veya form initialization state'i GUI radio button'ı Auto yaparken EC/BIOS Constant state'i korunabilir.

### 3. Yüksek güven: `GetManual()` cached değer kullanıyor

Manual component'i refresh edilmeden okunuyor. Dışarıdan değişen veya başlangıçta kalan EC state'i yanlış değerlendirilebilir.

### 4. Orta güven: Revert sonrası model BIOS Auto curve'ü çalışmıyor

Safety balloon görünmesine rağmen fanlar hızlanmıyorsa model-specific `RequiresAutoDrive`/`FanLevelReleaseViaEc` veya BIOS davranışı incelenmelidir.

### 5. Düşük-orta güven: Sıcaklık cache'i stale

Tray-only, dynamic icon aktif ve fan programı yokken `needForcedUpdate` genellikle taze sıcaklık okuması zorlar. Bu nedenle sıcaklık cache'i birincil neden görünmüyor. GUI görünürken cached değer kullanılması yine de ikincil risk olabilir.

## Kod Değiştirmeden Doğrulama

Sorun tekrarlandığında ana GUI'yi hemen açmadan önce 3-5 saniye beklenmeli.

### Balloon görünmüyorsa

```text
Constant fan mode cancelled, reverting to auto.
```

mesajı görünmüyorsa hata `RevertToAuto()` içinde değildir. Safety gate öncesinde kapanmıştır:

- `FanConstSafetyEnabled`
- `FormMain.IsConstMode`
- `Fans.GetManual()`
- aktif program state'i

### Balloon görünüyor ama fanlar başlamıyorsa

Safety revert çalışmış, fakat modelin BIOS/EC Auto curve'ü fanları sürmemiş olabilir. Bu durumda model ProductId ve `FanLevelReleaseViaEc` incelenmelidir.

### Diagnostic verisi

Hata anında GUI açılmadan önce mümkünse `OmenMon.exe -Diag` ile veya diagnostic clipboard özelliğiyle şu alanlar alınmalı:

```text
ProductId
resolved PlatformPreset
FanConstSafetyEnabled
FanConstSafetyTemp
FanLevelNeedManual
FanLevelReleaseViaEc
AutoCal mapping
EC trace
```

## Kavramsal Çözüm Yönü

Bu teşhis sırasında source dosyalarında değişiklik yapılmadı. İleride uygulanacak çözümün yönü:

1. Constant safety kontrolü `FormMain.IsConstMode` değerine bağlı olmamalı.
2. Safety kontrolü `FormMain` oluşturulmuş mu/görünür mü ayrımından bağımsız çalışmalı.
3. Constant state güvenilir bir hardware-state kaynağından belirlenmeli.
4. `GetManual()` gerçek EC değerini refresh etmeden cache'e güvenmemeli.
5. BIOS Auto curve'ü çalışmayan modellerde model-specific OmenMon Auto programı kullanılmalı.
6. `FanConstReapply` ve `FanConstSafety` aynı GUI radio gate'ini paylaşmamalı.

## Sınırlar

Bu inceleme kaynak kodu, runtime XML yapılandırmasını ve mevcut release notlarını kapsayan statik teşhistir. Fiziksel laptop üzerinde kontrollü tekrar üretim yapılmadı; bu nedenle kullanıcının ProductId'sine bağlı ikinci model-riskinin kesinliği ayrıca diagnostic çıktısıyla doğrulanmalıdır.

**Debug sonucu:** Kaynak kodda belirtilen semptomu açıklayan gerçek bir safety-state ayrışması mevcut. En güçlü neden, tray-only safety fallback'inin BIOS tabanlı Constant mode'u `GetManual()` üzerinden tespit edememesi ve safety gate'in GUI state'ine bağlanmış olmasıdır.
