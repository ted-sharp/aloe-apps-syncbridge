using System.Reflection;
using System.Text;
using System.Windows;

namespace Aloe.Apps.DummyWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadApplicationInfo();
        }

        private void LoadApplicationInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            AssemblyNameText.Text = assembly.GetName().Name ?? "Unknown";
            VersionText.Text = assembly.GetName().Version?.ToString() ?? "Unknown";
            WorkingDirectoryText.Text = Environment.CurrentDirectory;

            var args = Environment.GetCommandLineArgs();
            ArgsCountText.Text = $"Arguments Count: {args.Length}";

            var argsBuilder = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                argsBuilder.AppendLine($"[{i}] {args[i]}");
            }
            ArgsTextBox.Text = argsBuilder.ToString();

            LoadEnvironmentVariables();
        }

        private void LoadEnvironmentVariables()
        {
            var envBuilder = new StringBuilder();
            var envVars = Environment.GetEnvironmentVariables();

            var sortedKeys = new List<string>();
            foreach (var key in envVars.Keys)
            {
                sortedKeys.Add(key.ToString() ?? string.Empty);
            }
            sortedKeys.Sort();

            foreach (var key in sortedKeys)
            {
                var value = envVars[key];
                envBuilder.AppendLine($"{key} = {value}");
            }

            EnvironmentTextBox.Text = envBuilder.ToString();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadApplicationInfo();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}