using Gherkin.Generator.Utils;
using jcoliz.FunctionalTests;

namespace DotNetNuxt.StarterKit.Tests.Functional.Infrastructure;

/// <summary>
/// Base test class shared by all functional test classes
/// </summary>
[GeneratedTestBase(UseNamespace = "DotNetNuxt.StarterKit.Tests.Functional.Features")]
public abstract class FunctionalTestBase : FunctionalTest
{
}
