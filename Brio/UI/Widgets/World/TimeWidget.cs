using Brio.Capabilities.World;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class TimeWidget(TimeCapability timeCapability) : Widget<TimeCapability>(timeCapability)
{
    public override string HeaderName => "时间";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;

    public override void DrawBody()
    {
        Vector2 unlockPos = Vector2.Zero;
        var isLocked = Capability.TimeService.IsTimeFrozen;
        var isLockedPrevious = isLocked;
        int minuteOfDay = Capability.TimeService.MinuteOfDay;
        int dayOfMonth = Capability.TimeService.DayOfMonth;
        var displayTime = TimeSpan.FromMinutes(minuteOfDay);

        int originalMinute = minuteOfDay;
        int originalDay = dayOfMonth;


        ImGui.PushItemWidth(-((ImGui.GetStyle().FramePadding.X * 2) + ImGui.CalcTextSize("XXXXXXXXXXXXX").X));
        ImGui.SliderInt("当日时间", ref minuteOfDay, 0, 1439, $"{displayTime.Hours:D2}:{displayTime.Minutes:D2}");
        ImGui.SameLine();
        unlockPos = ImGui.GetCursorPos();
        ImGui.NewLine();
        ImGui.SliderInt("日期", ref dayOfMonth, 1, 31);
        ImGui.PopItemWidth();


        var preservePos = ImGui.GetCursorPos();
        ImGui.SetCursorPos(unlockPos);
        if(isLocked)
        {
            if(ImBrio.FontIconButtonRight("timelock", FontAwesomeIcon.Unlock, 1, "解锁时间", bordered: false))
                isLocked = false;
        }
        else
        {
            if(ImBrio.FontIconButtonRight("timelock", FontAwesomeIcon.Lock, 1, "锁定时间", bordered: false))
                isLocked = true;
        }
        ImGui.SetCursorPos(preservePos);

        if(originalMinute != minuteOfDay)
        {
            isLocked = true;
            Capability.TimeService.MinuteOfDay = minuteOfDay;
        }

        if(originalDay != dayOfMonth)
        {
            isLocked = true;
            Capability.TimeService.DayOfMonth = dayOfMonth;
        }

        if(isLocked != isLockedPrevious)
            Capability.TimeService.IsTimeFrozen = isLocked;
    }
}
