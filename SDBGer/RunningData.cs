namespace SDBGer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.IE;
    using OpenQA.Selenium.Remote;

    [Serializable]
    public class RunningData
    {
        #region Fields

        private readonly string[] fieldsOfChromeDriver = new[]
        {
            "sessionId.sessionOpaqueKey",
            "executor.service.ProcessId", // executor.service.driverServiceProcess
            "executor.service.Port",
        };

        private readonly Dictionary<string, object> savedData;

        #endregion

        #region Constructors and Destructors

        public RunningData()
        {
            this.savedData = new Dictionary<string, object>();
        }

        #endregion

        #region Public Properties

        public string CurrentFeature { get; set; }

        public string CurrentScenario { get; set; }

        public Dictionary<string, object> FeatureContext { get; set; }

        public Dictionary<string, object> ScenarioContext { get; set; }

        #endregion

        #region Public Methods and Operators

        public void ChangeDriverData(RemoteWebDriver chromeDriver)
        {
            // Close new browser.
            chromeDriver.Close();

            var unrequiredProcess = Process.GetProcessById((int)chromeDriver.GetMemberValue("executor.service.ProcessId"));

            foreach (var field in this.fieldsOfChromeDriver)
            {
                var value = this.savedData[field];

                if (field == "executor.service.ProcessId")
                {
                    chromeDriver.SetMemberValue("executor.service.driverServiceProcess", Process.GetProcessById((int)value));
                    continue;
                }

                chromeDriver.SetMemberValue(field, value);
            }

            var serviceUri = chromeDriver.GetMemberValue("executor.service.ServiceUrl");
            chromeDriver.SetMemberValue("executor.internalExecutor.remoteServerUri", serviceUri);

            // kill new ChromeDriver.
            unrequiredProcess.Kill();
        }

        public void SaveDriver(object chromeDriver)
        {
            foreach (var field in this.fieldsOfChromeDriver)
            {
                this.savedData.Add(field, chromeDriver.GetMemberValue(field));
            }
        }

        #endregion
    }
}