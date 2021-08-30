﻿namespace UniModules.UniGameFlow.GameFlow.Runtime.Nodes
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using global::UniGame.UniNodes.GameFlow.Runtime.Commands;
    using global::UniGame.UniNodes.Nodes.Runtime.Commands;
    using global::UniGame.UniNodes.Nodes.Runtime.Common;
    using NodeSystem.Runtime.Core.Attributes;
    using UniGame.AddressableTools.Runtime.Extensions;
    using UniGame.Context.Runtime.Connections;
    using UniGame.Core.Runtime.Interfaces;
    using UniGame.SerializableContext.Runtime.Addressables;
    using UnityEngine;

    [CreateNodeMenu("GameSystem/Parenting Local Context Source")]
    public class ParentingLocalContextSourceNode : InOutPortNode
    {
        private ContextConnection _contextConnection;

        [SerializeField]
        public AssetReferenceContextContainer _localContextContainer;
        [SerializeField]
        public AssetReferenceContextContainer _parentContextContainer;

        [Header("Data Source")]
        [SerializeField]
        private AssetReferenceDataSource _dataSources;

        protected override void UpdateCommands(List<ILifeTimeCommand> nodeCommands)
        {
            base.UpdateCommands(nodeCommands);

            _contextConnection = _contextConnection ?? new ContextConnection();
            
            LifeTime.AddDispose(_contextConnection);

            var outPort = UniTask.FromResult<IContext>(PortPair.OutputPort);
            var contextSource = UniTask.FromResult<IContext>(_contextConnection);

            var bindContextToOutput = new MessageBroadcastCommand(_contextConnection, PortPair.OutputPort);
            //register all Context Sources Data into target context asset
            var registerDataSourceIntoContext = new RegisterDataSourceCommand(contextSource, _dataSources);
            //Register context to output port
            var contextToOutputPortCommand = new DataSourceTaskCommand<IContext>(contextSource, outPort);

            var contextContainerBindCommand = new ParentContextContainerBindCommand(_contextConnection, _parentContextContainer);

            nodeCommands.Add(bindContextToOutput);
            nodeCommands.Add(registerDataSourceIntoContext);
            nodeCommands.Add(contextToOutputPortCommand);
            nodeCommands.Add(contextContainerBindCommand);
        }

        protected override async void OnExecute()
        {
            base.OnExecute();

            if (_localContextContainer == null) return;
            
            var localContextContainer = await _localContextContainer.LoadAssetTaskAsync(LifeTime);
            LifeTime.AddDispose(localContextContainer);
            localContextContainer.SetValue(_contextConnection);
        }
    }
}