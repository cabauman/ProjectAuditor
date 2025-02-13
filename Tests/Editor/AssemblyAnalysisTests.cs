using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class AssemblyAnalysisTests : TestFixtureBase
    {
#pragma warning disable 0414
        TempAsset m_TempAsset; // this is required to generate Assembly-CSharp.dll
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
class MyClass
{
}
");
        }

        [Test]
        public void AssemblyAnalysis_DefaultAssembly_IsReported()
        {
            var issues = Analyze(IssueCategory.Assembly, issue => issue.description.Equals(AssemblyInfo.DefaultAssemblyName));

            Assert.AreEqual(1, issues.Length);
            Assert.False(issues[0].GetCustomPropertyBool(AssemblyProperty.ReadOnly));
        }

        [Test]
#if UNITY_2018_4 || UNITY_2022_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure in Unity 2022+")]
#endif
        public void AssemblyAnalysis_BuiltinPackage_IsReported()
        {
            var issues = Analyze(IssueCategory.Assembly, issue => issue.description.Equals("UnityEngine.UI"));

            Assert.AreEqual(1, issues.Length);
            Assert.True(issues[0].GetCustomPropertyBool(AssemblyProperty.ReadOnly));
        }
    }
}
