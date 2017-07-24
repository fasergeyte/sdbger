namespace SDBGerUI
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;

    using SDBGer;

    /// <summary>
    /// MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly RunnerManager runnerManager = new RunnerManager();

        #endregion

        #region Constructors and Destructors

        public MainWindow()
        {
            InitializeComponent();
            Console.SetOut(new MyLogger(this.LogOutput));
            //TextWriterTraceListener myWriter = new TextWriterTraceListener(Console.Out);

            //Debug.Listeners.Add(myWriter);

            // Autoscroll for log.
            LogOutput.TextChanged += (sender, e) =>
            {
                LogOutput.Focus();
                LogOutput.CaretIndex = LogOutput.Text.Length;
                LogOutput.ScrollToEnd();
            };
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
                    Log.Red(e);
                }

                this.IsRedonlyMode = false;
            });
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.Load);
        }

        private void Rebuild_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.Rebuild);
        }

        private void ReleaseButton_OnClick_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCommand(RunnerManager.Commands.Release);
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            this.runnerManager.ScenarioBuffer.Append(this.StepsInput.Text);
            this.runnerManager.Tags.AddRange(Regex.Matches(this.StepsInput.Text, "^\\s*@(.*?)\\s*?$", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList());
            this.ExecuteCommand(RunnerManager.Commands.Run);
        }

        #endregion

        private class MyLogger : TextWriter
        {
            #region Fields

            private readonly TextBox tb;

            #endregion

            #region Constructors and Destructors

            public MyLogger(TextBox tb)
            {
                this.tb = tb;
            }

            #endregion

            #region Public Properties

            public override Encoding Encoding
            {
                get
                {
                    return null;
                }
            }

            #endregion

            #region Public Methods and Operators

            public override void Write(char value)
            {
                this.tb.Dispatcher.Invoke(() => { tb.AppendText(new string(value, 1)); });
            }

            #endregion
        }
    }
}