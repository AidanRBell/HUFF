using UnityEngine;

public class Pits : MonoBehaviour
{
    public LayerMask HuffLayer;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == HuffLayer)
        {
            collision.gameObject.GetComponent<HuffMovement>().Die();
        }
    }
}
