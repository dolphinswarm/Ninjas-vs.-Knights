using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicKyleController : MonoBehaviour
{

    private Animator animator;
    private CharacterController controller;

    void Start()
    {

        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

    }

    void Update()
    {
        float accelerator = 1.0f;

        if (controller.isGrounded)
        {

            if (Input.GetKeyDown(KeyCode.S))
            {
                animator.SetBool("Crouch", true);
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                animator.SetBool("Crouch", false);
            }

            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
            {

                accelerator = 2.0f;
            }

            float v = Input.GetAxis("Vertical");
            float zSpeed = v * accelerator;

            animator.SetFloat("Speed", zSpeed);

            if (Input.GetButtonDown("Jump") == true)
            {
                animator.SetBool("Jump", true);
                //print("jump");
            }
            else
            {
                animator.SetBool("Jump", false);
                //print("jump");
            }

        }
    }
}
