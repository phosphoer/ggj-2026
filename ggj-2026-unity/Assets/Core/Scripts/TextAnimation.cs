using UnityEngine;
using TMPro;

public class TextAnimation : MonoBehaviour
{
  [SerializeField] private TMP_Text _text = null;

  private bool _hasTextChanged = true;
  private bool _isTypingText = false;
  private TMP_TextInfo _textInfo;
  private TMP_MeshInfo[] _cachedMeshInfo;
  private float _animTimer;
  private float _typeTextTimer;
  private int _currentTypeCharIndex;

  private void OnEnable()
  {
    _animTimer = 0;
    _typeTextTimer = 0;
    _currentTypeCharIndex = 0;
    _isTypingText = false;
    _textInfo = _text.textInfo;
    _hasTextChanged = true;
    TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
  }

  private void OnDisable()
  {
    TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
  }

  private void Update()
  {
    if (_hasTextChanged && _textInfo.characterCount > 0)
    {
      _textInfo.ClearAllMeshInfo();
      _text.ForceMeshUpdate();
      _textInfo = _text.textInfo;
      _cachedMeshInfo = _textInfo.CopyMeshInfoVertexData();
      _hasTextChanged = false;

      if (!_isTypingText)
      {
        _typeTextTimer = 0;
        _isTypingText = true;
        _currentTypeCharIndex = 0;
        _text.maxVisibleCharacters = 0;
      }
    }

    _animTimer += Time.unscaledDeltaTime;

    if (_isTypingText)
    {
      TypeTextUpdate();
    }

    for (int linkIndex = 0; linkIndex < _textInfo.linkCount; ++linkIndex)
    {
      TMP_LinkInfo linkInfo = _textInfo.linkInfo[linkIndex];
      if (linkInfo.GetLinkID() == "exclaim")
      {
        for (int i = linkInfo.linkTextfirstCharacterIndex; i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; ++i)
        {
          TMP_CharacterInfo charInfo = _textInfo.characterInfo[i];
          if (!charInfo.isVisible)
            continue;

          // Get the index of the material used by the current character.
          int materialIndex = _textInfo.characterInfo[i].materialReferenceIndex;

          // Get the index of the first vertex used by this text element.
          int vertexIndex = _textInfo.characterInfo[i].vertexIndex;

          // Get the cached vertices of the mesh used by this text element (character or sprite).
          Vector3[] sourceVertices = _cachedMeshInfo[materialIndex].vertices;

          Vector3[] destinationVertices = _textInfo.meshInfo[materialIndex].vertices;

          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            destinationVertices[vertexIndex + vertOffset].y = sourceVertices[vertexIndex + vertOffset].y + Mathf.Sin(i + _animTimer * 15) * 3;
        }
      }
      else if (linkInfo.GetLinkID() == "spooky")
      {
        for (int i = linkInfo.linkTextfirstCharacterIndex; i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; ++i)
        {
          TMP_CharacterInfo charInfo = _textInfo.characterInfo[i];
          if (!charInfo.isVisible)
            continue;

          // Get the index of the material used by the current character.
          int materialIndex = _textInfo.characterInfo[i].materialReferenceIndex;

          // Get the index of the first vertex used by this text element.
          int vertexIndex = _textInfo.characterInfo[i].vertexIndex;

          // Get the cached vertices of the mesh used by this text element (character or sprite).
          Vector3[] sourceVertices = _cachedMeshInfo[materialIndex].vertices;

          Vector3[] destinationVertices = _textInfo.meshInfo[materialIndex].vertices;

          float x = sourceVertices[vertexIndex].x;
          Quaternion rot = Quaternion.Euler(0, 0, Mathf.Sin(x + _animTimer * 5) * 5);
          Vector3 centerPos = Vector3.zero;
          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            centerPos += sourceVertices[vertexIndex + vertOffset];
          centerPos /= 4;

          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            destinationVertices[vertexIndex + vertOffset] = rot * (sourceVertices[vertexIndex + vertOffset] - centerPos) + centerPos;
        }
      }
      else if (linkInfo.GetLinkID() == "emph")
      {
        for (int i = linkInfo.linkTextfirstCharacterIndex; i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; ++i)
        {
          TMP_CharacterInfo charInfo = _textInfo.characterInfo[i];
          if (!charInfo.isVisible)
            continue;

          // Get the index of the material used by the current character.
          int materialIndex = _textInfo.characterInfo[i].materialReferenceIndex;

          // Get the index of the first vertex used by this text element.
          int vertexIndex = _textInfo.characterInfo[i].vertexIndex;

          // Get the cached vertices of the mesh used by this text element (character or sprite).
          Vector3[] sourceVertices = _cachedMeshInfo[materialIndex].vertices;

          Vector3[] destinationVertices = _textInfo.meshInfo[materialIndex].vertices;

          float x = sourceVertices[vertexIndex].x;
          Quaternion rot = Quaternion.Euler(0, 0, Mathf.Sin(x + _animTimer * 10) * 8);
          Vector3 centerPos = Vector3.zero;
          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            centerPos += sourceVertices[vertexIndex + vertOffset];
          centerPos /= 4;

          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            destinationVertices[vertexIndex + vertOffset] = rot * (sourceVertices[vertexIndex + vertOffset] - centerPos) + centerPos;
        }
      }
      else if (linkInfo.GetLinkID() == "tiny")
      {
        for (int i = linkInfo.linkTextfirstCharacterIndex; i < linkInfo.linkTextfirstCharacterIndex + linkInfo.linkTextLength; ++i)
        {
          TMP_CharacterInfo charInfo = _textInfo.characterInfo[i];
          if (!charInfo.isVisible)
            continue;

          // Get the index of the material used by the current character.
          int materialIndex = _textInfo.characterInfo[i].materialReferenceIndex;

          // Get the index of the first vertex used by this text element.
          int vertexIndex = _textInfo.characterInfo[i].vertexIndex;

          // Get the cached vertices of the mesh used by this text element (character or sprite).
          Vector3[] sourceVertices = _cachedMeshInfo[materialIndex].vertices;

          Vector3[] destinationVertices = _textInfo.meshInfo[materialIndex].vertices;

          float x = sourceVertices[vertexIndex].x;
          float scaleAmount = 0.9f;
          Vector3 centerPos = Vector3.zero;
          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            centerPos += sourceVertices[vertexIndex + vertOffset];
          centerPos /= 4;

          for (int vertOffset = 0; vertOffset < 4; vertOffset++)
            destinationVertices[vertexIndex + vertOffset] = scaleAmount * (sourceVertices[vertexIndex + vertOffset] - centerPos) + centerPos;
        }
      }
    }

    // Push changes into meshes
    for (int i = 0; i < _textInfo.meshInfo.Length; i++)
    {
      _textInfo.meshInfo[i].mesh.vertices = _textInfo.meshInfo[i].vertices;
      _text.UpdateGeometry(_textInfo.meshInfo[i].mesh, i);
    }
  }

  private void TypeTextUpdate()
  {
    if (_currentTypeCharIndex < _text.textInfo.characterCount)
    {
      _typeTextTimer -= Time.unscaledDeltaTime;
      if (_typeTextTimer <= 0)
      {
        char letter = _text.textInfo.characterInfo[_currentTypeCharIndex].character;
        char prevLetter = _currentTypeCharIndex - 1 >= 0 ? _text.textInfo.characterInfo[_currentTypeCharIndex - 1].character : (char)0;
        char nextLetter = _currentTypeCharIndex + 1 < _text.textInfo.characterCount ? _text.textInfo.characterInfo[_currentTypeCharIndex + 1].character : (char)0;
        _text.maxVisibleCharacters = _currentTypeCharIndex + 1;

        _typeTextTimer = Random.Range(0.02f, 0.03f);
        if (IsPunctuation(letter) && !IsPunctuation(nextLetter))
          _typeTextTimer += 0.25f;

        if (letter == '-' && prevLetter == '-')
          _typeTextTimer += 0.5f;

        _currentTypeCharIndex += 1;
      }
    }
    else
    {
      _isTypingText = false;
    }
  }

  private void OnTextChanged(Object textObj)
  {
    if (ReferenceEquals(textObj, _text))
      _hasTextChanged = true;
  }

  private static bool IsPunctuation(char letter)
  {
    return letter == ',' || letter == '.' || letter == '?' || letter == '!' || letter == ';';
  }
}