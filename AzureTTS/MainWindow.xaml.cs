using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EleCho.WpfSuite;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Win32;

namespace AzureTTS
{
    public class StreamOutputAudioStreamCallback : PushAudioOutputStreamCallback
    {
        private readonly Stream _stream;

        public StreamOutputAudioStreamCallback(Stream stream)
        {
            _stream = stream;
        }

        public override uint Write(byte[] dataBuffer)
        {
            _stream.Write(dataBuffer, 0, dataBuffer.Length);
            return (uint)dataBuffer.Length;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ObservableObject]
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            if (WindowOption.CanSetBackdrop)
            {
                WindowOption.SetBackdrop(this, WindowBackdrop.Mica);
            }
        }

        [ObservableProperty]
        private string _speechKey = string.Empty;

        [ObservableProperty]
        private string _speechRegion = string.Empty;

        [ObservableProperty]
        private string _textToSpeak = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShownVoices))]
        private LocaleWrapper? _selectedLocale = null;

        [ObservableProperty]
        private VoiceInfo? _selectedVoice = null;

        [ObservableProperty]
        private bool _autoGenerateFileName = false;

        [ObservableProperty]
        private bool _autoOpenFileFolderAfterDownloading = false;

        private SpeechConfig? _currentConfig;
        private SpeechSynthesizer? _currentSpeechSynthesizer;
        private SpeechSynthesisResult? _currentSpeechSynthesisResult;
        private string? _currentSpeechText = null;
        private SaveFileDialog? _saveFileDialog;

        public ObservableCollection<LocaleWrapper?> AllLocales { get; } = new();
        public ObservableCollection<VoiceInfo> AllVoices { get; } = new();

        public IEnumerable<VoiceInfo> ShownVoices
        {
            get
            {
                if (SelectedLocale is null ||
                    SelectedLocale.Culture is null)
                    return AllVoices;

                return AllVoices.Where(voice => voice.Locale == SelectedLocale.Culture.ToString());
            }
        }

        void FreeOldResources()
        {
            if (_currentSpeechSynthesizer is not null)
            {
                _currentSpeechSynthesizer.Dispose();
            }
        }


        [RelayCommand]
        public async Task Play()
        {
            try
            {
                _currentConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
                if (SelectedVoice is not null)
                {
                    _currentConfig.SpeechSynthesisVoiceName = SelectedVoice.Name;
                }

                _currentSpeechSynthesizer = new SpeechSynthesizer(_currentConfig);

                var textToSpeak = TextToSpeak;
                _currentSpeechSynthesisResult = await _currentSpeechSynthesizer.SpeakTextAsync(textToSpeak);
                _currentSpeechText = textToSpeak;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void Stop()
        {
            if (_currentSpeechSynthesizer is not null)
            {
                _ = _currentSpeechSynthesizer.StopSpeakingAsync();
            }
        }

        [RelayCommand]
        public async Task Download()
        {
            try
            {
                string? outputFileName;

                if (AutoGenerateFileName)
                {
                    outputFileName = GenerateFileName(TextToSpeak);
                }
                else
                {
                    var saveFileDialog = _saveFileDialog ??= new SaveFileDialog()
                    {
                        Title = "Save speech",
                        FileName = "Output.wav",
                        CheckPathExists = true,
                        Filter = "WAV File|*.wav",
                    };

                    var dialogResult = saveFileDialog.ShowDialog();
                    if (dialogResult is null || !dialogResult.Value)
                        return;

                    outputFileName = saveFileDialog.FileName;
                }

                using var audioConfig = AudioConfig.FromWavFileOutput(outputFileName);

                _currentConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
                if (SelectedVoice is not null)
                {
                    _currentConfig.SpeechSynthesisVoiceName = SelectedVoice.Name;
                }

                _currentSpeechSynthesizer = new SpeechSynthesizer(_currentConfig, audioConfig);

                var textToSpeak = TextToSpeak;
                _currentSpeechSynthesisResult = await _currentSpeechSynthesizer.SpeakTextAsync(textToSpeak);
                _currentSpeechText = textToSpeak;

                _currentSpeechSynthesizer.Dispose();

                if (AutoOpenFileFolderAfterDownloading)
                {
                    var fullPath = System.IO.Path.GetFullPath(outputFileName);
                    if (System.IO.File.Exists(fullPath))
                    {
                        ShowInFileExplorer(fullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public async Task GetVoices()
        {
            try
            {
                _currentConfig = SpeechConfig.FromSubscription(SpeechKey, SpeechRegion);
                _currentSpeechSynthesizer = new SpeechSynthesizer(_currentConfig);

                var voices = await _currentSpeechSynthesizer.GetVoicesAsync();
                AllVoices.Clear();

                foreach (var voice in voices.Voices)
                {
                    AllVoices.Add(voice);
                }

                var cultures = voices.Voices
                    .Select(voice => new CultureInfo(voice.Locale))
                    .Distinct();

                AllLocales.Clear();
                AllLocales.Add(new LocaleWrapper(null));
                foreach (var culture in cultures)
                    AllLocales.Add(new LocaleWrapper(culture));

                SelectedLocale = AllLocales.FirstOrDefault();
                SelectedVoice = AllVoices.FirstOrDefault();

                OnPropertyChanged(nameof(ShownVoices));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        string GenerateFileName(string text)
        {
            StringBuilder sb = new StringBuilder(text);

            if (!text.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(".wav");
            }

            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                sb.Replace(c, ' ');
            }

            return sb.ToString();
        }

        [DllImport("shell32.dll", ExactSpelling = true)]
        static extern void ILFree(IntPtr pidlList);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr ILCreateFromPathW([MarshalAs(UnmanagedType.LPWStr)] string pszPath);

        [DllImport("shell32.dll")]
        static extern int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cidl, IntPtr[]? apidl, uint dwFlags);

        public void ShowInFileExplorer(string path)
        {
            IntPtr pidlList = ILCreateFromPathW(path);
            if (pidlList == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid path");
            }

            try
            {
                SHOpenFolderAndSelectItems(pidlList, 0, null, 0);
            }
            finally
            {
                ILFree(pidlList);
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}