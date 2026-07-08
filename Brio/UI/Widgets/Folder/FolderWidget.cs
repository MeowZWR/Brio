using Brio.Capabilities.Folder;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;

namespace Brio.UI.Widgets.Folder;

public class FolderWidget(FolderCapability capability) : Widget<FolderCapability>(capability)
{
    public override string HeaderName => "文件夹";
    public override WidgetFlags Flags => WidgetFlags.DrawPopup;

    public override void DrawPopup()
    {
        if(ImGui.MenuItem($"重命名 {Capability.FolderEntity.FriendlyName}###folder_rename"))
        {
            ImGui.CloseCurrentPopup();
            ModalManager.Instance.OpenRenameModal(Capability.FolderEntity);
        }

        string visLabel = Capability.FolderEntity.AreChildrenHidden
            ? "显示所有子项###folder_visibility"
            : "隐藏所有子项###folder_visibility";

        if(ImGui.MenuItem(visLabel))
            Capability.ToggleChildrenVisibility();

        ImGui.Separator();

        if(ImGui.BeginMenu("删除文件夹###folder_delete"))
        {
            if(ImGui.BeginMenu("将子项归还父级###folder_delete_return"))
            {
                if(ImGui.MenuItem("确认###folder_delete_return_confirm"))
                    Capability.DeleteFolderReturnChildren();
                ImGui.EndMenu();
            }

            if(ImGui.BeginMenu("删除所有子项###folder_delete_children"))
            {
                if(ImGui.MenuItem("确认###folder_delete_children_confirm"))
                    Capability.DeleteFolderDestroyChildren();
                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }
    }
}
