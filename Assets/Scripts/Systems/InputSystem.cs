using UnityEngine;
using UnityEngine.InputSystem;
using Match3Engine.Core;

namespace Match3Engine.Systems
{
    public class InputSystem : MonoBehaviour
    {
        public GameController gameController;

        private Vector2 startTouchPosition;
        private Vector2 endTouchPosition;
        private bool isDragging = false;

        private float swipeThreshold = 0.5f;

        private void Update()
        {
            // I'm checking for mouse clicks first for editor testing
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                startTouchPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                isDragging = true;
            }
            // I'm also checking for touch screen presses for the mobile build
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                startTouchPosition = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
                isDragging = true;
            }

            if (isDragging)
            {
                // Detecting when I release the mouse button
                if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    endTouchPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                    isDragging = false;
                    CalculateSwipe();
                }
                // Detecting when I lift my finger off the screen
                else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                {
                    endTouchPosition = Camera.main.ScreenToWorldPoint(Touchscreen.current.primaryTouch.position.ReadValue());
                    isDragging = false;
                    CalculateSwipe();
                }
            }
        }

        private void CalculateSwipe()
        {
            Vector2 swipeDelta = endTouchPosition - startTouchPosition;

            // Did I swipe far enough?
            if (swipeDelta.magnitude > swipeThreshold)
            {
                // I need to round the coordinates to find the exact grid slot I clicked
                int startX = Mathf.RoundToInt(startTouchPosition.x);
                int startY = Mathf.RoundToInt(startTouchPosition.y);

                Vector2Int posA = new Vector2Int(startX, startY);
                Vector2Int posB = posA;

                // Figuring out the direction of my swipe
                if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
                {
                    if (swipeDelta.x > 0) posB.x += 1;
                    else posB.x -= 1;
                }
                else
                {
                    if (swipeDelta.y > 0) posB.y += 1;
                    else posB.y -= 1;
                }

                // Sending the command to my engine
                gameController.ProcessPlayerSwap(posA, posB);
            }
        }
    }
}