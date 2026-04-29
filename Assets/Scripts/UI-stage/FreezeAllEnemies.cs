using UnityEngine;

public class FreezeAllEnemies : MonoBehaviour
{
    public interface IFreezable
    {
        void Freeze(bool v);
    }

    public void SetFreeze(bool v) => Apply(v);
    public void SetFrozen(bool v) => Apply(v); // ŒÝŠ·—p

    void Apply(bool v)
    {
        var all = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in all)
        {
            if (mb is IFreezable f) f.Freeze(v);
        }
    }
}