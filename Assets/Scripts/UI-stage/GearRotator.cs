using System.Collections;
using UnityEngine;

public class GearRotator : MonoBehaviour
{
    [Header("Room (physics)")]
    public Rigidbody2D roomRootRb;      // ‰ñ“]•”‰®‚Ìe‚ÌRigidbody2D(Kinematic)
    public float rotateTime = 0.30f;

    [Header("Input detect")]
    public float deadZone = 0.5f;
    public float requireAccumDeg = 120f;

    [Header("Freeze")]
    public FreezeAllEnemies freezer;   // Šù‘¶‚Ì“G’âŽ~Žd‘g‚Ý‚ª‚ ‚é‚È‚ç·‚µ‘Ö‚¦‚ÄOK

    Rigidbody2D playerRb;
    MawaruController mawaru;
    bool attached;
    bool rotating;

    float prevAngle;
    float accum;

    public void Attach(Rigidbody2D prb, MawaruController owner)
    {
        playerRb = prb;
        mawaru = owner;
        attached = true;
        rotating = false;
        accum = 0f;

        // ‰ŠúŠp“x
        var v = ReadAxis();
        prevAngle = (v.sqrMagnitude > 0.01f) ? Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg : 0f;
    }

    void Update()
    {
        if (!attached || rotating) return;
        if (mawaru == null || !mawaru.IsHangingNow) return; // ‚Ô‚ç‰º‚ª‚è’†‚¾‚¯—LŒø

        Vector2 v = ReadAxis();
        if (v.magnitude < deadZone) return;

        float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(prevAngle, ang); // + = CCW, - = CW
        prevAngle = ang;

        accum += delta;

        if (accum >= requireAccumDeg)
        {
            accum = 0f;
            StartCoroutine(RotateRoom(+1)); // CCW
        }
        else if (accum <= -requireAccumDeg)
        {
            accum = 0f;
            StartCoroutine(RotateRoom(-1)); // CW
        }
    }

    IEnumerator RotateRoom(int dir) // dir: +1 CCW / -1 CW
    {
        if (roomRootRb == null) yield break;
        rotating = true;

        // ‰ñ“]’†‚Í“G’âŽ~
        if (freezer) freezer.SetFreeze(true);

        // ‰ñ“]’†AƒvƒŒƒCƒ„[‚Ì–\‚ê–hŽ~iŒy‚­ŒÅ’èj
        var oldVel = playerRb.velocity;
        playerRb.velocity = Vector2.zero;

        float start = roomRootRb.rotation;
        float end = start + dir * 90f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / rotateTime;
            float e = Mathf.SmoothStep(0f, 1f, t);
            float a = Mathf.LerpAngle(start, end, e);
            roomRootRb.MoveRotation(a);
            yield return new WaitForFixedUpdate();
        }

        // 90“x‚ÉƒXƒiƒbƒviŒë·‘Îôj
        float snapped = Mathf.Round(end / 90f) * 90f;
        roomRootRb.MoveRotation(snapped);

        // “GÄŠJ
        if (freezer) freezer.SetFreeze(false);

        // ‚Ô‚ç‰º‚ª‚è‚ÍŒp‘±iƒvƒŒƒCƒ„[‚É”C‚¹‚éj
        // Jump‚Å—£’E‚Í MawaruController ‘¤‚ÌŠù‘¶“®ì‚Ì‚Ü‚Ü‚ÅOK

        rotating = false;
    }

    Vector2 ReadAxis()
    {
        if (mawaru == null) return Vector2.zero;
        var v = mawaru.MoveAxis;
        // ‰º•ûŒü‚Íg‰ñ‚·hŒŸo‚ÉŽg‚Á‚ÄOKi‚Í‚µ‚²“ü—Í‚Æ‹£‡‚·‚é‚È‚ç clamp‚µ‚Ä‚à‚¢‚¢j
        return v;
    }
}