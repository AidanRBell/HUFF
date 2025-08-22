using UnityEngine;

public class CrackedBlock : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {// if the colliding object is Huff and Huff is Wall Bouncing
        if (collision.gameObject.layer == 3 && collision.gameObject.GetComponent<HuffMovement>().huffState == HuffMovement.CharacterState.RoundAbout) 
        {
            gameObject.SetActive(false);
        }
    }


}
