﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Model;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.Scheduling;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Runners
{
    public class PreparingTestRunner : ITestRunner
    {
        public const string TestSetup = "Test setup";
        public const string TestTeardown = "Test teardown";

        private readonly ILogger _logger;
        private readonly SettingsWrapper _settings;
        private readonly ITestRunner _innerTestRunner;
        private readonly int _threadId;
        private readonly string _threadName;
        private readonly string _testDirectory;


        public PreparingTestRunner(int threadId, ITestFrameworkReporter reporter, ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
        {
            _logger = logger;
            _settings = settings;
            string threadName = ComputeThreadName(threadId, _settings.MaxNrOfThreads);
            _threadName = string.IsNullOrEmpty(threadName) ? "" : $"{threadName} ";
            _threadId = Math.Max(0, threadId);
            _testDirectory = Utils.GetTempDirectory();
            _innerTestRunner = new SequentialTestRunner(_threadName, _threadId, _testDirectory, reporter, _logger, _settings, schedulingAnalyzer);
        }

        public PreparingTestRunner(ITestFrameworkReporter reporter,
            ILogger logger, SettingsWrapper settings, SchedulingAnalyzer schedulingAnalyzer)
            : this(-1, reporter, logger, settings, schedulingAnalyzer){
        }


        public void RunTests(IEnumerable<TestCase> testCasesToRun, bool isBeingDebugged, 
            IDebuggedProcessLauncher debuggedLauncher, IProcessExecutor executor)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string batch = _settings.GetBatchForTestSetup(_testDirectory, _threadId);
                SafeRunBatch(TestSetup, _settings.SolutionDir, batch, isBeingDebugged);

                _innerTestRunner.RunTests(testCasesToRun, isBeingDebugged, debuggedLauncher, executor);

                batch = _settings.GetBatchForTestTeardown(_testDirectory, _threadId);
                SafeRunBatch(TestTeardown, _settings.SolutionDir, batch, isBeingDebugged);

                stopwatch.Stop();
                _logger.DebugInfo($"{_threadName}Execution took {stopwatch.Elapsed}");

                string errorMessage;
                if (!Utils.DeleteDirectory(_testDirectory, out errorMessage))
                {
                    _logger.DebugWarning(
                        $"{_threadName}Could not delete test directory '" + _testDirectory + "': " + errorMessage);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"{_threadName}Exception while running tests: " + e);
            }
        }

        public void Cancel()
        {
            _innerTestRunner.Cancel();
        }


        private void SafeRunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            if (string.IsNullOrEmpty(batch))
            {
                return;
            }
            if (!File.Exists(batch))
            {
                _logger.LogError($"{_threadName}Did not find " + batchType.ToLower() + " batch file: " + batch);
                return;
            }

            try
            {
                RunBatch(batchType, workingDirectory, batch, isBeingDebugged);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"{_threadName}{batchType} batch caused exception, msg: \'{e.Message}\', executed command: \'{batch}\'");
            }
        }

        private void RunBatch(string batchType, string workingDirectory, string batch, bool isBeingDebugged)
        {
            int batchExitCode;
            if (_settings.UseNewTestExecutionFramework)
            {
                var executor = new ProcessExecutor(null, _logger);
                batchExitCode = executor.ExecuteBatchFileBlocking(batch, "", workingDirectory, "", s => { });
            }
            else
            {
                var executor = new TestProcessLauncher(_logger, _settings, isBeingDebugged);
                batchExitCode = executor.GetOutputOfCommand(workingDirectory, batch, "", false, false, 
                    null, new List<string>());
            }

            if (batchExitCode == 0)
            {
                _logger.DebugInfo(
                    $"{_threadName}Successfully ran {batchType} batch \'{batch}\'");
            }
            else
            {
                _logger.LogWarning(
                    $"{_threadName}{batchType} batch returned exit code {batchExitCode}, executed command: \'{batch}\'");
            }
        }

        private string ComputeThreadName(int threadId, int maxNrOfThreads)
        {
            if (threadId < 0)
                return "";

            int nrOfDigits = maxNrOfThreads.ToString().Length;
            string paddedThreadId = threadId.ToString().PadLeft(nrOfDigits, '0');

            return $"[T{paddedThreadId}]";
        }

    }

}