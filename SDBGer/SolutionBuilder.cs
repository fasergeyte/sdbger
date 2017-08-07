namespace SDBGer
{
    using System.Collections.Generic;

    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Execution;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Logging;

    public class SolutionBuilder
    {
        #region Public Methods and Operators

        public static void Build(string buildFileUri)
        {
            List<Microsoft.Build.Framework.ILogger> loggers = new List<Microsoft.Build.Framework.ILogger>();
            loggers.Add(new ConsoleLogger()
            {
                Verbosity = LoggerVerbosity.Minimal,
                ShowSummary = false,
                SkipProjectStartedText = true
            });
            var projectCollection = new ProjectCollection();
            projectCollection.RegisterLoggers(loggers);
            var project = projectCollection.LoadProject(buildFileUri); // Needs a reference to System.Xml
            try
            {
                project.Build();
            }
            finally
            {
                projectCollection.UnregisterAllLoggers();
            }
        }

        public static void BuildSolution(string buildFileUri)
        {
            var props = new Dictionary<string, string>();
            props["Configuration"] = "Debug";
            var request = new BuildRequestData(buildFileUri, props, null, new string[] { "Build" }, null);
            var parms = new BuildParameters();
            List<Microsoft.Build.Framework.ILogger> loggers = new List<Microsoft.Build.Framework.ILogger>();
            loggers.Add(new ConsoleLogger()
            {
                Verbosity = LoggerVerbosity.Minimal,
                ShowSummary = false,
                SkipProjectStartedText = true
            });
            parms.Loggers = loggers;
            parms.DetailedSummary = false;
            var result = BuildManager.DefaultBuildManager.Build(parms, request);
        }

        #endregion
    }
}