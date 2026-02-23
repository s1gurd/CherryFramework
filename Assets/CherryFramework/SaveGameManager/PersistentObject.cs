using System;
using System.Collections.Generic;
using System.Text;
using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using DG.Tweening;
using TriInspector;
using MathUtils = CherryFramework.Utils.MathUtils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
#endif

namespace CherryFramework.SaveGameManager
{
    public class PersistentObject : BehaviourBase, IGameSaveData
    {
        private const string ScenePrefix = "SceneId:";
        
        [SerializeField] private bool spawnableObject;
        [ShowIf(nameof(spawnableObject))][SerializeField] private string customId = "OBJ";
        [ShowIf(nameof(spawnableObject))][ReadOnly, ShowInInspector] private string _customSuffix = null;
        [HideIf(nameof(spawnableObject))][ReadOnly] public string guid = null;

        [SerializeField] private bool saveTransform;
        
        [field: SerializeField] public bool ForceReset { get; private set; }
        
        [Inject] private readonly SaveGameManager _saveGame;
        [Inject] private readonly ModelService _modelService;

        [SaveGameData] private float[] _position =  new float[3];
        [SaveGameData] private float[] _rotation =  new float[4];

        private readonly HashSet<IGameSaveData> _persistentComponents = new();
        
        private DateTime _lastSaveTime;

        private void Start()
        {
            if (saveTransform)
            {
                _position = MathUtils.Vector3ToArray(transform.position);
                _rotation = MathUtils.QuaternionToArray(transform.rotation);
                _saveGame.Register(this);
                _saveGame.LoadData(this);
                var anim = GetComponent<Animator>();
                if (anim && anim.applyRootMotion)
                {
                    anim.applyRootMotion = false;
                    var sequence = DOTween.Sequence();
                    sequence.AppendInterval(0.2f);
                    sequence.AppendCallback(() => anim.applyRootMotion = true);
                }
                transform.position = MathUtils.ArrayToVector3(_position);
                transform.rotation = MathUtils.ArrayToQuaternion(_rotation);
            }
            
            _lastSaveTime = DateTime.Now;
        }

        public string GetObjectId()
        {
            if (spawnableObject)
            {
                if (string.IsNullOrEmpty(customId))
                {
                    Debug.LogError($"[PersistentObject] Persistent object \"{gameObject.name}\" - Id is missing!", gameObject);
                    return null;
                }
                
                var sb = new StringBuilder();
                sb.Append(customId);
                if (_customSuffix != null)
                {
                    sb.Append(":");
                    sb.Append(_customSuffix);
                }

                return sb.ToString();
            }
            else
            {
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError($"[PersistentObject] Persistent object \"{gameObject.name}\" - guid is missing!", gameObject);
                    return null;
                }
                
                var sb = new StringBuilder();
                sb.Append(ScenePrefix);
                sb.Append(gameObject.scene.buildIndex);
                sb.Append(".");
                sb.Append(guid);
                return sb.ToString();
            }
        }

        public void SetSuffix(string suffix)
        {
            _customSuffix = suffix;
        }

        public void SetSpawnable(bool spawnable)
        {
            spawnableObject = spawnable;
        }
        
        public void RegisterComponent(IGameSaveData component)
        {
            _persistentComponents.Add(component);
        }

        public void SaveData()
        {
            if (DateTime.Now.Subtract(_lastSaveTime).TotalSeconds < 5)
                return;
            
            _lastSaveTime = DateTime.Now;
            _position = MathUtils.Vector3ToArray(transform.position);
            _rotation = MathUtils.QuaternionToArray(transform.rotation);
            
            foreach (var comp in _persistentComponents)
            {
                _saveGame.SaveData(comp);
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (gameObject.scene.buildIndex != -1)
            {
                FillGuid();
            }
            else
            {
                guid = null;
            }
        }
        
        private void FillGuid()
        {
            SerializedObject serializedObject = new SerializedObject(this);

            SerializedProperty guidProperty =
                serializedObject.FindProperty(nameof(guid));

            if (String.IsNullOrEmpty(guidProperty.stringValue) ||
                     (guidProperty.isInstantiatedPrefab &&!guidProperty.prefabOverride))
            {
                var objs = FindObjectsByType<PersistentObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                string newGuid;
                
                while (true)
                {
                    newGuid = Guid.NewGuid().ToString();
                    if (!objs.Any(o => o.guid.Equals(newGuid) && o != this)) break;
                }
                guidProperty.stringValue = newGuid;
            }

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}