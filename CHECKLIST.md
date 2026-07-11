# OmenMon-Reborn Auto-Revert & Overlay Geliştirme Kontrol Listesi

## 1. Aşama: Auto-Revert to Dynamic (Constant Mod İptali)
- [x] **Görev 1.1: Config (Ayar) Yapısı**
  - [x] `Library/ConfigData.cs` içerisine `FanConstSafetyEnabled` (bool) ve `FanConstSafetyTemp` (int, varsayılan 85) eklenecek.
  - [x] `Library/Config.cs` içerisindeki statik erişimciler ve property'ler güncellenecek.
- [x] **Görev 1.2: Kullanıcı Arayüzü (GUI)**
  - [x] `App/Gui/GuiFormMainInit.cs` içerisinde "Auto-Revert to Dynamic at [ 85 ] °C" kontrol elemanları (CheckBox, NumericUpDown) oluşturulacak.
  - [x] `App/Gui/GuiFormMain.cs` içerisinde bu elemanların olay yöneticileri (event handlers) ve değer güncellemeleri bağlanacak.
- [x] **Görev 1.3: Arka Plan Mantığı (Tray Uyumlu)**
  - [x] `App/Gui/GuiOp.cs` (veya ilgili fan/sıcaklık döngüsü, örn: `FanProgram`, `GuiTray`) incelenecek.
  - [x] Anlık sıcaklık kontrolü eklenecek.
  - [x] Sıcaklık `FanConstSafetyTemp` değerini geçerse ve cihaz "Constant" moddaysa "Dynamic" (Otomatik Eğri / Default / Auto) moda geçiş sağlanacak.
  - [x] Geçiş esnasında "Topmost Overlay" tetiklenecek.

## 2. Aşama: Topmost Overlay (Oyun İçi Şeffaf Uyarı)
- [x] **Görev 2.1: Yeni Form Oluşturma**
  - [x] `App/Gui/GuiFormOverlay.cs` eklenecek.
  - [x] `TopMost = true`, `FormBorderStyle = None`, `BackColor = Color.Magenta`, `TransparencyKey = Color.Magenta`.
  - [x] `CreateParams` içerisine `WS_EX_LAYERED` (0x80000) ve `WS_EX_TRANSPARENT` (0x20) eklenecek.
- [x] **Görev 2.2: Görsellik ve Davranış**
  - [x] Büyük kırmızı uyarı metni (Label) eklenecek.
  - [x] `ShowWindow(SW_SHOWNOACTIVATE)` API çağrısı entegre edilecek.
  - [x] Kendi kendini kapatan zamanlayıcı (Timer, 3 saniye) eklenecek.

## 3. Aşama: Testler
- [ ] Ayarların okunup yazıldığının doğrulanması.
- [ ] Constant modunda sıcaklık aşıldığında sistemin Dynamic moda sorunsuz geçmesi.
- [ ] Overlay'in odak çalmadan (focus) oyunun üstünde çıkması ve 3 saniye sonra kaybolması.
