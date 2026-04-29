using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class RopeGrabbable : MonoBehaviour
{
    public Rigidbody2D rb;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }
}