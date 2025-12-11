using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float heightOffset = 0.5f; // Height above ground
    
    private Vector3 targetPosition;
    private bool isMoving = false;
    
    public void Initialize(Vector3 startPos, Vector3 targetPos)
    {
        // Set starting position with height offset
        transform.position = startPos + Vector3.up * heightOffset;
        targetPosition = targetPos + Vector3.up * heightOffset;
        
        // Rotate arrow to face target direction
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        isMoving = true;
    }
    
    void Update()
    {
        if (!isMoving) return;
        
        // Move toward target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        
        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            // Destroy arrow when it reaches target
            Destroy(gameObject);
        }
    }
}

