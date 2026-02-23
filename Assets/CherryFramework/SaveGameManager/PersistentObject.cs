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
using System.Linq;
#endif

namespace CherryFramework.SaveGameManager
{
    public class PersistentObject : BehaviourBase, IGameSaveData
    {
        private const string ScenePrefix = "SceneId:";
        
        [SerializeField] private bool spawnableObject;
        [ShowIf(nameof(spawnableObject))][SerializeField] private string customId = "OBJ";
        [HideIf(nameof(spawnableObject))][InfoBox("For auto-filling of guid, you have to add scene to Build Settings!")]
        [ReadOnly] public string guid = null;
        
        [SerializeField] private bool saveTransform;

        [Title("DANGER ZONE")]
        [SerializeField] private bool forceReset;
        
        [Inject] private readonly SaveGameManager _saveGame;
        [Inject] private readonly ModelService _modelService;

        [SaveGameData] private float[] _position =  new float[3];
        [SaveGameData] private float[] _rotation =  new float[4];

        private readonly HashSet<IGameSaveData> _persistentComponents = new();

        public bool ForceReset => forceReset;
        public int? CustomSuffix { get; set; }

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
                if (CustomSuffix != null)
                {
                    sb.Append(":");
                    sb.Append(CustomSuffix.Value);
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
        
        public void RegisterComponent(IGameSaveData component)
        {
            _persistentComponents.Add(component);
        }

        public void SaveData()
        {
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
        
        [HideIf(nameof(spawnableObject))][Button("Generate Guid")]
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