using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific settings
    /// </summary>
    public class ProjectAuditorConfig : ScriptableObject
    {
        /// <summary>
        /// If enabled, ProjectAuditor will run every time the project is built.
        /// </summary>
        public bool AnalyzeOnBuild;

        /// <summary>
        /// If enabled, ProjectAuditor will try to partially analyze the project in the background.
        /// </summary>
        public bool AnalyzeInBackground = true;

        /// <summary>
        /// Compilation mode
        /// </summary>
        public CompilationMode CompilationMode;

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        public bool UseRoslynAnalyzers;

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        public bool FailBuildOnIssues;

        [SerializeField]
        List<Rule> m_Rules = new List<Rule>();

        public int NumRules => m_Rules.Count;

        public void AddRule(Rule ruleToAdd)
        {
            if (string.IsNullOrEmpty(ruleToAdd.filter))
            {
                ruleToAdd.filter = string.Empty; // make sure it's empty, as opposed to null

                var rules = m_Rules.Where(r => r.id == ruleToAdd.id).ToArray();
                foreach (var ruleToDelete in rules)
                    m_Rules.Remove(ruleToDelete);
            }

            m_Rules.Add(ruleToAdd);
        }

        public Rule GetRule(Descriptor descriptor, string filter = "")
        {
            // do not use Linq to avoid managed allocations
            foreach (var r in m_Rules)
            {
                if (r.id == descriptor.id && r.filter.Equals(filter))
                    return r;
            }
            return null;
        }

        public void ClearAllRules()
        {
            m_Rules.Clear();
        }

        public void ClearRules(Descriptor descriptor, string filter = "")
        {
            var rules = m_Rules.Where(r => r.id == descriptor.id && r.filter.Equals(filter)).ToArray();

            foreach (var rule in rules)
                m_Rules.Remove(rule);
        }

        public Severity GetAction(Descriptor descriptor, string filter = "")
        {
            // is there a rule that matches the filter?
            var projectRule = GetRule(descriptor, filter);
            if (projectRule != null)
                return projectRule.severity;

            // is there a rule that matches descriptor?
            projectRule = GetRule(descriptor);
            if (projectRule != null)
                return projectRule.severity;

            // return the default descriptor action
            return descriptor.severity;
        }
    }
}
