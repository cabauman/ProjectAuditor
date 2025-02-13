using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.EditorTests
{
    class SettingsAnalysisTests : TestFixtureBase
    {
        [Test]
        public void SettingsAnalysis_Default_AccelerometerFrequency()
        {
            Assert.True(PlayerSettingsAnalyzer.IsAccelerometerEnabled());
        }

        [Test]
        public void SettingsAnalysis_Default_PhysicsLayerCollisionMatrix()
        {
            Assert.True(PhysicsAnalyzer.IsDefaultLayerCollisionMatrix());
        }

        [Test]
        public void SettingsAnalysis_Default_Physics2DLayerCollisionMatrix()
        {
            Assert.True(Physics2DAnalyzer.IsDefaultLayerCollisionMatrix());
        }

        [Test]
        public void SettingsAnalysis_Default_QualitySettings()
        {
            Assert.True(QualitySettingsAnalyzer.IsUsingDefaultSettings());
        }

        [Test]
        public void SettingsAnalysis_Default_QualityAsyncUploadTimeSlice()
        {
            Assert.True(QualitySettingsAnalyzer.IsDefaultAsyncUploadTimeSlice());
        }

        [Test]
        public void SettingsAnalysis_Default_QualityAsyncUploadBufferSize()
        {
            Assert.True(QualitySettingsAnalyzer.IsDefaultAsyncUploadBufferSize());
        }

        [Test]
        public void SettingsAnalysis_Default_StaticBatchingEnabled()
        {
            Assert.True(PlayerSettingsUtil.IsStaticBatchingEnabled(EditorUserBuildSettings.activeBuildTarget));
        }

        [Test]
        public void SettingsAnalysis_Issue_IsReported()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.method.Equals("bakeCollisionMeshes"));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.NotNull(playerSettingIssue);
            Assert.AreEqual("Player: Prebake Collision Meshes", playerSettingIssue.description);
            Assert.AreEqual("Project/Player", playerSettingIssue.location.Path);
            Assert.AreEqual("Player", playerSettingIssue.location.Filename);
            Assert.AreEqual(2, playerSettingIssue.descriptor.GetAreas().Length);
            Assert.Contains(Area.BuildSize, playerSettingIssue.descriptor.GetAreas());
            Assert.Contains(Area.LoadTime, playerSettingIssue.descriptor.GetAreas());
            Assert.AreEqual("Any", playerSettingIssue.descriptor.GetPlatformsSummary());

            // restore bakeCollisionMeshes
            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }

        [Test]
        public void SettingsAnalysis_Issue_IsNotReportedOnceFixed()
        {
            var savedFixedDeltaTime = Time.fixedDeltaTime;
            // 0.02f is the default Time.fixedDeltaTime value and will be reported as an issue
            Time.fixedDeltaTime = 0.02f;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.title.Equals("Time: Fixed Timestep"));
            var fixedDeltaTimeIssue = issues.FirstOrDefault();
            Assert.NotNull(fixedDeltaTimeIssue);
            Assert.AreEqual("Time: Fixed Timestep", fixedDeltaTimeIssue.description);
            Assert.AreEqual("Project/Time", fixedDeltaTimeIssue.location.Path);

            // "fix" fixedDeltaTime so it's not reported anymore
            Time.fixedDeltaTime = 0.021f;

            issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.title.Equals("Time: Fixed Timestep"));
            Assert.Null(issues.FirstOrDefault());

            // restore Time.fixedDeltaTime
            Time.fixedDeltaTime = savedFixedDeltaTime;
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void SettingsAnalysis_SplashScreen_IsEnabledAndCanBeDisabled(bool splashScreenEnabled)
        {
            var prevSplashScreenEnabled = PlayerSettings.SplashScreen.show;
            PlayerSettings.SplashScreen.show = splashScreenEnabled;

            Assert.AreEqual(splashScreenEnabled, PlayerSettingsAnalyzer.IsSplashScreenEnabledAndCanBeDisabled());

            PlayerSettings.SplashScreen.show = prevSplashScreenEnabled;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SettingsAnalysis_GraphicsMixedStandardShaderQuality_WithBuiltinRenderPipeline_IsReported(bool isMixed)
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);
#if UNITY_2019_3_OR_NEWER
            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
#endif
            tier1settings.standardShaderQuality = ShaderQuality.High;
            tier2settings.standardShaderQuality = ShaderQuality.High;
            tier3settings.standardShaderQuality = isMixed ? ShaderQuality.Low : ShaderQuality.High;

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, tier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = null;
#endif
            Assert.AreEqual(isMixed, BuiltinRenderPipelineAnalyzer.IsMixedStandardShaderQuality(buildTarget));

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, savedTier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = defaultRenderPipeline;
#endif
        }

        [TestCase(RenderingPath.Forward)]
        [TestCase(RenderingPath.DeferredShading)]
        public void SettingsAnalysis_GraphicsUsingRenderingPath_WithBuiltinRenderPipeline_IsReported(RenderingPath renderingPath)
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);
#if UNITY_2019_3_OR_NEWER
            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
#endif
            tier1settings.renderingPath = renderingPath;
            tier2settings.renderingPath = renderingPath;
            tier3settings.renderingPath = renderingPath;

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, tier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = null;
#endif
            if (renderingPath == RenderingPath.Forward)
            {
                Assert.AreEqual(true, BuiltinRenderPipelineAnalyzer.IsUsingForwardRendering(buildTarget));
                Assert.AreEqual(false, BuiltinRenderPipelineAnalyzer.IsUsingDeferredRendering(buildTarget));
            }
            else
            {
                Assert.AreEqual(false, BuiltinRenderPipelineAnalyzer.IsUsingForwardRendering(buildTarget));
                Assert.AreEqual(true, BuiltinRenderPipelineAnalyzer.IsUsingDeferredRendering(buildTarget));
            }

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, savedTier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = defaultRenderPipeline;
#endif
        }
    }
}
