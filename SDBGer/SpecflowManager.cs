namespace SDBGer
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using NUnit.Core;
    using NUnit.Framework;

    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Bindings;
    using TechTalk.SpecFlow.Bindings.Discovery;
    using TechTalk.SpecFlow.Configuration;
    using TechTalk.SpecFlow.Infrastructure;
    using TechTalk.SpecFlow.UnitTestProvider;

    using TestRunner = TechTalk.SpecFlow.TestRunner;

    public class SpecflowManager : MarshalByRefObject
    {
        #region Fields

        private readonly IBindingRegistry bindingRegistry;

        private readonly RuntimeBindingRegistryBuilder bindingRegistryBuilder;

        private readonly IContextManager contextManager;

        private readonly TestExecutionEngine executionEngine;

        private readonly RuntimeConfiguration runtimeConfiguration;

        private readonly TestRunner testRunner;

        private readonly Assembly testsAssembly;

        private readonly IUnitTestRuntimeProvider unitTestRuntimeProvider;

        private Type currentFeatureType;

        private CultureInfo defaultBindingCulture;

        private ProgrammingLanguage defaultTargetLanguage;

        #endregion

        #region Constructors and Destructors

        public SpecflowManager(string assemblyPath)
        {
            CoreExtensions.Host.InitializeService();

            this.testRunner = (TestRunner)TestRunnerManager.GetTestRunner();
            this.executionEngine = this.testRunner.GetFieldValue<TestExecutionEngine>("executionEngine");
            this.bindingRegistryBuilder = this.executionEngine.GetFieldValue<RuntimeBindingRegistryBuilder>("bindingRegistryBuilder");
            this.bindingRegistry = this.executionEngine.GetFieldValue<IBindingRegistry>("bindingRegistry");
            this.unitTestRuntimeProvider = this.executionEngine.GetFieldValue<IUnitTestRuntimeProvider>("unitTestRuntimeProvider");
            this.contextManager = this.executionEngine.GetFieldValue<IContextManager>("contextManager");
            this.runtimeConfiguration = this.executionEngine.GetFieldValue<RuntimeConfiguration>("runtimeConfiguration");
            this.defaultTargetLanguage = this.executionEngine.GetFieldValue<ProgrammingLanguage>("defaultTargetLanguage");
            this.defaultBindingCulture = this.executionEngine.GetFieldValue<CultureInfo>("defaultBindingCulture");
            this.testsAssembly = Assembly.LoadFrom(assemblyPath);

            this.Bind(this.testsAssembly);
        }

        #endregion

        #region Public Methods and Operators

        public void AfterScenario()
        {
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
            this.executionEngine.InvokeMethod("OnTestRunnerStart");
        }

        public void InitFeature(string featureName, string nspase = null)
        {
            // get features with specified features description
            var features = this.testsAssembly.GetTypes()
                .Where(
                    t => t.GetCustomAttributes(typeof(DescriptionAttribute))
                        .Any(a => ((DescriptionAttribute)a).Description == featureName)).ToList();

            Type feature = null;

            if (nspase != null)
            {
                feature = features.FirstOrDefault(f => f.Namespace == nspase);
            }
            else if (features.Count() == 1)
            {
                feature = features.First();
            }

            Assert.NotNull(feature, "Feature '{0}' is not found.", featureName);

            this.currentFeatureType = feature;

            // get tags of feature
            var tags = feature.GetCustomAttributes(typeof(CategoryAttribute)).Select(a => ((CategoryAttribute)a).Name).ToArray();

            FeatureInfo featureInfo = new FeatureInfo(new CultureInfo("en-US"), featureName, "", ProgrammingLanguage.CSharp, tags);

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

        public void InitScenario(string scenarioName)
        {
            var scenarioMethod = this.currentFeatureType.GetMethods(
                ).FirstOrDefault(t => t.GetCustomAttributes(typeof(DescriptionAttribute))
                    .Any(a => ((DescriptionAttribute)a).Description == scenarioName));
            Assert.NotNull(scenarioMethod, "Scenario '{0}' is not found.", scenarioName);
            var tags = scenarioMethod.GetCustomAttributes(typeof(CategoryAttribute)).Select(a => ((CategoryAttribute)a).Name).ToArray();
            ScenarioInfo scenarioInfo = new ScenarioInfo(scenarioName, tags);

            this.contextManager.InitializeScenarioContext(scenarioInfo);
        }

        public void OnTestRunEnd()
        {
            this.testRunner.InvokeMethod("OnTestRunEnd");
        }

        public void RunScenario(string featureName, string scenarioName, string nspase = null)
        {
        }

        public void RunStep(string name, string type, string table = null)
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
        }

        #endregion

        #region Methods

        private static Table ParsTable(string table)
        {
            var rows = Regex.Split(table, @"\r\n|\n|\r").Where(r => r.Trim() != string.Empty);
            var rowWithCells = rows.Select(r => Regex.Split(r.Trim(), @"[^\\](\|)")).Select(col => col.Select(c => c.Trim())).ToList();
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

        #endregion
    }
}