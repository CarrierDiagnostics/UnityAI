using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.AI;
namespace HelloWorld
{
    public class NPCBehaviour : NetworkBehaviour
    {
        
        public string received_text;
        public float volumeScale = 0.5f;
        public AudioSource audiosource;
        public AudioClip audioclip;
        private float rotx;
        private float rotz;
        public float lerpSpeed = 8f;
        //external influence
        public GameObject NPC_attn;
        public Animator anim;
        //UnityEngine.AI.NavMeshAgent _navMeshAgent;
        public Shader Highlight;
        public Shader normal_shade;
        public Renderer rend;
        private bool highlighted;
        private GameObject player;
        public string myName;
        public string player_looking_at;
        
        //I know I use these
        public string filePath;
        public bool talk = false;
        public GameObject MyVilage;
        public GameObject MyFarm;
        public GameObject NextTask;
        private NavMeshAgent navMA;
        private bool SwitchingFlag;
        public int health;
        
        public override void OnNetworkSpawn()
        {
        }
        
        void Start()
        {
            //if (IsServer){
                health = 100;
                NextTask =MyFarm;
                myName = gameObject.name;
                SwitchingFlag = true;
                highlighted = false;
                normal_shade = Shader.Find("Legacy Shaders/Diffuse");
                Highlight = Shader.Find("Legacy Shaders/Reflective/Diffuse");
                //rend = this.gameObject.transform.GetChild(0).GetChild(1).gameObject.GetComponent<Renderer>();
                //_navMeshAgent = this.GetComponent<UnityEngine.AI.NavMeshAgent>();
                NPC_attn = null;
                received_text = null;
                rotz = transform.rotation.x;
                rotz = transform.rotation.z;	
                audiosource = GetComponent<AudioSource>();
                
                navMA = GetComponent<NavMeshAgent>();
                
                if (Application.platform == RuntimePlatform.Android){
                    filePath = "jar:file://" + Application.persistentDataPath;
                //} else if (Application.platform == RuntimePlatform.IPhonePlayer){
                //	filePath =Application.dataPath + "/Raw";
                }else{
                    filePath =  Application.persistentDataPath;
                }
           // }
            
        }
        
        
        public void highlight(){
		
            if (highlighted == false){
                rend.material.shader = Highlight;
                highlighted = true;
            }
        }
        
        public void unhighlight(){
            if (highlighted == true){
                rend.material.shader = normal_shade;
                highlighted = false;
            }
        }
        
        public string get_talking(){
            Debug.Log("got called to get talking");
            GetAudioClip();
            return "got called";
        }
        
        IEnumerator getNextTask(){
            //Vector3 getTaskPosition = NextTask.transform.position + Random.insideUnitCircle * 60;
            yield return new WaitForSeconds(5);
            if (NextTask == MyFarm){ NextTask = MyVilage;} else { NextTask = MyFarm;}
            SwitchingFlag = true;
            
        }
        
        IEnumerator GetAudioClip()
            {
                talk=false;
                string url =  filePath +"/reply.mp3"; 
                Debug.Log(url);
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
                {
                    yield return www.SendWebRequest();

                    if (www.isNetworkError)
                    {
                        Debug.LogError(www.error);
                    }
                    else
                    {
                        audiosource.clip = DownloadHandlerAudioClip.GetContent(www);
                            while (audiosource.clip.isReadyToPlay != true){
                            Debug.Log("not ready yet");
                            yield return null;
                        }
                        if (audiosource.clip.isReadyToPlay){
                            audiosource.Play();
                            Debug.Log( "isPlaying: " + audiosource.clip.name );
                        }else{
                            Debug.LogError("why isn't it ready?");
                        }
                        if (audiosource.isPlaying){
                            Debug.Log( "isPlaying: " + audiosource.clip.name );
                        }else{
                            Debug.Log("There is a proble");
                        } 
                            
                    }
                }
            }
        
        // Update is called once per frame
        void Update()
        {
            //if (IsServer){
                if(health <= 0){
                    //this.transform.gameObject.Destroy();
                    Debug.Log("how do I do this");
                }
               if (talk==true){
                    StartCoroutine(GetAudioClip());
               }else if (talk == false){
                   navMA.destination = NextTask.transform.position;
               }
               if (navMA.remainingDistance <= navMA.stoppingDistance && SwitchingFlag)
               {
                   SwitchingFlag = false;
                   StartCoroutine(getNextTask());
               }
            //}
           
        }
    }
}
