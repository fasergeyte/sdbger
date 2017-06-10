using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdber
{
    using System.IO;

    using NUnit.Core;
    using NUnit.Core.Filters;

    using TechTalk.SpecFlow;

    class Program
    {
        public static void Main(String[] args)
        {
            String pathToTestLibrary = @"C:\Users\sergey.vlasov\Documents\visual studio 2013\Projects\ConsoleApplication2\Sdber\bin\Debug\C:\Users\sergey.vlasov\Documents\visual studio 2013\Projects\ConsoleApplication2\Sdber\bin\Debug\Sdber.exe"; //get from command line args
            var runner = new SimpleTestRunner();
            runner.Load(new TestPackage(pathToTestLibrary));
            runner.Run(new NullListener(), TestFilter.Empty,true,LoggingThreshold.All);
        }

    }
}
