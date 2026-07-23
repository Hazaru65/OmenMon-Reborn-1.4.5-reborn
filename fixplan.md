# OmenMon-Reborn - Threshold Apply (Auto-Revert Safety) Düzeltme Planı

Bu belge, Manuel (Constant) fan modundayken belirlenen sıcaklık eşiğine (Threshold) ulaşıldığında sistemin Otomatik (Dynamic) fan moduna kalıcı ve sorunsuz bir şekilde geçmesini engelleyen hataların teknik analizini ve çözüm adımlarını içerir.

---

## 1. Problem Tespiti ve Kök Neden Analizi

Mevcut durumda sıcaklık eşiği (`FanConstSafetyTemp`, örn. 85°C) aşıldığında ekrana uyarı overlay'i (`GuiFormOverlay`) gelmekte fakat fanlar Dynamic (BIOS Auto) moda geçmemektedir. Yapılan kod incelemesinde soruna yol açan 3 temel etken saptanmıştır:

### A. Arayüz (GUI) Durumunun Senkronize Edilememesi
- Hardware seviyesinde `RevertToAuto()` çağrıldıktan sonra GUI'yi güncellemek üzere `SyncAfterRevert()` metodu tetiklenmektedir (`App/Gui/GuiFormMain.cs`).
- `SyncAfterRevert()` metodu doğrudan `UpdateFanCtl()` metodunu çağırmaktadır.
- `UpdateFanCtl()` metodu radyo butonlarının durumunu belirlerken:
  ```csharp
  else if(this.TrkFan0Lvl.Enabled)
      this.RdoFanConst.Checked = true;
  ```
  mantığını kullanır. Constant moddayken trackbar'lar (`TrkFan0Lvl.Enabled`) kilitli olmadığından (enabled = true) ve `SyncAfterRevert()` esnasında kapatılmadığından, `UpdateFanCtl()` arayüzde `RdoFanConst.Checked = true` durumunu **tekrar aktif hale getirir**. `RdoFanAuto.Checked = true` hiçbir zaman seçilmez.

### B. `UpdateFan` İzleme Döngüsünün Donanımı Tekrar Constant Moda Kilitlemesi
- `App/Gui/GuiFormMain.cs` içerisinde her saniye çalışan `UpdateFan()` metodu (satır 851-854):
  ```csharp
  if(this.RdoFanConst.Checked == true && countdown < Config.UpdateMonitorInterval + Config.FanCountdownExtendThreshold) {
      Context.Op.Platform.Fans.SetMode(Context.Op.Platform.Fans.GetMode());
      Context.Op.Platform.Fans.SetCountdown(Config.FanCountdownExtendInterval);
  }
  ```
  bloğunu içerir. `RdoFanConst.Checked` GUI üzerinde `false` yapılamadığı için, donanım `RevertToAuto()` ile BIOS Auto moda geçirilse bile 1 saniye sonra `UpdateFan()` çalışır ve EC'ye tekrar geri sayım (`SetCountdown`) ve sabit mod (`SetMode`) komutlarını basarak donanımı **tekrar Constant moda kilitler**.

### C. Sistem Tepsisi (Tray) Reapply Döngüsünün Donanıma Müdahalesi
- `App/Gui/GuiTray.cs` içerisinde `Config.FanConstReapplyEnabled` (Sabit hızı periyodik yeniden uygula) seçeneği açıksa:
  `FormMain.IsConstMode` değeri (`RdoFanConst.Checked`) `true` kaldığı için periyodik reapply zamanlayıcısı donanıma sürekli Constant hız seviyelerini (`SetLevels`) yazmaya devam eder.

---

## 2. Çözüm Planı ve Uygulanacak Değişiklikler

### Adım 1: `GuiFormMain.cs` Senkronizasyon Metodunun (`SyncAfterRevert`) Güncellenmesi
`App/Gui/GuiFormMain.cs` dosyasındaki `SyncAfterRevert()` metodu, sadece donanımdaki durumu sorgulayan `UpdateFanCtl()` metoduna bel bağlamak yerine GUI elemanlarının durumunu doğrudan Otomatik moda dönüştürecektir:

1. **Radyo Butonu Değişimi:** `this.RdoFanAuto.Checked = true;` atanacak. Bu işlem WinForms olay zincirinde `EventFanRdoChanged` tetikleyerek `TrkFan0Lvl.Enabled = false` ve `TrkFan1Lvl.Enabled = false` olmasını sağlayacaktır.
2. **Trackbar ve UI Kilitleme:** Trackbar'ların pasife çekildiği ve `BtnFanSet.Checked = false` olduğu doğrulanacak.
3. **`UpdateFanCtl()` Çağrısı:** Arayüz bileşenleri güncellendikten sonra `UpdateFanCtl()` çağrılarak tam görünüm senkronizasyonu tamamlanacaktır.

### Adım 2: `GuiOp.cs` / `RevertToAuto()` Kontrollerinin Güçlendirilmesi
`App/Gui/GuiOp.cs` içerisindeki `RevertToAuto()` metodu incelenip:
1. `Program.Terminate()` ile çalışan fan programlarının kapatıldığından emin olunacak.
2. EC / WMI seviyesinde `SetLevels(new byte[] { 0xFF, 0xFF })`, `SetManual(false)` (destekleyen modellerde) ve `SetCountdown(0)` komutlarının sırasıyla çalıştırıldığı doğrulanacak.
3. `SyncAfterRevert()` çağrısının UI thread üzerinde güvenli bir şekilde `Invoke` edildiğinden emin olunacak.

### Adım 3: Arka Plan (Tray-Only) Senkronizasyon Güvenliği
Form kapalı veya gizli durumdayken Tray simgesi üzerinden Auto-Revert gerçekleştiğinde:
1. `Context.FormMain != null` ise Form görünür olmasa dahi `FormMain.SyncAfterRevert()` çağrılarak arayüzdeki `RdoFanConst` radyo butonu sıfırlanacaktır.
2. Böylece form sonradan açılsa bile ekranda Auto mod seçili kalacaktır.

---

## 3. Test ve Doğrulama Adımları

Değişiklikler uygulandıktan sonra aşağıdaki senaryolar test edilecektir:

1. **GUI Açıkken Test:**
   - Eşik sıcaklığı düşük bir değere (örn. 50°C) ayarlanır.
   - Fan modu "Constant Mode" yapılır ve "Set" butonuna basılır.
   - Sıcaklık eşiği aşıldığı an ekranda şeffaf kırmızı uyarının (`GuiFormOverlay`) çıkıp 3 saniye sonra kaybolduğu,
   - Arayüzde "Constant Mode" radyo butonunun kalkıp "Auto Mode" radyo butonunun seçildiği,
   - Fanların BIOS'un otomatik sıcaklık eğrisine devredildiği ve devir düşüp/yükselmesinin BIOS kontrolünde olduğu doğrulanır.

2. **Sistem Tepsisinde (Tray Modunda) Test:**
   - Ana pencere kapatılır/küçültülür.
   - Eşik sıcaklık aşıldığında oyun içi overlay uyarısının geldiği ve arka planda Constant modun iptal edildiği doğrulanır.
   - Ana pencere tekrar açıldığında radyo butonunun "Auto" konumunda olduğu kontrol edilir.
