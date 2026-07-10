using Brio.Core;
using MessagePack;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Brio.Resources.Extra;

public enum PathKind
{
    Model,
    Vfx,
}

[MessagePackObject]
public sealed class PathData
{
    [Key(0)] public string Path { get; set; } = string.Empty;

    [Key(1)] public string Name { get; set; } = string.Empty;
    [Key(2)] public string Description { get; set; } = string.Empty;
    [Key(3)] public long LastModifiedUTC { get; set; }

    [Key(4)] public string Expansion { get; set; } = string.Empty;

    [Key(5)] public int Length { get; set; } = 0;
    [Key(6)] public bool Repeats { get; set; } = false;
    [Key(7)] public bool RequiresRefresh { get; set; } = false;

    [Key(8)] public List<int> KnownTerritoryLocations { get; set; } = [];
    [Key(9)] public List<string> Subtypes { get; set; } = [];
    [Key(10)] public List<string> AssetType { get; set; } = [];
    [Key(11)] public List<string> Tags { get; set; } = [];

    public PathData Clone() => new()
    {
        Path = Path,
        Name = Name,
        Description = Description,
        LastModifiedUTC = LastModifiedUTC,
        Expansion = Expansion,
        Length = Length,
        Repeats = Repeats,
        RequiresRefresh = RequiresRefresh,
        KnownTerritoryLocations = [.. KnownTerritoryLocations],
        Subtypes = [.. Subtypes],
        AssetType = [.. AssetType],
        Tags = [.. Tags],
    };

    public bool ContentEquals(PathData other)
    {
        if(Name != other.Name) return false;
        if(Description != other.Description) return false;
        if(Expansion != other.Expansion) return false;
        if(Length != other.Length) return false;
        if(Repeats != other.Repeats) return false;
        if(RequiresRefresh != other.RequiresRefresh) return false;

        if(KnownTerritoryLocations.Count != other.KnownTerritoryLocations.Count) return false;
        for(var i = 0; i < KnownTerritoryLocations.Count; i++)
            if(KnownTerritoryLocations[i] != other.KnownTerritoryLocations[i]) return false;

        if(Subtypes.Count != other.Subtypes.Count) return false;
        for(var i = 0; i < Subtypes.Count; i++)
            if(Subtypes[i] != other.Subtypes[i]) return false;

        if(AssetType.Count != other.AssetType.Count) return false;
        for(var i = 0; i < AssetType.Count; i++)
            if(AssetType[i] != other.AssetType[i]) return false;

        if(Tags.Count != other.Tags.Count) return false;
        for(var i = 0; i < Tags.Count; i++)
            if(Tags[i] != other.Tags[i]) return false;

        return true;
    }

    public static string Normalize(string path)
    {
        if(string.IsNullOrEmpty(path))
            return string.Empty;

        var s = path.Replace('\\', '/').Trim();

        while(s.Contains("//"))
            s = s.Replace("//", "/");

        return s.Trim('/').ToLowerInvariant();
    }

    public static ulong Hash(string path)
    {
        var normalizedPath = Normalize(path);
        var maxBytes = Encoding.UTF8.GetMaxByteCount(normalizedPath.Length);

        Span<byte> buffer = new byte[maxBytes];
        int written = Encoding.UTF8.GetBytes(normalizedPath, buffer);
        return XxHash3.HashToUInt64(buffer[..written]);
    }

    public static string FileName(string pathOrName)
    {
        var norm = Normalize(pathOrName);
        if(norm.Length == 0)
            return string.Empty;

        var slash = norm.LastIndexOf('/');
        var name = slash >= 0 ? norm[(slash + 1)..] : norm;

        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }
}

[MessagePackObject]
public sealed class PathStore
{
    public const int FormatVersion = 1;
    public static readonly byte[] Magic = "BRIOPATH"u8.ToArray();

    [Key(0)] public int KeyVersion { get; set; } = 1;
    [Key(1)] public Dictionary<ulong, PathData> Entries { get; set; } = [];
}

[MessagePackObject]
public sealed class PathMetaExport
{
    public const int FormatVersion = 1;
    public static readonly byte[] Magic = "BRIOPDBX"u8.ToArray();

    [Key(0)] public long ExportedUTC { get; set; }
    [Key(1)] public PathStore DB { get; set; } = new();
}

public readonly record struct GamePathInfo(string Path, string DisplayName, string Expansion, string Subtype, string AssetType);

public sealed partial class PathIndex
{
    public static readonly Dictionary<string, string> ExpansionNames = new()
    {
        { "ffxiv", "重生之境"   },
        { "ex1",   "苍穹之禁城"    },
        { "ex2",   "红莲之狂潮"     },
        { "ex3",   "暗影之逆焰" },
        { "ex4",   "晓月之终途"      },
        { "ex5",   "金曦之遗辉"      },
        //{ "ex6",   "Evercold"      },
    };

    public static readonly Dictionary<string, string> AssetTypeNames = new()
    {
        { "rck",  "岩石"      }, { "rock", "岩石"      }, { "roc",  "岩石"          },
        { "wal",  "墙壁"      }, { "wall", "墙壁"      },
        { "tre",  "树木"      }, { "tree", "树木"      },
        { "dor",  "门"        },
        { "cel",  "天花板"    },
        { "plr",  "柱子"      }, { "pil",  "柱子"      }, { "pill", "柱子"          },
        { "flo",  "地板"      },
        { "lmp",  "灯具"      }, { "lamp", "灯具"      }, { "ligt", "灯光"          }, { "lig",  "灯光"      },
        { "gat",  "大门"      }, { "gate", "大门"      },
        { "fen",  "栅栏"      },
        { "tow",  "塔"        },
        { "obj",  "物体"      },
        { "nat",  "自然"      },
        { "cry",  "水晶"      },
        { "wat",  "水"        }, { "sea",  "海"         },
        { "stc",  "结构"      },
        { "gls",  "玻璃"      }, { "grs",  "玻璃"       },
        { "box",  "箱子"      },
        { "flw",  "花卉"      },
        { "bos",  "首领"      },
        { "wep",  "武器"      },
        { "fnt",  "家具"      },
        { "rub",  "瓦砾"      },
        { "cin",  "钱币"      },
        { "lsf",  "景观"      },
        { "arf",  "杂项"      },
        { "ter",  "地形"      }, { "plt",  "植被"       }, { "bsh",  "植被"          },
        { "gren", "植被"      },
        { "itm",  "道具"      },
        { "chr",  "椅子"      }, { "chair", "椅子"     },
        { "dsk",  "书桌"      }, { "desk", "书桌"       },
        { "rug",  "地毯"      },
        { "slf",  "架子"      }, { "shelf", "架子"     }, { "she",  "架子"           },
        { "win",  "窗户"      },
        { "tbl",  "桌子"      }, { "table", "桌子"     },
        { "bed",  "床"        },
        { "sof",  "沙发"      }, { "sofa", "沙发"      },
        { "cab",  "柜子"      },
        { "sign", "招牌"      }, { "sgn",  "招牌"       },
        { "stl",  "摊位"      },
        { "ban",  "旗帜"      },
        { "pot",  "花盆"      },
        { "str",  "楼梯"      },
        { "brg",  "桥"        },
        { "rof",  "屋顶"      },
        { "fsh",  "鱼"        },
        { "door", "门"        },
        { "fnc",  "栅栏"      }, { "fenc", "栅栏"       },
        { "flr",  "地板"      }, { "flor", "地板"       },
        { "rom",  "房间"      }, { "room", "房间"       },
        { "bas",  "底座"      }, { "base", "底座"       },
        { "air",  "飞空艇"    },
        { "boss", "首领"      },
        { "step", "台阶"      }, { "stp",  "台阶"       },
        { "pip",  "管道"      },
        { "pol",  "立柱"      },
        { "ivy",  "藤蔓"      }, { "tuta", "藤蔓"       },
        { "dec",  "装饰品"    },
        { "bui",  "建筑"      },
        { "sak",  "栏杆"      }, { "saku", "栏杆"       },
        { "stn",  "石材"      },
        { "rok",  "岩石"      }, { "rk",   "岩石"       },
    };

    public static readonly Dictionary<string, string> SubtypeNames = new()
    {
        { "fld",    "野外"         },
        { "dun",    "迷宫"         },
        { "twn",    "城镇"         },
        { "rad",    "团队任务"     },
        { "evt",    "季节活动"     },
        { "cnt",    "通用内容"     },
        { "btl",    "尼尔"         },
        { "alx",    "亚历山大"     },
        // { "ome",    ""         },
        { "bah",    "巴哈姆特"     },
        { "chr",    "过场动画"     },
        { "pvp",    "PVP"          },
        { "ind",    "室内"         },
        { "ang",    "斗技场"       },
        { "nature", "自然"         },
        //{ "xbm",    ""        },
        { "jai",    "监狱"         },
        { "common", "通用"         },
    };

    //

    private readonly GamePathInfo[] _paths;
    private readonly FrozenDictionary<ulong, int> _byHash;
    private readonly MultiValueDictionary<string, string> _byFileName;

    public int Count => _paths.Length;
    public IReadOnlyList<GamePathInfo> Paths => _paths;

    //

    [GeneratedRegex(@"^[a-z]+", RegexOptions.Compiled)]
    private static partial Regex LeadingAlphaRegex();

    //

    private PathIndex(GamePathInfo[] paths, Dictionary<ulong, int> byHash, MultiValueDictionary<string, string> byFileName)
    {
        _paths = paths;
        _byHash = byHash.ToFrozenDictionary();
        _byFileName = byFileName;
    }

    public static PathIndex FromLines(IEnumerable<string> lines)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        foreach(var line in lines)
        {
            var norm = PathData.Normalize(line);
            if(norm.Length > 0)
                set.Add(norm);
        }

        var infos = new GamePathInfo[set.Count];
        var byHash = new Dictionary<ulong, int>(infos.Length);
        var byFileName = new MultiValueDictionary<string, string>();
        var i = 0;
        foreach(var path in set)
        {
            var info = ParsePath(path);
            infos[i] = info;
            byHash[PathData.Hash(info.Path)] = i;
            byFileName.Add(PathData.FileName(info.Path), info.Path);
            i++;
        }

        return new PathIndex(infos, byHash, byFileName);
    }

    private static GamePathInfo ParsePath(string path)
    {
        var splitPath = path.Split('/');
        string expansion = "本体";
        string subtype = "未知";

        if(splitPath.Length > 1 && splitPath[0] == "bg")
        {
            if(ExpansionNames.TryGetValue(splitPath[1], out var exp))
            {
                expansion = exp;
                if(splitPath.Length > 3 && SubtypeNames.TryGetValue(splitPath[3], out var subtypeName))
                    subtype = subtypeName;
                else if(splitPath.Length > 3)
                    subtype = splitPath[3];
            }
        }
        else if(splitPath[0] == "bgcommon" && splitPath.Length > 2)
        {
            subtype = SubtypeNames.TryGetValue(splitPath[1], out var subeTypeName) ? subeTypeName : splitPath[1];
        }

        var fileName = Path.GetFileNameWithoutExtension(path);
        var assetType = GetAssetType(fileName);

        return new GamePathInfo(path, $"{assetType} [{fileName}]", expansion, subtype, assetType);
    }

    private static string GetAssetType(string fileName)
    {
        var parts = fileName.Split('_', StringSplitOptions.RemoveEmptyEntries);

        string? group = null;
        foreach(var part in parts)
        {
            var match = LeadingAlphaRegex().Match(part);
            var alpha = match.Success ? match.Value : string.Empty;
            if(alpha.Length >= 2 && AssetTypeNames.TryGetValue(alpha, out var typename))
            {
                group = typename;
                break;
            }
        }

        return group ?? "其他";
    }

    public static PathIndex FromFile(string path)
        => FromLines(File.ReadLines(path));

    public bool TryGetPath(ulong hash, out string path)
    {
        if(_byHash.TryGetValue(hash, out var i))
        {
            path = _paths[i].Path;
            return true;
        }
        path = string.Empty;

        return false;
    }

    public bool TryGetByFileName(string fileName, out IReadOnlyList<string> paths)
    {
        if(_byFileName.TryGetValues(PathData.FileName(fileName), out var list))
        {
            paths = list;
            return true;
        }
        paths = [];

        return false;
    }

    public bool Contains(string path)
        => _byHash.ContainsKey(PathData.Hash(path));

    public IEnumerable<string> WithPrefix(string prefix, int limit = 100)
    {
        var prefixNormalized = PathData.Normalize(prefix);

        var lo = LowerBound(prefixNormalized);
        var count = 0;

        for(var i = lo; i < _paths.Length && count < limit; i++)
        {
            if(!_paths[i].Path.StartsWith(prefixNormalized, StringComparison.Ordinal))
                break;
            yield return _paths[i].Path;
            count++;
        }
    }

    private int LowerBound(string key)
    {
        int lo = 0, hi = _paths.Length;
        while(lo < hi)
        {
            var mid = (lo + hi) >> 1;
            if(string.CompareOrdinal(_paths[mid].Path, key) < 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        return lo;
    }

}

public sealed class PathDatabase
{
    private readonly PathIndex _modelIndex;
    private readonly PathIndex _vfxIndex;
    private readonly PathStore _pluginStore;
    private readonly PathStore _userStore;

    private readonly List<string> _modelSubtypeOptions = [];
    private readonly List<string> _modelExpansionOptions = [];

    public PathDatabase(PathIndex models, PathIndex vfx, PathStore pluginStore, PathStore userStore)
    {
        _modelIndex = models;
        _vfxIndex = vfx;
        _pluginStore = pluginStore;
        _userStore = userStore;

        _modelExpansionOptions = [.. _modelIndex.Paths.Select(m => m.Expansion).Distinct().Order()];
        _modelSubtypeOptions = [.. _modelIndex.Paths.Select(m => m.Subtype).Distinct().Order()];

    }

    public PathIndex Models => _modelIndex;
    public PathIndex Vfx => _vfxIndex;
    public PathIndex GetIndex(PathKind kind) => kind == PathKind.Vfx ? _vfxIndex : _modelIndex;

    public PathStore PluginStore => _pluginStore;
    public PathStore UserStore => _userStore;

    public List<string> ModelSubtypeOptions => _modelSubtypeOptions;
    public List<string> ModelExpansionOptions => _modelExpansionOptions;

    public PathData? GetPathDataByHash(ulong hash) =>
        _userStore.Entries.TryGetValue(hash, out var user) ? user
        : _pluginStore.Entries.TryGetValue(hash, out var plugin) ? plugin
        : null;
    public PathData? GetPathDataByPath(string path)
        => GetPathDataByHash(PathData.Hash(path));

    public IReadOnlyList<PathMeta> GetByFileName(PathKind kind, string fileName)
    {
        if(!GetIndex(kind).TryGetByFileName(fileName, out var paths))
            return [];

        var results = new List<PathMeta>(paths.Count);
        foreach(var path in paths)
        {
            var meta = GetPathDataByPath(path);
            if(meta is not null)
                results.Add(new PathMeta(path, meta));
        }
        return results;
    }
    public IReadOnlyList<string> GetPathsByFileName(PathKind kind, string fileName)
        => GetIndex(kind).TryGetByFileName(fileName, out var paths) ? paths : [];

    public void SetMetaData(string path, PathData meta)
        => SetMetaData(_userStore, path, meta);
    public bool RemoveMetaData(string path)
        => RemoveMetaData(_userStore, path);

    public void SetMetaData(PathStore store, string path, PathData meta)
    {
        meta.LastModifiedUTC = DateTime.UtcNow.Ticks;
        store.Entries[PathData.Hash(path)] = meta;
    }
    public bool RemoveMetaData(PathStore store, string path)
        => store.Entries.Remove(PathData.Hash(path));

    //

    public PathMetaExport Export()
    {
        var delta = new PathStore
        {
            KeyVersion = _userStore.KeyVersion,
        };

        foreach(var items in _userStore.Entries)
        {
            if(_pluginStore.Entries.TryGetValue(items.Key, out var pluginMeta)
                && items.Value.ContentEquals(pluginMeta))
                continue;

            delta.Entries[items.Key] = items.Value.Clone();
        }

        return new PathMetaExport
        {
            ExportedUTC = DateTime.UtcNow.Ticks,
            DB = delta,
        };
    }

    //

    public enum ConflictPolicy
    {
        LastWriteWins,
        PreferIncoming,
        PreferExisting,
    }

    public static MergeResult MergeStore(PathStore target, PathMetaExport import, ConflictPolicy policy = ConflictPolicy.LastWriteWins)
    {
        if(import.DB.KeyVersion != target.KeyVersion)
            throw new InvalidDataException(
                $"Key version mismatch: import={import.DB.KeyVersion}, target={target.KeyVersion}. ");

        var result = new MergeResult();
        foreach(var item in import.DB.Entries)
        {
            if(!target.Entries.TryGetValue(item.Key, out var existing))
            {
                target.Entries[item.Key] = item.Value.Clone();
                result.Added++;
                continue;
            }

            var take = policy switch
            {
                ConflictPolicy.PreferIncoming => true,
                ConflictPolicy.PreferExisting => false,
                _ => item.Value.LastModifiedUTC > existing.LastModifiedUTC,
            };

            if(take)
            {
                target.Entries[item.Key] = item.Value.Clone();
                result.Updated++;
            }
            else
            {
                result.Skipped++;
            }
        }
        return result;
    }

    //

    public static (PathIndex Models, PathIndex Vfx) LoadIndexes(Stream stream)
    {
        PathsFile? data = System.Text.Json.JsonSerializer.Deserialize<PathsFile>(stream, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

        if(data == null)
            return (PathIndex.FromLines([]), PathIndex.FromLines([]));

        return (PathIndex.FromLines(data.MdlPaths), PathIndex.FromLines(data.AvfxPaths));
    }

    public static PathDatabase LoadFromGz(Stream file, PathStore pluginStore, PathStore userStore)
    {
        using var decompress = new GZipStream(file, CompressionMode.Decompress);
        var (models, vfx) = LoadIndexes(decompress);
        return new PathDatabase(models, vfx, pluginStore, userStore);
    }

    //

    private static readonly MessagePackSerializerOptions SerializerOptions =
        MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

    public static byte[] Serialize<T>(T value, byte[] magic, int version)
    {
        var payload = MessagePackSerializer.Serialize(value, SerializerOptions);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(magic);
        writer.Write(version);
        writer.Write(payload.Length);
        writer.Write(payload);

        writer.Flush();
        return stream.ToArray();
    }

    public static T Deserialize<T>(byte[] bytes, byte[] magic)
    {
        if(!HasMagic(bytes, magic))
            throw new InvalidDataException("Not a valid file (bad MAGIC).");

        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);

        reader.ReadBytes(magic.Length);
        _ = reader.ReadInt32();
        var length = reader.ReadInt32();
        var payload = reader.ReadBytes(length);

        return MessagePackSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    private static bool HasMagic(byte[] data, byte[] magic)
    {
        if(data.Length < magic.Length)
            return false;

        for(int i = 0; i < magic.Length; i++)
        {
            if(data[i] != magic[i])
                return false;
        }

        return true;
    }

    public static void Save<T>(string path, T value, byte[] magic, int version)
    {
        var bytes = Serialize(value, magic, version);
        var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(dir);

        var tmp = Path.Combine(dir, Path.GetFileName(path) + ".tmp");
        File.WriteAllBytes(tmp, bytes);
        File.Move(tmp, path, overwrite: true);
    }
}

public sealed class PathsFile
{
    public List<string> MdlPaths { get; set; } = [];
    public List<string> AvfxPaths { get; set; } = [];
}

public readonly record struct PathMeta(string Path, PathData Data);

public struct MergeResult
{
    public int Added;
    public int Updated;
    public int Skipped;

    public override readonly string ToString()
        => $"added={Added}, updated={Updated}, skipped={Skipped}";
}
