using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Platform : RaycastController {

    public LayerMask passengerMask;
    public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;
    public float speed;
    public bool cyclic;
    public float waitTime;
    int fromWaypointIndex;
    float percentBetweenWaypoints;
    float nextMoveTime;
    [Range(0,2)]
    public float easeAmount;

    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform,Controller2D>();
    List<PassengerMovement> passengerMovement;

    // struct to store info about things on platforms
    struct PassengerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _strandingOnPlatform, bool _moveBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _strandingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

	// Use this for initialization
	public override void Start () {
        base.Start();
        // init array to store world waypoints for the platform
        globalWaypoints = new Vector3[localWaypoints.Length];
        // set global way point positions based off platforms starting position and local offsets
        for (int i = 0; i < localWaypoints.Length; ++i)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
	}
	
	// Update is called once per frame
	void Update () {
        UpdateRaycastOrigins();
        Vector3 velocity = CalculatePlatformMovement();
        CalculatePassengerMovement(velocity);

        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);
	}

    Vector3 CalculatePlatformMovement()
    {
        // if waiting for next movement, return no movement
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        // get current waypoint and next and make sure they wrap if last waypoint
        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        // get distance between the two waypoints
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        // get how far to move
        percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
        // ease movement so that it isn't so linear looking
        float easedPercent = Ease(percentBetweenWaypoints);
        // get new position based of percent between current waypoint and next waypoint
        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercent);

        // if reached next waypoint
        if (percentBetweenWaypoints >= 1)
        {
            // reset percent and set to next waypoint
            percentBetweenWaypoints = 0.0f;
            fromWaypointIndex++;
            // if not supposed to cycle
            if (!cyclic)
            {
                // if last waypoint
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    // set current waypoint to 0
                    fromWaypointIndex = 0;
                    // reverse the waypoint
                    System.Array.Reverse(globalWaypoints);
                }
            }
            // set wait time
            nextMoveTime = Time.time + waitTime;
        }
        // return how far to move platform
        return newPos - transform.position;
    }

    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovement)
        {
            if (!passengerDictionary.ContainsKey(passenger.transform))
            {
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
            }
            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();

        // get the x and y direction of the platform
        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        // Vertically moving platform
        if (velocity.y != 0)
        {
            // shoot a ray as far as the platform is moving in the y direction
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;
            // cast number of calculated rays
            for (int i = 0; i < verticalRayCount; ++i)
            {
                // if moving down draw from bottom, else top
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                // set offset from left side
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                // see if ray hit any potential passengers
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);

                // if passenger hit
                if (hit && hit.distance != 0)
                {
                    // if passenger not already determined to be hit
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        // if moving up into passenger, move passenger by horizontal velocity
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        // move passenger by platform velocity after subtracting ray length
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;
                        // save how to move the passenger, if moving up then they are standing on platform and move them before the platform
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true));
                        movedPassengers.Add(hit.transform);
                    }
                }
            }
        }

        // Horizontally moving platform
        if (velocity.x != 0.0f)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;
            for (int i = 0; i < horizontalRayCount; ++i)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        // apply a little downward force
                        float pushY = -skinWidth;
                        // save movement into and say not standing on platform
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
                        movedPassengers.Add(hit.transform);
                    }
                }
            }
        }

        // Passenger on top of a horizontally or downward moving platform
        if (directionY == -1 || (velocity.y == 0 && velocity.x != 0.0f))
        {
            float rayLength = skinWidth * 2;
            for (int i = 0; i < verticalRayCount; ++i)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);

                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;
                        // move passenger the same as the platform
                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));

                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = 0.3f;

            for (int i = 0; i < localWaypoints.Length; ++i)
            {
                Vector3 globalPos = (Application.isPlaying)?globalWaypoints[i]:localWaypoints[i];
                Gizmos.DrawLine(globalPos - Vector3.up * size, globalPos + Vector3.up * size);
                Gizmos.DrawLine(globalPos - Vector3.right * size, globalPos + Vector3.right * size);
            }
        }
    }
}
