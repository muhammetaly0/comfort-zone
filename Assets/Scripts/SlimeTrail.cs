using UnityEngine;
using System.Collections.Generic;

// Gregor'un arkasında bıraktığı yapışkan sıvı izi.
// Bu scripti Gregor'a ekle. Line Renderer da aynı objede olmalı.
[RequireComponent(typeof(LineRenderer))]
public class SlimeTrail : MonoBehaviour
{
    [SerializeField] private float minPointDistance = 0.08f;
    [SerializeField] private int maxPoints = 120;
    [SerializeField] private float fadeTime = 12f;

    private LineRenderer _lr;
    private readonly Queue<Vector3> _positions  = new();
    private readonly Queue<float>   _timestamps = new();
    private Vector3 _lastAdded = Vector3.positiveInfinity;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true;
        _lr.positionCount = 0;
    }

    private void Update()
    {
        // Eski noktaları temizle
        while (_timestamps.Count > 0 && Time.time - _timestamps.Peek() > fadeTime)
        {
            _timestamps.Dequeue();
            _positions.Dequeue();
        }

        RefreshLineRenderer();
    }

    // GregorAI her FixedUpdate'te çağırır
    public void AddPoint(Vector3 worldPos)
    {
        if (Vector3.Distance(worldPos, _lastAdded) < minPointDistance) return;

        _lastAdded = worldPos;
        _positions.Enqueue(worldPos);
        _timestamps.Enqueue(Time.time);

        if (_positions.Count > maxPoints)
        {
            _positions.Dequeue();
            _timestamps.Dequeue();
        }
    }

    private void RefreshLineRenderer()
    {
        Vector3[] pts = _positions.ToArray();
        _lr.positionCount = pts.Length;
        if (pts.Length > 0) _lr.SetPositions(pts);
    }
}
