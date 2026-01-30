using UnityEngine;
using System.Collections.Generic;

public enum ValueModifierOperation
{
  Add,
  AddMultiple,
}

[System.Serializable]
public struct ValueModifier
{
  public ValueModifierOperation Operation;
  public float Amount;
  public string Key;
}

[System.Serializable]
public struct ModdedValue
{
  public float BaseValue;

  private List<ValueModifier> _modifiers;

  public static implicit operator float(ModdedValue moddedValue) => moddedValue.GetValue();
  public static implicit operator ModdedValue(float value) => new ModdedValue() { BaseValue = value };

  public float GetValue()
  {
    float finalValue = BaseValue;

    for (int i = 0; _modifiers != null && i < _modifiers.Count; ++i)
    {
      ValueModifier valueModifier = _modifiers[i];
      if (valueModifier.Operation == ValueModifierOperation.Add)
        finalValue += valueModifier.Amount;
      else
        finalValue += BaseValue * valueModifier.Amount;
    }

    return finalValue;
  }

  public void AddModifier(ValueModifierOperation operation, float amount, string key)
  {
    if (_modifiers == null)
      _modifiers = new List<ValueModifier>();

    _modifiers.Add(new ValueModifier()
    {
      Operation = operation,
      Amount = amount,
      Key = key,
    });
  }

  public void RemoveModifier(string key)
  {
    if (_modifiers != null)
    {
      for (int i = _modifiers.Count - 1; i >= 0; --i)
      {
        if (_modifiers[i].Key == key)
          _modifiers.RemoveAt(i);
      }
    }
  }
}

public struct FloatModifier
{
  private float _addedAmount;
  private bool _didAdd;

  public float TryAddValue(float value, float amount)
  {
    if (!_didAdd)
    {
      _didAdd = true;
      _addedAmount = amount;
      return value + amount;
    }

    return value;
  }

  public float TryRemoveValue(float value)
  {
    if (_didAdd)
    {
      _didAdd = false;
      return value - _addedAmount;
    }

    return value;
  }
}