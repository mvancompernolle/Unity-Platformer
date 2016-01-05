using UnityEngine;
using System.Collections;

public class Controller2D : RaycastController {

    float maxClimbAngle = 80.0f;
    float maxDescendAngle = 75.0f;
    [HideInInspector]
    public Vector2 playerInput;

    public CollisionInfo collisions;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    // struct to store collision and other info
    public struct CollisionInfo
    {
        public bool above, below, left, right;
        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector3 velocityOld;
        public int faceDir;
        public bool fallingThrough;

        public void Reset()
        {
            above = below = left = right = climbingSlope = descendingSlope =  false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0.0f;
        }
    }

    // overloaded function to move without passing input in
    public void Move(Vector3 velocity, bool standingOnPlatform)
    {
        Move(velocity, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector3 velocity, Vector2 input, bool standingOnPlatform = false)
    {
        // get new raycast positions
        UpdateRaycastOrigins();
        // reset collision info
        collisions.Reset();
        collisions.velocityOld = velocity;
        // save player input
        playerInput = input;

        // if horizontal movement, set which way player is facing
        if (velocity.x != 0)
        {
            collisions.faceDir = (int) Mathf.Sign(velocity.x);
        }

        // if moving down see if descending slope
        if (velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }
        // see if colliding with anything on right or left
        HorizontalCollisions(ref velocity);
        // if moveing vertically, see if colliding with anything on top or bottom
        if( velocity.y != 0.0f )
            VerticalCollisions(ref velocity);
        // move the player based on adjusted velocity
        transform.Translate(velocity);

        // if standing on platform, mark as colliding with something below
        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    void HorizontalCollisions(ref Vector3 velocity)
    {
        float dirX = collisions.faceDir;
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        // if moving slower than skin width, increase ray length to double skin width
        if (Mathf.Abs(velocity.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        for (int i = 0; i < horizontalRayCount; ++i)
        {
            Vector2 rayOrigin = (dirX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * dirX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * dirX * rayLength, Color.blue);

            // if horizontal ray hit obstacle
            if (hit)
            {
                // if distance to obstacle is 0 ignore it
                if (hit.distance == 0)
                {
                    continue;
                }
                
                // determine slope of obstacle hit
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // if bottom ray and slope is climbable
                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        // if descending slope, set to not descending and get old velocity
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
                    float distanceToSlopeStart = 0.0f;
                    // if different slope
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        // move horizontall to new slope
                        distanceToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distanceToSlopeStart * dirX;
                    }
                    // climb slope
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * dirX;
                }

                // if not climbing slope or slope is too steap
                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * dirX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = dirX == -1;
                    collisions.right = dirX == 1;
                }
            }
        }
    }

    void VerticalCollisions(ref Vector3 velocity)
    {
        float dirY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;
        for (int i = 0; i < verticalRayCount; ++i)
        {
            Vector2 rayOrigin = (dirY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * dirY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * dirY * rayLength, Color.blue);

            if (hit)
            {
                // if hit a through object
                if (hit.collider.tag == "Through")
                {
                    // if going up or inside ignore
                    if (dirY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    // if falling through ignore
                    if (collisions.fallingThrough)
                    {
                        continue;
                    }
                    // if pushed down, set to falling through and ignore
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThrough = true;
                        Invoke("ResetFallingThrough", 0.5f);
                        continue;
                    }
                }

                velocity.y = (hit.distance - skinWidth) * dirY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisions.below = dirY == -1;
                collisions.above = dirY == 1;
            }
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {
        // get x speed
        float moveDistance = Mathf.Abs(velocity.x);

        float climbVelY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        // if moving up slower than speed neede to climb
        if (velocity.y <= climbVelY)
        {
            // climb the hil
            velocity.y = climbVelY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector3 velocity)
    {
        // get x direction
        float directionX = Mathf.Sign(velocity.x);
        // start ray from right if moving left, reverse otherwise
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        // see if there was obstance underneath
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            // get the angle of the slope
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            // if angled and is steap enough to descend
            if(slopeAngle != 0.0f && slopeAngle <= maxDescendAngle){
                // if slope is in same direction that player is moving
                if(Mathf.Sign(hit.normal.x) == directionX){
                    // if distance to slope less than how far we have to move on y axis
                    if(hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x)){
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelY;
                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }

    void ResetFallingThrough()
    {
        collisions.fallingThrough = false;
    }

}
