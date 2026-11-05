<p align="center"><img src="windows/GTool.Windows/Assets/source/wordmark-dark.webp" width="520" alt="G-Tool"></p>

# G-Tool

Windows için yerel dosya dönüştürme ve düzenleme aracı. Dosyaları sürükle, çarkı aç, ne yapmak istediğini seç. Dosyaların bilgisayarında kalır.

[English](#english)

## Özellikler

- **Dönüştürme:** görsel, belge, ses, video, arşiv ve e-tablo biçimleri arasında dönüştürme
- **Fotoğraf:** kırpma, işaretleme, karartma, çerçeve, kolaj ve renk ayarları
- **Video ve ses:** kırpma, bölme, birleştirme, sesi kapatma ve hız değiştirme
- **PDF:** birleştirme, bölme, sayfa düzenleme ve sıkıştırma
- **Web'den indirme:** herkese açık bağlantılardan medya indirme
- **Tasarım:** duvar kağıdını gösteren cam arayüz, koyu ve açık tema, Türkçe ve İngilizce

## Kurulum

1. [Son sürümden](https://github.com/Sinan0011/G-Tool/releases/latest) `G-Tool-Windows-x64.zip` dosyasını indir.
2. ZIP'in tamamını kendine ait, yazılabilir bir klasöre çıkar. `Program Files` kullanma, yoksa güncellemeler kurulamaz.
3. `G-Tool.exe` dosyasını aç. .NET kurmana gerek yok.

Uygulama henüz bir kod imzalama sertifikasına sahip olmadığı için Windows SmartScreen uyarı gösterebilir.

İlk açılışta "Eksik araçları indir" adımı FFmpeg, yt-dlp, Deno ve LibreOffice gibi araçları kendi resmi kaynaklarından indirir ve parmak izlerini doğrular.

## Güncellemeler

Yeni sürüm çıktığında uygulama açıksa Windows bildirimi gösterir, tek tıkla kurulur. Her güncelleme imzalıdır; imzası doğrulanmayan dosya kurulmaz. İnternet yoksa uygulama normal çalışmaya devam eder. Otomatik kontrol Ayarlar'dan kapatılabilir.

## Kaynak ve lisans

[Orkun Işıtmak](https://github.com/orkunisitmak)'ın [Nashi](https://github.com/orkunisitmak/nashi-app) (MIT) projesi temel alınarak geliştirilmiştir.

MIT lisansıyla dağıtılır, bkz. [LICENSE](LICENSE). Birlikte kullanılan bileşenlerin kendi lisansları vardır, bkz. [Windows üçüncü taraf bildirimleri](windows/THIRD-PARTY-NOTICES.txt) ve [Resources/licenses](Resources/licenses).

---

## English

G-Tool is a local file conversion and editing tool for Windows. Drag files, open the wheel, choose what to do. Your files stay on your computer.

**Install:** download `G-Tool-Windows-x64.zip` from the [latest release](https://github.com/Sinan0011/G-Tool/releases/latest), extract the whole archive into a writable folder of your own (not `Program Files`) and run `G-Tool.exe`. The app is not code-signed yet, so Windows SmartScreen may warn.

**Updates:** while the app is running it notifies you about new versions and installs them with one click. Every update is signed. Without internet the app keeps working normally.

**Source and license:** built on the [Nashi](https://github.com/orkunisitmak/nashi-app) (MIT) project by [Orkun Işıtmak](https://github.com/orkunisitmak). Released under the MIT License, see [LICENSE](LICENSE). Bundled components keep their own licenses, see the [Windows third-party notices](windows/THIRD-PARTY-NOTICES.txt) and [Resources/licenses](Resources/licenses).
