using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Runners;

namespace R2API.Test {


    public class MessageSink : IMessageSinkWithTypes {
        public IList<ITestCase> TestCases { get; } = new List<ITestCase>();
        public EventWaitHandle DiscoveryCompletionWaitHandle { get; } =
            new EventWaitHandle(false, EventResetMode.ManualReset);
        public Action<TestInfo> OnTest { get; set; } = null;
        public Action<ExecutionCompleteInfo> OnExecutionComplete { get; set; } = null;
        public EventWaitHandle ExecutionCompletionWaitHandle { get; } =
            new EventWaitHandle(false, EventResetMode.ManualReset);

        public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes) {
            switch (message) {
                case ITestCaseDiscoveryMessage m:
                    foreach (ITestCase testCase in m.TestCases) {
                        var format = string.Format("Discovered: {0}", testCase.DisplayName);
                        R2APITest.Logger.LogInfo(format);
                        TestCases.Add(testCase);
                    }
                    break;

                case IDiscoveryCompleteMessage _:
                    DiscoveryCompletionWaitHandle.Set();
                    break;

                case ITestPassed m:
                    OnTest?.Invoke(
                        new TestPassedInfo(
                            m.TestClass.Class.Name,
                            m.TestMethod.Method.Name,
                            m.TestCase.Traits,
                            m.Test.DisplayName,
                            m.TestCollection.DisplayName,
                            m.ExecutionTime,
                            m.Output
                        )
                    );
                    break;

                case ITestFailed m:
                    OnTest?.Invoke(
                        new TestFailedInfo(
                            m.TestClass.Class.Name,
                            m.TestMethod.Method.Name,
                            m.TestCase.Traits,
                            m.Test.DisplayName,
                            m.TestCollection.DisplayName,
                            m.ExecutionTime,
                            m.Output,
                            m.ExceptionTypes.FirstOrDefault(),
                            m.Messages.FirstOrDefault(),
                            m.StackTraces.FirstOrDefault()
                        )
                    );
                    break;

                case ITestSkipped m:
                    OnTest?.Invoke(
                        new TestSkippedInfo(
                            m.TestClass.Class.Name,
                            m.TestMethod.Method.Name,
                            m.TestCase.Traits,
                            m.Test.DisplayName,
                            m.TestCollection.DisplayName,
                            m.Reason
                        )
                    );
                    break;

                case ITestAssemblyFinished m:
                    OnExecutionComplete(
                        new ExecutionCompleteInfo(
                            m.TestsRun,
                            m.TestsFailed,
                            m.TestsSkipped,
                            m.ExecutionTime
                        )
                    );
                    ExecutionCompletionWaitHandle.Set();
                    break;

                default:
                    R2APITest.Logger.LogInfo($"Received message: {message}");
                    break;
            }

            return true;
        }

        public void Dispose() {
        }
    }

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class R2APITest : BaseUnityPlugin {
        public const string PluginGUID = "com.bepis.r2apitest";
        public const string PluginName = "R2APITest";
        public const string PluginVersion = "0.0.1";

        internal new static ManualLogSource Logger { get; set; }

        private void Awake() {
            Logger = base.Logger;

            var assembly = typeof(R2APITest).Assembly;
            var path = assembly.Location;

            R2APITest.Logger.LogError($"Discovering tests in {path}...");
            XElement assemblyElement = new XElement("assembly");
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
                        R2APITest.Logger.LogError(
                            $"{testCases.Length} test cases were found in {path}:"
                        );
                        foreach (ITestCase testCase in testCases) {
                            R2APITest.Logger.LogError($"- {testCase.DisplayName}");
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
