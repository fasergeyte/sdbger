using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Policy;

using NUnit.Core;
using NUnit.Core.Filters;
using NUnit.Util;

using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings.Discovery;
using TechTalk.SpecFlow.Infrastructure;

using TestRunner = TechTalk.SpecFlow.TestRunner;

internal class Program
{
    #region Public Methods and Operators

    static String path1Folder = @"c:/open_u/ConsoleApplication2/1/"; //get from command line args
    static String path2Folder = @"c:/open_u/ConsoleApplication2/2/"; //get from command line args
    static String path1 = @"c:/open_u/ConsoleApplication2/1/Wilco.UITest.Spec.dll"; //get from command line args
    static String path2 = @"c:/open_u/ConsoleApplication2/2/Wilco.UITest.Spec.dll"; //get from command line args
    static String pathR = @"c:/open/Source/Wilco.UITest/Wilco.UITest.Spec/bin/Debug/Wilco.UITest.Spec.dll"; //get from command line args

    public static void Main(String[] args)
    {
//        var ass = Assembly.LoadFrom(path1);
//        var as2 = Assembly.LoadFrom(path2);
//        var as3 = Assembly.LoadFile(path2);
        var ass1 = LoadAssembly(path1);
        var ass2 = LoadAssembly(path2);

        //LoadFromPath(path1Folder);
        //CoreExtensions.Host.InitializeService();
//        String pathToTestLibrary = @"C:\open_u\ConsoleApplication2\Sdber\bin\Debug\Sdber.exe"; //get from command line args
//        pathToTestLibrary = @"c:\open\Source\Wilco.UITest\Wilco.UITest.Spec\bin\Debug\Wilco.UITest.Spec.dll"; //get from command line args
        //run(pathToTestLibrary);
//        var a = LoadAllBinDirectoryAssemblies(path1Folder);
//        var b = LoadAllBinDirectoryAssemblies(path2Folder);

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

        var ass = LoadAssembly(path1);

        testRunner.InitializeTestRunner(new[] { ass });

        ReinitialiseTestRunner((TestRunner)testRunner, path2);

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
        //ReloadAssembly(path2);
       // ReinitialiseTestRunner(testRunner, path2);
        testRunner.Given("given");
        testRunner.Given("the user has logged in as admin", ((string)(null)), ((Table)(null)), "Given ");
    }

    //private static Assembly ass;

    public static void ReloadAssembly(string path)
    {

        //var a1 = Assembly.Load(path1);
        AppDomain dom = AppDomain.CreateDomain("some");
        AssemblyName assemblyName = new AssemblyName();
        assemblyName.CodeBase = path2;
        Assembly assembly = dom.Load(assemblyName);
        Type[] types = assembly.GetTypes();
        AppDomain.Unload(dom);
        
        
        //System.Reflection.RuntimeAssembly
        
        Assembly.LoadFrom(path);
    }

    public static void ReinitialiseTestRunner(TestRunner testRunner, string newPath)
    {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        AppDomain dom = AppDomain.CreateDomain("some");
        AssemblyName assemblyName = new AssemblyName();
        assemblyName.CodeBase = newPath;
        Assembly assembly = dom.Load(assemblyName);

        // Binding new assembly in TestRunner
        var executionEngine = typeof(TestRunner).GetField("executionEngine", bindFlags).GetValue(testRunner);

        var bindingRegistryBuilder = (RuntimeBindingRegistryBuilder) typeof(TestExecutionEngine).GetField("bindingRegistryBuilder", bindFlags).GetValue(executionEngine);
        bindingRegistryBuilder.BuildBindingsFromAssembly(assembly);

        //AppDomain.Unload(dom);
    }

//    public static Assembly LoadAssembly(string path)
//    {
//        return Assembly.LoadFrom(path);
//    }

    private static int i = 1;


    public static Assembly LoadAssemblyNew(string path)
    {
        Assembly.LoadFrom(path1);
        AppDomain dom = AppDomain.CreateDomain("some" + ++i, null,
            new AppDomainSetup
            {
                ApplicationBase = "c:\\open_u\\ConsoleApplication2\\1"
            });

        AssemblyName assemblyName = new AssemblyName();
        assemblyName.CodeBase = path;
        Assembly assembly = dom.Load(assemblyName);
       // AppDomain.Unload(dom);
        return assembly; 
    }

    public static Assembly LoadAssembly(string path)
    {
        var domainName = "domain" + ++i;
        AppDomain domain = AppDomain.CreateDomain(domainName);
        var type = typeof(Proxy);
        var value = (Proxy)domain.CreateInstanceAndUnwrap(
            type.Assembly.FullName,
            type.FullName);
        return value.GetAssembly(path);
    }

    static Assembly MyAssemblyResolveHandler(object source, ResolveEventArgs e)
    {
        // Assembly.LoadFrom("Assembly1.dll")
        // Assembly.LoadFrom("Assembly2.dll")

        return Assembly.Load(e.Name);
    }

//    public static Assembly LoadAssambly2(string path)
//    {
//        
//        AppDomain domain = AppDomain.CreateDomain("newDomain");//, new Evidence(), path, "", true);
//        //domain.Load(path1);
////        AppDomain domain = AppDomain.CreateDomain("MyDomain", adevidence, domaininfo);
//        //domain.
//        //domain.Load()
//        Type type = typeof(Proxy);
//        var value = (Proxy)domain.CreateInstanceAndUnwrap(
//            type.Assembly.FullName,
//            type.FullName);
//        value.GetAssembly(path1);
//        Assembly spec = null;
//        foreach (string dll in dlls)
//        {
//            try
//            {
//                //Assembly loadedAssembly = domain.Load(dll);
//                //loadedAssembly.GetTypes();
//                //var a = domain.asse;
//                if (dll.EndsWith("Wilco.UITest.Spec.dll"))
//                {
//                    Assembly loadedAssembly = domain.Load(dll);
//                    spec = loadedAssembly;
//                }
//            }
//            catch (FileLoadException loadEx)
//            { } // The Assembly has already been loaded.
//            catch (BadImageFormatException imgEx)
//            { } // If a BadImageFormatException exception is thrown, the file is not an assembly.
//
//        } // foreach dll
//
//        return spec;
//    }    
//    
    public static Assembly LoadAllBinDirectoryAssemblies(string path)
    {
        Assembly spec = null;
        foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                Assembly loadedAssembly = Assembly.LoadFile(dll);
                //loadedAssembly.GetTypes();
                if (dll.EndsWith("Wilco.UITest.Spec.dll"))
                {
                    spec = loadedAssembly;
                }
            }
            catch (FileLoadException loadEx)
            { } // The Assembly has already been loaded.
            catch (BadImageFormatException imgEx)
            { } // If a BadImageFormatException exception is thrown, the file is not an assembly.

        } // foreach dll
        var a = spec.GetTypes();

        return spec;
    }


    public class Proxy : MarshalByRefObject
    {
    public Assembly GetAssembly(string assemblyPath)
    {
        try
        {
            var a = Assembly.LoadFrom(assemblyPath);
            a.GetTypes();
            return a;
        }
        catch (Exception)
        {
            return null;
            // throw new InvalidOperationException(ex);
        }
    }
}

    #endregion
}