using System;
using System.Linq;
using System.Reflection;
using R2API.TestingLibrary;

namespace R2API.Test;

internal class AwakeRunner
{
    internal void DiscoverAndRun()
    {
        var assembly = typeof(R2APITest).Assembly;
        const BindingFlags allFlags = (BindingFlags)(-1);
        var testMethods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(allFlags))
            .Where(m => m.GetCustomAttributes(typeof(FactAttribute), false).Length > 0)
            .ToArray();

        int passedTests = 0;
        int failedTests = 0;
        R2APITest.Logger.LogInfo($"Test Count: {testMethods.Length}");
        foreach (var method in testMethods)
        {
            var instance = Activator.CreateInstance(method.DeclaringType);
            try
            {
                method.Invoke(instance, null);
                R2APITest.Logger.LogInfo($"[Tests] [PASS] {method.DeclaringType.Name}.{method.Name}");
                passedTests++;
            }
            catch (Exception ex)
            {
                R2APITest.Logger.LogError($"[Tests] [FAIL] {method.DeclaringType.Name}.{method.Name} - {ex}");
                failedTests++;
            }
        }
        R2APITest.Logger.LogInfo($"Test Count: {passedTests}");
        R2APITest.Logger.LogInfo($"Passed Tests: {passedTests}");
        if (failedTests > 0)
        {
            R2APITest.Logger.LogError($"Failed Tests: {failedTests}");
        }
        else
        {
            R2APITest.Logger.LogInfo($"Zero Failed Tests");
        }
    }
}
