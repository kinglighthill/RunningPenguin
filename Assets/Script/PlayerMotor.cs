using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;

    private float jumpForce = 8.0f;
    private float gravity = 12.0f;
    private float verticalVelocity;
    private int desiredLane = 1;


    private float originalSpeed = 7.0f;
    private float speed;
    private float speedIncreaseLastTick;
    private float speedIncreaseTime = 2.5f;
    private float speedIncreaseAmount = 0.1f;

    private Animator anim; 

    private const float LANE_DISTANCE = 2.5f;
    private const float TURN_SPEED = 0.05f;

    private bool isRunning = false;

    void Start()
    {
        speed = originalSpeed;
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isRunning)
            return;

        if (Time.time - speedIncreaseLastTick > speedIncreaseTime)
        {
            speedIncreaseLastTick = Time.time;
            speed += speedIncreaseAmount;
            GameManager.Instance.UpdateModifier(speed - originalSpeed);
        }

        //if (Input.GetKeyDown(KeyCode.LeftArrow))
        if (MobileInput.Instance.SwipeLeft)
                MoveLane(false);


        //if (Input.GetKeyDown(KeyCode.RightArrow))
        if (MobileInput.Instance.SwipeRight)
                MoveLane(true);

        Vector3 targetPosition = transform.position.z * Vector3.forward;

        switch (desiredLane)
        {
            case 0:
                targetPosition += Vector3.left * LANE_DISTANCE;
                break;
            case 2:
                targetPosition += Vector3.right * LANE_DISTANCE;
                break;
            default:
                break;
        }

        Vector3 moveVector = Vector3.zero;
        moveVector.x = (targetPosition - transform.position).normalized.x * speed;

        bool isGrounded = IsGrounded();
        anim.SetBool("IsGrounded", isGrounded);

        if (isGrounded)
        {
            verticalVelocity = -0.1f;


            //if (Input.GetKeyDown(KeyCode.Space))
            if (MobileInput.Instance.SwipeUp)
            {
                anim.SetTrigger("Jump");
                verticalVelocity = jumpForce;
            }
            else if (MobileInput.Instance.SwipeDown)
            {
                StartSliding();
                Invoke("StopSliding", 1.0f);
            }
        }
        else
        {
            verticalVelocity -= (gravity * Time.deltaTime);

            //if (Input.GetKeyDown(KeyCode.Space))
            if (MobileInput.Instance.SwipeDown)
            {
                verticalVelocity = -jumpForce;
            }
        }

        moveVector.y = verticalVelocity;
        moveVector.z = speed;

        controller.Move(moveVector * Time.deltaTime);

        Vector3 dir = controller.velocity;
        if (dir != Vector3.zero)
        {
            dir.y = 0;
            transform.forward = Vector3.Lerp(transform.forward, dir, TURN_SPEED);
        }
    }

    private void MoveLane(bool goingRight)
    {
        desiredLane += goingRight ? 1 : -1;
        desiredLane = Mathf.Clamp(desiredLane, 0, 2);
    }

    private bool IsGrounded()
    {
        Ray groundRay = new Ray(new Vector3(controller.bounds.center.x, (controller.bounds.center.y - controller.bounds.extents.y) + 0.2f, controller.bounds.center.z), Vector3.down);
        Debug.DrawRay(groundRay.origin, groundRay.direction, Color.cyan, 1.0f);

        return (Physics.Raycast(groundRay, 0.2f + 0.1f));
    }

    private void StartSliding()
    {
        anim.SetTrigger("Slide");
        //anim.SetBool("Sliding", true);
        controller.height /= 2;
        controller.center = new Vector3(controller.center.x, controller.center.y / 2, controller.center.z);
    }

    private void StopSliding()
    { 
        anim.SetBool("Sliding", false);
        controller.height *= 2;
        controller.center = new Vector3(controller.center.x, controller.center.y * 2, controller.center.z);
    }

    private void Crash()
    {
        anim.SetTrigger("Death");
        isRunning = false;
        GameManager.Instance.OnDeath();
    }

    public void StartRunning()
    {
        isRunning = true;
        anim.SetTrigger("Running");
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        switch (hit.gameObject.tag)
        {
            case "Obstacle":
                Crash();
                break;
        }
    }
}
