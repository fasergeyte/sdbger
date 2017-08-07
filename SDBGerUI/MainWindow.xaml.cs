namespace SDBGerUI
{
    using System;
    using System.Linq;
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
                this.CurrentSidTextBlock.Dispatcher.Invoke(() => this.CurrentSidTextBlock.IsReadOnly = value);
            }
        }

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
            this.logger.Log.Clear();
            this.LogOutput.Clear();
        }

        private void ExecuteCommand(string command, params object[] parameters)
        {
            this.ExecuteLongAction(() => { this.runnerManager.ExecuteCommand(command, parameters); });
        }

        private void ExecuteLongAction(Action action)
        {
            this.IsRedonlyMode = true;
            Task.Run(() =>
            {
                try
                {
                    action();
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
                this.ExecuteLongAction(() =>
                {
                    this.runnerManager.Load();
                    this.UpdateSidTextbox();
                });

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

            if (this.CurrentSidTextBlock.Text == string.Empty)
            {
                this.CurrentSidTextBlock.Text = Guid.NewGuid().ToString();
            }
            var sid = this.CurrentSidTextBlock.Text;

            this.ExecuteLongAction(() =>
            {
                this.runnerManager.ExecuteCommand(RunnerManager.Commands.SetScenarioContextValue, "{sid}", sid);
                this.runnerManager.ExecuteCommand(RunnerManager.Commands.Run);
            });
        }

        private void ScrollOutputTextboxToEnd()
        {
            this.LogOutput.Focus();
            this.LogOutput.CaretIndex = this.LogOutput.Text.Length;
            this.LogOutput.ScrollToEnd();
        }

        private void UpdateOutput()
        {
            if (!this.logger.IsUptodate)
            {
                this.LogOutput.Text = this.logger.Log.ToString();
                this.logger.IsUptodate = true;
            }
        }

        private void UpdateSidTextbox()
        {
            this.CurrentSidTextBlock.Dispatcher.Invoke(() =>
                this.CurrentSidTextBlock.Text = this.runnerManager.Runner.GetValueFromScenarioContext("{sid}").ToString()
                );
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
                Console.Beep();
            }

            public void Error(Exception exception)
            {
                this.Write(exception.Message + "\n" + exception.StackTrace);
                Console.Beep();
            }

            // https://social.msdn.microsoft.com/Forums/en-US/3ab17b40-546f-4373-8c08-f0f072d818c9/remotingexception-when-raising-events-across-appdomains?forum=netfxremoting
            public override object InitializeLifetimeService()
            {
                return null;
            }

            public void Success(string st, params object[] parameters)
            {
                this.Write(st, parameters);
                Console.Beep();
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