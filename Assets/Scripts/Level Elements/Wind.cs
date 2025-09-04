using UnityEngine;

public class Wind : MonoBehaviour
{
    public float boostFactor = 1.0f;
    public Vector2 direction = Vector2.zero;
    private AreaEffector2D areaEffector;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        areaEffector = GetComponent<AreaEffector2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3) // layer for Huff
        {

            if (collision.gameObject.GetComponent<HuffMovement>().EvaluateState().ToString() == "RoundAbout")
            {
                collision.gameObject.GetComponent<HuffMovement>().windAddForce(direction * boostFactor);
            }
        }
    }
}
