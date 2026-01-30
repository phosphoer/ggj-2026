using UnityEngine;

public class RuntimeReferenceAsset : ScriptableObject
{
  [SerializeField] private string _valueTypeName;

  [System.NonSerialized] public object Value;

  public System.Type ValueType
  {
    get => string.IsNullOrEmpty(_valueTypeName)
        ? null
        : System.Type.GetType(_valueTypeName);
    set => _valueTypeName = value?.AssemblyQualifiedName;
  }
}

[System.Serializable]
public struct ReferenceAsset<T>
{
  [SerializeField]
  private RuntimeReferenceAsset _asset;

  public T Value
  {
    get
    {
      if (_asset == null)
        return default;

      return (T)_asset.Value;
    }

    set
    {
      if (!_asset)
        return;

      _asset.Value = value;
      _asset.ValueType = typeof(T);
    }
  }
}

// public abstract class ReferenceAssetBase<T> : ReferenceAssetBase
// {
//   [SerializeField]
//   protected T value;

//   public T Value
//   {
//     get => value;
//     set => this.value = value;
//   }

//   public override System.Type ValueType => typeof(T);
// }

// public class RenderTextureReferenceAsset : ReferenceAssetBase<RenderTexture> { }