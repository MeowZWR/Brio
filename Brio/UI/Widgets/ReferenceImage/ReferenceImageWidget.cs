using Brio.Capabilities.ReferenceImage;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.ReferenceImage;

public class ReferenceImageWidget(ReferenceImageCapability capability) : Widget<ReferenceImageCapability>(capability)
{
    public override string HeaderName => "参考图";

    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        var entity = Capability.ReferenceImageEntity;

        if(ImGui.MenuItem($"重命名 {entity.FriendlyName}###image_rename"))
        {
            ImGui.CloseCurrentPopup();

            ModalManager.Instance.OpenRenameModal(entity);
        }

        var toggele = entity.IsWindowOpen ? "Hide" : "Show";
        if(ImGui.MenuItem($"{toggele} {entity.FriendlyName}###image_toggle"))
            entity.SetVisibility(!entity.IsWindowOpen);

        var lockLabel = entity.IsLocked ? "解锁" : "锁定";
        if(ImGui.MenuItem($"{lockLabel}###image_lock"))
            entity.IsLocked = !entity.IsLocked;

        ImGui.Separator();

        if(ImGui.BeginMenu("销毁##image_destroy"))
        {
            if(ImGui.MenuItem("确认销毁###image_destroy_confirm"))
                Capability.Destroy();

            ImGui.EndMenu();
        }
    }
}
