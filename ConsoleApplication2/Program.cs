using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Safari;

    class Program
    {
        static void Main(string[] args)
        {
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            service.EnableVerboseLogging = true;
            service.LogPath = "log.log";

            var options = new ChromeOptions();
            //options.AddArgument("--test-type");
            //options.AddArgument("--disable-extensions");
            //options.AddArguments(string.Format("--lang={0}", "en-US"));
            options.AddArguments("user-data-dir=C:/temp/test_i_forchrome");

            IWebDriver driver = new ChromeDriver(service,options);
            driver.Url = "http://localhost/Open/Conflicts/ConflictsSearchResults.aspx?searchid=1";
            var rad = driver.FindElement(By.Id("Y_X_A_B_TC_T1_CS_CSR_CSRP_rbResolutionConditionSome"));
            var res = rad.FindElement(By.XPath("following-sibling::*[1][name() = 'span' or name() = 'label']|(./..)"));
        }
    }
}
