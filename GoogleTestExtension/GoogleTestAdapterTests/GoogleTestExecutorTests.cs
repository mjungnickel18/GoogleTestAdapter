﻿using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter
{
    [TestClass]
    public class GoogleTestExecutorTests : AbstractGoogleTestExtensionTests
    {
        [TestMethod]
        public void RunsExternallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x86externallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public void RunsStaticallyLinkedX86TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x86staticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public void RunsExternallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x64externallyLinkedTests, 2, 0, 0);
        }

        [TestMethod]
        public void RunsStaticallyLinkedX64TestsWithResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x64staticallyLinkedTests, 1, 1, 0);
        }

        [TestMethod]
        public void RunsCrashingX64TestsWithoutResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x64crashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public void RunsCrashingX86TestsWithoutResult()
        {
            RunAndVerifyTests(GoogleTestDiscovererTests.x86crashingTests, 0, 1, 0, 1);
        }

        [TestMethod]
        public void RunsHardCrashingX86TestsWithoutResult()
        {
            Mock<IFrameworkHandle> MockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> MockRunContext = new Mock<IRunContext>();

            GoogleTestExecutor Executor = new GoogleTestExecutor(MockOptions.Object);
            Executor.RunTests(GoogleTestDiscovererTests.x86hardcrashingTests.Yield(), MockRunContext.Object, MockHandle.Object);

            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(0));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed && tr.ErrorMessage == "!! This is probably the test that crashed !!")),
                Times.Exactly(1));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(0));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped && tr.ErrorMessage == "reason is probably a crash of test Crashing.TheCrash")),
                Times.Exactly(2));
        }

        private void RunAndVerifyTests(string executable, int nrOfPassedTests, int nrOfFailedTests, int nrOfUnexecutedTests, int nrOfNotFoundTests = 0)
        {
            Mock<IFrameworkHandle> MockHandle = new Mock<IFrameworkHandle>();
            Mock<IRunContext> MockRunContext = new Mock<IRunContext>();

            GoogleTestExecutor Executor = new GoogleTestExecutor(MockOptions.Object);
            Executor.RunTests(executable.Yield(), MockRunContext.Object, MockHandle.Object);

            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Passed)),
                Times.Exactly(nrOfPassedTests));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Failed)),
                Times.Exactly(nrOfFailedTests));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.None)),
                Times.Exactly(nrOfUnexecutedTests));
            MockHandle.Verify(h => h.RecordResult(It.Is<TestResult>(tr => tr.Outcome == TestOutcome.Skipped)),
                Times.Exactly(nrOfNotFoundTests));
        }

    }

}