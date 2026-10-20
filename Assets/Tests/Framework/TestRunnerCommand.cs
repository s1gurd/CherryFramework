using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Cherry.Framework.Tests
{
    /// <summary>
    /// Runs the whole suite and prints a summary per run. The Test Runner
    /// window is the proper way to run the tests; this is the shortcut for an
    /// agent that wants one command and one log.
    /// </summary>
    public static class TestRunnerCommand
    {
        private const string EditModeMenu = "Tools/CherryFramework/Run EditMode Tests";
        private const string AllMenu = "Tools/CherryFramework/Run All Tests";

        private const string EditModeSummaryKey = "CherryFramework.EditModeSummary";
        private const string FailuresKey = "CherryFramework.Failures";

        [MenuItem(EditModeMenu, false, 50)]
        public static void RunEditMode() => Start(false);

        [MenuItem(AllMenu, false, 51)]
        public static void RunAll() => Start(true);

        private static void Start(bool withPlayMode)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new RunnerCallbacks(api, withPlayMode));
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        private class RunnerCallbacks : ICallbacks
        {
            private readonly TestRunnerApi _api;
            private readonly bool _withPlayMode;
            private bool _playModeStarted;

            public RunnerCallbacks(TestRunnerApi api, bool withPlayMode)
            {
                _api = api;
                _withPlayMode = withPlayMode;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus != TestStatus.Failed)
                {
                    return;
                }

                var line = "FAILED " + result.FullName + "\n" + result.Message;

                if (_withPlayMode)
                {
                    // The play mode run reloads the domain, which clears the
                    // console. The failures have to survive that.
                    SessionState.SetString(FailuresKey, SessionState.GetString(FailuresKey, "") + line + "\n\n");
                    return;
                }

                Debug.Log("[CherryFramework] " + line);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var summary = Describe(result);

                if (!_withPlayMode)
                {
                    Report(summary, SessionState.GetString(FailuresKey, ""));
                    SessionState.EraseString(FailuresKey);
                    return;
                }

                if (!_playModeStarted)
                {
                    _playModeStarted = true;
                    SessionState.SetString(EditModeSummaryKey, summary);
                    _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
                    return;
                }

                var editModeSummary = SessionState.GetString(EditModeSummaryKey, "");
                SessionState.EraseString(EditModeSummaryKey);

                Report(editModeSummary, SessionState.GetString(FailuresKey, ""));
                SessionState.EraseString(FailuresKey);
                Report(summary, string.Empty);
                Debug.Log("[CherryFramework] The test run is finished.");
            }

            private static void Report(string summary, string failures)
            {
                Debug.Log("[CherryFramework] " + summary);

                if (string.IsNullOrEmpty(failures))
                {
                    return;
                }

                var builder = new StringBuilder();
                builder.Append("[CherryFramework] Failures:\n");
                builder.Append(failures);

                Debug.Log(builder.ToString());
            }

            private static string Describe(ITestResultAdaptor result)
            {
                var builder = new StringBuilder();

                builder.Append(result.Name);
                builder.Append(" - passed: ").Append(result.PassCount);
                builder.Append(", failed: ").Append(result.FailCount);
                builder.Append(", skipped: ").Append(result.SkipCount);
                builder.Append(", inconclusive: ").Append(result.InconclusiveCount);
                builder.Append(", total: ").Append(
                    result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount);

                return builder.ToString();
            }
        }
    }
}
