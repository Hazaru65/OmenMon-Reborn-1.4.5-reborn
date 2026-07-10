Her konuşmanın başına "Selammmm hazar" yaz.

# OmenMon-Reborn - Proje Rehberi (Routing Guide & Sitemap)

Bu proje HP Omen/Victus dizüstü bilgisayarlar için donanım kontrolü (Fan, Sıcaklık, Klavye Işığı) sağlayan bir C# masaüstü uygulamasıdır (WMI ve Embedded Controller/EC üzerinden çalışır).

## Hızlı Yönlendirme Haritası (Neyi Nerede Bulurum?)

- **Uygulama Girişi ve Arayüz (UI/CLI)**: `App/` dizini
  - **GUI** (Pencereler, Menüler, Sistem Tepsisi vb.): `App/Gui/`
  - **CLI** (Komut satırı argümanları, Probe işlemleri): `App/Cli/`
  - Uygulama başlangıcı ve hata yakalama: `App/App.cs`, `App/Crash.cs`

- **Donanım ve Sensör Kontrolü (İş Mantığı)**: `Hardware/` dizini
  - **EC (Embedded Controller)** okuma/yazma: `Hardware/Ec.cs`, `Hardware/EcData.cs`
  - **BIOS / WMI** etkileşimleri: `Hardware/Bios.cs`, `Hardware/BiosCtl.cs`
  - **Fan ve Sıcaklık** yönetimi: `Hardware/Fan.cs`, `Hardware/FanArray.cs`, `Hardware/FanProgram.cs`
  - **OmenMon-Reborn Özel Yenilikleri (Önemli!)**:
    - Dinamik Cihaz Register Haritalama: `Hardware/Platform.cs`, `Hardware/PlatformPreset.cs`
    - Bilinmeyen cihazlar için Akıllı Tarama: `Hardware/AutoDetector.cs`, `Hardware/EcDiffScanner.cs`

- **Yapılandırma, Veri ve Araçlar**: `Library/` dizini
  - Ayarlar (`OmenMon.xml` yönetimi): `Library/Config.cs`, `Library/ConfigData.cs`
  - Auto-Kalibrasyon yöneticisi: `Library/AutoCal.cs`
  - İşletim sistemi / WMI bildirimleri: `Library/Os.cs`, `Library/WmiEvent.cs`
  - Çoklu dil ve yerelleştirme: `Library/Locale.cs`

- **Sürücü Seviyesi**: `Driver/` dizini
  - Donanıma doğrudan erişimi sağlayan WinRing0 çekirdek sürücüsü (Kernel Driver) sarmalayıcıları.

## Önemli Mimari Kurallar ve İpuçları
- **Dinamik Modeller:** Eski donanıma gömülü (hardcoded) cihaz listesi yerine, XML tabanlı esnek `PlatformPreset` sınıfı kullanılır. Modeller bellekten `Config.Models` ile okunur.
- **Tarama ve Güvenlik:** Bilinmeyen bir cihaz bağlandığında `Hardware/AutoDetector.cs` salt-okunur (read-only) güvenli bir donanım taraması yapar ve riskli register'lara yazma yapmaz.
- **Sorunlu Sensörler:** Cihazların bazılarında (TNT2-TNT5 sensörleri) gerçek dışı sıcaklıklar raporlandığı (ve fanları %100'de kilitlediği) için `ConfigData.TemperatureSensor` üzerinde bunlar varsayılan olarak kapalıdır.