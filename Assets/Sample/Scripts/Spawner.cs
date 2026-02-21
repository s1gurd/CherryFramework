using UnityEngine;

namespace Sample.Scripts
{
    public class Spawner : MonoBehaviour
    {
        [System.Serializable]
        public struct SpawnableObject{
            public GameObject prefab;
            [Range(0f,1f)]
            public float spawnchance;
        }
        public SpawnableObject[] objects;
        public float minSpawnRate = 1f;
        public float maxSpawnRate = 2f;
    
        public void OnEnable(){
            Invoke(nameof(Spawn), Random.Range(minSpawnRate,maxSpawnRate));
        }
        void OnDisable(){
            CancelInvoke();
        }
        void Spawn(){
            float spawnchance = Random.value;
            foreach(var obj in objects){
                if(spawnchance<obj.spawnchance){
                    GameObject obstacle=Instantiate(obj.prefab);
                    obstacle.transform.position+= transform.position;
                    break;
                }
                spawnchance -= obj.spawnchance;
            }
            Invoke(nameof(Spawn), Random.Range(minSpawnRate,maxSpawnRate));
        
        }
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
