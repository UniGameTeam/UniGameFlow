﻿namespace UniGame.UniNodes.Nodes.Runtime.Commands
{
    using System;
    using UniModules.GameFlow.Runtime.Interfaces;
    using UniModules.UniCore.Runtime.Rx.Extensions;
    using UniModules.UniGame.Core.Runtime.DataFlow.Interfaces;
    using UniModules.UniGame.Core.Runtime.Interfaces;
    using UniRx;

    [Serializable]
    public class MessageBroadcastCommand : ILifeTimeCommand
    {
        private readonly IBroadcaster<IMessagePublisher> _source;
        private readonly IMessagePublisher             _target;

        public MessageBroadcastCommand(IBroadcaster<IMessagePublisher> source, IMessagePublisher target)
        {
            _source = source;
            _target = target;
        }
        
        public void Execute(ILifeTime lifeTime)
        {
            _source.Broadcast(_target).AddTo(lifeTime);
        }
    }
}