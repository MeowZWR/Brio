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
        var isWeatherOverrideEnabledLocked = Capability.WeatherService.WeatherOverrideEnabled;
        var isWeatherOverrideEnabledLockedPrevious = isWeatherOverrideEnabledLocked;
        var currentWeather = (int)Capability.WeatherService.CurrentWeather;
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
        var realTime = ImGui.SliderInt("##time_real"u8, ref minuteOfDay, 0, DayTime, dateTime.ToShortTimeString(), ImGuiSliderFlags.NoInput);
        ImBrio.AttachToolTip("一天中的时间");

        var time = false;
        var dragday = false;
        using(ImRaii.ItemWidth((ImBrio.GetRemainingWidth() / 2) - ImGui.GetStyle().ItemInnerSpacing.X))
        {
            time = ImGui.SliderInt("##time_set"u8, ref minuteOfDay, 10, DayTime, "%.0f"u8);
            ImBrio.AttachToolTip("一天中的时间（分钟）");

            ImGui.SameLine();

            dragday = ImGui.SliderInt("##day_set"u8, ref dayOfMonth, 10, 31, "%.0f"u8);
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

        ImBrio.VerticalPadding(5);
        ImGui.Separator();
        ImBrio.VerticalPadding(5);

        if(ImBrio.BorderedGameIcon("current_weather", (WeatherUnion)Capability.WeatherService.CurrentWeather, showText: false))
        {
            _weatherSelector.SetNaturalWeathers(Capability.WeatherService.TerritoryWeatherTable);
            _weatherSelector.Select(Capability.WeatherService.CurrentWeather);
            ImGui.OpenPopup("weather_selector");
        }

        ImBrio.VerticalPadding(1);
        ImGui.SameLine();
        ImBrio.VerticalPadding(1);

        var startAt = ImGui.GetCursorPos();

        unlockPos = ImGui.GetCursorPos();

        ImGui.SetCursorPosX(startAt.X);
        ImGui.SetCursorPosY(startAt.Y + (ImGui.GetTextLineHeight() * 1.2f));

        WeatherUnion union = (WeatherId)currentWeather;
        union.Switch(
            row => ImGui.Text(row.Name.ToString()),
            none => ImGui.NewLine()
        );

        ImBrio.CenterNextElementWithPadding(10);
        ImGui.InputInt("###current_weather_input", ref currentWeather, 0, 0, default, ImGuiInputTextFlags.EnterReturnsTrue);
        ImBrio.AttachToolTip("天气ID");

        ImBrio.VerticalPadding(5);

        using(var popup = ImRaii.Popup("weather_selector"))
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

        var preservePos = ImGui.GetCursorPos();
        ImGui.SetCursorPos(unlockPos);
        if(isWeatherOverrideEnabledLocked)
        {
            if(ImBrio.FontIconButtonRight("lock", FontAwesomeIcon.Unlock, 1, "解锁天气", bordered: false))
                isWeatherOverrideEnabledLocked = false;
        }
        else
        {
            if(ImBrio.FontIconButtonRight("lock", FontAwesomeIcon.Lock, 1, "锁定天气", bordered: false))
                isWeatherOverrideEnabledLocked = true;
        }
        ImGui.SetCursorPos(preservePos);

        if(currentWeather != previousWeather)
        {
            isWeatherOverrideEnabledLocked = true;
            Capability.WeatherService.CurrentWeather = currentWeather;
        }

        if(isWeatherOverrideEnabledLocked != isWeatherOverrideEnabledLockedPrevious)
            Capability.WeatherService.WeatherOverrideEnabled = isWeatherOverrideEnabledLocked;
    }
}
