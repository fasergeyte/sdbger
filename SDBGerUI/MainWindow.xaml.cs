namespace SDBGerUI
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;

    using SDBGer;

    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields

        private Thread currentExecutingThread;

        private bool isRedonlyMode = false;

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

        #region Public Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Properties

        public Thread CurrentExecutingThread
        {
            get
            {
                return this.currentExecutingThread;
            }
            set
            {
                this.currentExecutingThread = value;
                this.OnPropertyChanged("CurrentExecutingThread");
            }
        }

        public bool IsLoaded
        {
            get
            {
                return this.runnerManager.Runner.IsLoaded;
            }
        }

        public bool IsRedonlyMode
        {
            get
            {
                return this.isRedonlyMode;
            }
            set
            {
                this.isRedonlyMode = value;
                this.OnPropertyChanged("IsRedonlyMode");
            }
        }

        #endregion

        #region Methods

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

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

        private void CancelButton_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            this.CurrentExecutingThread.Abort();
        }

        private void Clear()
        {
            this.logger.Log.Clear();
            this.LogOutput.Clear();
        }

        private void ClearContextAfterErrors()
        {
            this.ExecuteAction(this.runnerManager.Runner.ClearContextAfterErrors);
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            this.Clear();
        }

        private void ExecuteAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                this.logger.Error(e);
            }
        }

        private void ExecuteActionAsync(Action action)
        {
            this.IsRedonlyMode = true;

            Task.Run(() =>
            {
                this.CurrentExecutingThread = Thread.CurrentThread;

                // try finally for cancellation by abort
                try
                {
                    this.ExecuteAction(action);
                }
                finally
                {
                    this.IsRedonlyMode = false;
                    this.CurrentExecutingThread = null;

                    if (this.IsLoaded)
                    {
                        this.ClearContextAfterErrors();
                    }

                    this.UpdateLoadButtonName();
                }
            });
        }

        private void ExecuteCommand(string command, params object[] parameters)
        {
            this.ExecuteActionAsync(() => { this.runnerManager.ExecuteCommand(command, parameters); });
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

        private void Load()
        {
            if (!this.IsLoaded)
            {
                this.ExecuteActionAsync(() =>
                {
                    this.runnerManager.Load();
                    this.UpdateSidTextbox();
                });
            }
            else
            {
                this.ExecuteCommand(RunnerManager.Commands.Release);
            }
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            this.Load();
        }

        private void Rebuild_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.Rebuild);
        }

        private void Run()
        {
            this.runnerManager.ScenarioBuffer.Clear();
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

            this.ExecuteActionAsync(() =>
            {
                this.runnerManager.ExecuteCommand(RunnerManager.Commands.SetScenarioContextValue, "{sid}", sid);
                this.runnerManager.ExecuteCommand(RunnerManager.Commands.Run);
            });
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            this.Run();
        }

        private void RunScenario_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.RunScenario, this.NameSpaceTextBox.Text.Trim(), this.FeatureNameTextBox.Text.Trim(), this.ScenarioNameTextBox.Text.Trim());
        }

        private void ScrollOutputTextboxToEnd()
        {
            this.LogOutput.Focus();
            this.LogOutput.CaretIndex = this.LogOutput.Text.Length;
            this.LogOutput.ScrollToEnd();
        }

        private void UpdateLoadButtonName()
        {
            this.LoadButton.Dispatcher.Invoke(() => this.LoadButton.Content = this.IsLoaded ? "Release" : "Load");
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
                this.CurrentSidTextBlock.Text = this.runnerManager.Runner.GetValueFromScenarioContext("{sid}") as string);
        }

        #endregion
    }
}