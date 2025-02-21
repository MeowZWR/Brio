using Brio.Capabilities.Actor;
using Brio.Capabilities.Posing;
using Brio.Config;
using Brio.Core;
using Brio.Entities;
using Brio.Files;
using Brio.Game.Actor.Appearance;
using Brio.Game.Actor.Interop;
using Brio.Game.Core;
using Brio.Game.Posing;
using Brio.Game.Scene;
using Brio.Game.Types;
using Brio.Library;
using Brio.Library.Filters;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Editors;
using Brio.UI.Windows;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using MessagePack;
using OneOf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Brio.UI.Controls.Stateless;

public class FileUIHelpers
{
    public static void DrawProjectPopup(SceneService sceneService, EntityManager entityManager, ProjectWindow projectWindow, AutoSaveService autoSaveService)
    {
        using var popup = ImRaii.Popup("DrawProjectPopup");
        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {
                if(ImGui.Button("保存/加载项目"))
                {
                    projectWindow.IsOpen = true;
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("保存或加载此场景");

                if(ImGui.Button("查看自动保存"))
                {
                    autoSaveService.ShowAutoSaves();
                }
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip("View Scene Auto-Saves");

                ImGui.Separator();

                using(ImRaii.Disabled(true))
                {
                    if(ImGui.Button("导出场景"))
                    {
                        ShowExportSceneModal(entityManager, sceneService);
                    }
                    if(ImGui.Button("导入场景"))
                    {
                        ShowImportSceneModal(sceneService);
                    }
                }
            }
        }
    }

    static bool freezeOnLoad = false;
    static bool smartDefaults = false;

    static bool doExpression = false;
    static bool doBody = false;
    static bool doTransform = false;
    static TransformComponents? transformComponents = null;
    public static void DrawImportPoseMenuPopup(PosingCapability capability, bool showImportOptions = true)
    {
        using var popup = ImRaii.Popup("DrawImportPoseMenuPopup");

        if(popup.Success)
        {
            using(ImRaii.PushColor(ImGuiCol.Button, UIConstants.Transparent))
            {

                var size = ImGui.GetContentRegionAvail(); //ImGui.CalcTextSize("XXXX Freeze Actor on Import");
                size.Y = 44;

                var buttonSize = size / 8;

                var with = buttonSize * 4;

                ImGui.Checkbox("导入时冻结角色", ref freezeOnLoad);

                ImGui.Separator();

                using(ImRaii.Disabled(true))
                    ImGui.Checkbox("智能导入", ref smartDefaults);

                transformComponents ??= capability.PosingService.DefaultImporterOptions.TransformComponents;

                using(ImRaii.Disabled(smartDefaults))
                {
                    using(ImRaii.Disabled(doExpression))
                    {
                        if(ImBrio.ToggelFontIconButton("ImportPosition", FontAwesomeIcon.ArrowsUpDownLeftRight, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Position), hoverText: "导入位置"))
                        {
                            if(transformComponents.Value.HasFlag(TransformComponents.Position))
                                transformComponents &= ~TransformComponents.Position;
                            else
                                transformComponents |= TransformComponents.Position;
                        }
                        ImGui.SameLine();
                        if(ImBrio.ToggelFontIconButton("ImportRotation", FontAwesomeIcon.ArrowsSpin, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Rotation), hoverText: "导入旋转"))
                        {
                            if(transformComponents.Value.HasFlag(TransformComponents.Rotation))
                                transformComponents &= ~TransformComponents.Rotation;
                            else
                                transformComponents |= TransformComponents.Rotation;
                        }
                        ImGui.SameLine();
                        if(ImBrio.ToggelFontIconButton("ImportScale", FontAwesomeIcon.ExpandAlt, buttonSize, transformComponents.Value.HasFlag(TransformComponents.Scale), hoverText: "导入缩放"))
                        {
                            if(transformComponents.Value.HasFlag(TransformComponents.Scale))
                                transformComponents &= ~TransformComponents.Scale;
                            else
                                transformComponents |= TransformComponents.Scale;
                        }
                    }

                    ImGui.SameLine();
                    if(ImBrio.ToggelFontIconButton("ImportTransform", FontAwesomeIcon.ArrowsToCircle, buttonSize, doTransform, hoverText: "导入模型变换"))
                    {
                        doTransform = !doTransform;
                    }

                    if(smartDefaults == true)
                    {
                        transformComponents = null;
                    }
                }

                ImGui.Separator();

                if(ImBrio.ToggelButton("导入身体", new(size.X, 35), doBody))
                {
                    doBody = !doBody;
                }

                if(ImBrio.ToggelButton("导入表情", new(size.X, 35), doExpression))
                {
                    doExpression = !doExpression;
                }

                using(ImRaii.Disabled(doExpression || doBody))
                {
                    if(ImBrio.Button("导入选项", FontAwesomeIcon.Cog, new(size.X, 25), centerTest: true, hoverText: "导入选项"))
                        ImGui.OpenPopup("import_optionsImportPoseMenuPopup");
                }

                ImGui.Separator();

                if(ImGui.Button("导入", new(size.X, 25)))
                {
                    //bool? modelTransformOverride = null;
                    //if(doTransform)
                    //{
                    //    modelTransformOverride = doTransform;
                    //}
                    ShowImportPoseModal(capability, freezeOnLoad: freezeOnLoad, transformComponents: transformComponents, applyModelTransformOverride: doTransform);
                }

                ImGui.Separator();

                if(ImGui.Button("导入 A-Pose", new(size.X, 25)))
                {
                    capability.LoadResourcesPose("Data.BrioAPose.pose", freezeOnLoad: freezeOnLoad, asBody: true);
                    ImGui.CloseCurrentPopup();
                }

                if(ImGui.Button("导入 T-Pose", new(size.X, 25)))
                {
                    capability.LoadResourcesPose("Data.BrioTPose.pose", freezeOnLoad: freezeOnLoad, asBody: true);
                    ImGui.CloseCurrentPopup();
                }
            }

            using(var popup2 = ImRaii.Popup("import_optionsImportPoseMenuPopup"))
            {
                if(popup2.Success && showImportOptions && Brio.TryGetService<PosingService>(out var service))
                {
                    PosingEditorCommon.DrawImportOptionEditor(service.DefaultImporterOptions, true);
                }
            }
        }
    }

    public static void ShowImportPoseModal(PosingCapability capability, PoseImporterOptions? options = null, bool asExpression = false,
        bool asBody = false, bool freezeOnLoad = false, TransformComponents? transformComponents = null, bool? applyModelTransformOverride = false)
    {
        TypeFilter filter = new("姿势", typeof(CMToolPoseFile), typeof(PoseFile));


        if(ConfigurationService.Instance.Configuration.UseLibraryWhenImporting)
        {
            LibraryManager.Get(filter, (r) =>
            {
                if(r is CMToolPoseFile cmPose)
                {
                    ImportPose(capability, cmPose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
                else if(r is PoseFile pose)
                {
                    ImportPose(capability, pose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
            });
        }
        else
        {
            LibraryManager.GetWithFilePicker(filter, (r) =>
            {
                if(r is CMToolPoseFile cmPose)
                {
                    ImportPose(capability, cmPose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
                else if(r is PoseFile pose)
                {
                    ImportPose(capability, pose, options: options, transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
                }
            });
        }
    }

    private static void ImportPose(PosingCapability capability, OneOf<PoseFile, CMToolPoseFile> rawPoseFile, PoseImporterOptions? options = null,
        TransformComponents? transformComponents = null, bool? applyModelTransformOverride = false)
    {
        if(doBody && doExpression)
        {
            capability.ImportPose(rawPoseFile, options: capability.PosingService.DefaultIPCImporterOptions, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: applyModelTransformOverride);
            return;
        }

        if(doBody)
        {
            capability.ImportPose(rawPoseFile, options: null, asExpression: false, asBody: true, freezeOnLoad: freezeOnLoad,
                transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
        }
        else if(doExpression)
        {
            capability.ImportPose(rawPoseFile, options: null, asExpression: true, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: null, applyModelTransformOverride: null);
        }
        else
        {
            capability.ImportPose(rawPoseFile, options: options, asExpression: false, asBody: false, freezeOnLoad: freezeOnLoad,
                transformComponents: transformComponents, applyModelTransformOverride: applyModelTransformOverride);
        }
    }

    public static void ShowExportPoseModal(PosingCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("导出姿势###export_pose", "Pose File (*.pose){.pose}", "brio", ".pose",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".pose"))
                            path += ".pose";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        capability.ExportSavePose(path);
                    }
                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void ShowImportCharacterModal(ActorAppearanceCapability capability, AppearanceImportOptions options)
    {
        List<Type> types = [typeof(ActorAppearanceUnion), typeof(AnamnesisCharaFile)];

        if(capability.CanMcdf)
            types.Add(typeof(MareCharacterDataFile));

        TypeFilter filter = new TypeFilter("角色", [.. types]);

        if(ConfigurationService.Instance.Configuration.UseLibraryWhenImporting)
        {
            LibraryManager.Get(filter, (r) =>
            {
                if(r is ActorAppearanceUnion appearance)
                {
                    _ = capability.SetAppearance(appearance, options);
                }
                else if(r is AnamnesisCharaFile appearanceFile)
                {
                    if (options.HasFlag(AppearanceImportOptions.Shaders))
                    {
                        BrioHuman.ShaderParams shaderParams = appearanceFile;
                        BrioUtilities.ImportShadersFromFile(ref capability._modelShaderOverride, shaderParams);
                    }
                    _ = capability.SetAppearance(appearanceFile, options);
                }
                else if(r is MareCharacterDataFile mareFile)
                {
                    capability.LoadMcdf(mareFile.GetPath());
                }
            });

        }
        else
        {
            LibraryManager.GetWithFilePicker(filter, (r) =>
            {
                if(r is ActorAppearanceUnion appearance)
                {
                    _ = capability.SetAppearance(appearance, options);
                }
                else if(r is AnamnesisCharaFile appearanceFile)
                {
                    if(options.HasFlag(AppearanceImportOptions.Shaders))
                    {
                        BrioHuman.ShaderParams shaderParams = appearanceFile;
                        BrioUtilities.ImportShadersFromFile(ref capability._modelShaderOverride, shaderParams);
                    }
                    _ = capability.SetAppearance(appearanceFile, options);
                }
                else if(r is MareCharacterDataFile mareFile)
                {
                    capability.LoadMcdf(mareFile.GetPath());
                }
            });
        }
    }

    public static void ShowExportCharacterModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("导出角色文件###export_character_window", "Character File (*.chara){.chara}", "brio", "{.chara}",
                (success, path) =>
                {
                    if(success)
                    {
                        if(!path.EndsWith(".chara"))
                            path += ".chara";

                        var directory = Path.GetDirectoryName(path);
                        if(directory is not null)
                        {
                            ConfigurationService.Instance.Configuration.LastExportPath = directory;
                            ConfigurationService.Instance.Save();
                        }

                        capability.ExportAppearance(path);
                    }

                }, ConfigurationService.Instance.Configuration.LastExportPath, true);
    }

    public static void ShowImportMCDFModal(ActorAppearanceCapability capability)
    {
        UIManager.Instance.FileDialogManager.OpenFileDialog("导入MCDF文件###import_character_window", "月海角色数据文件(*.mcdf){.mcdf}",
                 (success, paths) =>
                 {
                     if(success && paths.Count == 1)
                     {
                         var path = paths[0];
                         var directory = Path.GetDirectoryName(path);
                         if(directory is not null)
                         {
                             ConfigurationService.Instance.Configuration.LastMCDFPath = directory;
                             ConfigurationService.Instance.Save();
                         }
                         capability.LoadMcdf(path);
                     }
                 }, 1, ConfigurationService.Instance.Configuration.LastMCDFPath, true);
    }

    public static void ShowExportSceneModal(EntityManager entityManager, SceneService sceneService)
    {
        UIManager.Instance.FileDialogManager.SaveFileDialog("导出场景文件###export_scene_window", "Brio Scene File (*.brioscn){.brioscn}", "brioscn", "{.brioscn}",
            (success, path) =>
            {
                if(success)
                {
                    Brio.Log.Info("正在导出场景...");
                    if(!path.EndsWith(".brioscn"))
                        path += ".brioscn";

                    var directory = Path.GetDirectoryName(path);
                    if(directory is not null)
                    {
                        ConfigurationService.Instance.Configuration.LastScenePath = directory;
                        ConfigurationService.Instance.Save();
                    }

                    SceneFile sceneFile = sceneService.GenerateSceneFile();
                    //ResourceProvider.Instance.SaveFileDocument(path, sceneFile);

                    byte[] bytes = MessagePackSerializer.Serialize(sceneFile);
                    File.WriteAllBytes(path, bytes);

                    //TODO REMOVE
                    path += ".json";
                    var json = MessagePackSerializer.ConvertToJson(bytes);
                    File.WriteAllText(path, json);

                    Brio.Log.Info("场景导出完成");
                }
            }, ConfigurationService.Instance.Configuration.LastScenePath, true);
    }

    public static void ShowImportSceneModal(SceneService sceneService)
    {
        List<Type> types = [typeof(SceneFile)];
        TypeFilter filter = new("场景", [.. types]);

        LibraryManager.GetWithFilePicker(filter, r =>
        {
            Brio.Log.Verbose("正在导入场景...");
            if(r is SceneFile importedFile)
            {
                sceneService.LoadScene(importedFile);
                Brio.Log.Verbose("场景导入完成！");
            }
            else
            {
                throw new IOException("所选文件不是有效的场景文件");
            }
        }, true);
    }
}


