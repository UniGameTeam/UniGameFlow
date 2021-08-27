﻿namespace UniGame.UniNodes.NodeSystem.Inspector.Editor.UniGraphWindowInspector.BaseEditor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UniModules.GameFlow.Runtime.Core;
    using UniModules.GameFlow.Runtime.Interfaces;
    using UnityEditor;
    using UnityEngine;

    /// <summary> Contains reflection-related info </summary>
    public partial class NodeEditorWindow
    {
        /// <summary> Custom node tint colors defined with [NodeColor(r, g, b)] </summary>
        public static Dictionary<Type, Color> nodeTint
        {
            get { return _nodeTint != null ? _nodeTint : _nodeTint = GetNodeTint(); }
        }

        [NonSerialized] private static Dictionary<Type, Color> _nodeTint;

        /// <summary> Custom node widths defined with [NodeWidth(width)] </summary>
        public static Dictionary<Type, int> nodeWidth
        {
            get { return _nodeWidth != null ? _nodeWidth : _nodeWidth = GetNodeWidth(); }
        }

        [NonSerialized] private static Dictionary<Type, int> _nodeWidth;

        
        /// <summary> All available node types </summary>
        public static List<Type> NodeTypes => NodeEditorUtilities.NodeTypes;

        private Func<bool> isDocked
        {
            get
            {
                if (_isDocked == null)
                {
                    var fullBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                               BindingFlags.Static;
                    var isDockedMethod =
                        typeof(NodeEditorWindow).GetProperty("docked", fullBinding).GetGetMethod(true);
                    _isDocked = (Func<bool>) Delegate.CreateDelegate(typeof(Func<bool>), this, isDockedMethod);
                }

                return _isDocked;
            }
        }

        private Func<bool> _isDocked;

        public static Dictionary<Type, Color> GetNodeTint()
        {
            var tints = new Dictionary<Type, Color>();
            for (var i = 0; i < NodeTypes.Count; i++)
            {
                var attribs = NodeTypes[i].GetCustomAttributes(typeof(NodeTint), true);
                if (attribs == null || attribs.Length == 0) continue;
                var attrib = attribs[0] as NodeTint;
                tints.Add(NodeTypes[i], attrib.color);
            }

            return tints;
        }

        public static Dictionary<Type, int> GetNodeWidth()
        {
            var widths = new Dictionary<Type, int>();
            for (var i = 0; i < NodeTypes.Count; i++)
            {
                var attribs = NodeTypes[i].GetCustomAttributes(typeof(NodeWidth), true);
                if (attribs == null || attribs.Length == 0) continue;
                var attrib = attribs[0] as NodeWidth;
                widths.Add(NodeTypes[i], attrib.width);
            }

            return widths;
        }

        public static object ObjectFromType(Type type)
        {
            return Activator.CreateInstance(type);
        }

        public static object ObjectFromFieldName(object obj, string fieldName)
        {
            var type = obj.GetType();
            var fieldInfo = type.GetField(fieldName);
            return fieldInfo.GetValue(obj);
        }

        public static KeyValuePair<ContextMenu, MethodInfo>[] GetContextMenuMethods(object obj)
        {
            var type = obj.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                                   BindingFlags.NonPublic);
            var kvp = new List<KeyValuePair<ContextMenu, MethodInfo>>();
            for (var i = 0; i < methods.Length; i++)
            {
                var attribs = methods[i].GetCustomAttributes(typeof(ContextMenu), true)
                    .Select(x => x as ContextMenu).ToArray();
                if (attribs == null || attribs.Length == 0) continue;
                if (methods[i].GetParameters().Length != 0)
                {
                    Debug.LogWarning("Method " + methods[i].DeclaringType.Name + "." + methods[i].Name +
                                     " has parameters and cannot be used for context menu commands.");
                    continue;
                }

                if (methods[i].IsStatic)
                {
                    Debug.LogWarning("Method " + methods[i].DeclaringType.Name + "." + methods[i].Name +
                                     " is static and cannot be used for context menu commands.");
                    continue;
                }

                for (var k = 0; k < attribs.Length; k++)
                {
                    kvp.Add(new KeyValuePair<ContextMenu, MethodInfo>(attribs[k], methods[i]));
                }
            }
#if UNITY_5_5_OR_NEWER
            //Sort menu items
            kvp.Sort((x, y) => x.Key.priority.CompareTo(y.Key.priority));
#endif
            return kvp.ToArray();
        }

        /// <summary> Very crude. Uses a lot of reflection. </summary>
        public static void OpenPreferences()
        {
            try
            {
                //Open preferences window
                var assembly = Assembly.GetAssembly(typeof(EditorWindow));
                var type = assembly.GetType("UnityEditor.PreferencesWindow");
                type.GetMethod("ShowPreferencesWindow", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, null);

                //Get the window
                var window = GetWindow(type);

                //Make sure custom sections are added (because waiting for it to happen automatically is too slow)
                var refreshField = type.GetField("m_RefreshCustomPreferences",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if ((bool) refreshField.GetValue(window))
                {
                    type.GetMethod("AddCustomSections", BindingFlags.NonPublic | BindingFlags.Instance)
                        .Invoke(window, null);
                    refreshField.SetValue(window, false);
                }

                //Get sections
                var sectionsField = type.GetField("m_Sections", BindingFlags.Instance | BindingFlags.NonPublic);
                var sections = sectionsField.GetValue(window) as IList;

                //Iterate through sections and check contents
                var sectionType = sectionsField.FieldType.GetGenericArguments()[0];
                var sectionContentField =
                    sectionType.GetField("content", BindingFlags.Instance | BindingFlags.Public);
                for (var i = 0; i < sections.Count; i++)
                {
                    var sectionContent = sectionContentField.GetValue(sections[i]) as GUIContent;
                    if (sectionContent.text == "Node Editor")
                    {
                        //Found contents - Set index
                        var sectionIndexField = type.GetField("m_SelectedSectionIndex",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        sectionIndexField.SetValue(window, i);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                Debug.LogWarning(
                    "Unity has changed around internally. Can't open properties through reflection. Please contact UniNodeSystem developer and supply unity version number.");
            }
        }
    }
}