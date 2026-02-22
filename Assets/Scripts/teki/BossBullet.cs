using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossBullet : MonoBehaviour
{
    public float lifeTime = 5f;
    public int damage = 1;

    void Start()
    {
        // 自分自身もダメージ源として機能
        var dd = GetComponent<DamageDealer>();
        if (!dd) dd = gameObject.AddComponent<DamageDealer>();
        dd.damage = damage;

        Destroy(gameObject, lifeTime);
    }
}
