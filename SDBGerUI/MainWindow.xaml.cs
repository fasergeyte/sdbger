namespace SDBGerUI
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;

    using SDBGer;

    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private bool isConsoleVisible = false;

        private bool isLoaded = false;

        private MyLogger logger;

        private DispatcherTimer outputUpdateTimmer;

        private RunnerManager runnerManager;

        #endregion

        #region Constructors and Destructors

        public MainWindow()
        {
            this.InitializeComponent();
            //Console.SetOut(new MyLogger(this.LogOutput));

            // TextWriterTraceListener myWriter = new TextWriterTraceListener(Console.Out);

            // Debug.Listeners.Add(myWriter);

            // Autoscroll for logger.
            this.LogOutput.TextChanged += (sender, e) => this.ScrollOutputTextboxToEnd();

            this.InitLogger();
        }

        #endregion

        #region Properties

        private bool IsRedonlyMode
        {
            set
            {
                this.ActionsPanel.Dispatcher.Invoke(() => this.ActionsPanel.IsEnabled = !value);
            }
        }

        #endregion

        #region Public Methods and Operators

        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        [DllImport("Kernel32")]
        public static extern void FreeConsole();

        #endregion

        #region Methods

        private void BeforeFeature_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.BeforeFeature);
        }

        private void BeforeScenario_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.BeforeScenario);
        }

        private void Build_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.Build);
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            this.LogOutput.Clear();
        }

        private void ExecuteCommand(string command, params object[] parameters)
        {
            this.IsRedonlyMode = true;
            Task.Run(() =>
            {
                try
                {
                    this.runnerManager.ExecuteCommand(command, parameters);
                }
                catch (Exception e)
                {
                    this.logger.Error(e);
                }

                this.IsRedonlyMode = false;
            });
        }

        private void InitLogger()
        {
            this.logger = new MyLogger();
            this.runnerManager = new RunnerManager(this.logger);

            // updating of log by timer
            this.outputUpdateTimmer = new DispatcherTimer();
            this.outputUpdateTimmer.Tick += (s, e) => this.UpdateOutput();
            this.outputUpdateTimmer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            this.outputUpdateTimmer.Start();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (!this.isLoaded)
            {
                this.ExecuteCommand(RunnerManager.Commands.Load);
                this.isLoaded = true;
                this.LoadButton.Content = "Release";
            }
            else
            {
                this.ExecuteCommand(RunnerManager.Commands.Release);
                this.isLoaded = false;
                this.LoadButton.Content = "Load";
            }
        }

        private void Rebuild_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.Rebuild);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            this.runnerManager.ScenarioBuffer.Append(this.StepsInput.Text);
            this.runnerManager.Tags.AddRange(
                Regex.Matches(this.StepsInput.Text, "^\\s*@(.*?)\\s*?$", RegexOptions.Multiline)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .ToList());
            this.ExecuteCommand(RunnerManager.Commands.Run);
        }

        private void ScrollOutputTextboxToEnd()
        {
            this.LogOutput.Focus();
            this.LogOutput.CaretIndex = this.LogOutput.Text.Length;
            this.LogOutput.ScrollToEnd();
        }

        private void ShowConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.isConsoleVisible)
            {
                AllocConsole();
                this.isConsoleVisible = true;
                this.ShowConsoleButton.Content = "Hide Console";
            }
            else
            {
                FreeConsole();
                this.isConsoleVisible = false;
                this.ShowConsoleButton.Content = "Show Console";
            }
        }

        private void UpdateOutput()
        {
            if (!this.logger.IsUptodate)
            {
                this.LogOutput.Text = this.logger.Log.ToString();
                this.logger.IsUptodate = true;
            }
        }

        #endregion

        [Serializable]
        private class MyLogger : MarshalByRefObject, ILogger
        {
            #region Constructors and Destructors

            public MyLogger()
            {
                this.Log = new StringBuilder();
            }

            #endregion

            #region Public Properties

            public bool IsUptodate { get; set; }

            public StringBuilder Log { get; set; }

            #endregion

            #region Public Methods and Operators

            public void Error(string st, params object[] parameters)
            {
                this.Write(st, parameters);
            }

            public void Error(Exception exception)
            {
                this.Write(exception.Message + "\n" + exception.StackTrace);
            }

            public void Success(string st, params object[] parameters)
            {
                this.Write(st, parameters);
            }

            public void Trace(string st, params object[] parameters)
            {
                this.Write(st, parameters);
            }

            #endregion

            #region Methods

            private void Write(string value, params object[] parameters)
            {
                if (parameters.Any())
                {
                    this.Log.AppendLine(string.Format(value, parameters));
                }
                else
                {
                    this.Log.AppendLine(value);
                }
                this.IsUptodate = false;
            }

            #endregion
        }
    }
}