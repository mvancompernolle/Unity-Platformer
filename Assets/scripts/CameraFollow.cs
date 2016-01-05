using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {

    public Controller2D target;
    public Vector2 focusAreaSize;
    FocusArea focusArea;
    public float verticalOffset;
    public float lookAheadDistX, lookSmoothTimeX, verticalSmoothTime;


    float currentLookAheadX;
    float targetLookAheadX;
    float lookAheadDirX;
    float smoothLookVelocityX;
    float smoothVelocityY;
    bool lookAheadStopped;

    struct FocusArea
    {
        public Vector2 center;
        float left, right;
        float top, bottom;
        public Vector2 velocity;

        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2.0f;
            right = targetBounds.center.x + size.x / 2.0f;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;
            center = new Vector2((left+right)/2.0f, (top+bottom)/2.0f);

            velocity = Vector2.zero;
        }

        public void Update(Bounds targetBounds)
        {
            float shiftX = 0.0f;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }
            left += shiftX;
            right += shiftX;

            float shiftY = 0.0f;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top;
            }
            top += shiftY;
            bottom += shiftY;

            center = new Vector2((left + right) / 2.0f, (top + bottom) / 2.0f);
            velocity = new Vector2(shiftX, shiftY);
        }
    }

	// Use this for initialization
	void Start () {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
	}

    void LateUpdate()
    {
        focusArea.Update(target.collider.bounds);
        Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;

        if (focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0)
            {
                lookAheadStopped = false;
                targetLookAheadX = lookAheadDirX * lookAheadDistX;
            }
            else
            {
                if (!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDistX - currentLookAheadX) / 4f;
                }
            }
        }

        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);
        focusPosition += Vector2.right * currentLookAheadX;

        transform.position = (Vector3) focusPosition + Vector3.forward * -10;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.center, focusAreaSize);
    }

	// Update is called once per frame
	void Update () {
	
	}
}
