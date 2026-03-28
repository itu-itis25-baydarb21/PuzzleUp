using UnityEngine;
using Match3Engine.Core;

namespace Match3Engine.View
{
    public class TileView : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        private Vector2 targetPosition;

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
        }

        private void Update()
        {
            // Smoothly sliding the tile to its target destination
            Vector3 target = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 15f);
        }
    }
}