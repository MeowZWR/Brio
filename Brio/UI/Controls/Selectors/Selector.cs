using Brio.UI.Controls.Stateless;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Brio.UI.Controls.Selectors;

public abstract class Selector<T> where T : class
{
    public T? Selected => _selected;
    public T? SoftSelected => _softSelected;

    public bool SoftSelectionChanged { get; private set; }
    public bool SelectionChanged { get; private set; }

    protected string _id;

    protected volatile T? _selected;
    protected volatile T? _softSelected;

    private readonly List<T> _items = [];
    private List<T>? _filteredAndSortedItems;

    private string _search = "";
    private string _lastSearch = "";

    private bool _scrollToSelected = false;
    private bool _shouldFocusSearch = false;

    protected bool _useAvailableSpace = false;

    protected abstract Vector2 MinimumListSize { get; }
    protected abstract float EntrySize { get; }
    protected abstract SelectorFlags Flags { get; }

    protected virtual int Columns => 1;
    protected virtual float EntryButtonWidth => 0f;
    protected virtual float SearchRightReservedWidth => 0f;
    protected Vector2 CurrentSelectableSize => _selectableSize;

    private Task _taskQueue = Task.CompletedTask;

    private Vector2 _selectableSize = new();

    public Selector(string id)
    {
        _id = id;
        _filteredAndSortedItems = null;

        InitList();
    }

    public void Select(T? selected, bool shouldScroll = true, bool shouldUpdate = true, bool shouldClear = false)
    {
        _selected = selected;
        _softSelected = selected;

        if(selected != null)
            _shouldFocusSearch = true;

        if(shouldScroll)
            _scrollToSelected = true;

        if(shouldUpdate)
            UpdateList(shouldClear);
    }

    public void ClearSearch()
    {
        _search = string.Empty;
        _lastSearch = string.Empty;
    }

    public unsafe void Draw()
    {

        ImBrio.BlurPopup();

        var items = _filteredAndSortedItems;

        SoftSelectionChanged = false;
        SelectionChanged = false;

        using(ImRaii.PushId($"selector_{_id}"))
        {
            if(Flags.HasFlag(SelectorFlags.AllowSearch))
            {
                float reserve = SearchRightReservedWidth;
                if(reserve > 0f)
                    ImGui.SetNextItemWidth(Math.Max(1f, ImGui.GetContentRegionAvail().X - reserve));
                else
                    ImGui.SetNextItemWidth(-1);

                if(_shouldFocusSearch)
                    ImGui.SetKeyboardFocusHere();

                if(ImGui.InputTextWithHint($"###search", "搜索", ref _search, 256))
                {
                    // Only update if search actually changed
                    if(_search != _lastSearch)
                    {
                        _lastSearch = _search;
                        UpdateList();
                    }
                }

                if(reserve > 0f)
                {
                    ImGui.SameLine();
                    DrawSearchRight();
                }
            }
            _shouldFocusSearch = false;

            if(Flags.HasFlag(SelectorFlags.ShowOptions))
            {
                using(ImRaii.PushId("options_container"))
                {
                    DrawOptions();
                }
            }

            var minSize = MinimumListSize * ImGuiHelpers.GlobalScale;
            var listSize = minSize;

            if(_useAvailableSpace)
            {
                // Use all available space in the current context (e.g. pinned window) I hate this, kill it
                var availableSize = ImGui.GetContentRegionAvail();
                listSize.X = availableSize.X;
                listSize.Y = availableSize.Y;
            }
            else if(Flags.HasFlag(SelectorFlags.AdaptiveSizing))
            {
                var maxSize = ImGui.GetContentRegionAvail();

                if(listSize.X < maxSize.X)
                {
                    listSize.X = maxSize.X;
                    listSize.Y = minSize.Y * (1.0f + (listSize.X / minSize.X));
                }
            }

            using(var listbox = ImRaii.ListBox($"###listbox", listSize))
            {
                if(items == null)
                    return;

                if(listbox.Success)
                {
                    int columns = Math.Max(1, Columns);
                    var style = ImGui.GetStyle();
                    float availX = ImGui.GetContentRegionAvail().X;
                    float spacingX = style.ItemSpacing.X;
                    float reserve = EntryButtonWidth;
                    float cellWidth;

                    if(columns > 1)
                    {
                        cellWidth = MathF.Max(1f, (availX - spacingX * (columns - 1)) / columns);
                        _selectableSize.X = reserve > 0f ? MathF.Max(1f, cellWidth - reserve) : cellWidth;
                    }
                    else
                    {
                        cellWidth = availX;
                        _selectableSize.X = reserve > 0f ? MathF.Max(1f, availX - reserve) : 0f;
                    }

                    _selectableSize.Y = EntrySize;

                    float rowPitch = EntrySize + style.ItemSpacing.Y;
                    int rowCount = (items.Count + columns - 1) / columns;

                    if(_scrollToSelected)
                    {
                        int selIndex = items.FindIndex(IsItemSoftSelected);
                        if(selIndex >= 0)
                        {
                            int row = selIndex / columns;
                            ImGui.SetScrollY(Math.Max(0f, row * rowPitch - (listSize.Y - rowPitch) * 0.5f));
                        }

                        _scrollToSelected = false;
                    }

                    var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper());
                    clipper.Begin(rowCount, rowPitch);
                    while(clipper.Step())
                    {
                        for(int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                        {
                            var rowPos = ImGui.GetCursorPos();

                            for(int col = 0; col < columns; col++)
                            {
                                int index = row * columns + col;
                                if(index >= items.Count)
                                    break;

                                if(columns > 1)
                                    ImGui.SetCursorPos(new Vector2(rowPos.X + col * (cellWidth + spacingX), rowPos.Y));

                                var item = items[index];

                                using(ImRaii.PushId(index))
                                {
                                    var startPos = ImGui.GetCursorPos();
                                    bool isSoftSelected = IsItemSoftSelected(item);
                                    bool wasSoftSelected = ImGui.Selectable($"###entry", isSoftSelected, ImGuiSelectableFlags.AllowDoubleClick, _selectableSize);
                                    bool wasSelected = wasSoftSelected && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
                                    var endPos = ImGui.GetCursorPos();

                                    if(ImGui.IsItemVisible())
                                    {
                                        ImGui.SetCursorPos(startPos);
                                        using(ImRaii.PushId("item_container"))
                                        {
                                            using(var itemGroup = ImRaii.Group())
                                            {
                                                DrawItem(item, isSoftSelected);
                                            }
                                            if(ImGui.IsItemHovered())
                                                DrawTooltip(item);
                                        }
                                        ImGui.SetCursorPos(endPos);
                                    }

                                    if(wasSoftSelected)
                                    {
                                        _softSelected = item;
                                        SoftSelectionChanged = true;

                                        if(wasSelected)
                                        {
                                            _selected = item;
                                            SelectionChanged = true;
                                        }
                                    }
                                }
                            }

                            if(columns > 1)
                                ImGui.SetCursorPos(new Vector2(rowPos.X, rowPos.Y + rowPitch));
                        }
                    }
                    clipper.End();
                    clipper.Destroy();
                }
            }
        }
    }

    protected void AddItem(T item)
    {
        _items.Add(item);
    }

    protected void AddItems(IEnumerable<T> items)
    {
        _items.AddRange(items);
    }

    protected abstract void DrawItem(T item, bool isSoftSelected);

    protected virtual void DrawSearchRight()
    {

    }

    protected virtual void DrawOptions()
    {

    }

    protected virtual void DrawTooltip(T item)
    {

    }

    protected virtual void PopulateList()
    {

    }

    private void InitList()
    {
        _taskQueue = _taskQueue.ContinueWith(_ =>
        {
            PopulateList();
        }, TaskScheduler.Default);

        UpdateList();
    }

    protected void UpdateList(bool shouldClear = false)
    {
        if(shouldClear)
        {
            Interlocked.Exchange(ref _filteredAndSortedItems, null);
        }

        _taskQueue = _taskQueue.ContinueWith(_ =>
        {
            var newList = _items.Where(x =>
            {
                // Selected is always shown
                if(IsItemSelected(x))
                    return true;

                return Filter(x, _search);
            }).ToList();
            newList.Sort(Compare);

            Interlocked.Exchange(ref _filteredAndSortedItems, newList);

        }, TaskScheduler.Default);
    }

    protected virtual bool Filter(T item, string search)
    {
        return true;
    }

    protected virtual int Compare(T itemA, T itemB)
    {
        return 0;
    }

    protected virtual bool IsItemSoftSelected(T item)
    {
        return _softSelected?.Equals(item) ?? false;
    }

    protected virtual bool IsItemSelected(T item)
    {
        return _selected?.Equals(item) ?? false;
    }
}

[Flags]
public enum SelectorFlags
{
    None = 0,
    AllowSearch = 1 << 0,
    ShowOptions = 1 << 1,
    AdaptiveSizing = 1 << 2,
}
