using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzureTTS;

public partial class AppConfig : ObservableObject
{
    public static readonly string FileName = "AppConfig.json";

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
        File.WriteAllText(FileName, JsonSerializer.Serialize(this));
    }

    public static AppConfig Load()
    {
        if (File.Exists(FileName))
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(FileName)) ?? new();

        return new AppConfig();
    }
}
