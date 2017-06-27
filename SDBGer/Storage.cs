namespace SDBGer
{
    using System;
    using System.Collections.Generic;
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

    public class Storage : MarshalByRefObject
    {
       public Dictionary<string, object> data = new Dictionary<string, object>();
    }
}