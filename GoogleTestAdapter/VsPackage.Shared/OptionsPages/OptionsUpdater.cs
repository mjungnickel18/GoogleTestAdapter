﻿using System;
using System.Linq;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.VsPackage.OptionsPages;
using GoogleTestAdapter.VsPackage.ReleaseNotes;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;

namespace VsPackage.Shared.Settings
{
    public class OptionsUpdater
    {
        private static readonly string OptionsBase = $@"SOFTWARE\Microsoft\VisualStudio\{VsVersionUtils.GetVisualStudioVersion().VersionString()}\DialogPage\GoogleTestAdapter.VsPackage.OptionsPages.";

        private static readonly string GeneralOptionsPage = OptionsBase + "GeneralOptionsDialogPage";
        private static readonly string ParallelizationOptionsPage = OptionsBase + "ParallelizationOptionsDialogPage";

        private const string SettingsVersion = "SettingsVersion";

        private readonly TestDiscoveryOptionsDialogPage _testDiscoveryOptions;
        private readonly TestExecutionOptionsDialogPage _testExecutionOptions;
        private readonly WritableSettingsStore _settingsStore;

        public OptionsUpdater(TestDiscoveryOptionsDialogPage testDiscoveryOptions, TestExecutionOptionsDialogPage testExecutionOptions, 
            IServiceProvider serviceProvider)
        {
            _testDiscoveryOptions = testDiscoveryOptions;
            _testExecutionOptions = testExecutionOptions;

            var settingsManager = new ShellSettingsManager(serviceProvider);
            _settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }

        public void UpdateIfNecessary()
        {
            if (_settingsStore.PropertyExists(VersionProvider.CollectionName, SettingsVersion))
                return;

            UpdateSettings();

            _settingsStore.SetString(VersionProvider.CollectionName, SettingsVersion, History.Versions.Last().ToString());
        }

        private void UpdateSettings()
        {
            _testDiscoveryOptions.TestDiscoveryTimeoutInSeconds = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestDiscoveryOptionsDialogPage.TestDiscoveryTimeoutInSeconds), 
                int.Parse, 
                SettingsWrapper.OptionTestDiscoveryTimeoutInSecondsDefaultValue);
            _testDiscoveryOptions.TestDiscoveryRegex = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestDiscoveryOptionsDialogPage.TestDiscoveryRegex), 
                s => s, 
                SettingsWrapper.OptionTestDiscoveryRegexDefaultValue);
            _testDiscoveryOptions.ParseSymbolInformation = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestDiscoveryOptionsDialogPage.ParseSymbolInformation), 
                bool.Parse, 
                SettingsWrapper.OptionParseSymbolInformationDefaultValue);
            _testDiscoveryOptions.TestNameSeparator = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestDiscoveryOptionsDialogPage.TestNameSeparator), 
                s => s, 
                SettingsWrapper.OptionTestNameSeparatorDefaultValue);
            _testDiscoveryOptions.TraitsRegexesBefore = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestDiscoveryOptionsDialogPage.TraitsRegexesBefore), 
                s => s, 
                SettingsWrapper.OptionTraitsRegexesDefaultValue);
            _testDiscoveryOptions.TraitsRegexesAfter = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestDiscoveryOptionsDialogPage.TraitsRegexesAfter), 
                s => s, 
                SettingsWrapper.OptionTraitsRegexesDefaultValue);

            _testExecutionOptions.EnableParallelTestExecution = GetAndDeleteValue(
                ParallelizationOptionsPage,
                nameof(TestExecutionOptionsDialogPage.EnableParallelTestExecution),
                bool.Parse,
                SettingsWrapper.OptionEnableParallelTestExecutionDefaultValue);
            _testExecutionOptions.MaxNrOfThreads = GetAndDeleteValue(
                ParallelizationOptionsPage,
                nameof(TestExecutionOptionsDialogPage.MaxNrOfThreads), 
                int.Parse, 
                SettingsWrapper.OptionMaxNrOfThreadsDefaultValue);
            _testExecutionOptions.AdditionalPdbs = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.AdditionalPdbs), 
                s => s, 
                SettingsWrapper.OptionAdditionalPdbsDefaultValue);
            _testExecutionOptions.AdditionalTestExecutionParams = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.AdditionalTestExecutionParams), 
                s => s, 
                SettingsWrapper.OptionAdditionalTestExecutionParamsDefaultValue);
            _testExecutionOptions.BatchForTestSetup = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.BatchForTestSetup), 
                s => s, 
                SettingsWrapper.OptionBatchForTestSetupDefaultValue);
            _testExecutionOptions.BatchForTestTeardown = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.BatchForTestTeardown), 
                s => s, 
                SettingsWrapper.OptionBatchForTestTeardownDefaultValue);
            _testExecutionOptions.ExitCodeTestCase = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.ExitCodeTestCase), 
                s => s, 
                SettingsWrapper.OptionExitCodeTestCaseDefaultValue);
            _testExecutionOptions.PathExtension = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.PathExtension), 
                s => s, 
                SettingsWrapper.OptionPathExtensionDefaultValue);
            _testExecutionOptions.KillProcessesOnCancel = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.KillProcessesOnCancel),
                bool.Parse,
                SettingsWrapper.OptionKillProcessesOnCancelDefaultValue);
            _testExecutionOptions.UseNewTestExecutionFramework2 = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.UseNewTestExecutionFramework2),
                bool.Parse,
                SettingsWrapper.OptionUseNewTestExecutionFrameworkDefaultValue);
            _testExecutionOptions.WorkingDir = GetAndDeleteValue(
                GeneralOptionsPage,
                nameof(TestExecutionOptionsDialogPage.WorkingDir),
                s => s,
                SettingsWrapper.OptionWorkingDirDefaultValue);
        }

        private static T GetAndDeleteValue<T>(string optionsKey, string propertyName, Func<string, T> map, T defaultValue)
        {
            try
            {
                var registryKey = Registry.CurrentUser.OpenSubKey(optionsKey, true);
                string value = registryKey?.GetValue(propertyName)?.ToString();
                if (value != null)
                {
                    try
                    {
                        registryKey.DeleteValue(propertyName);
                    }
                    catch (Exception)
                    {
                        // so what...
                    }
                    return map(value);
                }
            }
            catch (Exception)
            {
                // too bad
            }

            return defaultValue;
        }

        //private void SetTestDiscoveryTimeoutInSeconds()
        //{
        //    try
        //    {
        //        string generalOptionsKey =
        //            @"SOFTWARE\Microsoft\VisualStudio\15.0\DialogPage\GoogleTestAdapter.VsPackage.OptionsPages.GeneralOptionsDialogPage";
        //        var key = Registry.CurrentUser.OpenSubKey(generalOptionsKey, true);
        //        if (key != null)
        //        {
        //            string value = key.GetValue(nameof(_testDiscoveryOptions.TestDiscoveryTimeoutInSeconds))?.ToString();
        //            if (value != null)
        //            {
        //                _testDiscoveryOptions.TestDiscoveryTimeoutInSeconds = int.Parse(value);
        //                key.DeleteValue(nameof(_testDiscoveryOptions.TestDiscoveryTimeoutInSeconds));
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw;
        //    }
        //}

    }
}