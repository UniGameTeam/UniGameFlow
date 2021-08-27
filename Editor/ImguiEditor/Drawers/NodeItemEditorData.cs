﻿namespace UniGame.UniNodes.NodeSystem.Inspector.Editor.UniGraphWindowInspector.Drawers
{
    using System;
    using UniModules.GameFlow.Runtime.Interfaces;
    using UnityEditor;

    public class PropertyEditorData
    {
        public Type               Type;
        public string             Name;
        public string             Tooltip;
        public object             Target;
        public object             Source;
        public SerializedProperty Property;
    }
}