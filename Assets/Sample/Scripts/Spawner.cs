using System.Collections.Generic;
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.SimplePool;
using CherryFramework.StateService;
using DG.Tweening;
using Sample.Scripts.Settings;
using UnityEngine;

namespace Sample.Scripts
{
    public class Spawner : BehaviourBase, IGameSaveData
    {
        [Inject] private readonly GameSettings _gameSettings;
        [Inject] private readonly StateService _stateService;
        [Inject] private readonly SaveGameManager _saveGame;

        private SimplePool<PersistentObject> _objectPool = new ();
        private Sequence _spawnTimer;
        private List<int> _objectsToSpawnChanced = new();
        // This is needed only to reload spawned objects at Start()
        [SaveGameData] private List<int> _spawnedObjects = new();

        private void Start()
        {
            if (_gameSettings.spawnObjects.Length == 0)
            {
                Debug.LogError("Spawn Objects can't be empty!");
            }
            
            for (var index = 0; index < _gameSettings.spawnObjects.Length; index++)
            {
                for (int i = 0; i <= _gameSettings.spawnObjects[index].spawnChance; i++)
                {
                    _objectsToSpawnChanced.Add(index);
                }
            }
            
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameActive(EventKeys.GameRunning), SpawnStart);
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), () =>
            {
                _spawnTimer?.Kill();
            });
            
            _saveGame.Register(this);
            _saveGame.LoadData(this);

            // Respawn all objects from last session, including inactive
            // Of course, feel free to make this in a more fashionable way
            
            for (var index = 0; index < _spawnedObjects.Count; index++)
            {
                var objIndex = _spawnedObjects[index];
                var newObj = _objectPool.Get(_gameSettings.spawnObjects[objIndex].source);
                newObj.SetCustomSuffix(index);
            }
        }

        private void SpawnStart()
        {
            _spawnTimer?.Kill();
            _spawnTimer = DOTween.Sequence();
            _spawnTimer.AppendInterval(Random.Range(_gameSettings.minSpawnRate, _gameSettings.maxSpawnRate));
            _spawnTimer.AppendCallback(Spawn);
            _spawnTimer.AppendCallback(SpawnStart);
        }
        
        void Spawn()
        {
            var randomIndex = _objectsToSpawnChanced[Random.Range(0, _objectsToSpawnChanced.Count)];
            var newObj = _objectPool.Get(_gameSettings.spawnObjects[randomIndex].source, transform.position, Quaternion.identity);
            
            // We ensure that _spawnedObjects contains only objects in scene. Objects reused from pool will have value in CustomSuffix
            if (newObj.CustomSuffix == null)
            {
                newObj.SetCustomSuffix(_spawnedObjects.Count);
                _spawnedObjects.Add(randomIndex);
            }
            
            newObj.gameObject.SetActive(true);
        }
    }
}
