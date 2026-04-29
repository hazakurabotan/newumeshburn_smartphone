using UnityEngine;

public class TradeItem : MonoBehaviour
{
    public int itemType; // 0,1,2（インスペクターで設定 or 生成時にセット）

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // インベントリを探してカウントUP
            TradeItemInventory inventory = FindObjectOfType<TradeItemInventory>();
            if (inventory != null)
            {
                inventory.AddItem(itemType);
            }

            // アイテム自体は消す
            Destroy(gameObject);
        }
    }
}
