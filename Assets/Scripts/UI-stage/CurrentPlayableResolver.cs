using UnityEngine;

public enum ControlledCharacter
{
    None,
    Mawaru13,
    Player
}

public class CurrentPlayableResolver : MonoBehaviour
{
    public enum DetectMode
    {
        AutoDetect,
        ForceMawaru13,
        ForcePlayer
    }

    [Header("Mode")]
    [SerializeField] private DetectMode detectMode = DetectMode.AutoDetect;

    [Header("Scene Refs")]
    [SerializeField] private GameObject mawaru13Object;
    [SerializeField] private GameObject playerObject;

    [Header("Optional Control Script Check")]
    [SerializeField] private Behaviour mawaru13ControlScript;
    [SerializeField] private Behaviour playerControlScript;

    public ControlledCharacter GetCurrentCharacter()
    {
        if (detectMode == DetectMode.ForceMawaru13)
            return ControlledCharacter.Mawaru13;

        if (detectMode == DetectMode.ForcePlayer)
            return ControlledCharacter.Player;

        bool playerActive = IsCharacterActive(playerObject, playerControlScript);
        bool mawaruActive = IsCharacterActive(mawaru13Object, mawaru13ControlScript);

        if (playerActive && !mawaruActive)
            return ControlledCharacter.Player;

        if (mawaruActive && !playerActive)
            return ControlledCharacter.Mawaru13;

        if (playerActive)
            return ControlledCharacter.Player;

        if (mawaruActive)
            return ControlledCharacter.Mawaru13;

        return ControlledCharacter.None;
    }

    private bool IsCharacterActive(GameObject obj, Behaviour controlScript)
    {
        if (obj == null) return false;
        if (!obj.activeInHierarchy) return false;

        if (controlScript != null)
        {
            return controlScript.enabled;
        }

        return true;
    }
}