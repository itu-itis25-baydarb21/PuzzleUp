using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.View
{
    public class TileView : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        private Vector2 targetPosition;

        // Falling uses real acceleration instead of the lerp, so it reads as gravity
        private const float dropAcceleration = 50f;
        private const float maxDropSpeed = 25f;
        private float dropSpeed;

        public bool IsDropping { get; private set; }

        public void UpdateVisuals(TileType type, Sprite sprite)
        {
            if (type == TileType.None || sprite == null)
            {
                spriteRenderer.sprite = null;
            }
            else
            {
                spriteRenderer.sprite = sprite;

                transform.localScale = Vector3.one;
                float spriteWidth = sprite.bounds.size.x;
                float spriteHeight = sprite.bounds.size.y;
                float targetSize = 0.95f;
                float scaleX = targetSize / spriteWidth;
                float scaleY = targetSize / spriteHeight;
                float finalScale = Mathf.Min(scaleX, scaleY);
                transform.localScale = new Vector3(finalScale, finalScale, 1f);
            }
        }

        public void MoveToPosition(Vector2 targetPos)
        {
            // Setting the target destination
            targetPosition = targetPos;
            IsDropping = false;
        }

        // Jumping straight to a spot with no animation
        public void SnapToPosition(Vector2 pos)
        {
            targetPosition = pos;
            IsDropping = false;
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        }

        // Falling down to a slot from wherever I currently am
        public void DropToPosition(Vector2 targetPos)
        {
            targetPosition = targetPos;
            dropSpeed = 0f;
            IsDropping = true;
        }

        private void Update()
        {
            if (IsDropping)
            {
                dropSpeed = Mathf.Min(dropSpeed + dropAcceleration * Time.deltaTime, maxDropSpeed);
                float newY = transform.position.y - dropSpeed * Time.deltaTime;

                // Landed on my slot
                if (newY <= targetPosition.y)
                {
                    newY = targetPosition.y;
                    IsDropping = false;
                }

                transform.position = new Vector3(targetPosition.x, newY, transform.position.z);
                return;
            }

            // Smoothly sliding the tile to its target destination
            Vector3 target = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 15f);
        }
    }
}
