using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Library.Sources;
using Brio.Library.Tags;
using Brio.Resources;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Brio.UI.Modals;

public class MetadataModal : Modal
{
    private PosingCapability? _capability;
    private string _path = string.Empty;

    private FileEntry? _fileEntry;

    private string _author = string.Empty;
    private string _version = string.Empty;
    private string _description = string.Empty;
    private string _tags = string.Empty;

    private IDalamudTextureWrap? _previewImage;
    private string? _base64Image;
    private int? _previewImageFileSize;

    private bool _pickingImage;

    public MetadataModal() : base("导出姿势###brio_export_pose_metadata_modal", new(450, 600), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize)
    {
    }

    public void Open(PosingCapability capability, string path)
    {
        _capability = capability;
        _path = path;

        base.Open();
    }

    public void Open(FileEntry fileEntry)
    {
        _fileEntry = fileEntry;

        _author = fileEntry.Author ?? string.Empty;
        _version = fileEntry.Version ?? string.Empty;
        _description = fileEntry.Description ?? string.Empty;
        _tags = fileEntry.Tags != null
            ? string.Join(", ", fileEntry.Tags.Where(x => x.Name != fileEntry.Author).Select(x => x.Name))
            : string.Empty;

        var (base64, img) = fileEntry.LoadPreviewForEdit();
        _previewImage = img;
        _base64Image = base64;
        _previewImageFileSize = base64 != null ? System.Text.Encoding.UTF8.GetByteCount(base64) : null;

        base.Open();
    }

    public override void OnClose()
    {
        if(_pickingImage)
            return;

        _capability = null;
        _fileEntry = null;
        _path = string.Empty;
        _author = string.Empty;
        _version = string.Empty;
        _description = string.Empty;
        _tags = string.Empty;

        _previewImage?.Dispose();
        _previewImage = null;
        _base64Image = null;
        _previewImageFileSize = null;
    }

    public override void DrawContent()
    {
        bool editing = _fileEntry != null;

        if(editing)
            ImBrio.SeparatorText($"编辑元数据 [{_fileEntry!.Name}]");
        else
            ImBrio.SeparatorText($"保存姿势并附带元数据 [{_capability?.Actor.FriendlyName} -> {Path.GetFileNameWithoutExtension(_path)}.pose]");

        float labelColumnWidth = ImGui.CalcTextSize("描述：").X + ImGui.GetStyle().ItemSpacing.X;

        // I hate this. I hate imgui, I hate imgui, I hate imgui - darkarchon
        using(ImRaii.Table("##export_pose_fields", 2, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, labelColumnWidth);
            ImGui.TableSetupColumn("##input", ImGuiTableColumnFlags.WidthStretch);

            Row("作者：", () => ImGui.InputText("###export_pose_author", ref _author, 100));
            Row("版本：", () => ImGui.InputText("###export_pose_version", ref _version, 32));
            Row("标签：", () =>
            {
                ImGui.InputText("###export_pose_tags", ref _tags, 250);
                ImBrio.AttachToolTip("以逗号分隔的标签列表");
            });
            Row("描述：", () => ImGui.InputTextMultiline("###xport_pose_description", ref _description, 1024, new Vector2(-1, 5 * ImGui.GetTextLineHeight())));

            static void Row(string label, Action input)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                input();
            }
        }

        ImBrio.SeparatorText("预览图");

        if(ImGui.Button(_previewImage == null ? "添加##export_pose_preview" : "替换##export_pose_preview"))
        {
            _pickingImage = true;
            Close();

            FileUIHelpers.ShowImportPreviewImageModal(path =>
            {
                var (base64, img) = ResourceProvider.Instance.GetNewPreviewImage(path);
                _previewImage?.Dispose();
                _previewImage = img;
                _base64Image = base64;
                _previewImageFileSize = System.Text.Encoding.UTF8.GetByteCount(base64);
            },
            () =>
            {
                _pickingImage = false;
                Open();
            });
        }

        if(_previewImage != null)
        {
            ImGui.SameLine();
            if(ImGui.Button("移除##export_pose_remove_preview"))
            {
                _previewImage?.Dispose();
                _previewImage = null;
                _base64Image = null;
                _previewImageFileSize = null;
            }
        }

        if(_previewImage != null && _previewImageFileSize != null)
        {
            ImGui.SameLine();
            ImBrio.HorizontalPadding(10);

            long fileSize = _previewImageFileSize.Value;
            string sizeLabel = fileSize >= (1 << 20)
                ? $"{fileSize / (float)(1 << 20):f2} MB"
                : $"{fileSize >> 10} KB";
            ImGui.Text(sizeLabel);

            float width = _previewImage.Width;
            float height = _previewImage.Height;
            float aspectRatio = width / height;
            float scaledHeight = Math.Min(500 / aspectRatio, 500);
            ImBrio.ImageFit(_previewImage, new Vector2(500, scaledHeight));
        }

        ImBrio.VerticalPadding(ImGui.GetTextLineHeight() / 2);

        float buttonW = (MinimumSize.X / 2) - 8;

        if(editing)
        {
            if(ImGui.Button("保存", new(buttonW, 0)))
            {
                _fileEntry!.SaveMetadata(_author, _version, _description, _tags, _base64Image);
                Close();
            }
            ImBrio.AttachToolTip("将元数据保存到文件");
        }
        else
        {
            if(ImGui.Button("导出", new(buttonW, 0)))
            {
                if(_capability is not null)
                {
                    var poseFile = _capability.ExportPoseAsFileData();

                    if(_capability.Entity.TryGetCapability<ActorAppearanceCapability>(out var appearanceCapability))
                    {
                        var poseMetaData = appearanceCapability.GetPoseMetaData();
                        poseFile.ModelId = poseMetaData.ModelId;
                        poseFile.RaceSexId = poseMetaData.RaceSexId;
                        poseFile.FaceID = poseMetaData.FaceID;
                    }

                    poseFile.Author = string.IsNullOrEmpty(_author) ? null : _author;
                    poseFile.Version = string.IsNullOrEmpty(_version) ? null : _version;
                    poseFile.Description = string.IsNullOrEmpty(_description) ? null : _description;
                    poseFile.Base64Image = _base64Image;

                    if(!string.IsNullOrEmpty(_tags))
                    {
                        var tags = new TagCollection();
                        foreach(var tag in _tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            tags.Add(tag);

                        poseFile.Tags = tags;
                    }

                    ResourceProvider.Instance.SaveFileDocument(_path, poseFile);
                }

                Close();
            }
            ImBrio.AttachToolTip("将当前姿势导出为文件并附带指定元数据");
        }

        ImGui.SameLine();

        if(ImGui.Button("取消", new(buttonW, 0)))
            Close();
    }
}
