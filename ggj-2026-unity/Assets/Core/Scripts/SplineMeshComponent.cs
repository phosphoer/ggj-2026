using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public abstract class SplineMeshComponent : MonoBehaviour
{
  public bool NeedsRebuild { get; protected set; }
  public abstract void ApplyMeshModifier(Mesh targetMesh, Transform meshTransform);
}
