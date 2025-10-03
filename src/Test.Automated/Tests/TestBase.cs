namespace Test.Automated.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class TestBase
    {
        public abstract TestResult TestResult { get; set; }
        public abstract string Name { get; set; }
        public abstract TestEnvironment TestEnvironment { get; set; }
        public abstract Task Run(TestResult result);
    }
}
