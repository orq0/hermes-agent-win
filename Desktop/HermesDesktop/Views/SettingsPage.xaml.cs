using HermesDesktop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace HermesDesktop.Views;

public sealed partial class SettingsPage : Page
{
    private static readonly ResourceLoader ResourceLoader = new();

    public SettingsPage()
    {
        InitializeComponent();
    }

    public string Provider => HermesEnvironment.DisplayModelProvider;

    public string BaseUrl => HermesEnvironment.DisplayModelBaseUrl;

    public string DefaultModel => HermesEnvironment.DisplayDefaultModel;

    public string HermesHomePath => HermesEnvironment.DisplayHermesHomePath;

    public string HermesConfigPath => HermesEnvironment.DisplayHermesConfigPath;

    public string HermesLogsPath => HermesEnvironment.DisplayHermesLogsPath;

    public string HermesWorkspacePath => HermesEnvironment.DisplayHermesWorkspacePath;

    public string TelegramStatus => HermesEnvironment.TelegramConfigured
        ? ResourceLoader.GetString("StatusDetected")
        : ResourceLoader.GetString("StatusNotDetected");

    public string DiscordStatus => HermesEnvironment.DiscordConfigured
        ? ResourceLoader.GetString("StatusDetected")
        : ResourceLoader.GetString("StatusNotDetected");

    private void OpenHome_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenHermesHome();
    }

    private void OpenConfig_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenConfig();
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenLogs();
    }

    private void OpenWorkspace_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenWorkspace();
    }
}
