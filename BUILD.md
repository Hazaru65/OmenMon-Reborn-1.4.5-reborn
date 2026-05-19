# OmenMon Reborn Derleme Rehberi (Build Guide)

Bu doküman, **OmenMon Reborn** projesinin kaynak kodundan nasıl derleneceğini (build) ve test edileceğini açıklamaktadır.

---

## Gereksinimler

Proje **.NET Framework 4.8** hedeflemektedir ve derleme araçları olarak modern .NET CLI veya MSBuild kullanmaktadır. Derleme yapabilmek için aşağıdaki araçlardan birine sahip olmalısınız:

1. **.NET SDK 8.0+** (Tavsiye edilen / En kolay yöntem)
2. **Visual Studio 2022** (Community, Professional, Enterprise) veya **VS Build Tools 2022** (MSBuild yüklü olmalıdır).

> [!IMPORTANT]
> OmenMon donanım kayıtçısı sürücüsünü yüklediği ve doğrudan Embedded Controller (EC) ile iletişim kurduğu için, derlenen uygulamayı çalıştırırken **Yönetici Hakları (Administrator)** gereklidir.

---

## Derleme Yöntemleri

### Yöntem 1: Gömülü veya Yerel .NET SDK ile Derleme (Tavsiye Edilen)

Proje dizininde yer alan `.sdk` altındaki taşınabilir SDK'yı veya sisteminizde yüklü olan .NET SDK'yı kullanarak projeyi tek bir komutla derleyebilirsiniz.

**Sistem Tepsisindeki OmenMon uygulamasını tamamen kapattıktan sonra** terminalde (PowerShell veya CMD) şu komutu çalıştırın:

```powershell
# Gömülü (Embedded) SDK ile Derleme:
.\.sdk\dotnet.exe build -c Release

# Varsa sisteminizde yüklü olan global .NET SDK ile Derleme:
dotnet build -c Release
```

Derleme bittikten sonra çalıştırılabilir dosya ve bağımlılıklar aşağıdaki klasörde oluşacaktır:
`Bin\` klasöründe `OmenMon.exe`, `OmenMon.exe.config` ve bağımlı DLL dosyaları yer alacaktır.

---

### Yöntem 2: `make.cmd` Betiği ve MSBuild ile Derleme

Proje kök dizininde yer alan `make.cmd` dosyası, bilgisayarınızda kurulu olan MSBuild aracını bularak projeyi derlemenizi sağlar.

1. Komut İstemi'ni (CMD) veya PowerShell'i açın.
2. Öncelikle NuGet paketlerini hazırlamak/indirmek için hazırlık adımını çalıştırın:
   ```cmd
   make.cmd prepare
   ```
3. Ardından projeyi derlemek için:
   ```cmd
   make.cmd build
   ```
4. Derleme bittikten sonra temizlik yapmak için:
   ```cmd
   make.cmd clean
   ```

---

## Testlerin Çalıştırılması

Projede bulunan birim testlerini (Unit Tests) çalıştırmak ve her şeyin doğru çalıştığını doğrulamak için:

```powershell
# Testleri derleyip çalıştırır
.\.sdk\dotnet.exe test -c Release
```

---

## Sık Karşılaşılan Sorunlar ve Çözümleri

### 1. Kilitli Dosya Hatası (The process cannot access the file ... OmenMon.exe)
Eğer OmenMon arka planda (saatin yanındaki gizli simgelerde) çalışmaya devam ediyorsa, derleme aracı `Bin\OmenMon.exe` dosyasının üzerine yazamaz ve derleme başarısız olur.
*   **Çözüm:** Sağ alttaki saatin yanındaki OmenMon simgesine sağ tıklayıp **Exit (Çıkış)** butonuna basın veya görev yöneticisinden `OmenMon.exe` işlemini sonlandırıp derlemeyi tekrar deneyin.

### 2. Assembly Binding/Bağımlılık Yükleme Hatası (HRESULT: 0x80131040)
Eğer derlenmiş dosyayı (`OmenMon.exe`) başka bir klasöre taşıdıysanız veya adını değiştirdiyseniz (örneğin `OmenMon_test.exe`), .NET Framework bağımlılık yönlendirmelerini (özellikle `System.Resources.Extensions`) bulamaz.
*   **Çözüm:** `OmenMon.exe` dosyasını çalıştırırken yanında mutlaka kendi adıyla eşleşen `OmenMon.exe.config` yapılandırma dosyasının da bulunduğundan emin olun. Dosya adını değiştirirseniz config dosyasının adını da (örn: `OmenMon_test.exe.config`) değiştirmelisiniz.
