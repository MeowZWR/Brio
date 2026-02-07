using Brio.Capabilities.World;
using Brio.Game.Types;
using Brio.UI.Controls.Selectors;
using Brio.UI.Controls.Stateless;
using Brio.UI.Widgets.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace Brio.UI.Widgets.World;

public class TimeWeatherWidget(TimeWeatherCapability weatherCapability) : Widget<TimeWeatherCapability>(weatherCapability)
{
    public const int DayTime = 86400 / 60;

    public override string HeaderName => "时间与天气";
    public override WidgetFlags Flags => WidgetFlags.DefaultOpen | WidgetFlags.DrawBody;


    private static readonly WeatherSelector _weatherSelector = new("global_weather_selector");

    public override void DrawBody()
    {
        var isWeatherOverrideEnabledLocked = Capability.EnvironmentService.WeatherOverrideEnabled;
        var isWeatherOverrideEnabledLockedPrevious = isWeatherOverrideEnabledLocked;
        var currentWeather = (int)Capability.EnvironmentService.CurrentWeather;
        var previousWeather = currentWeather;

        var isTimeFrozen = Capability.TimeService.IsTimeFrozen;
        var isTimeFrozenPrevious = isTimeFrozen;

        int minuteOfDay = Capability.TimeService.MinuteOfDay;
        int dayOfMonth = Capability.TimeService.DayOfMonth;

        Vector2 unlockPos = ImGui.GetCursorPos();

        var dateTime = new DateTime().AddMinutes(minuteOfDay);

        ImBrio.VerticalPadding(5);
        ImGui.Text("一天中的时间"u8);
        ImBrio.VerticalPadding(5);

        var preservePostime = ImGui.GetCursorPos();
        ImGui.SetCursorPos(unlockPos);
        if(ImBrio.FontIconButtonRight("timeLock", isTimeFrozen ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock, 1, isTimeFrozen ? "解锁时间" : "锁定时间", bordered: false))
            isTimeFrozen = !isTimeFrozen;
        ImGui.SetCursorPos(preservePostime);

        ImBrio.CenterNextElementWithPadding(15);
        var realTime = ImGui.SliderInt("##time_real"u8, ref minuteOfDay, 0, DayTime - 1, dateTime.ToShortTimeString(), ImGuiSliderFlags.NoInput);
        ImBrio.AttachToolTip("一天中的时间");

        var time = false;
        var dragday = false;
        using(ImRaii.ItemWidth((ImBrio.GetRemainingWidth() / 2) - ImGui.GetStyle().ItemInnerSpacing.X))
        {
            time = ImGui.SliderInt("##time_set"u8, ref minuteOfDay, 0, DayTime - 1, "%.0f"u8);
            ImBrio.AttachToolTip("一天中的时间（分钟）");

            ImGui.SameLine();

            dragday = ImGui.SliderInt("##day_set"u8, ref dayOfMonth, 1, 31);
            ImBrio.AttachToolTip("月份中的日期");
        }

        if(realTime || time || dragday)
        {
            isTimeFrozen = true;
            Capability.TimeService.MinuteOfDay = minuteOfDay;
            Capability.TimeService.DayOfMonth = dayOfMonth;
        }

        if(isTimeFrozen != isTimeFrozenPrevious)
            Capability.TimeService.IsTimeFrozen = isTimeFrozen;

        //
        //

        ImBrio.VerticalPadding(10);
        ImGui.Separator();

        unlockPos = ImGui.GetCursorPos();

        ImGui.Text("当前天气 /"u8);
        ImGui.SameLine();
        WeatherUnion union = (WeatherId)currentWeather;
        union.Switch(
            row => ImGui.Text($"[{row.Name.ToString()}]"),
            none => ImGui.Text("未知天气"u8)
        );
        ImBrio.VerticalPadding(5);

        var preservePos = ImGui.GetCursorPos();

        ImGui.SetCursorPos(unlockPos);
        if(ImBrio.FontIconButtonRight("weatherLock", isWeatherOverrideEnabledLocked ? FontAwesomeIcon.Unlock : FontAwesomeIcon.Lock, 1, isWeatherOverrideEnabledLocked ? "解锁天气" : "锁定天气", bordered: false))
            isWeatherOverrideEnabledLocked = !isWeatherOverrideEnabledLocked;
        ImGui.SetCursorPos(preservePos);

        ImBrio.CenterNextElementWithPadding(10);
        if(ImBrio.BorderedWeatherGameIcon("current_weather", (WeatherUnion)Capability.EnvironmentService.CurrentWeather, showText: false))
        {
            _weatherSelector.SetNaturalWeathers(Capability.EnvironmentService.TerritoryWeatherTable);
            _weatherSelector.Select(Capability.EnvironmentService.CurrentWeather);
            ImGui.OpenPopup("weather_selector"u8);
        }

        var startAt = ImGui.GetCursorPos();

        ImGui.SameLine();

        ImBrio.CenterNextElementWithPadding(10);
        ImBrio.VerticalPadding(5);
        ImGui.InputInt("###current_weather_input"u8, ref currentWeather, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue);
        ImBrio.AttachToolTip("天气 ID");

        using(var popup = ImRaii.Popup("weather_selector"u8))
        {
            if(popup.Success)
            {
                _weatherSelector.Draw();

                if(_weatherSelector.SoftSelectionChanged && _weatherSelector.SoftSelected != null)
                {
                    currentWeather = _weatherSelector.SoftSelected.Match(
                        naturalWeather => (int)naturalWeather.RowId,
                        weatherOverride => 0
                    );
                }

                if(_weatherSelector.SelectionChanged)
                    ImGui.CloseCurrentPopup();
            }
        }

        if(currentWeather != previousWeather)
        {
            isWeatherOverrideEnabledLocked = true;
            Capability.EnvironmentService.CurrentWeather = currentWeather;
        }

        if(isWeatherOverrideEnabledLocked != isWeatherOverrideEnabledLockedPrevious)
            Capability.EnvironmentService.WeatherOverrideEnabled = isWeatherOverrideEnabledLocked;
    }
}
