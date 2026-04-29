using UnityEngine;

[ExecuteAlways]
public class ParallaxLayer2D : MonoBehaviour
{
    [Header("Target Camera")]
    [SerializeField] private Transform targetCamera;

    [Header("Parallax")]
    [SerializeField][Range(0f, 1.5f)] private float parallaxX = 0.5f;
    [SerializeField][Range(0f, 1.5f)] private float parallaxY = 0f;

    [Header("Options")]
    [SerializeField] private bool applyInPlayModeOnly = true;
    [SerializeField] private bool keepZPosition = true;

    private Vector3 startLayerPosition;
    private Vector3 startCameraPosition;
    private float fixedZ;

    private void Awake()
    {
        CacheStartState();
    }

    private void OnEnable()
    {
        CacheStartState();
    }

    private void Start()
    {
        CacheStartState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && !applyInPlayModeOnly)
        {
            CacheStartState();
        }
    }
#endif

    private void LateUpdate()
    {
        if (applyInPlayModeOnly && !Application.isPlaying)
        {
            return;
        }

        if (targetCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                targetCamera = mainCam.transform;
                CacheStartState();
            }
            else
            {
                return;
            }
        }

        Vector3 cameraDelta = targetCamera.position - startCameraPosition;

        Vector3 newPosition = new Vector3(
            startLayerPosition.x + cameraDelta.x * parallaxX,
            startLayerPosition.y + cameraDelta.y * parallaxY,
            keepZPosition ? fixedZ : transform.position.z
        );

        transform.position = newPosition;
    }

    [ContextMenu("Reset Start State")]
    public void ResetStartState()
    {
        CacheStartState();
    }

    private void CacheStartState()
    {
        if (targetCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                targetCamera = mainCam.transform;
            }
        }

        startLayerPosition = transform.position;
        fixedZ = transform.position.z;

        if (targetCamera != null)
        {
            startCameraPosition = targetCamera.position;
        }
    }
}