using PhoA_AP_client.UI.Panels;
using UniverseLib.UI;

namespace PhoA_AP_client.UI;

internal class UIManager
{
    private UIBase _uiBase;
    public SaveWarpPanel saveWarpPanel;

    public UIManager()
    {
        _uiBase = UniversalUI.RegisterUI($"{MyPluginInfo.PLUGIN_GUID}.saveWarpMenu", null);
        saveWarpPanel = new SaveWarpPanel(_uiBase);
        saveWarpPanel.SetActive(false);
    }
}