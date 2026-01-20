using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARCeye
{
    /// <summary>
    /// VLSDK Project Validation Window
    /// Similar to XR Plugin Management's Project Validation
    /// </summary>
    public class VLSDKProjectValidationWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<VLSDKValidationRule> validationRules;
        private Dictionary<VLSDKValidationRule, bool> ruleResults;
        private bool showAll = true;
        private bool stylesInitialized = false;

        [MenuItem("ARC eye/VLSDK/Project Validation")]
        public static void ShowWindow()
        {
            var window = GetWindow<VLSDKProjectValidationWindow>("VLSDK Project Validation");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshValidation();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            stylesInitialized = true;
        }

        private void RefreshValidation()
        {
            VLSDKProjectValidation.RefreshRules();
            validationRules = VLSDKProjectValidation.GetRules();
            ruleResults = new Dictionary<VLSDKValidationRule, bool>();

            foreach (var rule in validationRules)
            {
                bool passed = rule.checkPredicate?.Invoke() ?? true;
                ruleResults[rule] = passed;
            }
        }

        private void FixAllIssues()
        {
            foreach (var kvp in ruleResults)
            {
                if (!kvp.Value && kvp.Key.CanFix)
                {
                    kvp.Key.fixIt?.Invoke();
                }
            }
            RefreshValidation();
        }

        private void OnGUI()
        {
            InitializeStyles();

            // Header with issue count
            DrawHeader();

            EditorGUILayout.Space(5);

            // Validation rules list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawValidationRules();
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            int totalRules = ruleResults.Count;
            int issueCount = ruleResults.Count(kvp => !kvp.Value);
            int fixableIssueCount = ruleResults.Count(kvp => !kvp.Value && kvp.Key.CanFix);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // "Issues (X) of Checks (Y)"
            GUILayout.Label($"Issues ({issueCount}) of Checks ({totalRules})", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Fix all button
            EditorGUI.BeginDisabledGroup(fixableIssueCount == 0);
            if (GUILayout.Button("Fix All", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                FixAllIssues();
            }
            EditorGUI.EndDisabledGroup();

            // Refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshValidation();
            }

            // Show all toggle
            showAll = GUILayout.Toggle(showAll, "Show all", EditorStyles.toolbarButton, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationRules()
        {
            if (validationRules == null || validationRules.Count == 0)
            {
                EditorGUILayout.LabelField("No validation rules found.");
                return;
            }

            foreach (var rule in validationRules)
            {
                bool passed = ruleResults.ContainsKey(rule) && ruleResults[rule];

                // Filter by showAll toggle
                if (!showAll && passed)
                    continue;

                DrawValidationRule(rule, passed);
            }
        }

        private void DrawValidationRule(VLSDKValidationRule rule, bool passed)
        {
            // Background color based on state
            Color backgroundColor;
            if (passed)
            {
                backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            }
            else if (rule.error)
            {
                // Error: red background
                backgroundColor = new Color(0.5f, 0.2f, 0.2f, 0.3f);
            }
            else
            {
                // Warning: yellow/orange background
                backgroundColor = new Color(0.5f, 0.4f, 0.1f, 0.3f);
            }

            Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(24));
            EditorGUI.DrawRect(rect, backgroundColor);

            GUILayout.Space(5);

            // Icon with color (✓ for passed, ✗ for error, ⚠ for warning) - vertically centered
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUIStyle iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 14;
            iconStyle.alignment = TextAnchor.MiddleCenter;

            if (passed)
            {
                iconStyle.normal.textColor = Color.green;
                GUILayout.Label("✓", iconStyle, GUILayout.Width(20), GUILayout.Height(18));
            }
            else if (rule.error)
            {
                // Error icon in red
                iconStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
                GUILayout.Label("✗", iconStyle, GUILayout.Width(20), GUILayout.Height(18));
            }
            else
            {
                // Warning icon in yellow
                iconStyle.normal.textColor = new Color(1f, 0.8f, 0f);
                GUILayout.Label("⚠", iconStyle, GUILayout.Width(20), GUILayout.Height(18));
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Category prefix in brackets
            string categoryPrefix = !string.IsNullOrEmpty(rule.category) ? $"[{rule.category}] " : "";

            // Rule message - vertically centered
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUIStyle messageStyle = new GUIStyle(EditorStyles.label);
            messageStyle.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label(categoryPrefix + rule.message, messageStyle, GUILayout.Height(18));

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Fix or Edit button - vertically centered
            GUILayout.BeginVertical();
            GUILayout.Space(3);
            if (!passed && rule.CanFix)
            {
                if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(50), GUILayout.Height(18)))
                {
                    rule.fixIt?.Invoke();
                    RefreshValidation();
                }
            }
            else if (!passed && !rule.CanFix && !string.IsNullOrEmpty(rule.fixItMessage))
            {
                if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50), GUILayout.Height(18)))
                {
                    // Open relevant settings window
                    string settingsPath = !string.IsNullOrEmpty(rule.settingsPath) ? rule.settingsPath : "Project/Player";
                    SettingsService.OpenProjectSettings(settingsPath);
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.EndHorizontal();
        }
    }
}
