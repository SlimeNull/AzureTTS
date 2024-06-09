using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzureTTS;

public partial class AppConfig : ObservableObject
{
    public static readonly string FileName = "AppConfig.json";
    private static string Path => System.IO.Path.Combine(AppContext.BaseDirectory, FileName);

    [ObservableProperty] 
    private string _speechKey = string.Empty;

    [ObservableProperty] 
    private string _speechRegion = string.Empty;

    [ObservableProperty] 
    private bool _autoGenerateFileName = false;

    [ObservableProperty] 
    private bool _autoOpenFileFolderAfterDownloading = false;


    public void Save()
    {
        File.WriteAllText(Path, JsonSerializer.Serialize(this));
    }

    public static AppConfig Load()
    {
        if (File.Exists(FileName))
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(Path)) ?? new();

        return new AppConfig();
    }
}
