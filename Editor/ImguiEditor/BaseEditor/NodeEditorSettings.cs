namespace UniGame.UniNodes.NodeSystem.Inspector.Editor.UniGraphWindowInspector.BaseEditor
{
    using System;
    using UniModules.UniGame.Core.Runtime.DataStructure;
    using UnityEngine;

    public class StringColorMap : SerializableDictionary<string, Color>
    {
    }

    [Serializable]
    public class GameFlowEditorSettings
    {
        [SerializeField] private Color32 _gridLineColor = new Color(0.45f, 0.45f, 0.45f);

        public Color32 gridLineColor {
            get { return _gridLineColor; }
            set {
                _gridLineColor = value;
                _gridTexture   = null;
                _crossTexture  = null;
            }
        }

        [SerializeField] private Color32 _gridBgColor = new Color(0.18f, 0.18f, 0.18f);

        public Color32 gridBgColor {
            get { return _gridBgColor; }
            set {
                _gridBgColor = value;
                _gridTexture = null;
            }
        }

        public                   Color32 highlightColor = new Color32(255, 255, 255, 255);
        public                   bool    gridSnap       = true;
        public                   bool    autoSave       = false;
        [SerializeField] private string  typeColorsData = "";

        public StringColorMap typeColors = new StringColorMap();

        public NodeEditorNoodleType noodleType = NodeEditorNoodleType.Angled;

        public bool isDebug = false;

        private Texture2D _gridTexture;

        public Texture2D gridTexture {
            get {
                if (_gridTexture == null) _gridTexture = NodeEditorResources.GenerateGridTexture(gridLineColor, gridBgColor);
                return _gridTexture;
            }
        }

        private Texture2D _crossTexture;

        public Texture2D crossTexture {
            get {
                if (_crossTexture == null) _crossTexture = NodeEditorResources.GenerateCrossTexture(gridLineColor);
                return _crossTexture;
            }
        }
    }
}