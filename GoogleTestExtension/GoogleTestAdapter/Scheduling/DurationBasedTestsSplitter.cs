﻿using System.Collections.Generic;
using System.Linq;
using GoogleTestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace GoogleTestAdapter.Scheduling
{
    class DurationBasedTestsSplitter : AbstractGoogleTestAdapterClass, ITestsSplitter
    {
        private int OverallDuration { get; }
        private IDictionary<TestCase, int> TestcaseDurations { get; }

        internal DurationBasedTestsSplitter(IDictionary<TestCase, int> testcaseDurations) : this(testcaseDurations, null) { }

        internal DurationBasedTestsSplitter(IDictionary<TestCase, int> testcaseDurations, AbstractOptions options) : base(options)
        {
            this.TestcaseDurations = testcaseDurations;
            this.OverallDuration = testcaseDurations.Values.Sum();
        }

        public List<List<TestCase>> SplitTestcases()
        {
            List<TestCase> sortedTestcases = TestcaseDurations.Keys.OrderByDescending(tc => TestcaseDurations[tc]).ToList();
            int nrOfThreadsToUse = Options.MaxNrOfThreads;
            int targetDuration = OverallDuration / nrOfThreadsToUse;

            List<List<TestCase>> splitTestcases = new List<List<TestCase>>();
            List<TestCase> currentList = new List<TestCase>();
            int currentDuration = 0;
            while (sortedTestcases.Count > 0 && splitTestcases.Count < nrOfThreadsToUse)
            {
                do
                {
                    TestCase testcase = sortedTestcases[0];

                    sortedTestcases.RemoveAt(0);
                    currentList.Add(testcase);
                    currentDuration += TestcaseDurations[testcase];
                } while (sortedTestcases.Count > 0 && currentDuration + TestcaseDurations[sortedTestcases[0]] <= targetDuration);
                       
                splitTestcases.Add(currentList);
                currentList = new List<TestCase>();
                currentDuration = 0;
            }

            while (sortedTestcases.Count > 0)
            {
                // TODO performance
                int index = GetIndexOfListWithShortestDuration(splitTestcases);
                splitTestcases[index].Add(sortedTestcases[0]);
                sortedTestcases.RemoveAt(0);
            }

            return splitTestcases;
        }

        private int GetIndexOfListWithShortestDuration(List<List<TestCase>> splitTestcases)
        {
            int index = 0;
            int minDuration = int.MaxValue;
            for (int i = 0; i < splitTestcases.Count; i++)
            {
                List<TestCase> testcases = splitTestcases[i];
                int duration = testcases.Sum(tc => TestcaseDurations[tc]);
                if (duration < minDuration)
                {
                    minDuration = duration;
                    index = i;
                }
            }
            return index;
        }

    }

}