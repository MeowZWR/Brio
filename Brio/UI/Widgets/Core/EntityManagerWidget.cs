using Brio.Capabilities.Core;
using Brio.Entities.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Linq;

namespace Brio.UI.Widgets.Core;

public class EntityManagerWidget(EntitManagerCapability capability) : Widget<EntitManagerCapability>(capability)
{
    public override string HeaderName => "多选";

    public override WidgetFlags Flags => Capability.Entity.EntityManager.SelectedEntities.Count > 1 ?
        WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup | WidgetFlags.DrawBody | WidgetFlags.DefaultOpen :
        WidgetFlags.DrawQuickIcons | WidgetFlags.DrawPopup;

    public override void DrawBody()
    {
        if(Capability.Entity.EntityManager.SelectedEntities.Count > 1)
        {
            ImBrio.VerticalPadding(3);

            ImGui.AlignTextToFramePadding();
            using(ImRaii.PushColor(ImGuiCol.Text, ThemeManager.CurrentTheme.Accent.AccentColor))
                ImGui.Text($"{Capability.Entity.EntityManager.SelectedEntities.Count} Selected");

            ImBrio.VerticalPadding(7);

            ImBrio.SeparatorText("变换");
            Capability.DrawMultiTransform();

            ImBrio.VerticalPadding(10);
        }
    }

    public override void DrawPopup()
    {
        if(ImGui.BeginMenu("全部销毁...###containerwidgetpopup_destroy"))
        {
            using(ImRaii.Disabled(Capability.HasFolders == false))
            {
                if(ImGui.BeginMenu("文件夹###entitymanager_destroyall_folders"))
                {
                    if(ImGui.BeginMenu("将子项归还根目录###entitymanager_destroyall_folders_return"))
                    {
                        if(ImGui.MenuItem("确认###entitymanager_destroyall_folders_return_confirm"))
                            Capability.ReturnAllFolderChildren();

                        ImGui.EndMenu();
                    }

                    if(ImGui.BeginMenu("销毁所有子项###entitymanager_destroyall_folders_destroy"))
                    {
                        if(ImGui.MenuItem("确认###entitymanager_destroyall_folders_destroy_confirm"))
                            Capability.DestroyAllFolderChildren();

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenu();
                }
            }

            using(ImRaii.Disabled(Capability.HasWorldObjects == false))
            {
                if(ImGui.BeginMenu("世界物体###entitymanager_destroyall_worldobjects"))
                {
                    if(ImGui.MenuItem("确认销毁###entitymanager_destroyall_worldobjects_confirm"))
                        Capability.DestroyAllWorldObjects();

                    ImGui.EndMenu();
                }
            }

            ImGui.EndMenu();
        }
    }

    public override void DrawQuickIcons()
    {
        using(ImRaii.Disabled(Capability.CanControlCharacters is false))
        {
            bool hasSelection = Capability.Entity.EntityManager.SelectedEntity != null;

            if(ImBrio.FontIconButton("Manager_clone", FontAwesomeIcon.Clone, "克隆所选", hasSelection))
            {
                Capability.CloneSelected();
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("Manager_selectinhierarchy", FontAwesomeIcon.CheckSquare, "全选"))
            {
                Capability.SelectAllInHierarchy();
            }

            ImBrio.VerticalSeparator(24, 1);

            if(ImBrio.HoldButton("manager_destroyall", "", FontAwesomeIcon.Bomb, 1f, new(40, 0), centerTest: true, tooltip: "[长按销毁全部]", onlyIcon: true))
            {
                Capability.DestroyAllSelected();
            }

            ImBrio.VerticalSeparator(24, 1);

            if(ImBrio.FontIconButton("Manager_move", FontAwesomeIcon.FolderTree, "移动到文件夹...", hasSelection))
            {
                ImGui.OpenPopup("manager_move_to_folder_popup");
            }

            using(var popup = ImRaii.Popup("manager_move_to_folder_popup"))
            {
                if(popup.Success)
                {
                    foreach(var folder in Capability.Entity.Children.OfType<FolderEntity>().Where(f => f.IsEditable))
                    {
                        if(ImGui.MenuItem($"{folder.FriendlyName}###manager_move_to_folder_{folder.Id}"))
                            Capability.MoveSelectedToFolder(folder);
                    }

                    ImGui.Separator();

                    if(ImGui.MenuItem("新建文件夹...###manager_move_to_new_folder"))
                        Capability.MoveSelectedToNewFolder();
                }
            }

            ImGui.SameLine();

            if(ImBrio.FontIconButton("Manager_folderoptions", FontAwesomeIcon.EllipsisV, "文件夹选项", Capability.HasFolders))
            {
                ImGui.OpenPopup("manager_folder_options_popup");
            }

            using(var popup = ImRaii.Popup("manager_folder_options_popup"))
            {
                if(popup.Success)
                {
                    if(ImGui.MenuItem("将所有子项归还实体管理器###manager_folderoptions_return"))
                        Capability.ReturnAllFolderChildren();

                    if(ImGui.MenuItem("销毁所有文件夹及子项###manager_folderoptions_destroy"))
                        Capability.DestroyAllFolderChildren();
                }
            }
        }
    }
}
