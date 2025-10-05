namespace Test.Automated
{
    using System;
    using System.Collections.Specialized;
    using System.Net.WebSockets;
    using System.Runtime.CompilerServices;
    using OllamaFlow.Core;
    using OllamaFlow.Core.Enums;
    using OllamaFlow.Core.Models;
    using OllamaFlow.Core.Models.Ollama;
    using OllamaFlow.Core.Serialization;
    using RestWrapper;
    using Test.Automated.Tests;

    /// <summary>
    /// OllamaFlow automated tests.
    /// </summary>
    public static partial class Program
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /*
         * 
         * Modify TestEnvironment.cs to set the OllamaFlow hostname and port, along with other 
         * important variables.
         * 
         */

        private static Serializer _Serializer = new Serializer();
        private static string _Line = new string('-', Console.WindowWidth - 1);

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Task.</returns>
        public static async Task Main(string[] args)
        {
            #region Welcome

            Console.WriteLine();
            Console.WriteLine("OllamaFlow Automated Tests");
            Console.WriteLine(_Line);

            List<TestResult> results = new List<TestResult>();

            #endregion

            #region Tests

            Console.WriteLine("Running tests");
            results.Add(await RunTest(new SuccessTest(), true));
            results.Add(await RunTest(new FailureTest(), true));
            results.Add(await RunTest(new Test1(), true));

            #endregion

            #region Summary

            Console.WriteLine();
            Console.WriteLine("Summary results");
            Console.WriteLine(_Line);

            int success = 0;
            int failure = 0;

            foreach (TestResult result in results)
            {
                if (result.Success) success++;
                else failure++;
                Console.WriteLine($"| {result.ToString()}");

                if (!result.Success)
                {
                    Console.WriteLine(_Serializer.SerializeJson(result, true));

                    if (result.Exception != null)
                        Console.WriteLine(result.Exception.ToString());

                    Console.WriteLine();
                }
            }

            Console.WriteLine();
            Console.WriteLine($"{success} test(s) passed");
            Console.WriteLine($"{failure} test(s) failed (1 failed test expected, 'Sample failure test')");
            Console.WriteLine();

            if (failure < 2) Console.WriteLine("Test succeeded"); // account for the failure test at the beginning
            else Console.WriteLine("Test failed");
            Console.WriteLine();

            #endregion
        }

        private static async Task<TestResult> RunTest(TestBase test, bool cleanAfter = true)
        {
            Console.WriteLine("Running test: " + test.Name);

            TestResult result = new TestResult
            {
                Name = test.Name,
                TestEnvironemnt = test.TestEnvironment,
                StartUtc = DateTime.Now
            };

            try
            {
                await test.Run(result); // Pass result to the test function
                // Do not overwrite Success if the test already set it
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
            }
            finally
            {
                if (cleanAfter)
                {
                    test.OllamaFlowDaemon?.Dispose();
                    Helpers.Cleanup(true, true);
                }
            }

            result.EndUtc = DateTime.Now;
            return result;
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}