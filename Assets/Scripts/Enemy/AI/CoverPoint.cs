using System.Collections.Generic;
using UnityEngine;

public class CoverPoint : MonoBehaviour
{
    public static readonly HashSet<CoverPoint> Active = new();

    [SerializeField] private Transform hidePoint;
    [SerializeField] private Transform peekLeftPoint;
    [SerializeField] private Transform peekRightPoint;

    public Vector3 HidePosition => hidePoint != null ? hidePoint.position : transform.position;
    public Vector3 PeekLeftPosition => peekLeftPoint != null ? peekLeftPoint.position : HidePosition;
    public Vector3 PeekRightPosition => peekRightPoint != null ? peekRightPoint.position : HidePosition;

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
}
