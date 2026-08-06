using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

internal static class UnityStaticTestRunner
{
    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: UnityStaticTestRunner <test-assembly>");
            return 2;
        }

        var testAssembly = Assembly.LoadFrom(args[0]);
        var passed = 0;
        var failed = 0;
        foreach (var type in GetLoadableTypes(testAssembly))
        {
            object instance = null;
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attributes = method.GetCustomAttributes(false);
                var cases = new ArrayList();
                var isTest = false;
                foreach (var attribute in attributes)
                {
                    var attributeName = attribute.GetType().FullName;
                    if (attributeName == "NUnit.Framework.TestAttribute") isTest = true;
                    if (attributeName == "NUnit.Framework.TestCaseAttribute") cases.Add(attribute);
                }
                if (!isTest && cases.Count == 0) continue;
                if (instance == null) instance = Activator.CreateInstance(type);
                if (cases.Count == 0) cases.Add(null);

                foreach (var testCase in cases)
                {
                    var arguments = testCase == null
                        ? new object[0]
                        : (object[])testCase.GetType().GetProperty("Arguments").GetValue(testCase, null);
                    var testName = type.Name + "." + method.Name;
                    if (arguments.Length > 0) testName += "(" + string.Join(",", arguments) + ")";
                    try
                    {
                        var result = method.Invoke(instance, arguments);
                        var task = result as Task;
                        if (task != null) task.GetAwaiter().GetResult();
                        Console.WriteLine("PASS " + testName);
                        passed++;
                    }
                    catch (Exception exception)
                    {
                        var cause = exception is TargetInvocationException && exception.InnerException != null
                            ? exception.InnerException
                            : exception;
                        Console.WriteLine("FAIL " + testName + " :: " + cause.Message);
                        failed++;
                    }
                }
            }
        }

        Console.WriteLine("RESULT passed=" + passed + " failed=" + failed);
        return failed == 0 ? 0 : 1;
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            foreach (var loaderException in exception.LoaderExceptions)
                Console.Error.WriteLine(loaderException.Message);
            return exception.Types;
        }
    }
}
