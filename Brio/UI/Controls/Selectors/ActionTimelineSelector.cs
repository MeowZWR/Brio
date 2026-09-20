using Brio.Config;
using Brio.Resources;
using Brio.Resources.Sheets;
using Brio.Services;
using Brio.UI.Controls.Core;
using Brio.UI.Controls.Stateless;
using Brio.UI.Theming;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Lumina.Excel.Sheets;
using System;
using System.Numerics;
using static Brio.Game.Actor.ActionTimelineService;
using ActionSheet = Lumina.Excel.Sheets.Action;

namespace Brio.UI.Controls.Selectors;

public class ActionTimelineSelector(string id) : Selector<ActionTimelineSelectorEntry>(id)
{
    protected override Vector2 MinimumListSize { get; } = new(300, 300);

    protected override float EntrySize => _twoColumnLayout
        ? ImGui.GetTextLineHeight() * 2.2f
        : ImGui.GetTextLineHeight() * 3.2f;

    protected virtual Vector2 IconSize => _twoColumnLayout
        ? new(ImGui.GetTextLineHeight() * 2f)
        : new(ImGui.GetTextLineHeight() * 3f);

    protected override SelectorFlags Flags { get; } = SelectorFlags.AllowSearch | SelectorFlags.ShowOptions | SelectorFlags.AdaptiveSizing;

    protected override int Columns => _twoColumnLayout ? 2 : 1;

    protected override float EntryButtonWidth
    {
        get
        {
            if(_showFavorites || _showEmotes || ExpressionsOnly)
                return ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X;

            return 0f;
        }
    }

    protected override float SearchRightReservedWidth
        => 25f * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X;

    private bool _showRaw = true;
    private bool _showEmotes = true;
    private bool _showActions = false;
    private bool _showFavorites = true;
    private bool _twoColumnLayout = true;
    private bool _hoveringFavoriteButton = false;
    private bool _showBlendable = true;

    private bool _filterByDrawsWeapon = false;
    private bool _drawsWeaponValue = false;

    private bool _filterByEmoteCategory = false;
    private int _emoteCategoryValue = 0;

    private bool _showNonBlendInBlendMode = false;

    private bool _isPinned = false;
    private bool _isWindowOpen = false;

    public bool IsPinned => _isPinned;

    //TODO(KEN) at some point make all of them use `field`

    public bool AllowBlending
    {
        get => _showBlendable;
        set
        {
            _showBlendable = value;
            UpdateList();
        }
    }

    public bool ExpressionsOnly
    {
        get => field;
        set
        {
            field = value;
            UpdateList();
        }
    }

    public void TogglePin()
    {
        _isPinned = !_isPinned;
        if(_isPinned)
        {
            _isWindowOpen = true;
        }
    }

    public void DrawAsWindow()
    {
        if(!_isPinned)
            return;

        ImGui.SetNextWindowSize(new Vector2(400, 500), ImGuiCond.FirstUseEver);

        if(ImGui.Begin($"动画搜索选择器 ###{_id}_window2", ref _isWindowOpen, ImGuiWindowFlags.NoCollapse))
        {
            if(!_isWindowOpen)
            {
                _isPinned = false;
                ImGui.End();
                return;
            }

            ImBrio.BlurWindow(ImGuiWindowFlags.None);

            ResetToFavoritesIfAppearing();
            DrawPinButton();

            // Use available window space instead of adaptive sizing
            _useAvailableSpace = true;
            base.Draw();
            _useAvailableSpace = false;
        }
        ImGui.End();
    }

    private void DrawPinButton()
    {
        var pinIcon = _isPinned ? FontAwesomeIcon.Thumbtack : FontAwesomeIcon.Thumbtack;
        var pinColor = _isPinned ? UIConstants.GizmoRed : ThemeManager.CurrentTheme.Text.Text;

        var tooltip = _isPinned ? "取消固定 (关闭窗口)" : "固定以保持打开";

        if(ImBrio.FontIconButton($"pin_toggle_{_id}", pinIcon, tooltip, true, true, pinColor))
        {
            TogglePin();
        }

        ImGui.SameLine();
    }

    private void ResetToFavoritesIfAppearing()
    {
        if(!ImGui.IsWindowAppearing() || _showFavorites)
            return;

        _showFavorites = true;
        UpdateList();
    }

    public new void Draw()
    {
        if(_isPinned)
        {
            ImGui.TextDisabled("(选择器固定为单独窗口)");
            return;
        }

        ResetToFavoritesIfAppearing();
        DrawPinButton();

        base.Draw();
    }

    protected override void PopulateList()
    {
        foreach(var timeline in GameDataProvider.Instance.ActionTimelines)
        {
            if(!string.IsNullOrEmpty(timeline.Key.ToString()))
                AddItem(new ActionTimelineSelectorEntry(
                    timeline.Key.ToString(),
                    (ushort)timeline.RowId,
                    timeline.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Raw,
                    ActionTimelineSelectorEntry.AnimationPurpose.Unknown,
                    (ActionTimelineSlots)timeline.Slot,
                    0,
                    false,
                    0));
        }

        foreach(var emote in GameDataProvider.Instance.GetExcelSheet<Emote>())
        {
            BrioActionTimeline timeline;
            bool drawsWeapon = emote.DrawsWeapon;
            byte emoteCategory = (byte)emote.EmoteCategory.RowId;

            // Loop
            if(emote.ActionTimeline[0].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[0].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Standard,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Intro
            if(emote.ActionTimeline[1].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[1].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Intro,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Ground
            if(emote.ActionTimeline[2].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[2].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Ground,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Chair
            if(emote.ActionTimeline[3].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[3].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Chair,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }

            // Upper Body
            if(emote.ActionTimeline[4].RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(emote.ActionTimeline[4].RowId, out timeline))
            {
                AddItem(new ActionTimelineSelectorEntry(
                    emote.Name.ToString(),
                    (ushort)timeline.RowId,
                    emote.RowId,
                    timeline.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Emote,
                    ActionTimelineSelectorEntry.AnimationPurpose.Blend,
                    (ActionTimelineSlots)timeline.Slot,
                    emote.Icon,
                    drawsWeapon,
                    emoteCategory));
            }
        }

        foreach(var action in GameDataProvider.Instance.GetExcelSheet<ActionSheet>())
        {
            if(action.AnimationEnd.RowId != 0 && GameDataProvider.Instance.ActionTimelines.TryGetRow(action.AnimationEnd.RowId, out BrioActionTimeline timeline))
                AddItem(new ActionTimelineSelectorEntry(
                    action.Name.ToString(),
                    (ushort)action.AnimationEnd.RowId,
                    action.RowId,
                    action.AnimationEnd.Value.Key.ToString(),
                    ActionTimelineSelectorEntry.OriginalType.Action,
                    ActionTimelineSelectorEntry.AnimationPurpose.Action,
                    (ActionTimelineSlots)timeline.Slot,
                    action.Icon,
                    false,
                    0));
        }
    }

    protected override void DrawItem(ActionTimelineSelectorEntry item, bool isSoftSelected)
    {
        bool isFavorite = IsFavorite(item);
        _hoveringFavoriteButton = false;

        var cursor = ImGui.GetCursorPos();
        string label = _twoColumnLayout ? FormatCompactLabel(item) : FormatListLabel(item);

        ImBrio.BorderedGameIcon("icon", item.Icon, "Images.ActionTimeline.png", label, flags: ImGuiButtonFlags.None, size: IconSize);

        if(item.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote)
            return;

        float starSize = ImGui.GetFrameHeight();
        float starX = cursor.X + CurrentSelectableSize.X + ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetCursorPos(new Vector2(starX, cursor.Y + (EntrySize - starSize) * 0.5f));
        if(ImBrio.FavoriteStar(item.UniqueId, isFavorite, new Vector2(starSize)))
            ToggleFavorite(item);

        _hoveringFavoriteButton = ImGui.IsItemHovered();
    }

    protected override void DrawTooltip(ActionTimelineSelectorEntry item)
    {
        if(_hoveringFavoriteButton)
            return;

        var tooltip = _twoColumnLayout
            ? $"{item.Name}\n{item.SecondaryId} {item.TimelineType} {item.Slot} {item.Purpose}\n{item.TimelineId} {item.Key}"
            : $"{item.Name}\n{item.TimelineId} - {item.Key}";

        ImGui.SetTooltip(tooltip);
    }

    protected override void DrawOptions()
    {
        DrawFavoritesTabStrip();

        if(ExpressionsOnly)
            return;

        ImBrio.VerticalPadding(4);

        if(_showBlendable)
        {
            if(ImGui.Checkbox("显示非混合动画", ref _showNonBlendInBlendMode))
                UpdateList();

            ImBrio.VerticalPadding(2);
        }

        if(!_showBlendable)
        {
            ImGui.Text("拔刀状态");
            ImBrio.VerticalPadding(1);

            int drawsWeaponSelection = !_filterByDrawsWeapon ? 0 : (_drawsWeaponValue ? 2 : 1);

            if(ImBrio.ButtonSelectorStrip("draws_weapon_filter", Vector2.Zero, ref drawsWeaponSelection, ["全部", "收起", "拔出"]))
            {
                switch(drawsWeaponSelection)
                {
                    case 0:
                        _filterByDrawsWeapon = false;
                        _drawsWeaponValue = false;
                        break;
                    case 1:
                        _filterByDrawsWeapon = true;
                        _drawsWeaponValue = false;
                        break;
                    case 2:
                        _filterByDrawsWeapon = true;
                        _drawsWeaponValue = true;
                        break;
                }

                UpdateList();
            }

            ImBrio.VerticalPadding(4);
        }

        ImGui.Text("情感动作分类");
        ImBrio.VerticalPadding(1);

        int emoteCategorySelection = _emoteCategoryValue;

        if(ImBrio.ButtonSelectorStrip("emote_category_filter", Vector2.Zero, ref emoteCategorySelection, ["全部", "通常", "特别", "表情"]))
        {
            _emoteCategoryValue = emoteCategorySelection;
            _filterByEmoteCategory = _emoteCategoryValue != 0;
            UpdateList();
        }

        ImBrio.VerticalPadding(3);
    }

    private void DrawFavoritesTabStrip()
    {
        if(ExpressionsOnly)
        {
            bool[] tabs = [_showFavorites, !_showFavorites];
            if(ImBrio.ToggleSelecterStrip("actiontimeline_favorites_selector", Vector2.Zero, ref tabs, ["收藏", "表情"]))
            {
                if(tabs[1] && _showFavorites)
                    _showFavorites = false;
                else
                    _showFavorites = tabs[0];

                UpdateList();
            }

            return;
        }

        bool[] typeTabs = [_showFavorites, !_showFavorites && _showEmotes, !_showFavorites && _showActions, !_showFavorites && _showRaw];
        if(ImBrio.ToggleSelecterStrip("actiontimeline_filters_selector", Vector2.Zero, ref typeTabs, ["收藏", "情感动作", "技能", "时间线"]))
        {
            if(typeTabs[0] && !_showFavorites)
            {
                _showFavorites = true;
            }
            else
            {
                _showFavorites = false;
                _showEmotes = typeTabs[1];
                _showActions = typeTabs[2];
                _showRaw = typeTabs[3];

                if(!_showEmotes && !_showActions && !_showRaw)
                    _showEmotes = true;
            }

            UpdateList();
        }
    }

    protected override int Compare(ActionTimelineSelectorEntry itemA, ActionTimelineSelectorEntry itemB)
    {
        // Emotes first
        if(itemA.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote && itemB.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote)
            return -1;

        if(itemA.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote && itemB.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            return 1;

        // Then Actions
        if(itemA.TimelineType == ActionTimelineSelectorEntry.OriginalType.Action && itemB.TimelineType != ActionTimelineSelectorEntry.OriginalType.Action)
            return -1;

        if(itemA.TimelineType != ActionTimelineSelectorEntry.OriginalType.Action && itemB.TimelineType == ActionTimelineSelectorEntry.OriginalType.Action)
            return 1;

        // Blank to last
        if(string.IsNullOrEmpty(itemA.Name) && !string.IsNullOrEmpty(itemB.Name))
            return 1;

        if(!string.IsNullOrEmpty(itemA.Name) && string.IsNullOrEmpty(itemB.Name))
            return -1;

        // Alphabetical
        var comp = string.Compare(itemA.Name, itemB.Name, StringComparison.InvariantCultureIgnoreCase);
        if(comp != 0)
            return comp;

        // Base Actions first
        if(itemA.Slot == ActionTimelineSlots.Base && itemB.Slot != ActionTimelineSlots.Base)
            return -1;

        if(itemA.Slot != ActionTimelineSlots.Base && itemB.Slot == ActionTimelineSlots.Base)
            return 1;

        return 0;
    }

    protected override bool Filter(ActionTimelineSelectorEntry item, string search)
    {
        var searchText = $"{item.Name} {item.TimelineId} {item.TimelineType} {item.Slot} {item.Purpose} {item.Key} {item.SecondaryId}";

        if(!searchText.Contains(search, StringComparison.InvariantCultureIgnoreCase))
            return false;

        if(ExpressionsOnly)
        {
            if(item.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote || item.EmoteCategory != 3 || item.Purpose != ActionTimelineSelectorEntry.AnimationPurpose.Blend)
                return false;

            if(_showFavorites && !IsFavorite(item))
                return false;

            return true;
        }

        if(_showFavorites)
        {
            if(!IsFavorite(item))
                return false;
        }
        else if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote && !_showEmotes)
            return false;

        if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Action && !_showActions)
            return false;

        if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Raw && !_showRaw)
            return false;

        if(item.Slot != ActionTimelineSlots.Base && !_showBlendable)
            return false;

        // When in blend mode, filter out non-blend animations unless option is enabled
        if(_showBlendable && !_showNonBlendInBlendMode)
        {
            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.Purpose != ActionTimelineSelectorEntry.AnimationPurpose.Blend)
                    return false;
            }
        }

        if(_filterByDrawsWeapon)
        {
            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.DrawsWeapon != _drawsWeaponValue)
                    return false;
            }
        }

        if(_filterByEmoteCategory)
        {
            if(item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote)
            {
                if(item.EmoteCategory != _emoteCategoryValue)
                    return false;
            }
        }

        return true;
    }

    private static bool IsFavorite(ActionTimelineSelectorEntry item)
        => item.TimelineType == ActionTimelineSelectorEntry.OriginalType.Emote
            && ConfigurationService.Instance.Configuration.Emote.Favorites.Contains(item.UniqueId);

    private void ToggleFavorite(ActionTimelineSelectorEntry item)
    {
        if(item.TimelineType != ActionTimelineSelectorEntry.OriginalType.Emote)
            return;

        var favorites = ConfigurationService.Instance.Configuration.Emote.Favorites;
        if(!favorites.Add(item.UniqueId))
            favorites.Remove(item.UniqueId);

        ConfigurationService.Instance.Save();
        UpdateList();
    }

    protected override void DrawSearchRight()
    {
        var tooltip = _twoColumnLayout
            ? "当前为两列模式，点击切换为单列模式"
            : "当前为单列模式，点击切换为两列模式";
        uint color = _twoColumnLayout
            ? ThemeManager.CurrentTheme.Text.Text
            : ThemeManager.CurrentTheme.Text.TextDisabled;

        if(ImBrio.FontIconButton("column_layout", (FontAwesomeIcon)0xF0DB, tooltip, true, true, color))
            _twoColumnLayout = !_twoColumnLayout;
    }

    private static string FormatListLabel(ActionTimelineSelectorEntry item)
        => $"{item.Name}\n{item.SecondaryId} {item.TimelineType} {item.Slot} {item.Purpose}\n{item.TimelineId} {item.Key}";

    private string FormatCompactLabel(ActionTimelineSelectorEntry item)
    {
        float textWidth = CurrentSelectableSize.X - IconSize.X - ImGui.GetStyle().ItemSpacing.X;
        string name = TruncateName(item.Name, textWidth);
        string keyName = TruncateName(GetKeyFileName(item.Key), textWidth);
        return string.IsNullOrEmpty(keyName) ? name : $"{name}\n{keyName}";
    }

    private static string GetKeyFileName(string key)
    {
        if(string.IsNullOrEmpty(key))
            return string.Empty;

        int slash = key.LastIndexOf('/');
        return slash >= 0 && slash < key.Length - 1 ? key[(slash + 1)..] : key;
    }

    private static string TruncateName(string name, float maxWidth)
    {
        if(maxWidth <= 0 || ImGui.CalcTextSize(name).X <= maxWidth)
            return name;

        const string ellipsis = "…";
        while(name.Length > 0 && ImGui.CalcTextSize(name + ellipsis).X > maxWidth)
            name = name[..^1];

        return name.Length == 0 ? ellipsis : name + ellipsis;
    }
}

public record class ActionTimelineSelectorEntry(
    string Name,
    ushort TimelineId,
    uint SecondaryId,
    string Key,
    ActionTimelineSelectorEntry.OriginalType TimelineType,
    ActionTimelineSelectorEntry.AnimationPurpose Purpose,
    ActionTimelineSlots Slot,
    uint Icon,
    bool DrawsWeapon,
    byte EmoteCategory)
{
    public string UniqueId => $"{TimelineType}-{SecondaryId}-{Purpose}-{TimelineId}";

    public enum AnimationPurpose
    {
        Unknown,
        Action,
        Standard,
        Intro,
        Ground,
        Chair,
        Blend,
    }

    public enum OriginalType
    {
        Raw,
        Emote,
        Action,
    }
}
