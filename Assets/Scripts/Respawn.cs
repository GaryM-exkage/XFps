using UnityEngine;

public class Respawn : MonoBehaviour
{
    [SerializeField] FPSController player;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            player.transform.position = transform.position;
            player.velocity = Vector3.zero;
            player.momentum = Vector3.zero;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

}
