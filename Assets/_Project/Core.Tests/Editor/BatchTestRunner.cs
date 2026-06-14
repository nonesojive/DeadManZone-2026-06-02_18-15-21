#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DeadManZone.Core.Tests.Editor
{
    /// <summary>
    /// Runs Edit Mode tests from batchmode via -executeMethod after compilation settles.
    /// Avoids early -quit before the test runner when scripts recompile on startup.
    /// </summary>
    public static class BatchTestRunner
    {
        public static void RunEditModeTests()
        {
            RunTests(TestMode.EditMode, "TestResults-EditMode.xml");
        }

        public static void RunPlayModeTests()
        {
            RunTests(TestMode.PlayMode, "TestResults-PlayMode.xml");
        }

        private static void RunTests(TestMode mode, string defaultResultsFile)
        {
            string resultsPath = GetResultsPath(defaultResultsFile);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new RunCallback(resultsPath));

            var filter = new Filter { testMode = mode };
            string testFilter = GetCommandLineValue("-testFilter");
            if (!string.IsNullOrWhiteSpace(testFilter))
                filter.testNames = new[] { testFilter };

            var settings = new ExecutionSettings(filter);
            api.Execute(settings);
        }

        private static string GetCommandLineValue(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                    return args[i + 1];
            }

            return null;
        }

        private static string GetResultsPath(string defaultResultsFile)
        {
            string overridePath = GetCommandLineValue("-testResults");
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Path.GetFullPath(overridePath);

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), defaultResultsFile));
        }

        private sealed class RunCallback : ICallbacks
        {
            private readonly string _resultsPath;

            public RunCallback(string resultsPath) => _resultsPath = resultsPath;

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_resultsPath) ?? ".");
                    TestRunnerApi.SaveResultToFile(result, _resultsPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to write test results to {_resultsPath}: {ex.Message}");
                }

                EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }
        }
    }
}
#endif
