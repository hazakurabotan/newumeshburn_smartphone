using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class FallDeathZone2D : MonoBehaviour
{
    public enum FallbackAction
    {
        None,
        ReloadCurrentScene,
        MoveToRespawnPoint,
        DisableTarget
    }

    [Header("死亡対象にするTag")]
    [SerializeField]
    private string[] targetTags =
    {
        "Player",
        "Mawaru",
        "Mawaru13"
    };

    [Header("Tagが合わない時に名前で判定する")]
    [SerializeField]
    private string[] targetNameContains =
    {
        "Player",
        "mawaru13",
        "Mawaru"
    };

    [Header("HP処理がある場合に呼ぶダメージメソッド")]
    [SerializeField] private bool trySendFatalDamage = true;

    [SerializeField]
    private string[] damageMethodNames =
    {
        "TakeDamage",
        "Damage",
        "ApplyDamage"
    };

    [SerializeField] private int fatalDamage = 9999;

    [Header("即死メソッドがある場合に呼ぶ")]
    [SerializeField] private bool trySendDeathMessage = true;

    [SerializeField]
    private string[] deathMethodNames =
    {
        "OnDeadZoneTouched",
        "OnFallDeath",
        "Die",
        "Dead",
        "Kill",
        "GameOver",
        "OnDeath"
    };

    [Header("上のメソッドが見つからなかった時の予備処理")]
    [SerializeField] private FallbackAction fallbackAction = FallbackAction.ReloadCurrentScene;

    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallbackDelay = 0.1f;

    [Header("連続判定防止")]
    [SerializeField] private float sameTargetCooldown = 1.0f;

    private int lastTargetInstanceId = -1;
    private float lastHitTime = -999f;
    private bool isReloading;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject target = GetTargetRoot(other);

        if (target == null)
        {
            return;
        }

        if (!IsDeathTarget(target, other.gameObject))
        {
            return;
        }

        int id = target.GetInstanceID();

        if (lastTargetInstanceId == id && Time.unscaledTime - lastHitTime < sameTargetCooldown)
        {
            return;
        }

        lastTargetInstanceId = id;
        lastHitTime = Time.unscaledTime;

        ExecuteDeath(target);
    }

    private GameObject GetTargetRoot(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.gameObject;
        }

        return other.transform.root.gameObject;
    }

    private bool IsDeathTarget(GameObject root, GameObject hitObject)
    {
        if (root == null)
        {
            return false;
        }

        for (int i = 0; i < targetTags.Length; i++)
        {
            string tagName = targetTags[i];

            if (string.IsNullOrEmpty(tagName))
            {
                continue;
            }

            if (root.CompareTag(tagName))
            {
                return true;
            }

            if (hitObject != null && hitObject.CompareTag(tagName))
            {
                return true;
            }
        }

        string rootName = root.name;
        string hitName = hitObject != null ? hitObject.name : string.Empty;

        for (int i = 0; i < targetNameContains.Length; i++)
        {
            string keyword = targetNameContains[i];

            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }

            if (rootName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (hitName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void ExecuteDeath(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        bool handled = false;

        if (trySendFatalDamage)
        {
            handled = TryInvokeMethodWithNumber(target, damageMethodNames, fatalDamage);
        }

        if (!handled && trySendDeathMessage)
        {
            handled = TryInvokeMethodWithoutParameter(target, deathMethodNames);
        }

        if (!handled)
        {
            ExecuteFallback(target);
        }
    }

    private bool TryInvokeMethodWithoutParameter(GameObject target, string[] methodNames)
    {
        if (target == null || methodNames == null)
        {
            return false;
        }

        Component[] components = target.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();

            for (int j = 0; j < methodNames.Length; j++)
            {
                string methodName = methodNames[j];

                if (string.IsNullOrEmpty(methodName))
                {
                    continue;
                }

                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != 0)
                {
                    continue;
                }

                method.Invoke(component, null);
                return true;
            }
        }

        return false;
    }

    private bool TryInvokeMethodWithNumber(GameObject target, string[] methodNames, int value)
    {
        if (target == null || methodNames == null)
        {
            return false;
        }

        Component[] components = target.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();

            for (int j = 0; j < methodNames.Length; j++)
            {
                string methodName = methodNames[j];

                if (string.IsNullOrEmpty(methodName))
                {
                    continue;
                }

                MethodInfo method = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                Type parameterType = parameters[0].ParameterType;

                if (parameterType == typeof(int))
                {
                    method.Invoke(component, new object[] { value });
                    return true;
                }

                if (parameterType == typeof(float))
                {
                    method.Invoke(component, new object[] { (float)value });
                    return true;
                }
            }
        }

        return false;
    }

    private void ExecuteFallback(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        switch (fallbackAction)
        {
            case FallbackAction.None:
                break;

            case FallbackAction.ReloadCurrentScene:
                if (!isReloading)
                {
                    StartCoroutine(ReloadCurrentSceneAfterDelay());
                }
                break;

            case FallbackAction.MoveToRespawnPoint:
                MoveTargetToRespawnPoint(target);
                break;

            case FallbackAction.DisableTarget:
                target.SetActive(false);
                break;
        }
    }

    private IEnumerator ReloadCurrentSceneAfterDelay()
    {
        isReloading = true;

        if (fallbackDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(fallbackDelay);
        }

        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void MoveTargetToRespawnPoint(GameObject target)
    {
        if (target == null || respawnPoint == null)
        {
            return;
        }

        target.transform.position = respawnPoint.position;

        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = target.GetComponentInChildren<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}