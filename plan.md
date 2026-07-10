# OmenMon-Reborn - Fan Güvenliği (Auto-Revert) & Overlay Planı v2

Bu belge, Manuel (Sabit/Constant) fan modunda unutulan cihazların aşırı ısınıp (Thermal Throttle) zarar görmesini engellemek amacıyla eklenecek "Constant Mode İptali (Auto-Revert)" ve "Oyun İçi Uyarı" (Topmost Overlay) sistemlerinin güncellenmiş planını içerir. Mevcut `ThermalPanic` yapısıyla çakışmaması ve arka planda kusursuz çalışması için revize edilmiştir.

## 1. Fikir: Auto-Revert to Dynamic (Constant Mod İptali)
Sistem Sabit (Constant) fan modundayken cihaz yüksek sıcaklıklara ulaşırsa, cihazı 100% fana (Thermal Panic) geçirip gürültü yapmak yerine, Constant modu tamamen iptal edip sistemi varsayılan "Dynamic" (Otomatik Eğri) moduna güvenli bir şekilde devredeceğiz. 

**Not (Recovery - Geri Dönüş Yoktur):** Cihaz soğuduktan sonra eski Constant (Sabit) ayarına *geri dönmeyecektir*. Düşük fan ayarında unutulup oyuna girilmişse, kullanıcının faydasına olan en doğru hareket kontrolü tamamen Dynamic moda kalıcı olarak bırakmaktır. Aksi halde histerezis döngüsü (yo-yo etkisi) ile cihaz sürekli ısınıp soğur.

### Yapılacaklar:
1. **Config (Ayar) Yapısı:**
   - `Library/ConfigData.cs` ve `Library/Config.cs` dosyalarına `FanConstSafetyEnabled` (bool) ve `FanConstSafetyTemp` (int, örn. varsayılan 85°C) ayarları eklenecek.
2. **Kullanıcı Arayüzü (GUI):**
   - `App/Gui/GuiFormMainInit.cs` ve `App/Gui/GuiFormMain.cs` dosyalarında, "Constant Mode" (Sabit Mod) seçeneklerinin yanına (veya altına) "Auto-Revert to Dynamic at [ 85 ] °C" şeklinde bir ayar eklenecek.
3. **Arka Plan Mantığı (Tray Uyumlu):**
   - Kontrol döngüsü, GUI'ye değil doğrudan arka plan işlemlerini yöneten `App/Gui/GuiOp.cs` (veya ilgili Fan/Tray arka plan döngüsüne) eklenecek. Böylece uygulama sadece sistem tepsisinde (Tray) çalışırken bile koruma devrede olacak.
   - Eğer anlık sıcaklık `FanConstSafetyTemp` değerini geçerse ve mod "Constant" ise:
     - Constant Mode iptal edilip "Dynamic" moda geçilecek.
     - Eş zamanlı olarak Overlay uyarısı (Fikir 2) tetiklenecek.

## 2. Fikir: Topmost Overlay (Oyun İçi Şeffaf Uyarı)
Oyun sırasında cihaz koruma amacıyla Dynamic moda kendi kendine geçtiğinde, kullanıcının durumu anlayabilmesi için oyun odak noktasını bozmadan (focus çalmadan) ekranda beliren şeffaf bir uyarı penceresi tasarlanacaktır.

### Yapılacaklar:
1. **Yeni Form Oluşturma (`App/Gui/GuiFormOverlay.cs`):**
   - Form özellikleri:
     - `TopMost = true` (Her zaman en üstte)
     - `FormBorderStyle = None` (Kenarlıksız)
     - `BackColor = Color.Magenta`, `TransparencyKey = Color.Magenta` (Tamamen şeffaf)
     - `CreateParams` ezilerek `WS_EX_LAYERED` (0x80000) ve `WS_EX_TRANSPARENT` (0x20) eklenecek. Bu sayede farenin tıklamaları arkadaki oyuna geçecek.
2. **Görsellik ve Davranış:**
   - Kırmızı font ile "🔥 KRİTİK SICAKLIK: CONSTANT FAN İPTAL EDİLDİ - DYNAMIC MOD AKTİF" şeklinde büyük bir uyarı.
   - Odak çalmamak için `ShowWindow(SW_SHOWNOACTIVATE)` (API çağrısı) ile gösterilecek.
   - Gösterildikten 3-4 saniye sonra kendi kendini yok eden bir Timer barındıracak.

## Test Aşamaları
- [ ] Ayarların kaydedilip okunmasının testi.
- [ ] Düşük bir sıcaklık eşiği (örn: 60°C) girilerek, arka planda (Tray modundayken) Auto-Revert'ün sorunsuz olarak Dynamic'e geçtiğinin test edilmesi.
- [ ] Acil durum anında oyun-içi Overlay'in (odak kaybetmeden) ekrana gelip 3 saniye sonra kaybolduğunun test edilmesi.
