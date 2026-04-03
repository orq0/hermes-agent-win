using HermesDesktop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace HermesDesktop.Views;

public sealed partial class IntegrationsPage : Page
{
    private static readonly ResourceLoader ResourceLoader = new();

    public IntegrationsPage()
    {
        InitializeComponent();
    }

    public string TelegramStatus => HermesEnvironment.TelegramConfigured
        ? ResourceLoader.GetString("StatusConfigured")
        : ResourceLoader.GetString("StatusNotDetected");

    public string DiscordStatus => HermesEnvironment.DiscordConfigured
        ? ResourceLoader.GetString("StatusConfigured")
        : ResourceLoader.GetString("StatusNotDetected");

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenLogs();
    }

    private void OpenConfig_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenConfig();
    }
}
