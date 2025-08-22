using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

public class VoltBuddy : MonoBehaviour
{
    public int count = 0;
    private bool infinity = false;
    private bool bolt = false;

    [SerializeField] bool isFollowingHuff = true;

    public GameObject target;
    public float smoothTime;
    private Vector2 velocity = Vector2.zero;


    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
        anim.SetInteger("Count", count);
    }

    void Update()
    {
        if (count >= 0)
        {
            Vector2 currPos = transform.position;
            Vector2 targetPos = target.transform.position;

            Vector2 newPos = Vector2.SmoothDamp(currPos, targetPos, ref velocity, smoothTime);

            transform.position = newPos;
        }

        anim.SetInteger("Count", count);
    }

    public void setCount(int voltCount)
    {
        count = voltCount;
    }

    public void increaseVoltCount()
    {
        count++;
    }

    public void decreaseVoltCount()
    {
        count--;
    }

    // this function toggles weather following volt should be currently following huff
    public void setFollowingHuff(bool isFollowingHuff)
    {

    }

    public void setCountToInfinity()
    {

    }

}
