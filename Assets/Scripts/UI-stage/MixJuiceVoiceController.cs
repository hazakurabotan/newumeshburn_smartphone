using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MixJuiceVoiceController : MonoBehaviour
{
    public enum JuiceVoiceType
    {
        None = 0,
        Apple = 1,
        Cider = 2,
        Orange = 3,
        Lime = 4,
        Grape = 5
    }

    public enum DebugVoiceType
    {
        None = 0,
        Apple = 1,
        Cider = 2,
        Orange = 3,
        Lime = 4,
        Grape = 5,
        MixJuice = 6,
        MixingFinish = 7
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource voiceSource;

    [Header("Voice Clips - Juice")]
    [SerializeField] private AudioClip appleVoice;
    [SerializeField] private AudioClip ciderVoice;
    [SerializeField] private AudioClip orangeVoice;
    [SerializeField] private AudioClip limeVoice;
    [SerializeField] private AudioClip grapeVoice;

    [Header("Voice Clips - Common")]
    [SerializeField] private AudioClip mixJuiceVoice;
    [SerializeField] private AudioClip mixingFinishVoice;

    [Header("Playback Settings")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float intervalAfterVoice = 0.03f;
    [SerializeField] private bool interruptCurrentVoice = false;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;
    [SerializeField] private bool debugPlayOnEnable = false;
    [SerializeField] private DebugVoiceType debugVoiceType = DebugVoiceType.Apple;

    private readonly Queue<AudioClip> voiceQueue = new Queue<AudioClip>();
    private Coroutine queueCoroutine;

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void OnEnable()
    {
        if (debugPlayOnEnable)
        {
            StartCoroutine(DebugPlayOnEnableCoroutine());
        }
    }

    private IEnumerator DebugPlayOnEnableCoroutine()
    {
        yield return null;
        PlayDebugVoice();
    }

    private void EnsureAudioSource()
    {
        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;

        if (verboseLog)
        {
            string mixerName = voiceSource.outputAudioMixerGroup != null
                ? voiceSource.outputAudioMixerGroup.name
                : "None";

            Debug.Log(
                $"[MixVoice] AudioSource ready. " +
                $"GO={voiceSource.gameObject.name}, " +
                $"enabled={voiceSource.enabled}, " +
                $"activeInHierarchy={voiceSource.gameObject.activeInHierarchy}, " +
                $"volume={voiceSource.volume}, " +
                $"mute={voiceSource.mute}, " +
                $"mixer={mixerName}",
                this
            );
        }
    }

    public void StopAllVoices()
    {
        voiceQueue.Clear();

        if (queueCoroutine != null)
        {
            StopCoroutine(queueCoroutine);
            queueCoroutine = null;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
        }

        if (verboseLog)
        {
            Debug.Log("[MixVoice] StopAllVoices()", this);
        }
    }

    public void PlayFirstJuiceVoice(JuiceVoiceType juiceType)
    {
        if (verboseLog)
        {
            Debug.Log($"[MixVoice] PlayFirstJuiceVoice({juiceType})", this);
        }

        EnqueueVoice(GetJuiceClip(juiceType));
    }

    public void PlaySecondJuiceVoice(JuiceVoiceType juiceType)
    {
        if (verboseLog)
        {
            Debug.Log($"[MixVoice] PlaySecondJuiceVoice({juiceType})", this);
        }

        EnqueueVoice(GetJuiceClip(juiceType));
    }

    public void PlayJuiceVoice(JuiceVoiceType juiceType)
    {
        if (verboseLog)
        {
            Debug.Log($"[MixVoice] PlayJuiceVoice({juiceType})", this);
        }

        EnqueueVoice(GetJuiceClip(juiceType));
    }

    public void PlayJuiceVoiceByName(string juiceName)
    {
        JuiceVoiceType parsed = ParseJuiceName(juiceName);

        if (verboseLog)
        {
            Debug.Log($"[MixVoice] PlayJuiceVoiceByName(\"{juiceName}\") -> {parsed}", this);
        }

        EnqueueVoice(GetJuiceClip(parsed));
    }

    public void PlayMixJuiceVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayMixJuiceVoice()", this);
        }

        EnqueueVoice(mixJuiceVoice);
    }

    public void PlayMixingFinishVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayMixingFinishVoice()", this);
        }

        EnqueueVoice(mixingFinishVoice);
    }

    public void PlayAppleVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayAppleVoice()", this);
        }

        EnqueueVoice(appleVoice);
    }

    public void PlayCiderVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayCiderVoice()", this);
        }

        EnqueueVoice(ciderVoice);
    }

    public void PlayOrangeVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayOrangeVoice()", this);
        }

        EnqueueVoice(orangeVoice);
    }

    public void PlayLimeVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayLimeVoice()", this);
        }

        EnqueueVoice(limeVoice);
    }

    public void PlayGrapeVoice()
    {
        if (verboseLog)
        {
            Debug.Log("[MixVoice] PlayGrapeVoice()", this);
        }

        EnqueueVoice(grapeVoice);
    }

    [ContextMenu("Debug/Play Selected Debug Voice")]
    public void PlayDebugVoice()
    {
        if (verboseLog)
        {
            Debug.Log($"[MixVoice] PlayDebugVoice({debugVoiceType})", this);
        }

        switch (debugVoiceType)
        {
            case DebugVoiceType.Apple:
                PlayAppleVoice();
                break;

            case DebugVoiceType.Cider:
                PlayCiderVoice();
                break;

            case DebugVoiceType.Orange:
                PlayOrangeVoice();
                break;

            case DebugVoiceType.Lime:
                PlayLimeVoice();
                break;

            case DebugVoiceType.Grape:
                PlayGrapeVoice();
                break;

            case DebugVoiceType.MixJuice:
                PlayMixJuiceVoice();
                break;

            case DebugVoiceType.MixingFinish:
                PlayMixingFinishVoice();
                break;

            default:
                Debug.LogWarning("[MixVoice] DebugVoiceType is None.", this);
                break;
        }
    }

    [ContextMenu("Debug/Play Apple")]
    public void DebugPlayApple() => PlayAppleVoice();

    [ContextMenu("Debug/Play Cider")]
    public void DebugPlayCider() => PlayCiderVoice();

    [ContextMenu("Debug/Play Orange")]
    public void DebugPlayOrange() => PlayOrangeVoice();

    [ContextMenu("Debug/Play Lime")]
    public void DebugPlayLime() => PlayLimeVoice();

    [ContextMenu("Debug/Play Grape")]
    public void DebugPlayGrape() => PlayGrapeVoice();

    [ContextMenu("Debug/Play MixJuice")]
    public void DebugPlayMixJuice() => PlayMixJuiceVoice();

    [ContextMenu("Debug/Play MixingFinish")]
    public void DebugPlayMixingFinish() => PlayMixingFinishVoice();

    private void EnqueueVoice(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[MixVoice] EnqueueVoice failed. clip is null.", this);
            return;
        }

        EnsureAudioSource();

        if (voiceSource == null)
        {
            Debug.LogError("[MixVoice] AudioSource is null.", this);
            return;
        }

        if (!voiceSource.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[MixVoice] AudioSource GameObject is inactive.", voiceSource.gameObject);
        }

        if (!voiceSource.enabled)
        {
            Debug.LogWarning("[MixVoice] AudioSource component is disabled.", voiceSource);
        }

        if (interruptCurrentVoice)
        {
            StopAllVoices();
        }

        voiceQueue.Enqueue(clip);

        if (verboseLog)
        {
            Debug.Log($"[MixVoice] Enqueued clip: {clip.name}", this);
        }

        if (queueCoroutine == null)
        {
            queueCoroutine = StartCoroutine(PlayQueueCoroutine());
        }
    }

    private IEnumerator PlayQueueCoroutine()
    {
        while (voiceQueue.Count > 0)
        {
            AudioClip clip = voiceQueue.Dequeue();

            if (clip == null)
            {
                Debug.LogWarning("[MixVoice] Queue contained null clip.", this);
                continue;
            }

            voiceSource.clip = clip;
            voiceSource.Play();

            if (verboseLog)
            {
                string mixerName = voiceSource.outputAudioMixerGroup != null
                    ? voiceSource.outputAudioMixerGroup.name
                    : "None";

                Debug.Log(
                    $"[MixVoice] Playing clip: {clip.name}, " +
                    $"length={clip.length}, " +
                    $"sourceGO={voiceSource.gameObject.name}, " +
                    $"enabled={voiceSource.enabled}, " +
                    $"activeInHierarchy={voiceSource.gameObject.activeInHierarchy}, " +
                    $"mute={voiceSource.mute}, " +
                    $"volume={voiceSource.volume}, " +
                    $"mixer={mixerName}",
                    this
                );
            }

            float waitTime = clip.length + intervalAfterVoice;
            if (waitTime < 0f)
            {
                waitTime = 0f;
            }

            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(waitTime);
            }
            else
            {
                yield return new WaitForSeconds(waitTime);
            }
        }

        queueCoroutine = null;
    }

    private AudioClip GetJuiceClip(JuiceVoiceType juiceType)
    {
        switch (juiceType)
        {
            case JuiceVoiceType.Apple:
                return appleVoice;

            case JuiceVoiceType.Cider:
                return ciderVoice;

            case JuiceVoiceType.Orange:
                return orangeVoice;

            case JuiceVoiceType.Lime:
                return limeVoice;

            case JuiceVoiceType.Grape:
                return grapeVoice;

            default:
                return null;
        }
    }

    private JuiceVoiceType ParseJuiceName(string juiceName)
    {
        if (string.IsNullOrEmpty(juiceName))
        {
            return JuiceVoiceType.None;
        }

        string n = juiceName.Trim().ToLowerInvariant();

        switch (n)
        {
            case "apple":
            case "アップル":
            case "りんご":
            case "リンゴ":
                return JuiceVoiceType.Apple;

            case "cider":
            case "サイダー":
                return JuiceVoiceType.Cider;

            case "orange":
            case "オレンジ":
                return JuiceVoiceType.Orange;

            case "lime":
            case "ライム":
                return JuiceVoiceType.Lime;

            case "grape":
            case "グレープ":
            case "ぶどう":
            case "ブドウ":
                return JuiceVoiceType.Grape;

            default:
                return JuiceVoiceType.None;
        }
    }
}