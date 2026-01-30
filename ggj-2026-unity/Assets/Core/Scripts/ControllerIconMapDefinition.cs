using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "new-controller-icon-mapping", menuName = "Controller Icon Mapping")]
public class ControllerIconMapDefinition : ScriptableObject
{
  [SerializeField] private InputIconMapDefinition gamepadMap = null;
  [SerializeField] private InputIconMapDefinition keyboardMap = null;
  [SerializeField] private InputIconMapDefinition mouseMap = null;
  [SerializeField] private ControllerMapPair[] controllerMaps = null;

  [System.Serializable]
  private class ControllerMapPair
  {
    public string Name = string.Empty;
    public string ControllerGuid = string.Empty;
    public InputIconMapDefinition IconMap = null;
  }

  private Dictionary<string, InputIconMapDefinition> _controllerIdMap;

  public static void GetElementMapsForAction(int actionId, Rewired.Player player, List<Rewired.ActionElementMap> elementMaps)
  {
    Rewired.Controller lastController = player.controllers.GetLastActiveController();
    foreach (var elementMap in player.controllers.maps.ElementMapsWithAction(lastController, actionId, skipDisabledMaps: false))
    {
      elementMaps.Add(elementMap);
    }
  }

  public InputIconMapDefinition GetMapForHardware(Rewired.Controller controller)
  {
    EnsureControllerMaps();

    string hardwareTypeGuid = controller.hardwareTypeGuid.ToString();
    if (_controllerIdMap.TryGetValue(hardwareTypeGuid, out InputIconMapDefinition hardwareMap))
    {
      return hardwareMap;
    }

    if (controller.type == Rewired.ControllerType.Joystick)
      return gamepadMap;
    else if (controller == Rewired.ReInput.controllers.Keyboard)
      return keyboardMap;
    else if (controller == Rewired.ReInput.controllers.Mouse)
      return mouseMap;

    return null;
  }

  // Find icons for an input action, based on whatever the current detected controller type is
  public bool GetIconsForAction(int actionId, Rewired.Player player, List<InputIcon> inputIcons)
  {
    // Find out the last controller used
    EnsureControllerMaps();
    Rewired.Controller lastController = player.controllers.GetLastActiveController();
    if (lastController == null)
      lastController = GetPlatformDefaultController();

    // Store previous icon count so we can determine whether we successfully found more
    int prevCount = inputIcons.Count;

    // Helper method to get input icons based on last used controller
    System.Action getIconsFallback = () =>
    {
      var iconMap = GetMapForHardware(lastController);
      if (iconMap == mouseMap || iconMap == keyboardMap)
      {
        FillInputIcons(inputIcons, mouseMap, player, Rewired.ReInput.controllers.Mouse, actionId);
        FillInputIcons(inputIcons, keyboardMap, player, Rewired.ReInput.controllers.Keyboard, actionId);
      }
      else
      {
        FillInputIcons(inputIcons, iconMap, player, lastController, actionId);
      }
    };

    // Check to see if any of our controller maps should be forced into use on this platform
    ControllerMapPair platformMap = GetControllerMapPlatformOverride();

    // Check if there is a defined default icon set for this platfrom when no controller is detected
    bool noController = player.controllers.GetLastActiveController() == null && Rewired.ReInput.controllers.Joysticks.Count == 0;
    if (platformMap == null && noController)
    {
      platformMap = GetControllerMapPlatformDefault();
    }

    if (platformMap != null)
    {
      // Find the controller object based on the hardware type guid this mapping is for
      // Presumably if the platform is forced this device is connnected
      Rewired.Controller controller = null;
      foreach (var c in player.controllers.Joysticks)
      {
        if (c.hardwareTypeGuid.ToString() == platformMap.ControllerGuid)
        {
          controller = c;
        }
      }

      // Assuming we found the right controller, get the icons for it
      // Otherwise, we'll fallback to getting the icon via just the controller definition
      if (controller != null)
        FillInputIcons(inputIcons, platformMap.IconMap, player, controller, actionId);
      else
        FillInputIcons(inputIcons, platformMap, actionId);
    }
    // No maps are forced for this platform, so just do our usual fallback method
    else
    {
      getIconsFallback();
    }

    return inputIcons.Count > prevCount;
  }

  // Find an icon for a specific input mapping, which is already associated with a controller
  public InputIcon GetIconForInput(Rewired.ActionElementMap elementMap)
  {
    // This is an unassigned action, and therefore can't have any icon 
    if (elementMap.controllerMap == null)
      return null;

    // Get info about the input action
    EnsureControllerMaps();
    Rewired.Controller controller = elementMap.controllerMap.controller;

    // Get the icon map for this hardware
    InputIconMapDefinition iconMap = GetMapForHardware(controller);

    if (iconMap != null)
    {
      return iconMap.GetInputIcon(elementMap, controller);
    }

    return null;
  }

  private Rewired.Controller GetPlatformDefaultController()
  {
    // If there's any connected gamepad, use that
    if (Rewired.ReInput.controllers.Joysticks.Count > 0)
    {
      return Rewired.ReInput.controllers.Joysticks[0];
    }

    // Default to the keyboard (this does not affect console)
    return Rewired.ReInput.controllers.Keyboard;
  }

  // Find any default controller map defined for this specific platform
  private ControllerMapPair GetControllerMapPlatformDefault()
  {
    // var currentPlatform = QAG.ConsoleHelper.PlatformSpecificProperties.Platform;
    // foreach (var platformMap in controllerMaps)
    // {
    //   if ((platformMap.DefaultOnPlatforms & currentPlatform) == currentPlatform)
    //   {
    //     return platformMap;
    //   }
    // }

    return null;
  }

  // Find any controller map defined for this specific platform
  private ControllerMapPair GetControllerMapPlatformOverride()
  {
    // var currentPlatform = QAG.ConsoleHelper.PlatformSpecificProperties.Platform;
    // foreach (var platformMap in controllerMaps)
    // {
    //   if ((platformMap.ForceOnPlatforms & currentPlatform) == currentPlatform)
    //   {
    //     return platformMap;
    //   }
    // }

    return null;
  }

  private void EnsureControllerMaps()
  {
    if (_controllerIdMap == null)
    {
      _controllerIdMap = new Dictionary<string, InputIconMapDefinition>();
      for (int i = 0; i < controllerMaps.Length; ++i)
      {
        ControllerMapPair pair = controllerMaps[i];
        _controllerIdMap[pair.ControllerGuid.ToString()] = pair.IconMap;
      }
    }
  }

  // Fill input icons using the player and a controller
  private void FillInputIcons(List<InputIcon> inputIcons, InputIconMapDefinition iconMap, Rewired.Player player, Rewired.Controller controller, int actionId)
  {
    Rewired.InputAction action = Rewired.ReInput.mapping.GetAction(actionId);
    foreach (var elementMap in player.controllers.maps.ElementMapsWithAction(controller, actionId, skipDisabledMaps: false))
    {
      var inputIcon = iconMap.GetInputIcon(elementMap, controller);
      if (inputIcon != null)
        inputIcons.Add(inputIcon);
      else
        Debug.LogWarning($"Failed to get input icon for action {action.name} with input id {elementMap.elementIdentifierId}");
    }
  }

  // Fill input icons using just a controller map and an action, with no controller object 
  // This uses the default rewired gamepad template which most gamepads implement
  // NOTE: Using this will bypass any player-specific mappings!
  private void FillInputIcons(List<InputIcon> inputIcons, ControllerMapPair controllerIconMap, int actionId)
  {
    // Apply layout rule sets based on controller type, kind of a hack?
    // This allows Switch to properly show the correct mapped buttons while no controller is connected
    int layoutId = 0;
    var ruleSets = Rewired.ReInput.players.GetPlayer(0).controllers.maps.layoutManager.ruleSets;
    foreach (var ruleSet in ruleSets)
    {
      foreach (var rule in ruleSet)
      {
        // QAG.ConsoleDebug.Log($"Comparing rule with hardware id {rule.controllerSetSelector.hardwareTypeGuid.ToString()} to {controllerMap.ControllerGuid}");
        if (rule.controllerSetSelector.hardwareTypeGuid.ToString() == controllerIconMap.ControllerGuid)
        {
          layoutId = rule.layoutId;
          break;
        }
      }
    }

    // Now get the mapping for this controller template and layout
    Rewired.InputAction action = Rewired.ReInput.mapping.GetAction(actionId);
    Rewired.ControllerIdentifier controllerIdentifier = new Rewired.ControllerIdentifier();
    controllerIdentifier.hardwareTypeGuid = new System.Guid(controllerIconMap.ControllerGuid);
    controllerIdentifier.controllerType = Rewired.ControllerType.Joystick;

    foreach (var mapCategory in Rewired.ReInput.mapping.MapCategories)
    {
      if (controllerIconMap.IconMap.UseTemplateIds)
      {
        var mapInstance = Rewired.ReInput.mapping.GetControllerTemplateMapInstance(Rewired.GamepadTemplate.typeGuid, mapCategory.id, layoutId);
        if (mapInstance != null)
        {
          foreach (var actionElement in mapInstance.ElementMaps)
          {
            if (actionElement.actionId == actionId)
            {
              var inputIcon = controllerIconMap.IconMap.GetInputIcon(actionElement.elementIdentifierId, actionElement.actionId);
              if (inputIcon != null)
              {
                inputIcons.Add(inputIcon);
              }
            }
          }
        }
      }
      else
      {
        var controllerMap = Rewired.ReInput.mapping.GetControllerMapInstance(controllerIdentifier, mapCategory.id, layoutId);
        if (controllerMap != null)
        {
          foreach (var actionElement in controllerMap.AllMaps)
          {
            if (actionElement.actionId == actionId)
            {
              var inputIcon = controllerIconMap.IconMap.GetInputIcon(actionElement.elementIdentifierId, actionElement.actionId);
              if (inputIcon != null)
              {
                inputIcons.Add(inputIcon);
              }
            }
          }
        }
      }
    }
  }
}