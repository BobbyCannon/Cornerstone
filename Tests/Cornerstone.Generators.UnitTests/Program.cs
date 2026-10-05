#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

internal static class Program
{
	#region Methods

	public static int Main(string[] args)
	{
		var runner = new TestRunner(args);
		var assembly = typeof(Program).Assembly;

		foreach (var type in assembly.GetTypes().OrderBy(x => x.Name))
		{
			if (!type.IsClass || type.IsAbstract || type.GetCustomAttribute<TestClassAttribute>() == null)
			{
				continue;
			}

			var testMethods = GetTestMethods(type);
			if (testMethods.Length == 0)
			{
				continue;
			}

			runner.AddTest(new TestClassInfo
			{
				ClassName = type.Name,
				ConstructorInfo = type.GetConstructor(Type.EmptyTypes),
				SkipInAot = false,
				SkipInAotReason = "Not compatible with AOT",
				InitializeMethod = FindAttributedMethod(type, typeof(TestInitializeAttribute)),
				CleanupMethod = FindAttributedMethod(type, typeof(TestCleanupAttribute)),
				ClassInitializeMethod = FindAttributedMethod(type, typeof(ClassInitializeAttribute)),
				ClassCleanupMethod = FindAttributedMethod(type, typeof(ClassCleanupAttribute)),
				TestMethods = testMethods
			});
		}

		runner.Process();
		return 0;
	}

	private static TestMethodInfo FindAttributedMethod(Type type, Type attributeType)
	{
		for (var current = type; current != null && current != typeof(object); current = current.BaseType)
		{
			foreach (var method in current.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
			{
				if (method.GetCustomAttribute(attributeType) == null)
				{
					continue;
				}

				return new TestMethodInfo
				{
					Name = method.Name,
					MethodInfo = type.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				};
			}
		}

		return null;
	}

	private static TestMethodInfo[] GetTestMethods(Type type)
	{
		var methods = new List<TestMethodInfo>();

		for (var current = type; current != null && current != typeof(object); current = current.BaseType)
		{
			foreach (var method in current.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(x => x.Name))
			{
				if (!IsTestMethod(method) || methods.Any(x => x.Name == method.Name))
				{
					continue;
				}

				methods.Add(new TestMethodInfo
				{
					Name = method.Name,
					MethodInfo = type.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
					SkipInAot = false,
					SkipInAotReason = "Not compatible with AOT"
				});
			}
		}

		return methods.ToArray();
	}

	private static bool IsTestMethod(MethodInfo method)
	{
		foreach (var attribute in method.GetCustomAttributes(true))
		{
			var type = attribute.GetType();
			while (type != null)
			{
				if (type.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
				{
					return true;
				}

				type = type.BaseType;
			}
		}

		return false;
	}

	#endregion
}
