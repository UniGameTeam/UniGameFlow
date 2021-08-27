﻿namespace UniModules.UniGame.GameFlow.GameFlow.Runtime
{
    using System;
    using System.Collections.Generic;
    using AddressableTools.Runtime.Extensions;
    using Context.Runtime.Context;
    using Core.Runtime.DataFlow.Interfaces;
    using Core.Runtime.Interfaces;
    using Cysharp.Threading.Tasks;
    using global::UniModules.GameFlow.Runtime.Core;
    using SerializableContext.Runtime.Addressables;
    using Taktika.GameRuntime.Abstract;
    using UniContextData.Runtime.Entities;
    using UniContextData.Runtime.Interfaces;
    using UniCore.Runtime.Rx.Extensions;
    using UnityEngine;

    public class GameManager : MonoBehaviour, IGameManager
    {
        #region inspector

        [SerializeField]
        private AssetReferenceContextContainer _contextContainer;

        [SerializeField]
        private List<UniGraph> executionItems = new List<UniGraph>();

        [SerializeField]
        private List<AssetReferenceDataSource> _assetSources = new List<AssetReferenceDataSource>();

        [SerializeReference]
        private List<IAsyncContextDataSource> _sources = new List<IAsyncContextDataSource>();

        #endregion

        private EntityContext _gameContext = new EntityContext();

        #region public properties

        public static IGameManager Instance { get; protected set; }

        public IContext GameContext => _gameContext;

        public ILifeTime LifeTime => _gameContext.LifeTime;

        #endregion

        #region public methods

        public async UniTask Execute()
        {
            if (_contextContainer.RuntimeKeyIsValid())
            {
                var contextContainer = await _contextContainer.LoadAssetTaskAsync(LifeTime);
                contextContainer.SetValue(_gameContext);
            }

            await ExecuteSources(_gameContext);
            await ExecuteGraphs();
        }

        public void Dispose()
        {
            _gameContext?.Dispose();
            _gameContext = null;
        }

        #endregion

        #region private methods

        private async UniTask ExecuteGraphs()
        {
            foreach (var graph in this.executionItems)
            {
                graph.Execute();
            }
        }

        private async UniTask ExecuteSources(IContext context)
        {
            foreach (var source in _sources)
            {
                source.RegisterAsync(context);
            }

            foreach (var sourceReference in _assetSources)
            {
                RegisterSource(sourceReference, context);
            }
        }

        private async UniTask RegisterSource(AssetReferenceDataSource dataSource, IContext context)
        {
            var sourceAsset = await dataSource.LoadAssetTaskAsync(LifeTime);
            await sourceAsset.RegisterAsync(context);
        }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            foreach (var graph in executionItems)
            {
                graph.AddTo(LifeTime);
            }
            this.AddCleanUpAction(() => Instance = null);
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion
    }
}
