using UnityEngine;

public class DestroyMeshOnDestroy : MonoBehaviour
{
  public bool UseMeshPool = false;

  private void OnDestroy()
  {
    MeshFilter meshFilter = GetComponent<MeshFilter>();

    if (UseMeshPool)
      MeshPool.FreeMesh(meshFilter.sharedMesh);
    else
      Destroy(meshFilter.sharedMesh);

    meshFilter.sharedMesh = null;
  }
}