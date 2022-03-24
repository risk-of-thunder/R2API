using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

namespace R2API.Test {
    internal class AwakeRunner {
        internal void DiscoverAndRun() {
            var assembly = typeof(R2APITest).Assembly;
            var path = assembly.Location;

            R2APITest.Logger.LogInfo($"Discovering tests in {path}...");
            var assemblyElement = new XElement("assembly");
            try {
                var messageSink = new MessageSink {
                    OnTest = OnTest,
                    OnExecutionComplete = OnExecutionComplete,
                };

                using (
                    var controller = new XunitFrontController(
                        AppDomainSupport.Denied,
                        path
                    )
                ) {
                    var configuration = ConfigReader.Load(path);
                    configuration.AppDomain = AppDomainSupport.IfAvailable;
                    configuration.DiagnosticMessages = true;
                    configuration.StopOnFail = true;
                    configuration.MaxParallelThreads = 1;
                    configuration.LongRunningTestSeconds = 5;
                    ITestFrameworkDiscoveryOptions discoveryOptions =
                        TestFrameworkOptions.ForDiscovery(configuration);
                    discoveryOptions.SetSynchronousMessageReporting(true);
                    discoveryOptions.SetPreEnumerateTheories(false);
                    controller.Find(false, messageSink, discoveryOptions);
                    messageSink.DiscoveryCompletionWaitHandle.WaitOne();
                    ITestCase[] testCases = messageSink.TestCases.ToArray();
                    lock (this) {
                        R2APITest.Logger.LogInfo(
                            $"{testCases.Length} test cases were found in {path}:"
                        );
                        foreach (ITestCase testCase in testCases) {
                            R2APITest.Logger.LogInfo($"- {testCase.DisplayName}");
                        }

                        Console.Error.Flush();
                    }

                    ITestFrameworkExecutionOptions executionOptions =
                            TestFrameworkOptions.ForExecution(configuration);
                    executionOptions.SetDiagnosticMessages(true);
                    executionOptions.SetSynchronousMessageReporting(true);
                    executionOptions.SetStopOnTestFail(true);
                    executionOptions.SetDisableParallelization(true);

                    controller.RunTests(
                        testCases,
                        messageSink,
                        executionOptions
                    );
                    messageSink.ExecutionCompletionWaitHandle.WaitOne();
                }
            }
            catch (Exception e) {
                R2APITest.Logger.LogError($"{e.GetType().Name}: {e.Message}");
                R2APITest.Logger.LogError(e.StackTrace);
            }
            R2APITest.Logger.LogInfo($"All tests in {path} ran.");
        }

        private void OnExecutionComplete(ExecutionCompleteInfo info) {
            R2APITest.Logger.LogInfo($"Total:   {info.TotalTests}");
            R2APITest.Logger.LogInfo($"Passed:  {info.TotalTests - info.TestsFailed - info.TestsSkipped}");
            R2APITest.Logger.LogInfo($"Failed:  {info.TestsFailed}");
            R2APITest.Logger.LogInfo($"Skipped: {info.TestsSkipped}");
            if (info.TestsFailed > 0) {
                //ExitCode = 1;
            }
        }

        private void OnTest(TestInfo info) {
            switch (info) {
                case TestPassedInfo i:
                    R2APITest.Logger.LogInfo($"PASS {i.TestDisplayName}: {i.ExecutionTime}s");
                    return;

                case TestFailedInfo i:
                    R2APITest.Logger.LogError(
                        $"FAIL {i.TestDisplayName}:" +
                        $"{i.ExecutionTime}s\n" +
                        $"{i.ExceptionType}:" +
                        $"{string.Join("\n  ", i.ExceptionMessage.Split('\n'))}\n" +
                        $"{string.Join("\n  ", i.ExceptionStackTrace.Split('\n'))}\n\n" +
                        $"Output:\n" +
                        $"{i.Output.Replace("\n", "\n  ")}"
                    );
                    return;

                case TestSkippedInfo i:
                    R2APITest.Logger.LogWarning($"SKIP {i.TestDisplayName}: {i.SkipReason}");
                    return;
            }
        }
    }
}
