using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicController02 : MonoBehaviour
{

    private Animator animator;
    private CharacterController controller;
    public float transitionTime = 0.25f;
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject gun;
    public GameObject grenadePrefab;
    public float throwForce = 600f;
    GameObject grenade;


    void Start()
    {

        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (animator.layerCount >= 2)
        {
            animator.SetLayerWeight(1, 1);
        }

    } // end Start


    void Update()
    {

        float accelerator = 1.0f;

        if (controller.isGrounded)
        {
            // Start throwing animation on right click
            if (Input.GetMouseButton(1))
            {
                // Do not play the animation if these variables are not set.
                // Mainly for compatiblity to use the controller for Robot Kyle.
                if (grenadePrefab == null || rightHand == null || leftHand == null || gun == null)
                {
                    Debug.LogWarning("Variables not set within the script. Not playing throwing animation.");
                }
                else
                {
                    animator.SetBool("Throwing", true);
                }
            }



            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
            {

                accelerator = 2.0f;

            }
            else if (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftAlt))
            {

                accelerator = 1.5f;
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            float xSpeed = h * accelerator;
            float zSpeed = v * accelerator;

            animator.SetFloat("xSpeed", xSpeed, transitionTime, Time.deltaTime);
            animator.SetFloat("zSpeed", zSpeed, transitionTime, Time.deltaTime);
            animator.SetFloat("Speed", Mathf.Sqrt(h * h + v * v), transitionTime, Time.deltaTime);
        }

    } // end Update

    public void StartThrowAnimation()
    {
        // Reparent the gun to the left hand during the animation.
        gun.transform.parent = leftHand.transform;

        // The spawn point of the grenade. May depending on the model.
        Vector3 grenadeSpawnPoint = rightHand.transform.position + 0.1f * rightHand.transform.right - 0.05f * rightHand.transform.up;
        grenade = Instantiate(grenadePrefab, grenadeSpawnPoint, rightHand.transform.rotation);
        grenade.transform.parent = rightHand.transform;
    }

    public void ReleaseGrenade()
    {
        grenade.transform.parent = null;
        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;

            // The direction the grenade is thrown. May also need to be changed depending on the model.
            Vector3 throwForceDirection = (-grenade.transform.up - 0.4f * grenade.transform.forward).normalized;
            //Vector3 throwForceDirection = grenade.transform.root.forward;

            rb.AddForce(throwForceDirection * throwForce);
        }
        else
        {
            Debug.LogError("No rigidbody attached to the grenade.");
        }
    }

    public void EndThrowAnimation()
    {
        // Reparent the gun to the right hand at the end of the animation.
        gun.transform.parent = rightHand.transform;
        animator.SetBool("Throwing", false);
    }


} // end BasicController02 
