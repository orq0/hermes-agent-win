using HermesDesktop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace HermesDesktop.Views;

public sealed partial class DashboardPage : Page
{
    private static readonly ResourceLoader ResourceLoader = new();

    public DashboardPage()
    {
        InitializeComponent();
    }

    public string ModelProvider => HermesEnvironment.DisplayModelProvider;

    public string DefaultModel => HermesEnvironment.DisplayDefaultModel;

    public string BaseUrl => HermesEnvironment.DisplayModelBaseUrl;

    public string HermesHomePath => HermesEnvironment.DisplayHermesHomePath;

    public string HermesConfigPath => HermesEnvironment.DisplayHermesConfigPath;

    public string HermesLogsPath => HermesEnvironment.DisplayHermesLogsPath;

    public string HermesWorkspacePath => HermesEnvironment.DisplayHermesWorkspacePath;

    public string CliState => HermesEnvironment.HermesInstalled
        ? ResourceLoader.GetString("StatusInstalled")
        : ResourceLoader.GetString("StatusMissing");

    public string InstallSummary => HermesEnvironment.HermesInstalled
        ? ResourceLoader.GetString("CliReadySummary")
        : ResourceLoader.GetString("CliMissingSummary");

    public string MessagingState => HermesEnvironment.HasAnyMessagingToken
        ? ResourceLoader.GetString("StatusReady")
        : ResourceLoader.GetString("StatusNeedsSetup");

    public string MessagingSummary => HermesEnvironment.HasAnyMessagingToken
        ? ResourceLoader.GetString("MessagingReadySummary")
        : ResourceLoader.GetString("MessagingSetupSummary");

    public string ModelPort => HermesEnvironment.DisplayModelPort;

    private void LaunchHermesChat_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.LaunchHermesChat();
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenLogs();
    }

    private void OpenConfig_Click(object sender, RoutedEventArgs e)
    {
        HermesEnvironment.OpenConfig();
    }
}
