using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {
    float maxJumpVelocity, minJumpVelocity;
    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = 0.4f;
    float accelerationTimeAirborne = 0.2f;
    float acceleartionTimeGrounded = 0.1f;
    float moveSpeed = 6;
    float gravity;
    Vector3 velocity;
    float velocityXSmoothing;
    Controller2D controller;
    public float wallSlideSpeedMax = 3;
    public float wallStickTime = 0.25f;
    float timeToWallUnstick;

    public Vector2 wallJumpClimb, wallJumpOff, wallLeap;

	// Use this for initialization
	void Start () {
        // get the player controller that handles movement and collisions
        controller = GetComponent<Controller2D>();
        // deltaMovement = velocityInitiial * time + (acceleration * time^2)/2
        // jumpHeight = (gravity * timeToJumpApex^2)/2
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        // velFinal = velInitial + acceleration * time
        maxJumpVelocity = Mathf.Abs(gravity * timeToJumpApex);
        // velFinal^2 = velInit^2 + 2 * accleration * displacement
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
	}
	
	// Update is called once per frame
	void Update () {

        // get player input
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        // get whether wall is on the left or right
        int wallDirX = (controller.collisions.left) ? -1 : 1;
        // set player horizontal movement based on input
        float targetVelX = input.x * moveSpeed;
        // smoothly changes velocity to target velocity
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelX, ref velocityXSmoothing, (controller.collisions.below) ? acceleartionTimeGrounded : accelerationTimeAirborne);

        bool wallSliding = false;
        // if on a wall and not on the floor and sliding down then player is wallsliding
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;
            // if falling faster than wall slide speed, set fall speed to wall slide speed
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // if still stuck on wall
            if (timeToWallUnstick > 0)
            {
                // disable smooth velocity shifting
                velocityXSmoothing = 0.0f;
                // stop horizontal movement
                velocity.x = 0.0f;
                // if horizontal input is away from the wall stuck to
                if(input.x != wallDirX && input.x != 0){
                    // reduce wall stick time
                    timeToWallUnstick -= Time.deltaTime;
                }
                else
                {
                    // if moving into wall, reset wall stick time to max
                    timeToWallUnstick = wallStickTime;
                }
            }
            // if wall stick time is up, reset wall stick time variable
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }

        // if jump pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // if jumping while wall sliding
            if (wallSliding)
            {
                // if jumping towards wall while wallsliding
                if (wallDirX == input.x)
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                // if jumping off wall
                else if (input.x == 0)
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                // if leaping away from wall
                else
                {
                    velocity.x = -wallDirX * wallLeap.x;
                    velocity.y = wallLeap.y;
                }
            }
            // if jumping while on the ground
            if (controller.collisions.below)
            {
                velocity.y = maxJumpVelocity;
            }
        }
        // if jump is released
        if (Input.GetKeyUp(KeyCode.Space))
        {
            // if jumping faster than min jump, reduce to min jump speed
            if(velocity.y > minJumpVelocity)
                velocity.y = minJumpVelocity;
        }

        // apply gravity to y velocity
        velocity.y += gravity * Time.deltaTime;
        // tell controller to move the player by current velocity
        controller.Move(velocity * Time.deltaTime, input);

        // if player collides with an obstacle above or below, stop vertical movement
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0.0f;
        }
	}
}
