﻿namespace UniGame.UniNodes.NodeSystem.Inspector.Editor.UniGraphWindowInspector.Nodes
{
    using System.Collections.Generic;
    using System.Reflection;
    using BaseEditor;
    using BaseEditor.Extensions;
    using Drawers;
    using Drawers.Interfaces;
    using UniModules.GameFlow.Runtime.Core;
    using UniModules.GameFlow.Runtime.Core.Interfaces;
    using UniModules.GameFlow.Runtime.Extensions;
    using UniModules.GameFlow.Runtime.Interfaces;
    using Styles;
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine;

    [CustomNodeEditor(typeof(UniNode))]
    public class UniNodeEditor : NodeEditor, IUniNodeEditor
    {
        protected List<INodeEditorHandler> bodyDrawers = new List<INodeEditorHandler>();

        public override void OnHeaderGUI()
        {
            if (IsSelected)
            {
                EditorDrawerUtils.DrawWithContentColor(Color.red, base.OnHeaderGUI);
                return;
            }
            base.OnHeaderGUI();

        }

        public override void OnBodyGUI()
        {
            var node = Node as IUniNode;
            if (node == null) return;
            
            var idEditingMode = EditorApplication.isPlayingOrWillChangePlaymode == false && 
                                EditorApplication.isCompiling == false && 
                                EditorApplication.isUpdating == false;

            if (idEditingMode) {
                node.UpdateNodePorts();
                node.Validate();
            }
            
            base.OnBodyGUI();

            DrawPorts(node);

            SerializedObject?.ApplyModifiedPropertiesWithoutUndo();
            
        }

        public void DrawPorts(IUniNode node)
        {
            Draw(bodyDrawers);
        }
        

        protected override void OnEditorEnabled()
        {
            base.OnEditorEnabled();
            bodyDrawers = InitializeBodyHandlers(bodyDrawers);
        }
        
        protected virtual List<INodeEditorHandler> InitializeBodyHandlers(List<INodeEditorHandler> drawers)
        {
            drawers.Add(new UniPortsDrawer(new PortStyleSelector()));
            return drawers;
        }


    }
}