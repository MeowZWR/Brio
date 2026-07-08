using Brio.Game.Core;
using Brio.Services;
using Brio.Services.Models;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

namespace Brio.UI.Modals;

public class ExportSceneModal : Modal
{
    private readonly SceneService _sceneService;

    private string _author = string.Empty;
    private string _description = string.Empty;

    public ExportSceneModal(SceneService sceneService) : base("导出场景###export_scene_modal", new(420, 150), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
        _sceneService = sceneService;
    }

    public override void OnClose()
    {
        _author = string.Empty;
        _description = string.Empty;
    }

    public override void DrawContent()
    {
        ImBrio.SeparatorText($" 导出场景 ");

        ImBrio.SeparatorText($" 作者 ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###export_author", ref _author, 100);

        ImBrio.SeparatorText($" 描述 ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###export_description", ref _description, 250);

        float buttonW = (MinimumSize.X / 2) - 12;

        if(ImBrio.Button("导出", FontAwesomeIcon.FileExport, new(buttonW, 0), centerTest: true, tooltip: "将场景导出为文件"))
        {
            FileUIHelpers.ShowExportSceneModal(_sceneService, string.IsNullOrEmpty(_author) ? null : _author, string.IsNullOrEmpty(_description) ? null : _description);
            Close();
        }

        ImGui.SameLine();

        if(ImBrio.Button("取消", FontAwesomeIcon.Times, new(buttonW, 0), centerTest: true))
            Close();
    }
}

public class SaveProjectModal : Modal
{
    private readonly ProjectSystem _projectSystem;

    private string _name = string.Empty;
    private string _description = string.Empty;

    public SaveProjectModal(ProjectSystem projectSystem) : base("保存新项目###save_project_modal", new(420, 150), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
        _projectSystem = projectSystem;
    }

    public override void OnClose()
    {
        _name = string.Empty;
        _description = string.Empty;
    }

    public override void DrawContent()
    {
        ImBrio.SeparatorText($" 保存新项目 ");

        ImBrio.SeparatorText($" 名称 ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###save_project_name", ref _name, 100);

        ImBrio.SeparatorText($" 描述 ");
        ImGui.SetNextItemWidth(-float.Epsilon);
        ImGui.InputText("###save_project_description", ref _description, 250);

        float buttonW = (MinimumSize.X / 2) - 12;

        using(ImRaii.Disabled(string.IsNullOrEmpty(_name)))
        {
            if(ImBrio.Button("保存", FontAwesomeIcon.Save, new(buttonW, 0), centerTest: true, tooltip: "另存为新项目"))
            {
                _projectSystem.NewProject(_name, string.IsNullOrEmpty(_description) ? null : _description);
                Close();
            }
        }

        ImGui.SameLine();

        if(ImBrio.Button("取消", FontAwesomeIcon.Times, new(buttonW, 0), centerTest: true))
            Close();
    }
}

public class ImportSceneModal : Modal
{
    private readonly SceneService _sceneService;

    private bool _destroyAll = false;
    private bool _useRelativeLightPositions = true;
    private bool _useRelativeWorldObjectPositions = true;

    private SceneImportOptions _importOptions = SceneImportOptions.Default;

    public ImportSceneModal(SceneService sceneService) : base("导入场景###import_scene_modal", new(440, 150), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration)
    {
        _sceneService = sceneService;
    }

    public override void DrawContent()
    {
        ImBrio.SeparatorText($" 导入场景 ");

        ImGui.SameLine();

        FileUIHelpers.DrawImportSettingsPopup(ref _importOptions, ref _destroyAll, ref _useRelativeLightPositions, ref _useRelativeWorldObjectPositions);

        float buttonW = (MinimumSize.X / 2) - 12;

        if(ImBrio.Button("选择文件并加载", FontAwesomeIcon.FileImport, new(buttonW, 0), centerTest: true, tooltip: "选择场景文件并加载"))
        {
            FileUIHelpers.ShowImportSceneModal(_sceneService, _destroyAll, _useRelativeLightPositions, _useRelativeWorldObjectPositions, _importOptions);
            Close();
        }

        ImGui.SameLine();

        if(ImBrio.Button("取消", FontAwesomeIcon.Times, new(buttonW, 0), centerTest: true))
            Close();
    }
}
