using System;
using System.Globalization;
using System.IO;
using System.Reflection;

using NUnit.Core;
using NUnit.Core.Filters;
using NUnit.Util;

using TechTalk.SpecFlow;

internal class Program
{
    #region Public Methods and Operators

    static String path1 = @"c:\open_u\ConsoleApplication2\1\Wilco.UITest.Spec.dll"; //get from command line args
    static String path2 = @"c:\open_u\ConsoleApplication2\2\Wilco.UITest.Spec.dll"; //get from command line args
    static String pathR = @"c:\open\Source\Wilco.UITest\Wilco.UITest.Spec\bin\Debug\Wilco.UITest.Spec.dll"; //get from command line args

    

    public static void Main(String[] args)
    {
        CoreExtensions.Host.InitializeService();
//        String pathToTestLibrary = @"C:\open_u\ConsoleApplication2\Sdber\bin\Debug\Sdber.exe"; //get from command line args
//        pathToTestLibrary = @"c:\open\Source\Wilco.UITest\Wilco.UITest.Spec\bin\Debug\Wilco.UITest.Spec.dll"; //get from command line args
        //run(pathToTestLibrary);
        var a = Assembly.Load(path1);
        //RunSpec(path1);

        //Run(pathToTestLibrary);
//        TestPackage package = new TestPackage(pathToTestLibrary);
//        Assembly a = System.Reflection.Assembly.LoadFrom(pathToTestLibrary);
//        package.Assemblies.Add(a.Location);
//        var runner = new SimpleTestRunner();
//        var e = runner.Load(package);
//        runner.Run(new NullListener(), TestFilter.Empty, true, LoggingThreshold.All);
//        NUnit.ConsoleRunner.Runner.Main(new string[]
//        {
//            a.Location, 
//        });
    }

    public static void Run(string path)
    {
        TestSuiteBuilder builder = new TestSuiteBuilder();
        TestPackage testPackage = new TestPackage(path);
        SimpleTestRunner remoteTestRunner = new SimpleTestRunner();

        remoteTestRunner.Load(testPackage);
        TestSuite suite = builder.Build(testPackage);
        TestSuite test = suite.Tests[0] as TestSuite;

        TestName testName = ((TestMethod)((TestFixture)test.Tests[0]).Tests[0]).TestName;
        TestFilter filter = new NameFilter(testName);
        TestResult result = test.Run(new NullListener(), filter);
        ResultSummarizer summ = new ResultSummarizer(result);
    }

    public static void run2(String pathToTestLibrary)
    {
        CoreExtensions.Host.InitializeService();
        TestPackage testPackage = new TestPackage(@pathToTestLibrary);
        testPackage.BasePath = Path.GetDirectoryName(pathToTestLibrary);
        TestSuiteBuilder builder = new TestSuiteBuilder();
        TestSuite suite = builder.Build(testPackage);
        TestResult result = suite.Run(new NullListener(), TestFilter.Empty);

        Console.WriteLine("has results? " + result.HasResults);
        Console.WriteLine("results count: " + result.Results.Count);
        Console.WriteLine("success? " + result.IsSuccess);
    }

    public static void RunSpec(string path)
    {
        CoreExtensions.Host.InitializeService();
        //SimpleTestRunner remoteTestRunner = new SimpleTestRunner();

        //remoteTestRunner.Load(new TestPackage(path));

        var testRunner = TestRunnerManager.GetTestRunner();

        LoadAssembly(path1);
        ReloadAssembly(path2);

        testRunner.InitializeTestRunner(new[] { ass });

        testRunner = TestRunnerManager.GetTestRunner();
        FeatureInfo featureInfo = new FeatureInfo(new CultureInfo("en-US"), "SimpleFileList", "", ProgrammingLanguage.CSharp, new string[]
        {
            "RequestFormSPA"
        });
        testRunner.OnFeatureStart(featureInfo);
        ScenarioInfo scenarioInfo = new ScenarioInfo("When user adds a file to request it appears in simple files list and user can dow" +
                                                     "nload it", new string[]
                                                     {
                                                         "Automated"
                                                     });
        testRunner.OnScenarioStart(scenarioInfo);
        ReloadAssembly(path2);
        testRunner.Given("given");
        testRunner.Given("the user has logged in as admin", ((string)(null)), ((Table)(null)), "Given ");
    }

    private static Assembly ass;

    public static void ReloadAssembly(string path)
    {

        var a1 = Assembly.Load(path1);
        var a2 = Assembly.Load(path2);

        Assembly.Load(path);
    }

    public static void LoadAssembly(string path)
    {
        ass = Assembly.Load(pathR);
    }


    #endregion
}