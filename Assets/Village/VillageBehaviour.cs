using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


namespace HelloWorld
{
    public class VillageBehaviour : NetworkBehaviour
    {
        
        public int resources;
        public bool spawnCoolDown;
        public GameObject SpawnPoint;
        public GameObject NPCObject;
        public GameObject VilageFarm;
         
        public override void OnNetworkSpawn(){
            //if (IsServer){
                
            //}
        }
        void Start()
        {
            resources = 500;
            spawnCoolDown = true;
        }


        IEnumerator spawnCoolDownTimer()
        {
            yield return new WaitForSeconds(5);
            spawnCoolDown = true;
        }
        void Update()
        {
            
            if (spawnCoolDown && resources > 100)// && IsServer)
            {
                resources = resources - 100;
                GameObject spawnedNPC = Instantiate(NPCObject, SpawnPoint.transform.position, Quaternion.identity);
                spawnedNPC.GetComponent<NetworkObject>().Spawn();
                spawnedNPC.GetComponent<NPCBehaviour>().MyVilage = this.gameObject;
                spawnedNPC.GetComponent<NPCBehaviour>().MyFarm = VilageFarm;
                spawnCoolDown = false;
                StartCoroutine(spawnCoolDownTimer());
                
            }
        }
    }
}