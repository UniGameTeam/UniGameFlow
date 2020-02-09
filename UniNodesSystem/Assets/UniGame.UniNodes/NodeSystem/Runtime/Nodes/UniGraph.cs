﻿namespace UniGreenModules.UniNodeSystem.Nodes
{
    using System;
    using System.Collections.Generic;
    using Runtime;
    using Runtime.Connections;
    using Runtime.Core;
    using Runtime.Extensions;
    using Runtime.Interfaces;
    using UniCore.Runtime.Interfaces;
    using UniCore.Runtime.ObjectPool;
    using UniCore.Runtime.ObjectPool.Runtime;
    using UniCore.Runtime.ObjectPool.Runtime.Extensions;
    using UniCore.Runtime.Rx.Extensions;
    using UniGameFlow.UniNodesSystem.Assets.UniGame.UniNodes.NodeSystem.Runtime.Attributes;
    using UniRx;
    using UnityEngine;

    [HideNode]
    public class UniGraph : NodeGraph, IUniGraph
    {
        
        #region private properties

        /// <summary>
        /// graph cancelation
        /// </summary>
        [NonSerialized] private List<IGraphCancelationNode> cancelationNodes = new List<IGraphCancelationNode>();
        
        /// <summary>
        /// graph inputs
        /// </summary>
        [NonSerialized] private List<IGraphPortNode> inputs = new List<IGraphPortNode>();
        
        /// <summary>
        /// graph outputs
        /// </summary>
        [NonSerialized] private List<IGraphPortNode> outputs = new List<IGraphPortNode>();

        /// <summary>
        /// all child nodes
        /// </summary>
        [NonSerialized] private List<IUniNode> allNodes = new List<IUniNode>();

        #endregion

        public GameObject AssetInstance => gameObject;

        public IReadOnlyList<IGraphPortNode> OutputsPorts => outputs;
        
        public IReadOnlyList<IGraphPortNode> InputsPorts => inputs;
        
        public override void Dispose() => Exit();
        
        #region private methods

        protected override void OnInitialize()
        {
            
            base.OnInitialize();
            
            InitializeGraphNodes();
            
        }

        protected override void OnExecute()
        {
            ActiveGraphs.Add(this);

            LifeTime.AddCleanUpAction(() => ActiveGraphs.Remove(this));

            allNodes.ForEach( InitializeNode );

            cancelationNodes.ForEach(x => 
                x.PortValue.PortValueChanged.
                                         Subscribe(unit => Exit()).
                                         AddTo(LifeTime));
            
            inputs.ForEach(x => GetPortValue(x.ItemName).Connect(x.PortValue) );
            
            outputs.ForEach(x => GetPortValue(x.ItemName).Connect(x.PortValue) );

        }

        private void InitializeGraphNodes()
        {
            allNodes.Clear();
            cancelationNodes.Clear();
            inputs.Clear();
            outputs.Clear();
            
            for (var i = 0; i < nodes.Count; i++) {

                var node = nodes[i];
                
                //skip all not unigraph nodes
                if (!(node is IUniNode uniNode))
                    continue;

                //register graph ports by nodes
                UpdatePortNode(uniNode);

                //stop graph execution, if cancelation node output triggered
                if (uniNode is IGraphCancelationNode cancelationNode) {
                    cancelationNodes.Add(cancelationNode);
                }

                allNodes.Add(uniNode);
            }
        }

        private void UpdatePortNode(IUniNode uniNode)
        {
            //register input/output nodes
            if (!(uniNode is IGraphPortNode graphPortNode)) {
                return;
            }

            var container = graphPortNode.Direction == PortIO.Input ? 
                inputs : outputs;
      
            //add graph ports for exists port nodes
            this.UpdatePortValue(graphPortNode.ItemName, graphPortNode.Direction);
               
            container.Add(graphPortNode);

        }


        private void InitializeNode(IUniNode node)
        {
            LifeTime.AddCleanUpAction(node.Exit);
                
            node.Execute();
        }
        

        private void OnDisable() => Dispose();
        
        #endregion

    }
}