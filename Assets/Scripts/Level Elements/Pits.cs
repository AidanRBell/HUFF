using UnityEngine;

public class Pits : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 3)
        {
            collision.gameObject.GetComponent<HuffMovement>().Die();
        }
    }
}
