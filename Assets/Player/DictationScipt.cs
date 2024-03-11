using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

using System.Net.Sockets;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Random=UnityEngine.Random;

namespace HelloWorld
{
    public class DictationScript : NetworkBehaviour
    {


        private InputDevice targetDevice;
        private bool lastState;
        //socket
        
        public string received_text;
        
     
        RaycastHit HitInfo;
        RaycastHit AttnInfo;
        RaycastHit toHighlight;
        RaycastHit toAccuse;
        public GameObject talk_NPC;
        public string send_text;
        private string rt;
        
        
        
        private Thread ai_thread;	
        public GameObject lookingAtNpc;
        

        
        AudioSource audiosource;
        AudioClip recording;
        private float startRecordingTime;
        private byte[] recording_bytes;
        private int freq;
        public bool hasSpoken;
        private int recPos;
        private int stopPos;
        private int a_o_s;
        private float[] data;
        //temp need to change as are hackey
        public string getTheJson;
        
        public string filePath;
        //public Serverfunctions Serverfunc;
        
         public override void OnNetworkSpawn()
            {
                if (IsLocalPlayer) { 
                    hasSpoken = false;
                freq = 44100;
                audiosource = GetComponent<AudioSource>();
                lookingAtNpc = this.gameObject;
                send_text = null;
                received_text = null;
                lastState = false;
                recording = Microphone.Start("", true, 300, freq);
                
                if (Application.platform == RuntimePlatform.Android){
                    filePath = Application.persistentDataPath;
                    
                } else if (Application.platform == RuntimePlatform.IPhonePlayer){
                    filePath =Application.dataPath + "/Raw";
                }else{
                    filePath =  Application.persistentDataPath;
                    Cursor.lockState = CursorLockMode.Confined;
                }

                Debug.Log("player = " + filePath);
                }
            }
        void Start(){
            
        }

        
        
        private void send_recording_to_ai(){
            Debug.Log("going to connect");
            try{
                        
                TcpClient client = new TcpClient("88.98.228.164", 8674);	
                var byteArray = new byte[data.Length * 4];
                Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
                int bytecount = Encoding.ASCII.GetByteCount(send_text + 1);
                send_text = (bytecount+byteArray.Length).ToString()+"|"+send_text;
                bytecount = Encoding.ASCII.GetByteCount(send_text);
                byte[] send_data = new byte[bytecount];
                send_data = Encoding.ASCII.GetBytes(send_text);
                NetworkStream stream = client.GetStream();
                byte[] ca = send_data.Concat(byteArray).ToArray();
                stream.Write(ca, 0 , byteArray.Length);
                StreamReader sr = new StreamReader(stream);
                var bytes = default(byte[]);
                var buffer = new byte[1024];
                var size = default(int);
                sr.BaseStream.Read(buffer, 0, buffer.Length);			
                bool done = false;
                size = Convert.ToInt32(Encoding.Default.GetString(buffer));
                using (var memstream = new MemoryStream()){
                    var bytesRead = default(int);
                    while (!done){//(bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length))!=0)
                        bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length);
                        memstream.Write(buffer, 0, bytesRead);
                        if (memstream.Length >= size){done = true;}
                    }
                    memstream.Write(buffer, 0, bytesRead);
                    bytes = memstream.ToArray();
                }
                stream.Close();
                client.Close();
                System.IO.File.WriteAllBytes(filePath + "/reply.wav", bytes);			
                received_text = "reply.wav";
                send_text = null;
                
            }catch (Exception e){Debug.LogError("Couldn't send recording " + e.Message);
            }
            
        }
        
        public void onPointerDown(){
            
            hasSpoken = true;
            Debug.Log(hasSpoken);
            recPos = Microphone.GetPosition("");
            startRecordingTime = Time.time;	
        }
        public void onPointerUp(){
            Debug.Log(recPos);
            if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out HitInfo, 100f)){
                if (HitInfo.collider.gameObject.layer == 10){
                    talk_NPC = HitInfo.transform.gameObject;
                   
                    stopPos = Microphone.GetPosition("");
                    if (recPos > stopPos){
                        a_o_s = recording.samples - recPos + stopPos;
                    }else{
                        a_o_s = stopPos - recPos;
                    }
                    data = new float[a_o_s * recording.channels];
                    recording.GetData(data, recPos);
                    
                    getTheJson = "Murder1.json";
                  
                    send_text = "AIVR data |" + talk_NPC.name + "|" + getTheJson + "|" + recording.length.ToString() + "|" + recording.frequency.ToString() + "|" + recording.samples.ToString() + "|" + recording.channels.ToString() +"audio starts";
                    ai_thread = new Thread(send_recording_to_ai);
                    ai_thread.Start();
                }
            }
            
        }
        
        
        void Update(){
            if (Input.GetKeyDown("T")){ onPointerDown();
            } else if (Input.GetKeyUp("T")){onPointerUp();}
            
            if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out toHighlight, 100f)){
                Debug.Log("lookking at " + toHighlight.transform.gameObject.name);
                if (lookingAtNpc != toHighlight.transform.gameObject){
                        lookingAtNpc.transform.gameObject.GetComponent<NPCBehaviour>().unhighlight();
                    }
                if (toHighlight.transform.gameObject.layer == 8){
                    toHighlight.transform.gameObject.GetComponent<NPCBehaviour>().highlight();	
                }
                lookingAtNpc = toHighlight.transform.gameObject;
                Debug.Log(lookingAtNpc);
                
            }
                
            if (received_text != null){
                
                talk_NPC.GetComponent<NPCBehaviour>().received_text = received_text;
                
                received_text = null;
            }	
            
        }
        
    }
	

}