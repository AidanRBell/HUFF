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
            Debug.Log("colided with Huff");

            if (collision.gameObject.GetComponent<HuffMovement>().EvaluateState().ToString() == "RoundAbout")
            {
                Debug.Log("Doing roundabout");
                collision.gameObject.GetComponent<HuffMovement>().bodyAddForce(direction * boostFactor);
            }
        }
    }
}
