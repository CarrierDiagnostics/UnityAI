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
    public class PlayerDictation : NetworkBehaviour
    {
        private bool lastState;
        //socket
        
        public string received_text;
        public HelloWorld.NPCBehaviour talk_NPC_NPCBehaviour;
     
        RaycastHit HitInfo;
        RaycastHit AttnInfo;
        RaycastHit toHighlight;
        RaycastHit toAccuse;
        public GameObject talk_NPC;
        public string send_text;
        private string rt;
        public Camera Camera;
        
        
        private Thread ai_thread;	
        public GameObject lookingAtNpc;
        public int samples_length;
        public int channels = 1;
        
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
        public int layerMask;
        public string filePath;
        //public Serverfunctions Serverfunc;
        
        public override void OnNetworkSpawn()
            {
                if (IsLocalPlayer) { 
                    hasSpoken = false;
                    freq = 16000;
                    audiosource = GetComponent<AudioSource>();
                    lookingAtNpc = null;
                    send_text = null;
                    received_text = null;
                    lastState = false;
                    //recording = Microphone.Start("", true, 300, freq);
                    layerMask =  1 << 8;
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
            //try{
                        
            TcpClient client = new TcpClient("88.98.228.164", 8674);	
            //Debug.Log("starrting clinet");
            var byteArray = new byte[data.Length * 4 + 50];
            //Debug.Log("created array");
            /*Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
            int bytecount = Encoding.ASCII.GetByteCount(send_text + 1);
            send_text = (bytecount+byteArray.Length).ToString()+"|"+send_text;
            bytecount = Encoding.ASCII.GetByteCount(send_text);
            byte[] send_data = new byte[bytecount];
            send_data = Encoding.ASCII.GetBytes(send_text);*/
            
            NetworkStream stream = client.GetStream();
            byte[] ca = ConvertAndWriteByteArray(data.Length);
            //Debug.Log("should be sending = " + ca.Length);
            //byte[] ca = send_data.Concat(byteArray).ToArray();
            stream.Write(ca, 0 , ca.Length);
            StreamReader sr = new StreamReader(stream);
            var bytes = default(byte[]);
            var buffer = new byte[1024];
            var size = default(int);
            sr.BaseStream.Read(buffer, 0, buffer.Length);			
            bool done = false;
            //Debug.Log(System.Text.Encoding.UTF8.GetString(buffer));
            size = Int32.Parse(Encoding.Default.GetString(buffer));//Convert.ToInt32(Encoding.Default.GetString(buffer));
            //Debug.Log(size);
            using (var memstream = new MemoryStream()){
                var bytesRead = default(int);
                while (!done){//(bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length))!=0)
                    bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length);
                    memstream.Write(buffer, 0, bytesRead);
                    //Debug.Log(memstream.Length + " < " + size);
                    if (memstream.Length >= size){
                        Debug.Log("size reaches");
                        done = true;
                        break;
                        }
                }
                Debug.Log("writing buffer to bytes");
                memstream.Write(buffer, 0, bytesRead);
                bytes = memstream.ToArray();
            }
            stream.Close();
            client.Close();
            //Debug.Log("should be sending to " + filePath + "/reply.mp3");
            System.IO.File.WriteAllBytes(filePath + "/reply.mp3", bytes);			
            received_text = "reply.mp3";
            send_text = null;
            talk_NPC_NPCBehaviour.talk=true;
            Debug.Log("should have called " + talk_NPC_NPCBehaviour.myName);
            Debug.Log("should have called " + talk_NPC_NPCBehaviour.get_talking());
                
            //}catch (Exception e){Debug.LogError("Couldn't send recording " + e.Message);}
            
        }
        
        public IEnumerator onPointerDown(){
            
            hasSpoken = true;
            Debug.Log(hasSpoken);
            recording = Microphone.Start("", true, 10, freq);
            startRecordingTime = Time.time;	
            yield return null;
        }
        public void onPointerUp(){
            Debug.Log(recPos);
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out HitInfo)) {
                if (HitInfo.collider.gameObject.layer == 8){
                    talk_NPC = HitInfo.transform.gameObject;
                    talk_NPC_NPCBehaviour = talk_NPC.GetComponent<NPCBehaviour>();
                    stopPos = Microphone.GetPosition("");
                    if (recPos > stopPos){
                        a_o_s = recording.samples - recPos + stopPos;
                    }else{
                        a_o_s = stopPos - recPos;
                    }
                    data = new float[a_o_s * recording.channels];
                    recording.GetData(data, 0);
                    
                    FileStream f = new FileStream("a.wav", FileMode.Create);
                    Debug.Log(a_o_s);
                    //WriteHeader(f, recording, data.Length);
                    //ConvertAndWrite(f, recording, data.Length);
                    samples_length = a_o_s*2;
                    
                    
                    getTheJson = "Murder1.json";
                  
                    send_text = "AIVR data |" + talk_NPC.name + "|" + getTheJson + "|" + recording.length.ToString() + "|" 
                        + recording.frequency.ToString() + "|" + recording.samples.ToString() + "|" + recording.channels.ToString() +"audio starts";
                    ai_thread = new Thread(send_recording_to_ai);
                    ai_thread.Start();
                }
            }
            
        }
        
         public  byte[] ConvertAndWriteByteArray(int a_o_s) {
            
            var byteArray = new byte[data.Length * 4 + 50];
            //fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            Buffer.BlockCopy(riff, 0, byteArray, 0, riff.Length);
            Byte[] chunkSize = BitConverter.GetBytes(data.Length * 4 + 50);
            Buffer.BlockCopy(chunkSize, 0, byteArray, 4, chunkSize.Length);
            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            Buffer.BlockCopy(wave, 0, byteArray, 8, wave.Length);
            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            Buffer.BlockCopy(fmt, 0, byteArray, 12, fmt.Length);
            Byte[] subChunk1 = BitConverter.GetBytes(16);
            Buffer.BlockCopy(subChunk1, 0, byteArray, 16, subChunk1.Length);

            UInt16 two = 2;
            UInt16 one = 1;

            Byte[] audioFormat = BitConverter.GetBytes(one);
            Buffer.BlockCopy(audioFormat, 0, byteArray, 20, audioFormat.Length);
            Byte[] numChannels = BitConverter.GetBytes(channels);
            Buffer.BlockCopy(numChannels, 0, byteArray, 22, numChannels.Length);
            Byte[] sampleRate = BitConverter.GetBytes(freq);
            Buffer.BlockCopy(sampleRate, 0, byteArray, 24, sampleRate.Length);
            Byte[] byteRate = BitConverter.GetBytes(freq * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            Buffer.BlockCopy(byteRate, 0, byteArray, 28, byteRate.Length);

            UInt16 ba = (ushort) (channels * 2);
            Byte[] blockAlign = BitConverter.GetBytes(ba);
            Buffer.BlockCopy(blockAlign, 0, byteArray, 32, blockAlign.Length);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            Buffer.BlockCopy(bitsPerSample, 0, byteArray, 34, bitsPerSample.Length);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            Buffer.BlockCopy(datastring, 0, byteArray, 36, datastring.Length);

            Byte[] subChunk2 = BitConverter.GetBytes(a_o_s*2 * channels * 2);
            Buffer.BlockCopy(subChunk2, 0, byteArray, 40, subChunk2.Length);

            
            var the_samples = data;
             
             //clip.GetData(the_samples, a_o_s);
             
             Int16[] intData = new Int16[the_samples.Length];
             //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]
             
             Byte[] bytesData = new Byte[the_samples.Length * 2];
             //bytesData array is twice the size of
             //dataSource array because a float converted in Int16 is 2 bytes.
             
             const float rescaleFactor = 32767; //to convert float to Int16
             
             for (int i = 0; i<the_samples.Length; i++) {
                 intData[i] = (short) (the_samples[i] * rescaleFactor);
                 Byte[] byteArr = new Byte[2];
                 byteArr = BitConverter.GetBytes(intData[i]);
                 byteArr.CopyTo(bytesData, i * 2);
             }
             Debug.Log("");
             Buffer.BlockCopy(bytesData, 0, byteArray, 44, bytesData.Length);
             File.WriteAllBytes("a2.wav",  byteArray);
             return byteArray;
        }
 
        void Update(){
            if (Input.GetKeyDown(KeyCode.T)){ StartCoroutine(onPointerDown());
            } else if (Input.GetKeyUp(KeyCode.T)){onPointerUp();}
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out toHighlight) ) {
                if (lookingAtNpc != toHighlight.transform.gameObject && lookingAtNpc!=null){
                        lookingAtNpc.transform.gameObject.GetComponent<NPCBehaviour>().unhighlight();
                    }
                if (toHighlight.transform.gameObject.layer == 8){
                    toHighlight.transform.gameObject.GetComponent<NPCBehaviour>().highlight();
                    lookingAtNpc = toHighlight.transform.gameObject;                    
                }
                
                //Debug.Log("Now lookign at = " +lookingAtNpc);
                
            }
                
         
            
        }
        
    }
}