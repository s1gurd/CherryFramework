using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CherryFramework.Installer.Tests
{
    /// <summary>
    /// Runs the installer tests without opening the Test Runner window.
    /// The Unity Test Runner UI is the proper way to run them; this is the
    /// shortcut for a quick check from the menu or from a script.
    /// </summary>
    public static class EditorTestRunner
    {
        private const string MenuPath = "Tools/CherryFramework/Run Installer Tests";

        [MenuItem(MenuPath, false, 50)]
        public static void RunFromMenu()
        {
            var report = RunAll();

            Debug.Log("[CherryFramework] " + report);
        }

        public static string RunAll()
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == "CherryFramework.Installer.Tests");

            if (assembly == null)
            {
                return "The test assembly is not loaded.";
            }

            var testAttribute = typeof(TestAttribute);
            var setUpAttribute = typeof(SetUpAttribute);
            var tearDownAttribute = typeof(TearDownAttribute);

            var passed = 0;
            var failed = 0;
            var lines = new List<string>();

            foreach (var type in GetTypes(assembly))
            {
                var tests = type.GetMethods()
                    .Where(method => method.GetCustomAttributes(testAttribute, true).Length > 0)
                    .ToList();

                if (tests.Count == 0)
                {
                    continue;
                }

                var setUps = type.GetMethods()
                    .Where(method => method.GetCustomAttributes(setUpAttribute, true).Length > 0);

                var tearDowns = type.GetMethods()
                    .Where(method => method.GetCustomAttributes(tearDownAttribute, true).Length > 0);

                foreach (var test in tests)
                {
                    object instance = null;

                    try
                    {
                        instance = Activator.CreateInstance(type);

                        foreach (var setUp in setUps)
                        {
                            setUp.Invoke(instance, null);
                        }

                        test.Invoke(instance, null);

                        passed++;
                        lines.Add("PASS  " + type.Name + "." + test.Name);
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        var cause = exception is TargetInvocationException && exception.InnerException != null
                            ? exception.InnerException
                            : exception;

                        lines.Add("FAIL  " + type.Name + "." + test.Name + " :: " +
                                  cause.GetType().Name + ": " + cause.Message);
                        lines.Add(Indent(cause.StackTrace));
                    }
                    finally
                    {
                        if (instance != null)
                        {
                            foreach (var tearDown in tearDowns)
                            {
                                try
                                {
                                    tearDown.Invoke(instance, null);
                                }
                                catch (Exception)
                                {
                                    // A failing tear down must not hide the test result.
                                }
                            }
                        }
                    }
                }
            }

            lines.Add("passed: " + passed + ", failed: " + failed);

            return string.Join("\n", lines);
        }

        private static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        private static string Indent(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return string.Join("\n", text.Split('\n').Select(line => "        " + line.Trim()));
        }
    }
}
