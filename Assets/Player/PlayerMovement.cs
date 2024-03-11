using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
namespace HelloWorld
{
    
    public class PlayerMovement : NetworkBehaviour
    {
    public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>();
    public NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>();
    
    public GameObject CameraParent;
    public Transform BulletSpawn;
    public CharacterController characterController;
    public Camera cam;
    public AudioListener al;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float gravityValue = -9.81f;
    private float jumpHeight = 1.0f;
    public float mouseSensitivity = 100.0f;
    public float clampAngle = 80.0f;
 
     private float rotY = 0.0f; // rotation around the up/y axis
     private float rotX = 0.0f; // rotation around the right/x axis
     
     public override void OnNetworkSpawn()
        {
        //Debug.Log("is local player = " + IsLocalPlayer);
        if (IsLocalPlayer) { 
            Debug.Log(al.enabled);
            cam.enabled = true;
            al.enabled = true;
            Vector3 rot = transform.localRotation.eulerAngles;
             rotY = rot.y;
             rotX = rot.x;
            }
        }
     
     public void Move()
     {
         
         if (IsOwner)
         {
            Vector3 moveDir = (transform.forward * Input.GetAxis("Vertical"))
                + (transform.right * Input.GetAxis("Horizontal"));
            characterController.Move(5 * Time.deltaTime * moveDir);
            groundedPlayer = characterController.isGrounded;
            if (groundedPlayer && playerVelocity.y < 0)
                {
                    playerVelocity.y = 0f;
                }
            if (Input.GetButtonDown("Jump") && groundedPlayer)
                {
                    playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                }
                
            playerVelocity.y += gravityValue * Time.deltaTime;
            characterController.Move(playerVelocity * Time.deltaTime);
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = -Input.GetAxis("Mouse Y");

            rotY += mouseX * mouseSensitivity * Time.deltaTime;
            rotX += mouseY * mouseSensitivity * Time.deltaTime;

            rotX = Mathf.Clamp(rotX, -clampAngle, clampAngle);

            Quaternion localRotation = Quaternion.Euler(rotX, rotY, 0.0f);
            transform.rotation = rotation.Value;
            
            if (Input.GetKey(KeyCode.Mouse1)){
                RaycastHit hit;
                if (Physics.Raycast(BulletSpawn.position, BulletSpawn.forward, out hit,  Mathf.Infinity)){
                    if (hit.transform.gameObject.layer == 8)
                    {
                        hit.transform.gameObject.GetComponent<NPCBehaviour>().health-= 10;
                    }else if ( hit.transform.gameObject.layer == 9){
                        //hit.transform.gameObject.GetComponent<ZombieBehaviour>().health-= 10;
                    }
                }
            }
            
             if (IsServer)
             {
                 position.Value = transform.position;
                 rotation.Value = localRotation;
             }
             else if (IsClient)
             {            
                 MoveServerRpc(transform.position, localRotation);
                 
             }
         } else
         {
             transform.position = position.Value;
             transform.rotation = rotation.Value;
         }
     }
 
     [ServerRpc]
     void MoveServerRpc(Vector3 pos, Quaternion localRot)
     {
         position.Value = pos;
         rotation.Value = localRot;
     }
 
     // Update is called once per frame
     void Update()
     {
         Move();
     }
    }
}