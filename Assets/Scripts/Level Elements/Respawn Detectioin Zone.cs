using UnityEngine;

public class RespawnDetectioinZone : MonoBehaviour
{
    private Vector2 respawnPoint;
    [SerializeField] Transform respawnPointTransform;
    [SerializeField] RespawnManager respawnManagerScript;

    private void Start()
    {
        respawnPoint = respawnPointTransform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3) // Huff Layer
        {
            respawnManagerScript.setNewRespawnPoint(respawnPoint);
        }
    }

}
