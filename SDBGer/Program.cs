namespace SDBGer
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class Program
    {
        #region Static Fields

        private static readonly StringBuilder scenarioBuffer = new StringBuilder();

        private static readonly List<string> tags = new List<string>();

        private static bool autoClearIsEnabled = true;

        private static string lastPath;

        private static Runner r;

        #endregion

        #region Methods

        private static void ClearSteps()
        {
            scenarioBuffer.Clear();
            Log.WriteLine("Steps was cleared.");
        }

        private static void ExecCommand(string command)
        {
            Log.IsCursorBlinking = false;

            var match = Regex.Match(command, "\\s*([^\\s]+)(.*)");
            var beforeSpace = match.Groups[1].Value.Trim().ToLower();
            var parametr = match.Groups[2].Value.Trim();
            switch (beforeSpace)
            {
                case Commands.BufferSize:
                    Log.BufferSize = int.Parse(parametr);
                    break;
                case Commands.ClearSteps:
                case Commands.SContext:
                    var values = parametr.Split('=');
                    r.SetValueToScenarioContext(values[0], values[1]);
                    Log.WriteLine("Anonymous scenario was cleared");
                    break;
                case Commands.ClearTags:
                    tags.Clear();
                    r.InitAnonymousFeature(tags);
                    Log.WriteLine("Tags for anonymous scenario was cleared");
                    break;
                case Commands.AutoClear:
                    autoClearIsEnabled = bool.Parse(parametr);
                    Log.WriteLine("auto clear was change to " + autoClearIsEnabled);
                    break;
                case Commands.Clear:
                    tags.Clear();
                    Log.WriteLine("Tags for anonymous scenario was cleared");
                    ClearSteps();
                    Log.WriteLine("Anonymous scenario was cleared");
                    break;
                case Commands.RunScenario:
                    if (string.IsNullOrEmpty(parametr))
                    {
                        r.BeforeFeature();
                        r.BeforeScenario();
                        r.RunScenario();
                    }
                    else
                    {
                        var parameters = parametr.Split(new[] { "--" }, StringSplitOptions.None);
                        switch (parameters.Length)
                        {
                            case 1:
                                r.RunScenario(parameters[0]);
                                break;
                            case 2:
                                r.RunScenario(parameters[1], parameters[0]);
                                break;
                            case 3:
                                r.RunScenario(parameters[2], parameters[1], parameters[0]);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    break;
                case Commands.InitScenario:
                    if (string.IsNullOrEmpty(parametr))
                    {
                        r.InitAnonymousScenario();
                    }
                    else
                    {
                        r.InitScenario(parametr);
                    }

                    break;
                case Commands.InitFeature:
                    if (string.IsNullOrEmpty(parametr))
                    {
                        r.InitAnonymousFeature(tags);
                    }
                    else
                    {
                        r.InitFeature(parametr);
                    }

                    break;
                case Commands.BeforeFeature:
                    r.BeforeFeature();
                    break;
                case Commands.BeforeScenario:
                    r.BeforeScenario();
                    break;
                case Commands.Run:
                    r.UseLastOrInitAnonymousFeature(tags);
                    r.RunSteps(scenarioBuffer.ToString());
                    if (autoClearIsEnabled)
                    {
                        ClearSteps();
                    }
                    break;
                case Commands.Release:
                    r.SaveRunningData();
                    r.RealiseAssemblies();
                    break;
                case Commands.SetNewChrome:
                    r.SetNewChrome();
                    break;
                case Commands.Load:

                    lastPath = (string.IsNullOrEmpty(parametr) ? lastPath : parametr.Replace("\\", "/")) ?? ConfigurationManager.AppSettings["TestsPath"];

                    r.InitDomain(lastPath);
                    r.BeforeTestRun();
                    r.InitAnonymousScenario();
                    r.ApplyRunningData();

                    if (!r.IsChromeDriverInitialized)
                    {
                        r.BeforeFeature();
                        r.BeforeScenario();
                    }

                    break;
                default:
                    Log.WriteLine("Unknown command: {0}", command);
                    break;
            }
        }

        private static void Main(string[] args)
        {
            // Kill chrome driver
            SpecflowManager.KillChromeDriver();

            r = new Runner();

            try
            {
                ExecCommand("-load");
            }
            catch (Exception e)
            {
                Log.Red(e);
            }

            Log.InitializeCursoreAnimation();

            while (true)
            {
                try
                {
                    Console.Write("> ");
                    Log.IsCursorBlinking = true;
                    ProcessLine(Log.ReadLine());
                }
                catch (Exception e)
                {
                    Log.Red(e);
                    if (autoClearIsEnabled)
                    {
                        ClearSteps();
                    }
                }
            }
        }

        private static void ProcessLine(string line)
        {
            //line = line.Trim();
            line = line.TrimStart();

            if (line.Length > 0 && line[0] == '-')
            {
                ExecCommand(line);
                return;
            }

            // adds tags
            if (line.Length > 0 && line.TrimStart()[0] == '@')
            {
                tags.Add(line.Trim().Substring(1));
                return;
            }

            scenarioBuffer.AppendLine(line);
        }

        #endregion

        private static class Commands
        {
            #region Constants

            public const string AutoClear = "-autoclear";

            public const string BeforeFeature = "-beforefeature";

            public const string BeforeScenario = "-beforescenario";

            public const string BufferSize = "-buffersize";

            public const string Cancel = "-cancel";

            public const string Clear = "-clear";

            public const string ClearSteps = "-clearsteps";

            public const string ClearTags = "-cleartags";

            public const string InitFeature = "-initfeature";

            public const string InitScenario = "-initscenario";

            public const string Load = "-load";

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