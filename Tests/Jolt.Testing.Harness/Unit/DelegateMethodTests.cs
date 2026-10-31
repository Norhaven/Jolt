namespace Jolt.Testing.Harness.Unit
{
    public sealed class DelegateMethodTests : Testing.Unit.DelegateMethodTests
    {
        protected override IJsonContext CreateContext(TestType testType) => new TestContext().CreateJsonContext(testType);
    }
}
