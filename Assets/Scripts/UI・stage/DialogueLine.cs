using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite portrait;
    [TextArea(2, 3)] public string text;
    public AudioClip voice;   // ‚ ‚és‚¾‚¯‰¹º‚ğ•t‚¯‚½‚¢ê‡‚Éİ’è
}
