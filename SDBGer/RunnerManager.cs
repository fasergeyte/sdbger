namespace SDBGer
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text;

    public class RunnerManager
    {
        #region Fields

        private readonly ILogger logger;

        private bool autoClearIsEnabled = true;

        private string lastPath;

        #endregion

        #region Constructors and Destructors

        public RunnerManager(ILogger logger = null)
        {
            this.logger = logger ?? new DefaultLogger();
            this.ScenarioBuffer = new StringBuilder();
            this.Tags = new List<string>();
            this.Runner = new Runner(this.logger);
        }

        #endregion

        #region Public Properties

        public Runner Runner { get; set; }

        public StringBuilder ScenarioBuffer { get; private set; }

        public List<string> Tags { get; private set; }

        #endregion

        #region Public Methods and Operators

        public bool Build(ILogger logger = null)
        {
            return SolutionBuilder.Build(ConfigurationManager.AppSettings["TestsProjectPath"], logger);
        }

        public void ClearSteps()
        {
            this.ScenarioBuffer.Clear();
            this.logger.Trace("Steps was cleared.");
        }

        public object ExecuteCommand(string command, params object[] parameters)
        {
            switch (command)
            {
                case Commands.ClearSteps:
                    this.ScenarioBuffer.Clear();
                    break;
                case Commands.SetScenarioContextValue:
                    this.Runner.SetValueToScenarioContext((string)parameters[0], (string)parameters[1]);
                    break;
                case Commands.GetScenarioContextValue:
                    return this.Runner.GetValueFromScenarioContext((string)parameters[0]);
                    break;
                case Commands.ClearTags:
                    this.Tags.Clear();
                    this.Runner.InitAnonymousFeature(Tags);
                    this.logger.Trace("Tags for anonymous scenario was cleared");
                    break;
                case Commands.AutoClear:
                    this.autoClearIsEnabled = (bool)parameters[0];
                    this.logger.Trace("auto clear was change to " + autoClearIsEnabled);
                    break;
                case Commands.Clear:
                    Tags.Clear();
                    logger.Trace("Tags for anonymous scenario was cleared");
                    this.ClearSteps();
                    logger.Trace("Anonymous scenario was cleared");
                    break;
                case Commands.RunScenario:
                    Runner.RunScenario((string)parameters[2], (string)parameters[1], (string)parameters[0]);
                    break;
                case Commands.InitScenario:
                    if (!parameters.Any())
                    {
                        Runner.InitAnonymousScenario();
                    }
                    else
                    {
                        Runner.InitScenario((string)parameters[0]);
                    }

                    break;
                case Commands.InitFeature:
                    if (!parameters.Any())
                    {
                        Runner.InitAnonymousFeature(Tags);
                    }
                    else
                    {
                        Runner.InitFeature((string)parameters[0]);
                    }

                    break;
                case Commands.BeforeFeature:
                    Runner.BeforeFeature();
                    break;
                case Commands.BeforeScenario:
                    Runner.BeforeScenario();
                    break;
                case Commands.Run:
                    Runner.UseLastOrInitAnonymousFeature(Tags);
                    Runner.RunSteps(ScenarioBuffer.ToString());
                    if (autoClearIsEnabled)
                    {
                        ClearSteps();
                    }

                    break;
                case Commands.Release:
                    Release();
                    break;
                case Commands.SetNewChrome:
                    Runner.SetNewChrome();
                    break;
                case Commands.Load:
                    Load((string)(parameters.Any() ? parameters[0] : null));
                    break;
                case Commands.Rebuild:
                    Release();
                    if (Build(this.logger))
                    {
                        this.logger.Success("Build succeed");
                        Load();
                        return true;
                    }
                    else
                    {
                        this.logger.Error("Build failed");
                        return false;
                    }
                    break;
                case Commands.Build:
                    if (Build(this.logger))
                    {
                        this.logger.Success("Build succeed");
                        return true;
                    }
                    else
                    {
                        this.logger.Error("Build failed");
                        return false;
                    }
                    break;
                default:
                    logger.Trace("Unknown command: {0}", command);
                    break;
            }
            return null;
        }

        public void Load(string parametr = null)
        {
            lastPath = (string.IsNullOrEmpty(parametr) ? lastPath : parametr.Replace("\\", "/")) ??
                       ConfigurationManager.AppSettings["TestsPath"];

            Runner.InitDomain(lastPath);
            Runner.BeforeTestRun();
            Runner.InitAnonymousScenario();
            Runner.ApplyRunningData();

            if (!Runner.IsChromeDriverInitialized)
            {
                Runner.BeforeFeature();
                Runner.BeforeScenario();
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
            Runner.SaveRunningData();
            Runner.RealiseAssemblies();
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

            public const string GetScenarioContextValue = "-getscenariocontextvalue";

            public const string InitFeature = "-initfeature";

            public const string InitScenario = "-initscenario";

            public const string Load = "-load";

            public const string Rebuild = "-rebuild";

            public const string Release = "-rel";

            public const string Run = "-run";

            public const string RunFeature = "-runfeature";

            public const string RunScenario = "-runscenario";

            public const string SetNewChrome = "-chrome";

            public const string SetScenarioContextValue = "-setscenariocontextvalue";

            #endregion
        }
    }
}