namespace SDBGer
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text;

    public class RunnerManager
    {
        #region Fields

        private readonly Runner r;

        private bool autoClearIsEnabled = true;

        private string lastPath;

        #endregion

        #region Constructors and Destructors

        public RunnerManager()
        {
            this.ScenarioBuffer = new StringBuilder();
            this.Tags = new List<string>();
            this.r = new Runner();
        }

        #endregion

        #region Public Properties

        public StringBuilder ScenarioBuffer { get; private set; }

        public List<string> Tags { get; private set; }

        #endregion

        #region Public Methods and Operators

        public void Build()
        {
            SolutionBuilder.Build(ConfigurationManager.AppSettings["TestsProjectPath"]);
        }

        public void ClearSteps()
        {
            ScenarioBuffer.Clear();
            Log.WriteLine("Steps was cleared.");
        }

        public void ExecuteCommand(string command, params object[] parameters)
        {
            switch (command)
            {
                case Commands.ClearSteps:
                    ScenarioBuffer.Clear();
                    break;
                case Commands.SContext:
                    r.SetValueToScenarioContext((string)parameters[0], (string)parameters[1]);
                    Log.WriteLine("Anonymous scenario was cleared");
                    break;
                case Commands.ClearTags:
                    Tags.Clear();
                    r.InitAnonymousFeature(Tags);
                    Log.WriteLine("Tags for anonymous scenario was cleared");
                    break;
                case Commands.AutoClear:
                    autoClearIsEnabled = (bool)parameters[0];
                    Log.WriteLine("auto clear was change to " + autoClearIsEnabled);
                    break;
                case Commands.Clear:
                    Tags.Clear();
                    Log.WriteLine("Tags for anonymous scenario was cleared");
                    this.ClearSteps();
                    Log.WriteLine("Anonymous scenario was cleared");
                    break;
                case Commands.RunScenario:
                    r.RunScenario((string)parameters[2], (string)parameters[1], (string)parameters[0]);
                    break;
                case Commands.InitScenario:
                    if (!parameters.Any())
                    {
                        r.InitAnonymousScenario();
                    }
                    else
                    {
                        r.InitScenario((string)parameters[0]);
                    }

                    break;
                case Commands.InitFeature:
                    if (!parameters.Any())
                    {
                        r.InitAnonymousFeature(Tags);
                    }
                    else
                    {
                        r.InitFeature((string)parameters[0]);
                    }

                    break;
                case Commands.BeforeFeature:
                    r.BeforeFeature();
                    break;
                case Commands.BeforeScenario:
                    r.BeforeScenario();
                    break;
                case Commands.Run:
                    r.UseLastOrInitAnonymousFeature(Tags);
                    r.RunSteps(ScenarioBuffer.ToString());
                    if (autoClearIsEnabled)
                    {
                        ClearSteps();
                    }

                    break;
                case Commands.Release:
                    Release();
                    break;
                case Commands.SetNewChrome:
                    r.SetNewChrome();
                    break;
                case Commands.Load:
                    Load((string)(parameters.Any() ? parameters[0] : null));
                    break;
                case Commands.Rebuild:
                    Release();
                    Build();
                    Load();
                    break;
                case Commands.Build:
                    Build();
                    break;
                default:
                    Log.WriteLine("Unknown command: {0}", command);
                    break;
            }
        }

        public void Load(string parametr = null)
        {
            lastPath = (string.IsNullOrEmpty(parametr) ? lastPath : parametr.Replace("\\", "/")) ??
                       ConfigurationManager.AppSettings["TestsPath"];

            r.InitDomain(lastPath);
            r.BeforeTestRun();
            r.InitAnonymousScenario();
            r.ApplyRunningData();

            if (!r.IsChromeDriverInitialized)
            {
                r.BeforeFeature();
                r.BeforeScenario();
            }
        }

        public void ProcessLine(string line)
        {
            // line = line.Trim();
            line = line.TrimStart();

            if (line.Length > 0 && line[0] == '-')
            {
                this.ExecuteCommand(line);
                return;
            }

            // adds Tags
            if (line.Length > 0 && line.TrimStart()[0] == '@')
            {
                Tags.Add(line.Trim().Substring(1));
                return;
            }

            ScenarioBuffer.AppendLine(line);
        }

        public void Release()
        {
            r.SaveRunningData();
            r.RealiseAssemblies();
            return;
        }

        #endregion

        public static class Commands
        {
            #region Constants

            public const string AutoClear = "-autoclear";

            public const string BeforeFeature = "-beforefeature";

            public const string BeforeScenario = "-beforescenario";

            public const string BufferSize = "-buffersize";

            public const string Build = "-build";

            public const string Cancel = "-cancel";

            public const string Clear = "-clear";

            public const string ClearSteps = "-clearsteps";

            public const string ClearTags = "-cleartags";

            public const string InitFeature = "-initfeature";

            public const string InitScenario = "-initscenario";

            public const string Load = "-load";

            public const string Rebuild = "-rebuild";

            public const string Release = "-rel";

            public const string Run = "-run";

            public const string RunFeature = "-runfeature";

            public const string RunScenario = "-runscenario";

            public const string SContext = "-context";

            public const string SetNewChrome = "-chrome";

            #endregion
        }
    }
}