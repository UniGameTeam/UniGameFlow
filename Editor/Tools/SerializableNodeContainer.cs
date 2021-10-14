﻿namespace UniModules.UniGameFlow.GameFlowEditor.Editor.Tools
{
    using global::UniModules.GameFlow.Runtime.Core;
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine;

    public class SerializableNodeContainer : ScriptableObject
    {
        public MonoScript script;
        [HideInInspector]
        public string type;
        [HideInInspector]
        public string fullType;
        
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.HideLabel]
        [Sirenix.OdinInspector.InlineProperty]
        [Sirenix.OdinInspector.CustomValueDrawer(nameof(DrawNode))]
#endif
        [SerializeReference]
        public SerializableNode node;
        
        public NodeGraph graph;
        
        public SerializableNodeContainer Initialize(SerializableNode target, NodeGraph graphData)
        {
            node = target;
            graph = graphData;
            var nodeType = node?.GetType();
            type     = node?.GetType().Name;
            script   = nodeType.GetScriptAsset();
            fullType = node?.GetType().AssemblyQualifiedName;
            return this;
        }

#if ODIN_INSPECTOR

        public SerializableNode DrawNode(SerializableNode target, GUIContent label)
        {
            target.DrawSerializableNode(graph);
            return node;
        }
        
#endif
        
    }
}
