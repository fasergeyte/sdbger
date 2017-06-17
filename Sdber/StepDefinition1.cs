using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace Sdber
{
    using NUnit.Framework;

    [Binding]
    public sealed class StepDefinition1
    {
        // For additional details on SpecFlow step definitions see http://go.specflow.org/doc-stepdef

        [Given("I have entered (.*) into the calculator")]
        [Given(@"fail")]
        public void GivenIHaveEnteredSomethingIntoTheCalculator(int number)
        {
            throw new Exception("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            Console.WriteLine("Great!!!");
        }

        [Given(@"I have entered (.*) into the calculator 2")]
        [Given(@"given")]
        public void GivenIHaveEnteredIntoTheCalculator()
        {
            Console.WriteLine("Great!!!");
        }

    }
}
