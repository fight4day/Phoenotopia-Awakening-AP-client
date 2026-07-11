using UnityEngine;
using UnityEngine.UI;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace PhoA_AP_client.UI.Panels;

internal class SaveWarpPanel(UIBase owner) : PanelBase(owner)
{
    public override string Name => "Save warp menu";
    
    public override int MinWidth => 250;
    public override int MinHeight => 50;
    public override Vector2 DefaultAnchorMin => new(0.5f, 0.5f);
    public override Vector2 DefaultAnchorMax => new(0.5f, 0.5f);
    public override Vector2 DefaultPosition => new(0f, 0f);

    public override bool CanDragAndResize => false;
    
    public override void SetDefaultSizeAndPosition()
    {
        base.SetDefaultSizeAndPosition();
        Rect.pivot = new Vector2(0.5f, 0.5f);
    }
    
    protected override void ConstructPanelContent()
    {
        Text title = UIFactory.CreateLabel(ContentRoot, "Title", "Save warp menu", alignment: TextAnchor.MiddleCenter, fontSize: 20);
        UIFactory.SetLayoutElement(title.gameObject, minHeight: 25, preferredHeight: 25, flexibleWidth: 9999);

        for (int i = 1; i <= 10; i++)
        {
            GameObject buttonGroup = UIFactory.CreateHorizontalGroup(ContentRoot, $"ButtonGroup{i}", true, true, true, true, 2,
                new Vector4(0, 0, 0, 0));
            UIFactory.SetLayoutElement(buttonGroup, minHeight: 35, flexibleHeight: 0);
            Color? buttonColor = i > 7 ? new Color(0.1f, 0.1f, 0.1f) : null;
            ButtonRef button1 = UIFactory.CreateButton(buttonGroup, $"option{i}", $"Option {i}", buttonColor);
            UIFactory.SetLayoutElement(button1.GameObject, minHeight: 35, preferredHeight: 35);
        }
    }

    protected override void LateConstructUI()
    {
        base.LateConstructUI();
        ResizePanelHeightToContent();
    }

    private void ResizePanelHeightToContent()
    {
        float totalHeight = 2f;
        foreach (RectTransform child in ContentRoot.transform)
            totalHeight += child.rect.height + 2;
        PhoaAPClient.Logger.LogDebug(totalHeight);
        Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
        Dragger.OnEndResize();
    }
}