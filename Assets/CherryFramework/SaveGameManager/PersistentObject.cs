using System;
using System.Collections.Generic;
using System.Text;
using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using DG.Tweening;
using TriInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

namespace CherryFramework.SaveGameManager
{
    [DisallowMultipleComponent]
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
        
        [SaveGameData] private Vector3 _position;
        [SaveGameData] private Quaternion _rotation;
        [SaveGameData] private Vector3 _scale;

        public bool ForceReset => forceReset;
        public int? CustomSuffix { get; private set; }

        private void Start()
        {
            if (saveTransform)
            {
                _saveGame.Register(this);
                _saveGame.LoadData(this);
                
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

        public void SetCustomSuffix(int suffix)
        {
            if (spawnableObject)
            {
                CustomSuffix = suffix;
            }
            else
            {
                Debug.LogError($"[PersistentObject] Custom Suffix is only allowed for Spawnable objects!", gameObject);
            }
        }

        public void OnBeforeLoad()
        {
            StoreTransform();
        }

        public void OnAfterLoad()
        {
            var anim = GetComponent<Animator>();
            if (anim && anim.applyRootMotion)
            {
                anim.applyRootMotion = false;
                var sequence = DOTween.Sequence();
                sequence.AppendInterval(0.2f);
                sequence.AppendCallback(() => anim.applyRootMotion = true);
            }

            transform.localScale = _scale;
                
            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.Move(_position, _rotation);
                return;
            }

            var cc = GetComponent<CharacterController>();
            var ccEnabled = false;
            if (cc && cc.enabled)
            {
                ccEnabled = true;
                cc.enabled = false;
            }

            transform.position = _position;
            transform.rotation = _rotation;
            
            if (cc && ccEnabled)
            {
                cc.enabled = true;
            }
        }

        public void OnBeforeSave()
        {
            StoreTransform();
        }

        private void StoreTransform()
        {
            _position = transform.position;
            _rotation = transform.rotation;
            _scale = transform.localScale;
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
                    if (!objs.Any(o => o.guid != null && o.guid.Equals(newGuid) && o != this)) break;
                }
                guidProperty.stringValue = newGuid;
            }

            serializedObject.ApplyModifiedProperties();
        }
#endif
    }
}