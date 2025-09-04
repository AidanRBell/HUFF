using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    [SerializeField] Transform startingSpawnPosTransform;
    [SerializeField] Vector2 spawnPos; // set this as R0
    [SerializeField] private Transform huff;

    public bool testingMode = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnPos = startingSpawnPosTransform.position;
        if (!testingMode) huff.transform.position = spawnPos;
    }

    public void Respawn()
    {

    }

    public void HuffDied() // Do more than this
    { 
        huff.position = spawnPos;
    }

    public void setNewRespawnPoint(Vector2 respawnPoint)
    {
        spawnPos = respawnPoint;
    }

}
