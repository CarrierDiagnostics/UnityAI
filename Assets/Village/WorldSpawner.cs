using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace HelloWorld
{
    public class WorldSpawner : NetworkBehaviour
    {

        public GameObject village;

        public override void OnNetworkSpawn()
        {
            
            GameObject tv = Instantiate(village, this.transform.position, Quaternion.identity);
         tv.GetComponent<NetworkObject>().Spawn();
        }

        [ServerRpc]
     void SpawnVillageServerRpc()
     {
         GameObject tv = Instantiate(village, this.transform.position, Quaternion.identity);
         tv.GetComponent<NetworkObject>().Spawn();
     }
    }
}