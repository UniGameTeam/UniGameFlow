﻿using UnityEngine;

namespace UniGreenModules.UniGameSystems.Runtime.Commands
{
    using System;
    using UniContextData.Runtime.Interfaces;
    using UniCore.Runtime.DataFlow.Interfaces;
    using UniCore.Runtime.Interfaces;
    using UniCore.Runtime.ProfilerTools;
    using UniGame.AddressableTools.Runtime.Extensions;
    using UniGame.SerializableContext.Runtime.Addressables;
    using UniRx.Async;
    using UnityEngine.AddressableAssets;

    [Serializable]
    public class RegisterDataSourceCommand : ILifeTimeCommand
    {
        private readonly UniTask<IContext> contextTask;
        private readonly AssetReference resource;

        public Object asset;
        public IAsyncContextDataSource dataSource;

        public RegisterDataSourceCommand(UniTask<IContext> contextTask,AssetReference resource)
        {
            this.contextTask = contextTask;
            this.resource = resource;
        }

        public async void Execute(ILifeTime lifeTime)
        {
            asset = await resource.LoadAssetTaskAsync<ScriptableObject>();
            dataSource = asset as IAsyncContextDataSource;
            if (asset == null) {
                GameLog.LogError($"NULL asset loaded from {resource}");
                return;
            }
            
            await dataSource.RegisterAsync(await contextTask);
            
        }
    }
}
