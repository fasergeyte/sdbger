namespace SDBGer
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;

    using BoDi;

    using NUnit.Framework;

    using OpenQA.Selenium.Chrome;

    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Bindings;
    using TechTalk.SpecFlow.Bindings.Discovery;
    using TechTalk.SpecFlow.Configuration;
    using TechTalk.SpecFlow.Infrastructure;
    using TechTalk.SpecFlow.Tracing;
    using TechTalk.SpecFlow.UnitTestProvider;

    using TestStatus = TechTalk.SpecFlow.Infrastructure.TestStatus;

    public class SpecflowManager : MarshalByRefObject
    {
        #region Fields

        private readonly IBindingRegistry bindingRegistry;

        private readonly RuntimeBindingRegistryBuilder bindingRegistryBuilder;

        private readonly IContextManager contextManager;

        private readonly TestExecutionEngine executionEngine;

        private readonly ObjectContainer globalContainer;

        private readonly ILogger logger;

        private readonly RuntimeConfiguration runtimeConfiguration;

        private readonly Assembly testAssembly;

        private readonly Type testRunManType = typeof(TestRunnerManager);

        private readonly TestRunner testRunner;

        private readonly Dictionary<Assembly, ITestRunnerManager> testRunnerManagerRegistry;

        private readonly IUnitTestRuntimeProvider unitTestRuntimeProvider;

        private readonly Type webBrowser;

        private Type currentFeatureType;

        private MethodInfo currentScenarioMethod;

        private CultureInfo defaultBindingCulture;

        private ProgrammingLanguage defaultTargetLanguage;

        private object syncRoot;

        private ITestRunnerManager testRunnerManager;

        private Dictionary<int, ITestRunner> testRunnerRegistry;

        private Type webBrowserType;

        #endregion

        #region Constructors and Destructors

        public SpecflowManager(string assemblyPath, ILogger logger)
        {
            ConfigurationManager.AppSettings["SmtpServerEnabled"] = "false";

            //CoreExtensions.Host.InitializeService();
            this.testAssembly = Assembly.LoadFrom(assemblyPath);

            this.testRunner = this.InitTestRunner();

            this.executionEngine = this.testRunner.GetMemberValue<TestExecutionEngine>("executionEngine");
            this.bindingRegistry = this.executionEngine.GetMemberValue<IBindingRegistry>("bindingRegistry");
            this.unitTestRuntimeProvider = this.executionEngine.GetMemberValue<IUnitTestRuntimeProvider>("unitTestRuntimeProvider");
            this.contextManager = this.executionEngine.GetMemberValue<IContextManager>("contextManager");
            this.runtimeConfiguration = this.executionEngine.GetMemberValue<RuntimeConfiguration>("runtimeConfiguration");
            this.defaultTargetLanguage = this.executionEngine.GetMemberValue<ProgrammingLanguage>("defaultTargetLanguage");
            this.defaultBindingCulture = this.executionEngine.GetMemberValue<CultureInfo>("defaultBindingCulture");

            var core = this.testAssembly.GetReferencedAssemblies().First(a => a.FullName.Contains("Wilco.UITest.Core"));
            this.webBrowser = Assembly.Load(core).GetTypes().First(t => t.Name.Contains("WebBrowser"));
            this.globalContainer = this.testRunnerManager.GetMemberValue<ObjectContainer>("globalContainer");
            if (logger != null)
            {
                this.RegistrLogger(logger);
            }
            //this.Bind(this.testAssembly);
        }

        #endregion

        #region Public Properties

        public string CurrentFeature
        {
            get
            {
                return FeatureContext.Current == null ? null : FeatureContext.Current.FeatureInfo.Title;
            }
        }

        public string CurrentScenario
        {
            get
            {
                return ScenarioContext.Current == null ? null : ScenarioContext.Current.ScenarioInfo.Title;
            }
        }

        public bool IsChromeDriverInitialized
        {
            get
            {
                return this.webBrowser.GetMemberValue<ChromeDriver>("driver") != null;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void KillChromeDriver()
        {
            // Kill chrome driver withoutchrome
            foreach (var process in Process.GetProcesses().Where(p => p.ProcessName == "chromedriver"))
            {
                process.Kill();
            }
        }

        public void ApplyRunningData(RunningData rd)
        {
            rd.ChangeChromeDriverData(this.GetChromeDriver());

            // TODO: Implement for namespaces of feature
            if (rd.CurrentFeature != null)
            {
                try
                {
                    this.InitFeature(rd.CurrentFeature);
                }
                catch (Exception)
                {
                    this.logger.Trace("Feature '{0}' is not found.", rd.CurrentFeature);
                }
            }

            if (rd.CurrentScenario != null)
            {
                try
                {
                    this.InitScenario(rd.CurrentScenario);
                }
                catch (Exception)
                {
                    this.logger.Trace("Feature '{0}' is not found.", rd.CurrentScenario);
                }
            }

            foreach (var o in rd.FeatureContext)
            {
                FeatureContext.Current.Add(o.Key, o.Value);
            }
            foreach (var o in rd.ScenarioContext)
            {
                ScenarioContext.Current.Add(o.Key, o.Value);
            }
        }

        public void BeforeFeature()
        {
            this.executionEngine.InvokeMethod("FireEvents", HookType.BeforeFeature);
        }

        public void BeforeScenario()
        {
            this.executionEngine.InvokeMethod("FireScenarioEvents", HookType.BeforeScenario);
        }

        public void BeforeTestRun()
        {
            this.executionEngine.OnTestRunStart();
        }

        public void ClearContextAfterError()
        {
            this.SetStatusForTest(TestStatus.OK);
            ScenarioContext.Current.SetMemberValue("TestError", null);
        }

        public RunningData GetRunningData()
        {
            var rd = new RunningData();

            rd.SaveChromeDriver(this.webBrowser.GetPropertyValue<ChromeDriver>("Driver"));
            rd.FeatureContext = FeatureContext.Current.Where(
                item =>
                {
                    var isSer = item.Value.GetType().IsSerializable;
                    if (isSer)
                    {
                        return true;
                    }

                    this.logger.Trace("Item from feature context with key '{0}' and type '{1}' is not serialize.", item.Key, item.Value.GetType());
                    return false;
                }
                ).ToDictionary(k => k.Key, v => v.Value);

            rd.ScenarioContext = ScenarioContext.Current.Where(
                item =>
                {
                    var isSer = item.Value.GetType().IsSerializable;
                    if (isSer)
                    {
                        return true;
                    }

                    this.logger.Trace("Item from scenario context with key '{0}' and type '{1}' is not serialize.", item.Key, item.Value.GetType());
                    return false;
                }
                ).ToDictionary(k => k.Key, v => v.Value);

            rd.CurrentFeature = this.CurrentFeature;
            rd.CurrentScenario = this.CurrentScenario;

            return rd;
        }

        public void InitAnonymousFeature(string featureName, string[] tags = null)
        {
            var featureInfo = new FeatureInfo(new CultureInfo("en-US"), featureName, "", ProgrammingLanguage.CSharp, tags);

            var preContext = FeatureContext.Current;

            this.InitFeature(featureInfo);

            if (preContext != null)
            {
                foreach (var item in preContext)
                {
                    FeatureContext.Current.Add(item.Key, item.Value);
                }
            }
        }

        public void InitAnonymousScenario(string scenarioName, string featureName = null)
        {
            if (FeatureContext.Current == null)
            {
                this.InitAnonymousFeature(featureName ?? scenarioName + "Feature");
            }

            var preContext = ScenarioContext.Current;

            ScenarioInfo scenarioInfo = new ScenarioInfo(scenarioName);

            this.contextManager.InitializeScenarioContext(scenarioInfo);

            if (preContext != null)
            {
                foreach (var item in preContext)
                {
                    ScenarioContext.Current.Add(item.Key, item.Value);
                }
            }
        }

        public void InitFeature(string featureNameOrClassName, string nspase = null)
        {
            // get features with specified features description
            var features = this.testAssembly.GetTypes()
                .Where(
                    t =>
                        // return if class name or Description attribute is equals featureNameOrClassName
                        t.Name == featureNameOrClassName ||
                        t.GetCustomAttributes(typeof(DescriptionAttribute)).Any(a => ((DescriptionAttribute)a).Description == featureNameOrClassName)).ToList();

            Type feature = null;

            if (nspase != null)
            {
                feature = features.FirstOrDefault(f => f.Namespace == nspase);
            }
            else if (features.Count() == 1)
            {
                feature = features.First();
            }

            // TODO: make clearly
            Assert.NotNull(feature, "Feature '{0}' is not found.", featureNameOrClassName);

            this.currentFeatureType = feature;

            // get Tags of feature
            var tags = feature.GetCustomAttributes(typeof(CategoryAttribute)).Select(a => ((CategoryAttribute)a).Name).ToArray();

            FeatureInfo featureInfo = new FeatureInfo(new CultureInfo("en-US"), featureNameOrClassName, "", ProgrammingLanguage.CSharp, tags);

            this.InitFeature(featureInfo);
        }

        // https://social.msdn.microsoft.com/Forums/en-US/3ab17b40-546f-4373-8c08-f0f072d818c9/remotingexception-when-raising-events-across-appdomains?forum=netfxremoting
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void InitScenario(string scenarioNameOrMethodName)
        {
            this.currentScenarioMethod = this.currentFeatureType.GetMethods(
                ).FirstOrDefault(t =>
                    t.Name == scenarioNameOrMethodName ||
                    t.GetCustomAttributes(typeof(DescriptionAttribute))
                        .Any(a => ((DescriptionAttribute)a).Description == scenarioNameOrMethodName));
            Assert.NotNull(this.currentScenarioMethod, "Scenario '{0}' is not found.", scenarioNameOrMethodName);
            var tags = this.currentScenarioMethod.GetCustomAttributes(typeof(CategoryAttribute)).Select(a => ((CategoryAttribute)a).Name).ToArray();
            ScenarioInfo scenarioInfo = new ScenarioInfo(scenarioNameOrMethodName, tags);

            this.contextManager.InitializeScenarioContext(scenarioInfo);
        }

        public void OnTestRunEnd()
        {
            this.testRunner.InvokeMethod("OnTestRunEnd");
        }

        public void RegistrLogger(ILogger logger)
        {
            var tracer = new SdbgerTracer(logger);

            this.executionEngine.SetMemberValue("testTracer.traceListener", tracer);
        }

        public Exception RunScenario()
        {
            var feature = Activator.CreateInstance(this.currentFeatureType);
            feature.InvokeMethod("FeatureSetup");
            feature.SetMemberValue("testRunner", this.testRunner);
            try
            {
                this.currentScenarioMethod.Invoke(feature, null);
            }
            catch
            {
                // ignored
            }

            return this.GetResultException();
        }

        public Exception RunStep(string step)
        {
            var match = Regex.Match(step.Trim(), "(.+?)\\s(.+?)$((?:\\s*.*$)*)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return this.RunStep(match.Groups[2].Value.Trim(), match.Groups[1].Value.Trim(), match.Groups[3].Value.Trim());
        }

        public Exception RunStep(string name, string type, string table = null)
        {
            var t = table == null ? null : ParsTable(table);

            switch (type.ToLower())
            {
                case "when":
                    this.testRunner.When(name, ((string)(null)), t, "When ");
                    break;
                case "and":
                    this.testRunner.And(name, ((string)(null)), t, "And ");
                    break;
                case "then":
                    this.testRunner.Then(name, ((string)(null)), t, "Then ");
                    break;
                case "given":
                    this.testRunner.Given(name, ((string)(null)), t, "Given ");
                    break;
                default:
                    throw new NotImplementedException();
            }

            return this.GetResultException();
        }

        public void SetNewChrome()
        {
            KillChromeDriver();
            this.webBrowser.SetMemberValue("driver", null);
            this.webBrowser.GetMemberValue<ChromeDriver>("Driver");
        }

        public void SetValueToScenarioContext(string key, string value)
        {
            if (ScenarioContext.Current.ContainsKey(key))
            {
                ScenarioContext.Current[key] = value;
                this.logger.Trace("Key '{0}' was changed to '{1}'.", key, value);
            }
            else
            {
                ScenarioContext.Current.Add(key, value);
                this.logger.Trace("Key '{0}' was added with value '{1}'.", key, value);
            }
        }

        #endregion

        #region Methods

        private static Table ParsTable(string table)
        {
            if (string.IsNullOrEmpty(table))
            {
                return null;
            }

            var rows = Regex.Split(table, @"\r\n|\n|\r").Where(r => r.Trim() != string.Empty);
            var rowWithCells = rows.Select(r =>
            {
                var row = r.Trim();

                // Remove first and last "|"
                row = row.Substring(1, row.Length - 2);

                return Regex.Split(row, @"[^\\](?:\|)").Select(i => i.Trim()).ToList();
            }).Select(col => col.Select(c => c.Trim())).ToList();
            var t = new Table(rowWithCells.First().ToArray());

            foreach (var row in rowWithCells.Skip(1))
            {
                t.AddRow(row.ToArray());
            }

            return t;
        }

        private void Bind(Assembly assembly)
        {
            this.bindingRegistryBuilder.BuildBindingsFromAssembly(assembly);
            this.bindingRegistry.Ready = true;
        }

        private ChromeDriver GetChromeDriver()
        {
            return this.webBrowser.GetMemberValue<ChromeDriver>("Driver");
        }

        private Exception GetResultException()
        {
            var status = this.contextManager.ScenarioContext.GetMemberValue<TestStatus>("TestStatus");

            if (status == TestStatus.OK)
            {
                return null;
            }

            var e = ScenarioContext.Current.TestError ?? new Exception(status.ToString());
            this.ClearContextAfterError();
            return e;
        }

        private void InitFeature(FeatureInfo featureInfo)
        {
            #region code from SpecFlow\Runtime\Infrastructure\TestExecutionEngine.cs except 'FireEvents(HookType.BeforeFeature);'

            // if the unit test provider would execute the fixture teardown code 
            // only delayed (at the end of the execution), we automatically close 
            // the current feature if necessary
            if (this.unitTestRuntimeProvider.DelayedFixtureTearDown &&
                this.contextManager.FeatureContext != null)
            {
                this.executionEngine.OnFeatureEnd();
            }

            // The Generator defines the value of FeatureInfo.Language: either feature-language or language from App.config or the default
            // The runtime can define the binding-culture: Value is configured on App.config, else it is null
            CultureInfo bindingCulture = this.runtimeConfiguration.BindingCulture ?? featureInfo.Language;

            this.defaultTargetLanguage = featureInfo.GenerationTargetLanguage;
            this.defaultBindingCulture = bindingCulture;

            this.contextManager.InitializeFeatureContext(featureInfo, bindingCulture);

            #endregion cod from SpecFlow\Runtime\Infrastructure\TestExecutionEngine.cs
        }

        private TestRunner InitTestRunner()
        {
            //TestRunnerManager.GetTestRunner(); //without "BeforeTestRun" action.

            // TestRunnerManager.GetTestRunner(); => 
            this.testRunnerManager = this.testRunManType.InvokeMethod<ITestRunnerManager>("GetTestRunnerManager", testAssembly, null);
            this.testRunnerRegistry = this.testRunnerManager.GetMemberValue<Dictionary<int, ITestRunner>>("testRunnerRegistry");
            this.syncRoot = this.testRunnerManager.GetMemberValue<object>("syncRoot");

            #region (ITestRunnerManager) this.testRunnerManager => CreateTestRunner()

            //     aTestRunnerManager => GetTestRunner(int threadId) =>
            ITestRunner testRunner;
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (!this.testRunnerRegistry.TryGetValue(threadId, out testRunner))
            {
                object obj = this.syncRoot;
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(obj, ref lockTaken);
                    if (!this.testRunnerRegistry.TryGetValue(threadId, out testRunner))
                    {
                        // ITestRunnerManager => CreateTestRunner()

                        #region this.testRunnerManager => CreateTestRunner

                        /*
        public virtual ITestRunner CreateTestRunner(int threadId)
        {
            var testRunner = CreateTestRunnerInstance();
            testRunner.InitializeTestRunner(threadId);

            lock (this)
            {
                if (!isTestRunInitialized)
                {
                    InitializeBindingRegistry(testRunner);
                    isTestRunInitialized = true;
                }
            }

            return testRunner;
        }
                         */

                        testRunner = this.testRunnerManager.InvokeMethod<ITestRunner>("CreateTestRunnerInstance");
                        testRunner.InitializeTestRunner(threadId);

                        lock (this.testRunnerManager)
                        {
                            if (!this.testRunnerManager.GetMemberValue<bool>("isTestRunInitialized"))
                            {
                                #region this.testRunnerManager.InitializeBindingRegistry(testRunner);

                                /*
         protected virtual void InitializeBindingRegistry(ITestRunner testRunner)
        {
            var bindingAssemblies = GetBindingAssemblies();
            BuildBindingRegistry(bindingAssemblies);

            testRunner.OnTestRunStart();

#if !SILVERLIGHT
            EventHandler domainUnload = delegate { OnTestRunnerEnd(); };
            AppDomain.CurrentDomain.DomainUnload += domainUnload;
            AppDomain.CurrentDomain.ProcessExit += domainUnload;
#endif
        }
 */
                                var bindingAssemblies = this.testRunnerManager.InvokeMethod<object>("GetBindingAssemblies");
                                this.testRunnerManager.InvokeMethod<object>("BuildBindingRegistry", bindingAssemblies);

                                EventHandler domainUnload = delegate
                                {
                                    #region this.testRunnerManager.OnTestRunnerEnd();

                                    /*
        protected virtual void OnTestRunnerEnd()
        {
            var onTestRunnerEndExecutionHost = testRunnerRegistry.Values.FirstOrDefault();
            if (onTestRunnerEndExecutionHost != null)
                onTestRunnerEndExecutionHost.OnTestRunEnd();

            // this will dispose this object
            globalContainer.Dispose();
        }
                                     */
                                    var onTestRunnerEndExecutionHost = testRunnerRegistry.Values.FirstOrDefault();
                                    if (onTestRunnerEndExecutionHost != null)
                                    {
                                        #region onTestRunnerEndExecutionHost.OnTestRunEnd();

                                        /*
        public virtual void OnTestRunEnd()
        {
            if (testRunnerEndExecuted)
                return;

            testRunnerEndExecuted = true;
            FireEvents(HookType.AfterTestRun);
        }                               */

                                        if (executionEngine.GetMemberValue<bool>("testRunnerEndExecuted"))
                                        {
                                            return;
                                        }

                                        executionEngine.SetFieldValue("testRunnerEndExecuted", true);

                                        //executionEngine.InvokeMethod("FireEvents", (HookType.AfterTestRun));

                                        #endregion
                                    }

                                    // this will dispose this object
                                    this.globalContainer.Dispose();

                                    #endregion
                                };
                                AppDomain.CurrentDomain.DomainUnload += domainUnload;
                                AppDomain.CurrentDomain.ProcessExit += domainUnload;

                                #endregion

                                this.testRunnerManager.SetFieldValue("isTestRunInitialized", true);
                            }
                        }

                        #endregion

                        this.testRunnerRegistry.Add(threadId, testRunner);

                        if (this.testRunnerManager.IsMultiThreaded)
                        {
                            typeof(FeatureContext).InvokeMethod("DisableSingletonInstance");
                            typeof(ScenarioContext).InvokeMethod("DisableSingletonInstance");
                            typeof(ScenarioStepContext).InvokeMethod("DisableSingletonInstance");
                        }
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(obj);
                    }
                }
            }

            #endregion

            return (TestRunner)testRunner;
        }

        private void SetStatusForTest(TestStatus value)
        {
            this.contextManager.ScenarioContext.SetMemberValue("TestStatus", value);
        }

        #endregion

        internal class SdbgerTracer : ITraceListener
        {
            #region Fields

            private readonly ILogger logger;

            #endregion

            #region Constructors and Destructors

            public SdbgerTracer(ILogger logger)
            {
                this.logger = logger;
            }

            #endregion

            #region Public Methods and Operators

            public void WriteTestOutput(string message)
            {
                this.logger.Trace(message);
            }

            public void WriteToolOutput(string message)
            {
                this.logger.Trace(message);
            }

            #endregion
        }
    }
}