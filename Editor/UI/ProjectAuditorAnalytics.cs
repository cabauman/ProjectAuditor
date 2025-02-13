using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;

namespace Unity.ProjectAuditor.Editor.UI
{
    static class ProjectAuditorAnalytics
    {
        const int k_MaxEventsPerHour = 100;
        const int k_MaxEventItems = 1000;
        const int k_MaxIssuesInAnalyzeSummary = 10;

        const string k_VendorKey = "unity.projectauditor";
        const string k_EventTopicName = "projectAuditorUsage";

        static bool s_EnableAnalytics;

        public static void EnableAnalytics()
        {
            var result = EditorAnalytics.RegisterEventWithLimit(k_EventTopicName, k_MaxEventsPerHour, k_MaxEventItems, k_VendorKey);
            if (result == AnalyticsResult.Ok)
                s_EnableAnalytics = true;
        }

        public enum UIButton
        {
            Analyze,
            Export,
            AssemblySelect,
            AssemblySelectApply,
            AreaSelect,
            AreaSelectApply,
            Mute,
            Unmute,
            ShowMuted,
            OnlyCriticalIssues,
            Load,
            Save,
            // views
            Summary,
            ApiCalls,
            CodeCompilerMessages,
            Generics,
            ProjectSettings,
            Assets,
            Shaders,
            ShaderVariants,
            BuildFiles,
            BuildSteps,
            Assemblies,
            ShaderCompilerMessages,
            PrecompiledAssemblies,
            Packages,
            Textures,
            PackageVersion,
            AudioClip,
            ComputeShaderVariants,
            Meshes
        }

        // -------------------------------------------------------------------------------------------------------------

        [Serializable]
        struct ProjectAuditorEvent
        {
            // camelCase since these events get serialized to Json and naming convention in analytics is camelCase
            public string action;    // Name of the buttom
            public Int64 t_since_start; // Time since app start (in microseconds)
            public Int64 duration; // Duration of event in ticks - 100-nanosecond intervals.
            public Int64 ts; //Timestamp (milliseconds epoch) when action started.

            public ProjectAuditorEvent(string name, Analytic analytic)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
            }
        }

        [Serializable]
        struct ProjectAuditorEventWithKeyValues
        {
            [Serializable]
            public struct EventKeyValue
            {
                public string key;
                public string value;
            }

            public string action;
            public Int64 t_since_start;
            public Int64 duration;
            public Int64 ts;
            public EventKeyValue[] action_params;

            public ProjectAuditorEventWithKeyValues(string name, Analytic analytic, Dictionary<string, string> payload)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();

                // Convert dictionary to a serializable array of key/value pairs
                if (payload != null && payload.Count > 0)
                {
                    action_params = new EventKeyValue[payload.Count];
                    var i = 0;
                    foreach (var kvp in payload)
                    {
                        action_params[i].key = kvp.Key;
                        action_params[i].value = kvp.Value;
                        ++i;
                    }
                }
                else
                {
                    action_params = null;
                }
            }
        }

        [Serializable]
        public struct IssueStats
        {
            public string id;
            public int numOccurrences;
            public int numHotPathOccurrences;
        }

        [Serializable]
        class ProjectAuditorUIButtonEventWithIssueStats
        {
            public string action;
            public Int64 t_since_start;
            public Int64 duration;
            public Int64 ts;

            public IssueStats[] issue_stats;

            public ProjectAuditorUIButtonEventWithIssueStats(string name, Analytic analytic, IssueStats[] payload)
            {
                action = name;
                t_since_start = SecondsToMicroseconds(analytic.GetStartTime());
                duration = SecondsToTicks(analytic.GetDurationInSeconds());
                ts = analytic.GetTimestamp();
                issue_stats = payload;
            }
        }

        // -------------------------------------------------------------------------------------------------------------

        static string GetEventName(UIButton uiButton)
        {
            switch (uiButton)
            {
                case UIButton.Analyze:
                    return "analyze_button_click";
                case UIButton.Export:
                    return "export_button_click";
                case UIButton.Summary:
                    return "summary_tab";
                case UIButton.ApiCalls:
                    return "api_tab";
                case UIButton.Assets:
                    return "assets_tab";
                case UIButton.Shaders:
                    return "shaders_tab";
                case UIButton.ShaderCompilerMessages:
                    return "shader_compiler_messages_tab";
                case UIButton.ShaderVariants:
                    return "shader_variants_tab";
                case UIButton.ComputeShaderVariants:
                    return "compute_shader_variants_tab";
                case UIButton.ProjectSettings:
                    return "settings_tab";
                case UIButton.Generics:
                    return "generics_tab";
                case UIButton.BuildFiles:
                    return "build_files_tab";
                case UIButton.BuildSteps:
                    return "build_steps_tab";
                case UIButton.CodeCompilerMessages:
                    return "compiler_messages_tab";
                case UIButton.Assemblies:
                    return "assemblies_tab";
                case UIButton.PrecompiledAssemblies:
                    return "precompiled_assemblies_tab";
                case UIButton.AssemblySelect:
                    return "assembly_button_click";
                case UIButton.AssemblySelectApply:
                    return "assembly_apply";
                case UIButton.AreaSelect:
                    return "area_button_click";
                case UIButton.AreaSelectApply:
                    return "area_apply";
                case UIButton.Mute:
                    return "mute_button_click";
                case UIButton.Unmute:
                    return "unmute_button_click";
                case UIButton.ShowMuted:
                    return "show_muted_checkbox";
                case UIButton.OnlyCriticalIssues:
                    return "only_hotpath_checkbox";
                case UIButton.Save:
                    return "save";
                case UIButton.Load:
                    return "load";
                case UIButton.Packages:
                    return "packages";
                case UIButton.Textures:
                    return "textures";
                case UIButton.PackageVersion:
                    return "package_version";
                case UIButton.AudioClip:
                    return "audio_clip";
                case UIButton.Meshes:
                    return "meshes";
                default:
                    Debug.LogFormat("SendUIButtonEvent: Unsupported button type : {0}", uiButton);
                    return "";
            }
        }

        static Int64 SecondsToMilliseconds(float seconds)
        {
            return (Int64)(seconds * 1000);
        }

        static Int64 SecondsToTicks(float durationInSeconds)
        {
            return (Int64)(durationInSeconds * 10000);
        }

        static Int64 SecondsToMicroseconds(double seconds)
        {
            return (Int64)(seconds * 1000000);
        }

        // -------------------------------------------------------------------------------------------------------------

        static IssueStats[] CollectSelectionStats(ProjectIssue[] selectedIssues)
        {
            var selectionsDict = new Dictionary<string, IssueStats>();

            foreach (var issue in selectedIssues)
            {
                var id = issue.descriptor.id;

                IssueStats summary;
                if (!selectionsDict.TryGetValue(id, out summary))
                {
                    summary = new IssueStats
                    {
                        id = id
                    };
                    selectionsDict[id] = summary;
                }

                selectionsDict[id] = summary;
            }

            var selectionsArray =
                selectionsDict.Values.OrderByDescending(x => x.numOccurrences).Take(5).ToArray();

            return selectionsArray;
        }

        static IssueStats[] GetScriptIssuesSummary(ProjectReport projectReport)
        {
            var statsDict = new Dictionary<string, IssueStats>();

            var scriptIssues = projectReport.FindByCategory(IssueCategory.Code);
            foreach (var issue in scriptIssues)
            {
                var descriptor = issue.descriptor;

                var id = descriptor.id;
                IssueStats stats;
                if (!statsDict.TryGetValue(id, out stats))
                {
                    stats = new IssueStats { id = id };
                }

                ++stats.numOccurrences;

                if (issue.IsMajorOrCritical())
                {
                    ++stats.numHotPathOccurrences;
                }

                statsDict[id] = stats;
            }

            return statsDict.Values.OrderByDescending(x => x.numOccurrences).Take(k_MaxIssuesInAnalyzeSummary).ToArray();
        }

        // -------------------------------------------------------------------------------------------------------------

        public static bool SendEvent(UIButton uiButton, Analytic analytic)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var uiButtonEvent = new ProjectAuditorEvent(GetEventName(uiButton), analytic);

                var result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
            }
            return false;
        }

        public static bool SendEventWithKeyValues(UIButton uiButton, Analytic analytic, Dictionary<string, string> payload)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var uiButtonEvent = new ProjectAuditorEventWithKeyValues(GetEventName(uiButton), analytic, payload);

                var result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
            }
            return false;
        }

        public static bool SendEventWithSelectionSummary(UIButton uiButton, Analytic analytic, ProjectIssue[] selectedIssues)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var payload = CollectSelectionStats(selectedIssues);

                var uiButtonEvent = new ProjectAuditorUIButtonEventWithIssueStats(GetEventName(uiButton), analytic, payload);

                var result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
            }
            return false;
        }

        public static bool SendEventWithAnalyzeSummary(UIButton uiButton, Analytic analytic, ProjectReport projectReport)
        {
            analytic.End();

            if (s_EnableAnalytics)
            {
                var payload = GetScriptIssuesSummary(projectReport);

                var uiButtonEvent = new ProjectAuditorUIButtonEventWithIssueStats(GetEventName(uiButton), analytic, payload);

                var result = EditorAnalytics.SendEventWithLimit(k_EventTopicName, uiButtonEvent);
                return (result == AnalyticsResult.Ok);
            }
            return false;
        }

        // -------------------------------------------------------------------------------------------------------------
        public class Analytic
        {
            double m_StartTime;
            float m_DurationInSeconds;
            Int64 m_Timestamp;
            bool m_Blocking;

            public Analytic()
            {
                m_StartTime = EditorApplication.timeSinceStartup;
                m_DurationInSeconds = 0;
                m_Timestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                m_Blocking = true;
            }

            public void End()
            {
                m_DurationInSeconds = (float)(EditorApplication.timeSinceStartup - m_StartTime);
            }

            public double GetStartTime()
            {
                return m_StartTime;
            }

            public float GetDurationInSeconds()
            {
                return m_DurationInSeconds;
            }

            public Int64 GetTimestamp()
            {
                return m_Timestamp;
            }

            public bool GetBlocking()
            {
                return m_Blocking;
            }
        }

        public static Analytic BeginAnalytic()
        {
            return new Analytic();
        }
    }
}
