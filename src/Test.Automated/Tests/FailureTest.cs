namespace Test.Automated.Tests
{
    using System;
    using System.Collections.Specialized;

    /// <summary>
    /// Failure test.
    /// </summary>
    public class FailureTest: TestBase
    {
        /// <summary>
        /// Failure test.
        /// </summary>
        public FailureTest()
        {
            Name = "Failure test";
            TestEnvironment = new TestEnvironment();
        }

        /// <summary>
        /// Failure test.
        /// </summary>
        /// <param name="test">Test results.</param>
        /// <returns>Task.</returns>
        public override async Task Run(TestResult result)
        {
            result.Success = false;

            ApiDetails details = new ApiDetails
            {
                Step = "Failure test",
                Request = "Failure request",
                Response = "Failure response",
                Headers = new NameValueCollection(),
                StatusCode = 400,
            };

            result.ApiDetails.Add(details);
            result.Exception = new ArgumentException("This was supposed to fail");
            
            await Task.Delay(250);
            result.EndUtc = DateTime.UtcNow;
        }
    }
}