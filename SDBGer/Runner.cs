namespace SDBGer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Policy;
    using System.Text.RegularExpressions;

    internal class Runner
    {
        #region Constants

        public const string AnonymousFeatureName = "AnonymousFeature";

        public const string AnonymousScenarioName = "AnonymousScenario";

        #endregion

        #region Fields

        private AppDomain runnerDomain;

        private RunningData runningData;

        private SpecflowManager specManager;

        #endregion

        #region Public Properties

        public bool IsChromeDriverInitialized
        {
            get
            {
                return this.specManager.IsChromeDriverInitialized;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void ApplyRunningData()
        {
            if (this.runningData != null)
            {
                this.specManager.ApplyRunningData(this.runningData);
                Console.WriteLine("Data from last running was applied.");
            }
        }

        public void BeforeFeature()
        {
            this.specManager.BeforeFeature();
            Console.WriteLine("BeforeFeature was executed.");
        }

        public void BeforeScenario()
        {
            this.specManager.BeforeScenario();
            Console.WriteLine("BeforeScenario was executed.");
        }

        public void BeforeTestRun()
        {
            this.specManager.BeforeTestRun();
            Console.WriteLine("BeforeTestRun was executed.");
        }

        public void InitAnonymousFeature(IEnumerable<string> tags)
        {
            UseLastOrInitAnonymousFeature(tags);
            return;
            this.specManager.InitAnonymousFeature(AnonymousFeatureName, tags.ToArray());
            Console.WriteLine("Anonymous feature was initialized.");
        }

        public void InitAnonymousScenario()
        {
            this.specManager.InitAnonymousScenario(AnonymousScenarioName, AnonymousFeatureName);
            Console.WriteLine("Anonymous scenario was initialized.");
        }

        public void InitDomain(string assemblyPath)
        {
//            File.Copy(Path.Combine(Path.GetDirectoryName(assemblyPath), "Log4NetConfiguration.xml"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log4NetConfiguration.xml"), true);
//            File.Copy(Path.Combine(Path.GetDirectoryName(assemblyPath), "EntityFramework.dll"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EntityFramework.dll"), true);

            var stp = new AppDomainSetup();
            stp.ConfigurationFile = assemblyPath + ".config";
            stp.ApplicationBase = Path.GetDirectoryName(assemblyPath);
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var libs = new[]
            {
                //Path.Combine(baseDir, "SDBGer.vshost.exe"),
                Path.Combine(baseDir, "SDBGer.exe")
            };
            foreach (string dll in libs)
            {
                File.Copy(dll, Path.Combine(stp.ApplicationBase, Path.GetFileName(dll)), true);
            }

            runnerDomain = AppDomain.CreateDomain("SpecflowDebugRunner", new Evidence(), stp);
            var type = typeof(SpecflowManager);

            var value = (SpecflowManager)runnerDomain.CreateInstanceAndUnwrap(
                type.Assembly.FullName,
                type.FullName,
                false,
                BindingFlags.Default,
                null,
                new object[] { assemblyPath },
                null,
                null);

            specManager = value;

            Console.WriteLine("'{0}' loaded.", assemblyPath);
        }

        public void InitFeature(string featureNa, string nspase = null)
        {
            this.specManager.InitFeature(featureNa, nspase);
            Console.WriteLine("Feature '{0}' was initialized.", featureNa);
        }

        public void InitScenario(string scenName)
        {
            this.specManager.InitScenario(scenName);
            Console.WriteLine("Scenario '{0}' was initialized.", scenName);
        }

        public void InitScenario(string scenario, string featureName = null, string nspace = null)
        {
            if (featureName != null)
            {
                this.specManager.InitFeature(featureName, nspace);
            }

            this.specManager.InitScenario(scenario);
        }

        public void RealiseAssemblies()
        {
            // KillChromeDriver();

            AppDomain.Unload(this.runnerDomain);

            Console.WriteLine("Tests library was released.");
        }

        public void RunScenario(string scenarioName = null, string featureName = null, string nspace = null)
        {
            if (featureName != null)
            {
                this.InitFeature(featureName, nspace);
                //this.BeforeFeature();
            }

            if (scenarioName != null)
            {
                this.InitScenario(scenarioName);
                //this.BeforeScenario();
            }

            var error = this.specManager.RunScenario();

            if (error != null)
            {
                Console.Red(error);
            }
            else
            {
                Console.Green(string.Format("The scenario '{0}' completed successfully", scenarioName));
            }
        }

        public void RunSteps(string scenario)
        {
            var steps = Regex.Matches(scenario, "^\\s*?(given|when|and|then).+?$(\\s*?\\|.*?$)*", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Cast<Match>()
                .Select(m => m.Value).ToList();

            foreach (var step in steps.Select(s => s.Trim()))
            {
                var exception = this.specManager.RunStep(step);
                if (exception != null)
                {
                    Console.Red(exception);
                    return;
                }
            }

            Console.Green("Execution of steps was completed.");
        }

        public void SaveRunningData()
        {
            this.runningData = this.specManager.GetRunningData();
            Console.WriteLine("Running data was saved.");
        }

        public void SetNewChrome()
        {
            this.specManager.SetNewChrome();
        }

        public void UseLastOrInitAnonymousFeature(IEnumerable<string> tags)
        {
            var currentFeature = this.specManager.CurrentFeature;
            if (currentFeature == null || currentFeature == AnonymousFeatureName)
            {
                this.specManager.InitAnonymousFeature(AnonymousFeatureName, tags.ToArray());
                Console.WriteLine("Anonymous feature was initialized.");
            }
            else
            {
                Console.WriteLine(string.Format("App use feature context of '{0}'", currentFeature));
            }
        }

        #endregion
    }
}