﻿namespace UniGreenModules.UniNodeSystem.Inspector.Editor.BaseEditor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Runtime.Core;
    using UniCore.EditorTools.Editor.Utility;
    using UniCore.Runtime.ProfilerTools;
    using UniGameFlow.UniNodesSystem.Assets.UniGame.UniNodes.NodeSystem.Runtime.Attributes;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public struct NodeEditorGuiState
    {
        public Event        Event;
        public Vector2      MousePosition;
        public List<Object> PreSelection;
        public EventType EventType;
    }
    
    /// <summary> Contains GUI methods </summary>
    public partial class NodeEditorWindow
    {
        public  NodeGraphEditor   graphEditor;
        private List<Object>      selectionCache;
        private List<UniBaseNode> culledNodes;

        private List<UniBaseNode> _regularNodes  = new List<UniBaseNode>();
        private List<UniBaseNode> _selectedNodes = new List<UniBaseNode>();

        private int topPadding {
            get { return isDocked() ? 19 : 22; }
        }

        private void OnGUI()
        {
            var e = Event.current;
            var m = GUI.matrix;
            if (ActiveGraph == null) {
                if (NodeGraph.ActiveGraphs.Count > 0) {
                    Initialize(NodeGraph.ActiveGraphs.FirstOrDefault());
                }
                return;
            }

            graphEditor          = NodeGraphEditor.GetEditor(ActiveGraph);
            graphEditor.position = position;

            Controls();

            DrawGrid(position, Zoom, PanOffset);
            DrawConnections();
            DrawDraggedConnection();
            DrawZoomedNodes();
            DrawSelectionBox();
            DrawTooltip();
            DrawGraphsButtons();

            graphEditor.OnGUI();

            GUI.matrix = m;
        }

        public static void BeginZoomed(Rect rect, float zoom, float topPadding)
        {
            GUI.EndClip();

            GUIUtility.ScaleAroundPivot(Vector2.one / zoom, rect.size * 0.5f);
            var padding = new Vector4(0, topPadding, 0, 0);
            padding *= zoom;

            GUI.BeginClip(new Rect(-((rect.width * zoom) - rect.width) * 0.5f,
                -(((rect.height * zoom) - rect.height) * 0.5f) + (topPadding * zoom),
                rect.width * zoom,
                rect.height * zoom));
        }

        public static void EndZoomed(Rect rect, float zoom, float topPadding)
        {
            GUIUtility.ScaleAroundPivot(Vector2.one * zoom, rect.size * 0.5f);
            var offset = new Vector3(
                (((rect.width * zoom) - rect.width) * 0.5f),
                (((rect.height * zoom) - rect.height) * 0.5f) + (-topPadding * zoom) + topPadding,
                0);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
        }

        public void DrawGrid(Rect rect, float zoom, Vector2 panOffset)
        {
            rect.position = Vector2.zero;

            var center   = rect.size / 2f;
            var gridTex  = graphEditor.GetGridTexture();
            var crossTex = graphEditor.GetSecondaryGridTexture();

            // Offset from origin in tile units
            var xOffset = -(center.x * zoom + panOffset.x) / gridTex.width;
            var yOffset = ((center.y - rect.size.y) * zoom + panOffset.y) / gridTex.height;

            var tileOffset = new Vector2(xOffset, yOffset);

            // Amount of tiles
            var tileAmountX = Mathf.Round(rect.size.x * zoom) / gridTex.width;
            var tileAmountY = Mathf.Round(rect.size.y * zoom) / gridTex.height;

            var tileAmount = new Vector2(tileAmountX, tileAmountY);

            // Draw tiled background
            GUI.DrawTextureWithTexCoords(rect, gridTex, new Rect(tileOffset, tileAmount));
            GUI.DrawTextureWithTexCoords(rect, crossTex, new Rect(tileOffset + new Vector2(0.5f, 0.5f), tileAmount));
        }

        public void DrawSelectionBox()
        {
            if (currentActivity == NodeActivity.DragGrid) {
                var curPos = WindowToGridPosition(Event.current.mousePosition);
                var size   = curPos - dragBoxStart;
                var r      = new Rect(dragBoxStart, size);
                r.position =  GridToWindowPosition(r.position);
                r.size     /= Zoom;
                Handles.DrawSolidRectangleWithOutline(r, new Color(0, 0, 0, 0.1f), new Color(1, 1, 1, 0.6f));
            }
        }

        public static bool DropdownButton(string name, float width)
        {
            return GUILayout.Button(name, EditorStyles.toolbarDropDown, GUILayout.Width(width));
        }

        /// <summary> Show right-click context menu for hovered reroute </summary>
        void ShowRerouteContextMenu(RerouteReference reroute)
        {
            var contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Remove"), false, () => reroute.RemovePoint());
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));

            //if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
        }

        /// <summary> Show right-click context menu for hovered port </summary>
        void ShowPortContextMenu(INodePort hoveredPort)
        {
            var contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Clear Connections"), false, hoveredPort.ClearConnections);
            contextMenu.AddItem(new GUIContent("Show Content"), false, hoveredPort.ClearConnections);
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
            
        }

        /// <summary> Show right-click context menu for selected nodes </summary>
        public void ShowNodeContextMenu()
        {
            var contextMenu = new GenericMenu();
            // If only one node is selected
            if (Selection.objects.Length == 1 && Selection.activeObject is UniBaseNode) {
                var node = Selection.activeObject as UniBaseNode;
                contextMenu.AddItem(new GUIContent("Move To Top"), false, () => MoveNodeToTop(node));
                contextMenu.AddItem(new GUIContent("Rename"), false, RenameSelectedNode);
            }

            contextMenu.AddItem(new GUIContent("Duplicate"), false, DublicateSelectedNodes);
            contextMenu.AddItem(new GUIContent("Remove"), false, RemoveSelectedNodes);

            // If only one node is selected
            if (Selection.objects.Length == 1 && Selection.activeObject is UniBaseNode) {
                var node = Selection.activeObject as UniBaseNode;
                AddCustomContextMenuItems(contextMenu, node);
            }

            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        /// <summary> Show right-click context menu for current graph </summary>
        void ShowGraphContextMenu()
        {
            var contextMenu = new GenericMenu();
            var pos         = WindowToGridPosition(Event.current.mousePosition);
            for (var i = 0; i < NodeTypes.Count; i++) {
                var type = NodeTypes[i];

                if(IsValidNode(type) == false)
                    continue;
                
                //Get node context menu path
                var path = graphEditor.GetNodeMenuName(type);
                if (string.IsNullOrEmpty(path)) continue;

                contextMenu.AddItem(new GUIContent(path), false, () => { CreateNode(type, pos); });
            }

            contextMenu.AddSeparator("");
            contextMenu.AddItem(new GUIContent("Preferences"), false, () => OpenPreferences());
            AddCustomContextMenuItems(contextMenu, ActiveGraph);
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private bool IsValidNode(Type nodeType)
        {
            return !NodeEditorUtilities.GetAttrib<HideNodeAttribute>(nodeType, out var hideNodeAttribute);
        }

        void AddCustomContextMenuItems(GenericMenu contextMenu, object obj)
        {
            var items = GetContextMenuMethods(obj);
            if (items.Length != 0) {
                contextMenu.AddSeparator("");
                for (var i = 0; i < items.Length; i++) {
                    var kvp = items[i];
                    contextMenu.AddItem(new GUIContent(kvp.Key.menuItem), false, () => kvp.Value.Invoke(obj, null));
                }
            }
        }

        /// <summary> Draw a bezier from startpoint to endpoint, both in grid coordinates </summary>
        public void DrawConnection(Vector2 startPoint, Vector2 endPoint, Color col)
        {
            startPoint = GridToWindowPosition(startPoint);
            endPoint   = GridToWindowPosition(endPoint);

            switch (NodeEditorPreferences.GetSettings().noodleType) {
                case NodeEditorNoodleType.Curve:
                    var startTangent = startPoint;
                    if (startPoint.x < endPoint.x) startTangent.x = Mathf.LerpUnclamped(startPoint.x, endPoint.x, 0.7f);
                    else startTangent.x                           = Mathf.LerpUnclamped(startPoint.x, endPoint.x, -0.7f);

                    var endTangent = endPoint;
                    if (startPoint.x > endPoint.x) endTangent.x = Mathf.LerpUnclamped(endPoint.x, startPoint.x, -0.7f);
                    else endTangent.x                           = Mathf.LerpUnclamped(endPoint.x, startPoint.x, 0.7f);
                    Handles.DrawBezier(startPoint, endPoint, startTangent, endTangent, col, null, 4);
                    break;
                case NodeEditorNoodleType.Line:
                    Handles.color = col;
                    Handles.DrawAAPolyLine(5, startPoint, endPoint);
                    break;
                case NodeEditorNoodleType.Angled:
                    Handles.color = col;
                    if (startPoint.x <= endPoint.x - (50 / Zoom)) {
                        var midpoint = (startPoint.x + endPoint.x) * 0.5f;
                        var start_1  = startPoint;
                        var end_1    = endPoint;
                        start_1.x = midpoint;
                        end_1.x   = midpoint;
                        Handles.DrawAAPolyLine(5, startPoint, start_1);
                        Handles.DrawAAPolyLine(5, start_1, end_1);
                        Handles.DrawAAPolyLine(5, end_1, endPoint);
                    }
                    else {
                        var midpoint = (startPoint.y + endPoint.y) * 0.5f;
                        var start_1  = startPoint;
                        var end_1    = endPoint;
                        start_1.x += 25 / Zoom;
                        end_1.x   -= 25 / Zoom;
                        var start_2 = start_1;
                        var end_2   = end_1;
                        start_2.y = midpoint;
                        end_2.y   = midpoint;
                        Handles.DrawAAPolyLine(5, startPoint, start_1);
                        Handles.DrawAAPolyLine(5, start_1, start_2);
                        Handles.DrawAAPolyLine(5, start_2, end_2);
                        Handles.DrawAAPolyLine(5, end_2, end_1);
                        Handles.DrawAAPolyLine(5, end_1, endPoint);
                    }

                    break;
            }
        }

        /// <summary> Draws all connections </summary>
        public void DrawConnections()
        {
            var mousePos = Event.current.mousePosition;
            var selection = preBoxSelectionReroute != null
                ? new List<RerouteReference>(preBoxSelectionReroute)
                : new List<RerouteReference>();
            hoveredReroute = new RerouteReference();

            var col = GUI.color;
            foreach (var node in ActiveGraph.nodes) {
                //If a null node is found, return. This can happen if the nodes associated script is deleted. It is currently not possible in Unity to delete a null asset.
                if (node == null) continue;

                // Draw full connections and output > reroute
                foreach (var output in node.Outputs) {
                    //Needs cleanup. Null checks are ugly
                    var item = _portConnectionPoints.FirstOrDefault(x => x.Key.Id == output.Id);
                    if (item.Key == null) continue;

                    var fromRect        = item.Value;
                    var connectionColor = graphEditor.GetTypeColor(output.ValueType);

                    for (var k = 0; k < output.ConnectionCount; k++) {
                        var input = output.GetConnection(k);

                        // Error handling
                        if (input == null)
                            continue; //If a script has been updated and the port doesn't exist, it is removed and null is returned. If this happens, return.
                        if (!input.IsConnectedTo(output)) input.Connect(output);
                        Rect toRect;
                        if (!_portConnectionPoints.TryGetValue(input, out toRect)) continue;

                        var from          = fromRect.center;
                        var to            = Vector2.zero;
                        var reroutePoints = output.GetReroutePoints(k);
                        // Loop through reroute points and draw the path
                        for (var i = 0; i < reroutePoints.Count; i++) {
                            to = reroutePoints[i];
                            DrawConnection(from, to, connectionColor);
                            from = to;
                        }

                        to = toRect.center;

                        DrawConnection(from, to, connectionColor);

                        // Loop through reroute points again and draw the points
                        for (var i = 0; i < reroutePoints.Count; i++) {
                            var rerouteRef = new RerouteReference(output, k, i);
                            // Draw reroute point at position
                            var rect = new Rect(reroutePoints[i], new Vector2(12, 12));
                            rect.position = new Vector2(rect.position.x - 6, rect.position.y - 6);
                            rect          = GridToWindowRect(rect);

                            // Draw selected reroute points with an outline
                            if (selectedReroutes.Contains(rerouteRef)) {
                                GUI.color = NodeEditorPreferences.GetSettings().highlightColor;
                                GUI.DrawTexture(rect, NodeEditorResources.dotOuter);
                            }

                            GUI.color = connectionColor;
                            GUI.DrawTexture(rect, NodeEditorResources.dot);
                            if (rect.Overlaps(selectionBox)) selection.Add(rerouteRef);
                            if (rect.Contains(mousePos)) hoveredReroute = rerouteRef;
                        }
                    }
                }
            }

            GUI.color = col;
            if (Event.current.type != EventType.Layout && currentActivity == NodeActivity.DragGrid)
                selectedReroutes = selection;
        }

        private Vector2 _nodeGraphScroll;
        private bool    _showGraphsList;

        private void DrawGraphsButtons()
        {
            if (NodeGraphs == null) {
                UpdateEditorNodeGraphs();
            }

            DrawTopButtons();

            DrawActiveGraphs();
        }

        private Vector2 _activeGraphsScroll;

        private void DrawActiveGraphs()
        {
            return;
            EditorDrawerUtils.DrawHorizontalLayout(() => {
                _activeGraphsScroll = EditorDrawerUtils.DrawScroll(_activeGraphsScroll, () => {
                    EditorGUILayout.BeginHorizontal();

                    foreach (var graph in NodeGraph.ActiveGraphs) {
                        if (!graph) continue;
                        EditorDrawerUtils.DrawButton(graph.name, () => { Open(graph); }, GUILayout.Height(30), GUILayout.MaxWidth(200));
                    }

                    EditorGUILayout.EndHorizontal();
                }, GUILayout.ExpandWidth(true));
            }, GUILayout.Height(50), GUILayout.ExpandWidth(true));
        }

        private void DrawTopButtons()
        {
            EditorDrawerUtils.DrawHorizontalLayout(() => {
                
                EditorDrawerUtils.DrawButton("Apply Prefab", () => Open(Save(ActiveGraph)),
                    GUILayout.Height(20), GUILayout.Width(200));
                
                EditorDrawerUtils.DrawButton("Refresh", Refresh,
                    GUILayout.Height(20), GUILayout.Width(200));
                
            }, GUILayout.Height(100));
        }

        private void DrawNodes(Event activeEvent)
        {
            var mousePos = Event.current.mousePosition;

            if (activeEvent.type != EventType.Layout) {
                hoveredNode = null;
                hoveredPort = null;
            }

            var preSelection = preBoxSelection != null
                ? new List<Object>(preBoxSelection)
                : new List<Object>();

            // Selection box stuff
            var boxStartPos = GridToWindowPositionNoClipped(dragBoxStart);

            var boxSize = mousePos - boxStartPos;
            if (boxSize.x < 0) {
                boxStartPos.x += boxSize.x;
                boxSize.x     =  Mathf.Abs(boxSize.x);
            }

            if (boxSize.y < 0) {
                boxStartPos.y += boxSize.y;
                boxSize.y     =  Mathf.Abs(boxSize.y);
            }

            var selectionBox = new Rect(boxStartPos, boxSize);

            if (activeEvent.type == EventType.Layout)
                culledNodes = new List<UniBaseNode>();

            var nodes = ActiveGraph.nodes;
            for (int i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                if (Selection.Contains(node)) {
                    _selectedNodes.Add(node);
                    continue;
                }

                _regularNodes.Add(node);
            }

            var eventType = activeEvent.type == EventType.Ignore ||
                activeEvent.rawType == EventType.Ignore ? EventType.Ignore : 
                activeEvent.type;
            
            var editorGuiState = new NodeEditorGuiState() {
                MousePosition = mousePos,
                PreSelection = preSelection,
                Event =  activeEvent,
                EventType = eventType,
            };
            
            EditorDrawerUtils.DrawAndRevertColor(() => {
                DrawNodes(_regularNodes, editorGuiState);
                DrawNodes(_selectedNodes, editorGuiState);
            });

            _regularNodes.Clear();
            _selectedNodes.Clear();

            if (activeEvent.type != EventType.Layout && currentActivity == NodeActivity.DragGrid)
                Selection.objects = preSelection.ToArray();
        }

        private void DrawNodes(List<UniBaseNode> nodes, NodeEditorGuiState state)
        {
            for (var n = 0; n < nodes.Count; n++) {
                // Skip null nodes. The user could be in the process of renaming scripts, so removing them at this point is not advisable.
                var node = nodes[n];
                if (nodes[n] == null) continue;
                EditorDrawerUtils.DrawAndRevertColor(() => DrawNode(node, state));
            }
        }

        private void DrawZoomedNodes()
        {
            var e = Event.current;
            if (e.type == EventType.Layout) {
                selectionCache = new List<Object>(Selection.objects);
            }

            //Active node is hashed before and after node GUI to detect changes
            var        nodeHash   = 0;
            MethodInfo onValidate = null;
            if (Selection.activeObject != null && Selection.activeObject is UniBaseNode) {
                onValidate = Selection.activeObject.GetType().GetMethod("OnValidate");
                if (onValidate != null) nodeHash = Selection.activeObject.GetHashCode();
            }

            EditorDrawerUtils.DrawZoom(() => DrawNodes(e), position, Zoom, topPadding);

            //If a change in hash is detected in the selected node, call OnValidate method. 
            //This is done through reflection because OnValidate is only relevant in editor, 
            //and thus, the code should not be included in build.
            if (nodeHash != 0) {
                if (onValidate != null && nodeHash != Selection.activeObject.GetHashCode())
                    onValidate.Invoke(Selection.activeObject, null);
            }
        }

        private void DrawNode(UniBaseNode node, NodeEditorGuiState state)
        {
            switch (state.EventType) {
                case EventType.Ignore:    
                    return;
                // Culling
                case EventType.Layout: {
                    // Cull unselected nodes outside view
                    if (!Selection.Contains(node) && ShouldBeCulled(node)) {
                        culledNodes.Add(node);
                        return;
                    }
                    break;
                }
                default: {
                    if (culledNodes.Contains(node))
                        return;
                    break;
                }
            }

            if (state.EventType == EventType.Repaint) {
                _portConnectionPoints = _portConnectionPoints.Where(x => x.Key.Node != node)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            NodeEditor.PortPositions = new Dictionary<NodePort, Vector2>();

            DrawNodeArea(node, state);
        }

        private void DrawNodeArea(UniBaseNode node,NodeEditorGuiState state)
        {
            var nodeEditor = NodeEditor.GetEditor(node);
            //Get node position
            var nodePos = GridToWindowPositionNoClipped(node.position);

            var rectArea = new Rect(nodePos, new Vector2(nodeEditor.GetWidth(), 4000));

            DrawNodeArea(nodeEditor,node,nodePos,rectArea,state);
        }
        
        private void DrawNodeArea(
            NodeEditor nodeEditor,
            UniBaseNode node, 
            Vector2 nodePos,
            Rect rectArea, NodeEditorGuiState state)
        {
            if (state.EventType == EventType.Ignore) return;

            try {
                GUILayout.BeginArea(rectArea);

                DrawNodeEditorArea(nodeEditor, node, state);
            
                DrawNodePorts(node,nodePos, state);
            }
            catch (Exception e) {
                GameLog.LogWarning(e.Message);
                GUIUtility.ExitGUI();
            }

            GUILayout.EndArea();
        }

        private void DrawNodePorts(
            UniBaseNode node, 
            Vector2 nodePos,
            NodeEditorGuiState state)
        {
            var eventType  = state.EventType;
            var stateEvent = state.Event;
            if (eventType == EventType.Ignore ||
                stateEvent.type == EventType.Ignore || 
                stateEvent.rawType == EventType.Ignore)
                return;
            
            var mousePos = state.MousePosition;
            var preSelection = state.PreSelection;
            
            //Check if we are hovering this node
            var nodeSize                                   = GUILayoutUtility.GetLastRect().size;
            var windowRect                                 = new Rect(nodePos, nodeSize);
            if (windowRect.Contains(mousePos)) hoveredNode = node;

            //If dragging a selection box, add nodes inside to selection
            if (currentActivity == NodeActivity.DragGrid) {
                if (windowRect.Overlaps(selectionBox)) preSelection.Add(node);
            }

            //Check if we are hovering any of this nodes ports
            //Check input ports
            foreach (var input in node.Inputs) {
                //Check if port rect is available
                if (!PortConnectionPoints.ContainsKey(input)) continue;
                var r                                 = GridToWindowRectNoClipped(PortConnectionPoints[input]);
                if (r.Contains(mousePos)) hoveredPort = input;
            }

            //Check all output ports
            foreach (var output in node.Outputs) {
                //Check if port rect is available
                if (!PortConnectionPoints.ContainsKey(output)) continue;
                var r                                 = GridToWindowRectNoClipped(PortConnectionPoints[output]);
                if (r.Contains(mousePos)) hoveredPort = output;
            }

        }
        
        private void DrawNodeEditorArea(NodeEditor nodeEditor, UniBaseNode node, NodeEditorGuiState state)
        {
            var eventType = state.EventType;
            var stateEvent = state.Event;

            if (eventType == EventType.Ignore ||
                stateEvent.type == EventType.Ignore || 
                stateEvent.rawType == EventType.Ignore)
                return;
            
            var guiColor = GUI.color;

            var selected = selectionCache.Contains(node);

            if (selected) {
                var style          = new GUIStyle(nodeEditor.GetBodyStyle());
                var highlightStyle = new GUIStyle(NodeEditorResources.styles.nodeHighlight);
                highlightStyle.padding = style.padding;
                style.padding          = new RectOffset();
                GUI.color              = nodeEditor.GetTint();
                GUILayout.BeginVertical(style);
                GUI.color = NodeEditorPreferences.GetSettings().highlightColor;
                try {
                    GUILayout.BeginVertical(new GUIStyle(highlightStyle));
                }
                catch (Exception e) {
                    
                    GameLog.Log($"EventType {state.EventType} EventData[type: {state.Event.type} rawtype: {state.Event.type}");
                    GameLog.LogError(e);
                    GUILayout.EndVertical();
                    GUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }
            }
            else {
                var style = new GUIStyle(nodeEditor.GetBodyStyle());
                GUI.color = nodeEditor.GetTint();
                GUILayout.BeginVertical(style);
            }

            GUI.color = guiColor;

            EditorGUI.BeginChangeCheck();

            //Draw node contents
            nodeEditor.OnHeaderGUI();
            nodeEditor.OnBodyGUI();

            //If user changed a value, notify other scripts through onUpdateNode
            if (EditorGUI.EndChangeCheck()) {
                if (NodeEditor.OnUpdateNode != null) NodeEditor.OnUpdateNode(node);
                EditorUtility.SetDirty(node);
                nodeEditor.serializedObject.ApplyModifiedProperties();
            }

            GUILayout.EndVertical();

            //Cache data about the node for next frame
            if (state.EventType == EventType.Repaint) {
                var size = GUILayoutUtility.GetLastRect().size;
                if (NodeSizes.ContainsKey(node)) NodeSizes[node] = size;
                else NodeSizes.Add(node, size);

                foreach (var kvp in NodeEditor.PortPositions) {
                    var portHandlePos = kvp.Value;
                    portHandlePos += node.position;
                    var rect = new Rect(portHandlePos.x - 8, portHandlePos.y - 8, 16, 16);
                    if (PortConnectionPoints.ContainsKey(kvp.Key)) PortConnectionPoints[kvp.Key] = rect;
                    else PortConnectionPoints.Add(kvp.Key, rect);
                }
            }

            if (selected) GUILayout.EndVertical();
        }

        private bool ShouldBeCulled(UniBaseNode node)
        {
            var nodePos = GridToWindowPositionNoClipped(node.position);
            if (nodePos.x / _zoom > position.width) return true;  // Right
            if (nodePos.y / _zoom > position.height) return true; // Bottom
            if (NodeSizes.ContainsKey(node)) {
                var size = NodeSizes[node];
                if (nodePos.x + size.x < 0) return true; // Left
                if (nodePos.y + size.y < 0) return true; // Top
            }

            return false;
        }

        private void DrawTooltip()
        {
            if (hoveredPort != null) {
                var type    = hoveredPort.ValueType;
                var content = new GUIContent();
                content.text = type.PrettyName();
                if (hoveredPort.IsOutput) {
                    //TODO DRAW ACTUAL VALUE
                    //var obj = hoveredPort.node.GetValue(hoveredPort);
                    //content.text += " = " + (obj != null ? obj.ToString() : "null");
                }

                var size = NodeEditorResources.styles.tooltip.CalcSize(content);
                var rect = new Rect(Event.current.mousePosition - (size), size);
                EditorGUI.LabelField(rect, content, NodeEditorResources.styles.tooltip);
                Repaint();
            }
        }
    }
}