using System.Collections.Generic;
using System.Globalization;

namespace Insait_Audio_Player.Localization;

public sealed class UiLocalizer
{
    public static class Keys
    {
        public const string AppTitle = nameof(AppTitle);
        public const string PlaylistTitle = nameof(PlaylistTitle);
        public const string Minimize = nameof(Minimize);
        public const string Close = nameof(Close);
        public const string PreviousTrack = nameof(PreviousTrack);
        public const string PlayPause = nameof(PlayPause);
        public const string NextTrack = nameof(NextTrack);
        public const string SeekBackward = nameof(SeekBackward);
        public const string SeekForward = nameof(SeekForward);
        public const string Shuffle = nameof(Shuffle);
        public const string Repeat = nameof(Repeat);
        public const string PlaybackRate = nameof(PlaybackRate);
        public const string SetDefaultMp3 = nameof(SetDefaultMp3);
        public const string SetDefaultMp3Short = nameof(SetDefaultMp3Short);
        public const string OpenMp3File = nameof(OpenMp3File);
        public const string AddMp3Files = nameof(AddMp3Files);
        public const string SavePlaylistNow = nameof(SavePlaylistNow);
        public const string ClearPlaylist = nameof(ClearPlaylist);
        public const string PlaylistEncryptedInfo = nameof(PlaylistEncryptedInfo);
        public const string AddMp3Placeholder = nameof(AddMp3Placeholder);
        public const string PlaybackBackendDescription = nameof(PlaybackBackendDescription);
        public const string PlaylistAutoSaved = nameof(PlaylistAutoSaved);
        public const string PlaylistSummaryFormat = nameof(PlaylistSummaryFormat);
        public const string LocalMp3 = nameof(LocalMp3);
        public const string FolderFormat = nameof(FolderFormat);
        public const string PlaylistEncryptedNoPassword = nameof(PlaylistEncryptedNoPassword);
        public const string SelectSingleMp3 = nameof(SelectSingleMp3);
        public const string SelectMultipleMp3 = nameof(SelectMultipleMp3);
        public const string Mp3FilesLabel = nameof(Mp3FilesLabel);
        public const string PauseFailed = nameof(PauseFailed);
        public const string ResumeFailed = nameof(ResumeFailed);
        public const string TrackNotFound = nameof(TrackNotFound);
        public const string RewindFailed = nameof(RewindFailed);
        public const string UnsupportedPlaybackRate = nameof(UnsupportedPlaybackRate);
        public const string OpenMp3Failed = nameof(OpenMp3Failed);
        public const string StartPlaybackFailed = nameof(StartPlaybackFailed);
        public const string SliderSeekFailed = nameof(SliderSeekFailed);
        public const string DefaultMp3DialogOpened = nameof(DefaultMp3DialogOpened);
        public const string DefaultMp3SettingsOpened = nameof(DefaultMp3SettingsOpened);
        public const string DefaultMp3RegisteredOnly = nameof(DefaultMp3RegisteredOnly);
        public const string DefaultMp3NotSupported = nameof(DefaultMp3NotSupported);
        public const string DefaultMp3Failed = nameof(DefaultMp3Failed);
        public const string ChangeLanguage = nameof(ChangeLanguage);
        public const string AgreementWindowTitle = nameof(AgreementWindowTitle);
        public const string AgreementTitle = nameof(AgreementTitle);
        public const string AgreementSubtitle = nameof(AgreementSubtitle);
        public const string AgreementBody = nameof(AgreementBody);
        public const string AgreementFooter = nameof(AgreementFooter);
        public const string AgreementAccept = nameof(AgreementAccept);
        public const string AgreementDecline = nameof(AgreementDecline);
    }

    public static readonly string[] SupportedLanguageCodes = ["en", "de", "ru", "tr", "uk"];

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        [Keys.AppTitle] = "Insait Audio Player",
        [Keys.PlaylistTitle] = "Playlist",
        [Keys.Minimize] = "Minimize",
        [Keys.Close] = "Close",
        [Keys.PreviousTrack] = "Previous track",
        [Keys.PlayPause] = "Play / Pause",
        [Keys.NextTrack] = "Next track",
        [Keys.SeekBackward] = "Seek 10 seconds backward",
        [Keys.SeekForward] = "Seek 10 seconds forward",
        [Keys.Shuffle] = "Shuffle",
        [Keys.Repeat] = "Repeat",
        [Keys.PlaybackRate] = "Change playback speed",
        [Keys.SetDefaultMp3] = "Make Insait Audio Player the default app for .mp3",
        [Keys.SetDefaultMp3Short] = "MP3 default",
        [Keys.OpenMp3File] = "Open MP3 file",
        [Keys.AddMp3Files] = "Add MP3 files",
        [Keys.SavePlaylistNow] = "Save playlist now",
        [Keys.ClearPlaylist] = "Clear playlist",
        [Keys.PlaylistEncryptedInfo] = "Playlist is encrypted in SQLite",
        [Keys.AddMp3Placeholder] = "Add an MP3 file",
        [Keys.PlaybackBackendDescription] = "Playback via Windows audio backends",
        [Keys.PlaylistAutoSaved] = "The playlist will be automatically saved to SQLite",
        [Keys.PlaylistSummaryFormat] = "{0} tracks • {1} total",
        [Keys.LocalMp3] = "Local MP3",
        [Keys.FolderFormat] = "Folder: {0}",
        [Keys.PlaylistEncryptedNoPassword] = "SQLite stores the encrypted playlist without a password",
        [Keys.SelectSingleMp3] = "Select MP3 file",
        [Keys.SelectMultipleMp3] = "Select MP3 files",
        [Keys.Mp3FilesLabel] = "MP3 files",
        [Keys.PauseFailed] = "Could not pause the track.",
        [Keys.ResumeFailed] = "Could not resume playback.",
        [Keys.TrackNotFound] = "File not found. Remove the track from the playlist or add it again.",
        [Keys.RewindFailed] = "Could not seek the track.",
        [Keys.UnsupportedPlaybackRate] = "This MP3 does not support the selected playback speed.",
        [Keys.OpenMp3Failed] = "Could not open the MP3 file through Windows audio backends.",
        [Keys.StartPlaybackFailed] = "Could not start playback.",
        [Keys.SliderSeekFailed] = "Could not seek the track with the timeline slider.",
        [Keys.DefaultMp3DialogOpened] = "Windows opened the default-app chooser. Select Insait Audio Player for .mp3.",
        [Keys.DefaultMp3SettingsOpened] = "Windows Settings opened. Choose Insait Audio Player as the default app for .mp3.",
        [Keys.DefaultMp3RegisteredOnly] = "The player was registered for .mp3. If settings did not open, choose it manually in Default apps.",
        [Keys.DefaultMp3NotSupported] = "This feature is available only on Windows.",
        [Keys.DefaultMp3Failed] = "Could not prepare the app for .mp3 defaults: {0}",
        [Keys.ChangeLanguage] = "Change language",
        [Keys.AgreementWindowTitle] = "User Agreement",
        [Keys.AgreementTitle] = "User Agreement",
        [Keys.AgreementSubtitle] = "Please review and accept the terms before using the application.",
        [Keys.AgreementBody] = "This application is provided \"as is\", without warranty of any kind, express or implied.\n\nThe author and distributor make no guarantees regarding reliability, fitness for a particular purpose, availability, data safety, uninterrupted operation, or compatibility with your system.\n\nBy continuing to use this application, you acknowledge that you do so at your own risk. You are responsible for your files, settings, playlists, backups, and any consequences resulting from installation or use.",
        [Keys.AgreementFooter] = "If you do not agree with these terms, please exit the application.",
        [Keys.AgreementAccept] = "I Accept",
        [Keys.AgreementDecline] = "Exit"
    };

    private static readonly IReadOnlyDictionary<string, string> German = new Dictionary<string, string>
    {
        [Keys.AppTitle] = "Insait Audio Player",
        [Keys.PlaylistTitle] = "Wiedergabeliste",
        [Keys.Minimize] = "Minimieren",
        [Keys.Close] = "Schließen",
        [Keys.PreviousTrack] = "Vorheriger Titel",
        [Keys.PlayPause] = "Wiedergabe / Pause",
        [Keys.NextTrack] = "Nächster Titel",
        [Keys.SeekBackward] = "10 Sekunden zurückspulen",
        [Keys.SeekForward] = "10 Sekunden vorspulen",
        [Keys.Shuffle] = "Zufallswiedergabe",
        [Keys.Repeat] = "Wiederholen",
        [Keys.PlaybackRate] = "Wiedergabegeschwindigkeit ändern",
        [Keys.SetDefaultMp3] = "Insait Audio Player als Standard-App für .mp3 festlegen",
        [Keys.SetDefaultMp3Short] = "Für MP3",
        [Keys.OpenMp3File] = "MP3-Datei öffnen",
        [Keys.AddMp3Files] = "MP3-Dateien hinzufügen",
        [Keys.SavePlaylistNow] = "Wiedergabeliste jetzt speichern",
        [Keys.ClearPlaylist] = "Wiedergabeliste leeren",
        [Keys.PlaylistEncryptedInfo] = "Die Wiedergabeliste ist in SQLite verschlüsselt",
        [Keys.AddMp3Placeholder] = "MP3-Datei hinzufügen",
        [Keys.PlaybackBackendDescription] = "Wiedergabe über Windows-Audio-Backends",
        [Keys.PlaylistAutoSaved] = "Die Wiedergabeliste wird automatisch in SQLite gespeichert",
        [Keys.PlaylistSummaryFormat] = "{0} Titel • {1} gesamt",
        [Keys.LocalMp3] = "Lokale MP3",
        [Keys.FolderFormat] = "Ordner: {0}",
        [Keys.PlaylistEncryptedNoPassword] = "SQLite speichert die verschlüsselte Wiedergabeliste ohne Passwort",
        [Keys.SelectSingleMp3] = "MP3-Datei auswählen",
        [Keys.SelectMultipleMp3] = "MP3-Dateien auswählen",
        [Keys.Mp3FilesLabel] = "MP3-Dateien",
        [Keys.PauseFailed] = "Der Titel konnte nicht pausiert werden.",
        [Keys.ResumeFailed] = "Die Wiedergabe konnte nicht fortgesetzt werden.",
        [Keys.TrackNotFound] = "Datei nicht gefunden. Entfernen Sie den Titel aus der Wiedergabeliste oder fügen Sie ihn erneut hinzu.",
        [Keys.RewindFailed] = "Der Titel konnte nicht gespult werden.",
        [Keys.UnsupportedPlaybackRate] = "Diese MP3 unterstützt die gewählte Wiedergabegeschwindigkeit nicht.",
        [Keys.OpenMp3Failed] = "Die MP3-Datei konnte über die Windows-Audio-Backends nicht geöffnet werden.",
        [Keys.StartPlaybackFailed] = "Die Wiedergabe konnte nicht gestartet werden.",
        [Keys.SliderSeekFailed] = "Der Titel konnte mit dem Timeline-Schieberegler nicht gespult werden.",
        [Keys.DefaultMp3DialogOpened] = "Windows hat den Standard-App-Dialog geöffnet. Wählen Sie Insait Audio Player für .mp3.",
        [Keys.DefaultMp3SettingsOpened] = "Die Windows-Einstellungen wurden geöffnet. Wählen Sie Insait Audio Player als Standard-App für .mp3.",
        [Keys.DefaultMp3RegisteredOnly] = "Der Player wurde für .mp3 registriert. Wenn sich die Einstellungen nicht geöffnet haben, wählen Sie ihn manuell unter Standard-Apps aus.",
        [Keys.DefaultMp3NotSupported] = "Diese Funktion ist nur unter Windows verfügbar.",
        [Keys.DefaultMp3Failed] = "Die Standard-App für .mp3 konnte nicht vorbereitet werden: {0}",
        [Keys.ChangeLanguage] = "Sprache ändern",
        [Keys.AgreementWindowTitle] = "Nutzungsvereinbarung",
        [Keys.AgreementTitle] = "Nutzungsvereinbarung",
        [Keys.AgreementSubtitle] = "Bitte lesen und akzeptieren Sie die Bedingungen, bevor Sie die Anwendung verwenden.",
        [Keys.AgreementBody] = "Diese Anwendung wird \"wie besehen\" ohne jegliche ausdrückliche oder stillschweigende Gewährleistung bereitgestellt.\n\nDer Autor und der Vertrieb übernehmen keine Garantie hinsichtlich Zuverlässigkeit, Eignung für einen bestimmten Zweck, Verfügbarkeit, Datensicherheit, unterbrechungsfreiem Betrieb oder Kompatibilität mit Ihrem System.\n\nWenn Sie diese Anwendung weiter verwenden, erkennen Sie an, dass dies auf eigenes Risiko geschieht. Sie sind selbst für Ihre Dateien, Einstellungen, Wiedergabelisten, Sicherungen und alle Folgen verantwortlich, die aus Installation oder Nutzung entstehen.",
        [Keys.AgreementFooter] = "Wenn Sie diesen Bedingungen nicht zustimmen, beenden Sie bitte die Anwendung.",
        [Keys.AgreementAccept] = "Ich stimme zu",
        [Keys.AgreementDecline] = "Beenden"
    };

    private static readonly IReadOnlyDictionary<string, string> Russian = new Dictionary<string, string>
    {
        [Keys.AppTitle] = "Insait Audio Player",
        [Keys.PlaylistTitle] = "Плейлист",
        [Keys.Minimize] = "Свернуть",
        [Keys.Close] = "Закрыть",
        [Keys.PreviousTrack] = "Предыдущий трек",
        [Keys.PlayPause] = "Воспроизведение / Пауза",
        [Keys.NextTrack] = "Следующий трек",
        [Keys.SeekBackward] = "Перемотать на 10 секунд назад",
        [Keys.SeekForward] = "Перемотать на 10 секунд вперед",
        [Keys.Shuffle] = "Перемешать",
        [Keys.Repeat] = "Повторять",
        [Keys.PlaybackRate] = "Изменить скорость воспроизведения",
        [Keys.SetDefaultMp3] = "Сделать Insait Audio Player приложением по умолчанию для .mp3",
        [Keys.SetDefaultMp3Short] = "Для MP3",
        [Keys.OpenMp3File] = "Открыть MP3 файл",
        [Keys.AddMp3Files] = "Добавить MP3 файлы",
        [Keys.SavePlaylistNow] = "Сохранить плейлист сейчас",
        [Keys.ClearPlaylist] = "Очистить плейлист",
        [Keys.PlaylistEncryptedInfo] = "Плейлист шифруется в SQLite",
        [Keys.AddMp3Placeholder] = "Добавьте MP3 файл",
        [Keys.PlaybackBackendDescription] = "Воспроизведение через аудио-бэкенды Windows",
        [Keys.PlaylistAutoSaved] = "Плейлист будет автоматически сохранён в SQLite",
        [Keys.PlaylistSummaryFormat] = "{0} треков • {1} всего",
        [Keys.LocalMp3] = "Локальный MP3",
        [Keys.FolderFormat] = "Папка: {0}",
        [Keys.PlaylistEncryptedNoPassword] = "SQLite хранит зашифрованный плейлист без пароля",
        [Keys.SelectSingleMp3] = "Выберите MP3 файл",
        [Keys.SelectMultipleMp3] = "Выберите MP3 файлы",
        [Keys.Mp3FilesLabel] = "MP3 файлы",
        [Keys.PauseFailed] = "Не удалось поставить трек на паузу.",
        [Keys.ResumeFailed] = "Не удалось продолжить воспроизведение.",
        [Keys.TrackNotFound] = "Файл не найден. Удалите трек из плейлиста или добавьте его снова.",
        [Keys.RewindFailed] = "Не удалось перемотать трек.",
        [Keys.UnsupportedPlaybackRate] = "Этот MP3 не поддерживает выбранную скорость воспроизведения.",
        [Keys.OpenMp3Failed] = "Не удалось открыть MP3 файл через аудио-бэкенды Windows.",
        [Keys.StartPlaybackFailed] = "Не удалось начать воспроизведение.",
        [Keys.SliderSeekFailed] = "Не удалось перемотать трек ползунком таймлайна.",
        [Keys.DefaultMp3DialogOpened] = "Windows открыл окно выбора приложения по умолчанию. Выберите Insait Audio Player для .mp3.",
        [Keys.DefaultMp3SettingsOpened] = "Открыты параметры Windows. Выберите Insait Audio Player приложением по умолчанию для .mp3.",
        [Keys.DefaultMp3RegisteredOnly] = "Плеер зарегистрирован для .mp3. Если параметры не открылись, выберите его вручную в разделе приложений по умолчанию.",
        [Keys.DefaultMp3NotSupported] = "Эта функция доступна только в Windows.",
        [Keys.DefaultMp3Failed] = "Не удалось подготовить приложение для .mp3 по умолчанию: {0}",
        [Keys.ChangeLanguage] = "Сменить язык",
        [Keys.AgreementWindowTitle] = "Пользовательское соглашение",
        [Keys.AgreementTitle] = "Пользовательское соглашение",
        [Keys.AgreementSubtitle] = "Перед использованием приложения ознакомьтесь и примите условия.",
        [Keys.AgreementBody] = "Это приложение предоставляется \"как есть\", без каких-либо явных или подразумеваемых гарантий.\n\nАвтор и распространитель не дают гарантий в отношении надёжности, пригодности для конкретной цели, доступности, безопасности данных, непрерывной работы или совместимости с вашей системой.\n\nПродолжая использовать это приложение, вы подтверждаете, что делаете это на свой страх и риск. Вы сами несёте ответственность за свои файлы, настройки, плейлисты, резервные копии и любые последствия установки или использования.",
        [Keys.AgreementFooter] = "Если вы не согласны с этими условиями, пожалуйста, закройте приложение.",
        [Keys.AgreementAccept] = "Принимаю",
        [Keys.AgreementDecline] = "Выйти"
    };

    private static readonly IReadOnlyDictionary<string, string> Turkish = new Dictionary<string, string>
    {
        [Keys.AppTitle] = "Insait Audio Player",
        [Keys.PlaylistTitle] = "Çalma listesi",
        [Keys.Minimize] = "Simge durumuna küçült",
        [Keys.Close] = "Kapat",
        [Keys.PreviousTrack] = "Önceki parça",
        [Keys.PlayPause] = "Oynat / Duraklat",
        [Keys.NextTrack] = "Sonraki parça",
        [Keys.SeekBackward] = "10 saniye geri sar",
        [Keys.SeekForward] = "10 saniye ileri sar",
        [Keys.Shuffle] = "Karıştır",
        [Keys.Repeat] = "Tekrarla",
        [Keys.PlaybackRate] = "Çalma hızını değiştir",
        [Keys.SetDefaultMp3] = "Insait Audio Player'ı .mp3 için varsayılan uygulama yap",
        [Keys.SetDefaultMp3Short] = "MP3 için",
        [Keys.OpenMp3File] = "MP3 dosyası aç",
        [Keys.AddMp3Files] = "MP3 dosyaları ekle",
        [Keys.SavePlaylistNow] = "Çalma listesini şimdi kaydet",
        [Keys.ClearPlaylist] = "Çalma listesini temizle",
        [Keys.PlaylistEncryptedInfo] = "Çalma listesi SQLite içinde şifrelenir",
        [Keys.AddMp3Placeholder] = "Bir MP3 dosyası ekleyin",
        [Keys.PlaybackBackendDescription] = "Çalma Windows ses arka uçları üzerinden yapılır",
        [Keys.PlaylistAutoSaved] = "Çalma listesi otomatik olarak SQLite'a kaydedilecek",
        [Keys.PlaylistSummaryFormat] = "{0} parça • toplam {1}",
        [Keys.LocalMp3] = "Yerel MP3",
        [Keys.FolderFormat] = "Klasör: {0}",
        [Keys.PlaylistEncryptedNoPassword] = "SQLite şifrelenmiş çalma listesini parola olmadan saklar",
        [Keys.SelectSingleMp3] = "MP3 dosyası seç",
        [Keys.SelectMultipleMp3] = "MP3 dosyalarını seç",
        [Keys.Mp3FilesLabel] = "MP3 dosyaları",
        [Keys.PauseFailed] = "Parça duraklatılamadı.",
        [Keys.ResumeFailed] = "Çalma devam ettirilemedi.",
        [Keys.TrackNotFound] = "Dosya bulunamadı. Parçayı çalma listesinden kaldırın veya yeniden ekleyin.",
        [Keys.RewindFailed] = "Parça sarılamadı.",
        [Keys.UnsupportedPlaybackRate] = "Bu MP3 seçilen çalma hızını desteklemiyor.",
        [Keys.OpenMp3Failed] = "MP3 dosyası Windows ses arka uçları ile açılamadı.",
        [Keys.StartPlaybackFailed] = "Çalma başlatılamadı.",
        [Keys.SliderSeekFailed] = "Parça zaman çizelgesi kaydırıcısıyla sarılamadı.",
        [Keys.DefaultMp3DialogOpened] = "Windows varsayılan uygulama seçim penceresini açtı. .mp3 için Insait Audio Player'ı seçin.",
        [Keys.DefaultMp3SettingsOpened] = "Windows Ayarları açıldı. .mp3 için varsayılan uygulama olarak Insait Audio Player'ı seçin.",
        [Keys.DefaultMp3RegisteredOnly] = "Oynatıcı .mp3 için kaydedildi. Ayarlar açılmadıysa Varsayılan uygulamalar bölümünden elle seçin.",
        [Keys.DefaultMp3NotSupported] = "Bu özellik yalnızca Windows'ta kullanılabilir.",
        [Keys.DefaultMp3Failed] = ".mp3 varsayılanları için uygulama hazırlanamadı: {0}",
        [Keys.ChangeLanguage] = "Dili değiştir",
        [Keys.AgreementWindowTitle] = "Kullanıcı Sözleşmesi",
        [Keys.AgreementTitle] = "Kullanıcı Sözleşmesi",
        [Keys.AgreementSubtitle] = "Uygulamayı kullanmadan önce lütfen şartları inceleyin ve kabul edin.",
        [Keys.AgreementBody] = "Bu uygulama, açık veya zımni hiçbir garanti olmaksızın \"olduğu gibi\" sunulmaktadır.\n\nYazar ve dağıtıcı; güvenilirlik, belirli bir amaca uygunluk, kullanılabilirlik, veri güvenliği, kesintisiz çalışma veya sisteminizle uyumluluk konusunda hiçbir garanti vermez.\n\nBu uygulamayı kullanmaya devam ederek, bunu tamamen kendi sorumluluğunuzda yaptığınızı kabul etmiş olursunuz. Dosyalarınız, ayarlarınız, çalma listeleriniz, yedekleriniz ve kurulum ya da kullanım sonucunda doğabilecek tüm sonuçlardan siz sorumlusunuz.",
        [Keys.AgreementFooter] = "Bu şartları kabul etmiyorsanız lütfen uygulamadan çıkın.",
        [Keys.AgreementAccept] = "Kabul ediyorum",
        [Keys.AgreementDecline] = "Çıkış"
    };

    private static readonly IReadOnlyDictionary<string, string> Ukrainian = new Dictionary<string, string>
    {
        [Keys.AppTitle] = "Insait Audio Player",
        [Keys.PlaylistTitle] = "Плейлист",
        [Keys.Minimize] = "Згорнути",
        [Keys.Close] = "Закрити",
        [Keys.PreviousTrack] = "Попередній трек",
        [Keys.PlayPause] = "Відтворення / Пауза",
        [Keys.NextTrack] = "Наступний трек",
        [Keys.SeekBackward] = "Перемотати на 10 секунд назад",
        [Keys.SeekForward] = "Перемотати на 10 секунд вперед",
        [Keys.Shuffle] = "Перемішати",
        [Keys.Repeat] = "Повторювати",
        [Keys.PlaybackRate] = "Змінити швидкість відтворення",
        [Keys.SetDefaultMp3] = "Зробити Insait Audio Player застосунком за замовчуванням для .mp3",
        [Keys.SetDefaultMp3Short] = "Для MP3",
        [Keys.OpenMp3File] = "Відкрити MP3 файл",
        [Keys.AddMp3Files] = "Додати MP3 файли",
        [Keys.SavePlaylistNow] = "Зберегти плейлист зараз",
        [Keys.ClearPlaylist] = "Очистити плейлист",
        [Keys.PlaylistEncryptedInfo] = "Плейлист шифрується в SQLite",
        [Keys.AddMp3Placeholder] = "Додайте MP3 файл",
        [Keys.PlaybackBackendDescription] = "Відтворення через аудіо-бекенди Windows",
        [Keys.PlaylistAutoSaved] = "Плейлист буде автоматично збережено у SQLite",
        [Keys.PlaylistSummaryFormat] = "{0} треків • {1} загалом",
        [Keys.LocalMp3] = "Локальний MP3",
        [Keys.FolderFormat] = "Папка: {0}",
        [Keys.PlaylistEncryptedNoPassword] = "SQLite зберігає зашифрований плейлист без пароля",
        [Keys.SelectSingleMp3] = "Оберіть MP3 файл",
        [Keys.SelectMultipleMp3] = "Оберіть MP3 файли",
        [Keys.Mp3FilesLabel] = "MP3 файли",
        [Keys.PauseFailed] = "Не вдалося поставити трек на паузу.",
        [Keys.ResumeFailed] = "Не вдалося відновити відтворення.",
        [Keys.TrackNotFound] = "Файл не знайдено. Видаліть трек з плейлиста або додайте його знову.",
        [Keys.RewindFailed] = "Не вдалося перемотати трек.",
        [Keys.UnsupportedPlaybackRate] = "Цей MP3 не підтримує обрану швидкість відтворення.",
        [Keys.OpenMp3Failed] = "Не вдалося відкрити MP3 файл через аудіо-бекенди Windows.",
        [Keys.StartPlaybackFailed] = "Не вдалося розпочати відтворення.",
        [Keys.SliderSeekFailed] = "Не вдалося перемотати трек повзунком таймлайна.",
        [Keys.DefaultMp3DialogOpened] = "Windows відкрив вікно вибору застосунку за замовчуванням. Оберіть Insait Audio Player для .mp3.",
        [Keys.DefaultMp3SettingsOpened] = "Відкрито параметри Windows. Оберіть Insait Audio Player застосунком за замовчуванням для .mp3.",
        [Keys.DefaultMp3RegisteredOnly] = "Плеєр зареєстровано для .mp3. Якщо параметри не відкрилися, оберіть його вручну в розділі програм за замовчуванням.",
        [Keys.DefaultMp3NotSupported] = "Ця функція доступна лише у Windows.",
        [Keys.DefaultMp3Failed] = "Не вдалося підготувати застосунок для .mp3 за замовчуванням: {0}",
        [Keys.ChangeLanguage] = "Змінити мову",
        [Keys.AgreementWindowTitle] = "Угода користувача",
        [Keys.AgreementTitle] = "Угода користувача",
        [Keys.AgreementSubtitle] = "Перед використанням застосунку, будь ласка, ознайомтеся з умовами та прийміть їх.",
        [Keys.AgreementBody] = "Цей застосунок постачається \"як є\", без будь-яких явних чи неявних гарантій.\n\nАвтор і розповсюджувач не надають гарантій щодо надійності, придатності для певної мети, доступності, безпеки даних, безперервної роботи або сумісності з вашою системою.\n\nПродовжуючи користуватися цим застосунком, ви підтверджуєте, що робите це на власний ризик. Ви самі несете відповідальність за свої файли, налаштування, плейлисти, резервні копії та будь-які наслідки встановлення чи використання.",
        [Keys.AgreementFooter] = "Якщо ви не погоджуєтеся з цими умовами, будь ласка, закрийте застосунок.",
        [Keys.AgreementAccept] = "Приймаю",
        [Keys.AgreementDecline] = "Вийти"
    };

    private readonly IReadOnlyDictionary<string, string> _activeTranslations;
    public string LanguageCode { get; }

    private UiLocalizer(string languageCode, IReadOnlyDictionary<string, string> activeTranslations)
    {
        LanguageCode = languageCode;
        _activeTranslations = activeTranslations;
    }

    public static UiLocalizer CreateForCurrentCulture()
    {
        return Create("en");
    }

    public static UiLocalizer Create(string? languageCode)
    {
        var language = (languageCode ?? "en").ToLowerInvariant();
        return language switch
        {
            "de" => new UiLocalizer("de", German),
            "ru" => new UiLocalizer("ru", Russian),
            "tr" => new UiLocalizer("tr", Turkish),
            "uk" => new UiLocalizer("uk", Ukrainian),
            _ => new UiLocalizer("en", English)
        };
    }

    public string this[string key] => _activeTranslations.TryGetValue(key, out var value)
        ? value
        : English.TryGetValue(key, out var fallback)
            ? fallback
            : key;

    public string Format(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, this[key], args);
    }
}

