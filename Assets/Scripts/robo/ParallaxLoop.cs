using System.Collections.Generic;
using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;

    [Header("Parallax")]
    [Range(0f, 1f)]
    [Tooltip("0 = í èÌÇÃÉèÅ[ÉãÉhï®ëÃÇ¡Ç€Ç≠ìÆÇ≠ / 1 = ÉJÉÅÉâÇ…ã≠Ç≠í«è]ÇµÇƒâìåiÇ¡Ç€Ç≠å©Ç¶ÇÈ")]
    public float followCameraFraction = 0.35f;

    [Header("Loop")]
    [Min(0f)] public float recyclePadding = 1f;
    public bool collectChildrenOnStart = true;

    private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
    private float lastCameraX;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        RebuildTileList();

        if (targetCamera != null)
            lastCameraX = targetCamera.transform.position.x;
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        if (tiles.Count == 0)
            return;

        float camX = targetCamera.transform.position.x;
        float deltaX = camX - lastCameraX;

        if (Mathf.Abs(deltaX) > 0.0001f)
        {
            float shift = deltaX * followCameraFraction;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null)
                    continue;

                Transform t = tiles[i].transform;
                t.position += new Vector3(shift, 0f, 0f);
            }
        }

        RecycleTiles(camX);
        lastCameraX = camX;
    }

    [ContextMenu("Rebuild Tile List")]
    public void RebuildTileList()
    {
        tiles.Clear();

        if (collectChildrenOnStart)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].transform == transform)
                    continue;

                tiles.Add(renderers[i]);
            }
        }

        tiles.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
    }

    private void RecycleTiles(float camX)
    {
        if (tiles.Count == 0)
            return;

        float halfWidth = GetCameraHalfWidth();
        float leftLimit = camX - halfWidth - recyclePadding;
        float rightLimit = camX + halfWidth + recyclePadding;

        tiles.Sort((a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));

        for (int i = 0; i < tiles.Count; i++)
        {
            SpriteRenderer tile = tiles[i];
            if (tile == null)
                continue;

            if (tile.bounds.max.x < leftLimit)
            {
                SpriteRenderer rightMost = GetRightMostTile(tile);
                if (rightMost == null)
                    continue;

                float width = tile.bounds.size.x;
                float newX = rightMost.bounds.max.x + (width * 0.5f);
                tile.transform.position = new Vector3(newX, tile.transform.position.y, tile.transform.position.z);
            }
            else if (tile.bounds.min.x > rightLimit)
            {
                SpriteRenderer leftMost = GetLeftMostTile(tile);
                if (leftMost == null)
                    continue;

                float width = tile.bounds.size.x;
                float newX = leftMost.bounds.min.x - (width * 0.5f);
                tile.transform.position = new Vector3(newX, tile.transform.position.y, tile.transform.position.z);
            }
        }
    }

    private SpriteRenderer GetRightMostTile(SpriteRenderer ignore)
    {
        SpriteRenderer result = null;
        float maxX = float.MinValue;

        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null || tiles[i] == ignore)
                continue;

            if (tiles[i].bounds.max.x > maxX)
            {
                maxX = tiles[i].bounds.max.x;
                result = tiles[i];
            }
        }

        return result;
    }

    private SpriteRenderer GetLeftMostTile(SpriteRenderer ignore)
    {
        SpriteRenderer result = null;
        float minX = float.MaxValue;

        for (int i = 0; i < tiles.Count; i++)
        {
            if (tiles[i] == null || tiles[i] == ignore)
                continue;

            if (tiles[i].bounds.min.x < minX)
            {
                minX = tiles[i].bounds.min.x;
                result = tiles[i];
            }
        }

        return result;
    }

    private float GetCameraHalfWidth()
    {
        if (targetCamera == null)
            return 10f;

        if (targetCamera.orthographic)
            return targetCamera.orthographicSize * targetCamera.aspect;

        return 10f;
    }
}