using Lumina;
using Lumina.Data;
using Lumina.Excel;

namespace Brio.Resources.Sheets;

[Sheet("ActionTimeline", 0xD803699F)]
public class BrioActionTimeline : ExcelRow
{
    public string Key { get; private set; }
    public ushort WeaponTimelineId { get; private set; }
    public ushort Unknown { get; private set; }
    public byte Type { get; private set; }
    public byte Priority { get; private set; }
    public byte Stance { get; private set; }
    public byte Slot { get; private set; }
    public byte LookAtMode { get; private set; }
    public byte ActionTimelineIDMode { get; private set; }
    public byte LoadType { get; private set; }
    public byte StartAttach { get; private set; }
    public byte ResidentPap { get; private set; }
    public byte Unknown6 { get; private set; }
    public byte Unknown1 { get; private set; }
    public bool Pause { get; private set; }
    public bool Resident { get; private set; }
    public bool IsMotionCanceledByMoving { get; private set; }
    public bool Unknown2 { get; private set; }
    public bool Unknown3 { get; private set; }
    public bool IsLoop { get; private set; }
    public bool Unknown4 { get; private set; }

    // 实现 ExcelRow 的 PopulateData 方法
    public override void PopulateData(RowParser parser, GameData gameData, Language language)
    {
        RowId = parser.RowId;

        Key = parser.ReadColumn<string>(0) ?? string.Empty;
        WeaponTimelineId = parser.ReadColumn<ushort>(1);
        Unknown = parser.ReadColumn<ushort>(2);
        Type = parser.ReadColumn<byte>(3);
        Priority = parser.ReadColumn<byte>(4);
        Stance = parser.ReadColumn<byte>(5);
        Slot = parser.ReadColumn<byte>(6);
        LookAtMode = parser.ReadColumn<byte>(7);
        ActionTimelineIDMode = parser.ReadColumn<byte>(8);
        LoadType = parser.ReadColumn<byte>(9);
        StartAttach = parser.ReadColumn<byte>(10);
        ResidentPap = parser.ReadColumn<byte>(11);
        Unknown6 = parser.ReadColumn<byte>(12);
        Unknown1 = parser.ReadColumn<byte>(13);

        var packedBools = parser.ReadColumn<byte>(14);
        Pause = (packedBools & 0b00000001) != 0;
        Resident = (packedBools & 0b00000010) != 0;
        IsMotionCanceledByMoving = (packedBools & 0b00000100) != 0;
        Unknown2 = (packedBools & 0b00001000) != 0;
        Unknown3 = (packedBools & 0b00010000) != 0;
        IsLoop = (packedBools & 0b00100000) != 0;
        Unknown4 = (packedBools & 0b01000000) != 0;
    }
}
