using UnityEngine;

public class CarryCaseBomb : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float autoDestroyTime = 6f;

    bool exploded;

    void Start()
    {
        Destroy(gameObject, autoDestroyTime);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (exploded) return;
        if (((1 << col.gameObject.layer) & groundLayer.value) == 0) return;

        exploded = true;

        if (explosionPrefab)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
