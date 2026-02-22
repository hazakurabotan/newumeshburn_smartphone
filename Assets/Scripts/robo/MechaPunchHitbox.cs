using UnityEngine;

public class MechaPunchHitbox : MonoBehaviour
{
    public BossPatternController boss;
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ƒ{ƒX‚É“–‚½‚Á‚½H
        if (other.CompareTag("Boss"))
        {
            // HP‚ğŒ¸‚ç‚·
            var hp = other.GetComponent<BossHP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }

            // aŒ‚‚Ì—\”õ“®ì’†‚È‚ç’†’f‚³‚¹‚é
            if (boss != null)
            {
                boss.OnPunchedByPlayer();
            }

            Debug.Log("Punch hit BOSS from " + gameObject.name);
        }
    }
}
