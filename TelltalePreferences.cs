namespace BackToTheFutureLauncher;

internal sealed class TelltalePreferences
{
    private const ulong FloatTypeKey = 0xBAE4CBD77F139A91;
    private const ulong VolumeAmbientKey = 0x8AE2D5ACAEEB9A75;
    private const ulong VolumeMusicKey = 0xE0F0CA9C0B601F3A;
    private const ulong VolumeSoundKey = 0xF1CD8FFFFE1CB324;
    private const ulong VolumeVoiceKey = 0xD6C6E8FBC7217F7A;
    private const ulong RenderQualityKey = 0x4344F72EAEC1940E;
    private const ulong AntiAliasingKey = 0xA53A6082D030556C;
    private const ulong EnableSubtitlesKey = 0x01645B8163C2A614;
    private const ulong WindowedKey = 0x2812F4B4AC1DD8D2;
    private const ulong WindowSizeKey = 0x64CDE7932ABC79E4;
    private const ulong FullscreenSizeKey = 0xEE36F14C927951DC;

    private static readonly byte[] FileSignature = [0xAA, 0xDE, 0xAF, 0x64];
    private static readonly ulong[] RequiredKeys =
    [
        VolumeAmbientKey, VolumeMusicKey, VolumeSoundKey, RenderQualityKey,
        AntiAliasingKey, EnableSubtitlesKey, WindowedKey, WindowSizeKey, FullscreenSizeKey
    ];

    public int Width { get; set; }
    public int Height { get; set; }
    public bool Windowed { get; set; }
    public int RenderQuality { get; set; }
    public int AntiAliasingQuality { get; set; }
    public bool Subtitles { get; set; }
    public float MusicVolume { get; set; }
    public float VoiceVolume { get; set; }
    public float EffectsVolume { get; set; }
    public bool HasVoiceVolume { get; private set; }

    public static TelltalePreferences Load(string path)
    {
        byte[] data = ReadAndValidate(path);
        bool windowed = ReadBoolean(data, WindowedKey);
        (float width, float height) = ReadVector2(data, windowed ? WindowSizeKey : FullscreenSizeKey);

        bool hasVoiceVolume = TryFindValueOffset(data, VolumeVoiceKey, out int voiceOffset);
        float ambientVolume = ReadSingle(data, VolumeAmbientKey);
        float soundVolume = ReadSingle(data, VolumeSoundKey);

        return new TelltalePreferences
        {
            Width = (int)MathF.Round(width),
            Height = (int)MathF.Round(height),
            Windowed = windowed,
            RenderQuality = ReadInt32(data, RenderQualityKey),
            AntiAliasingQuality = ReadInt32(data, AntiAliasingKey),
            Subtitles = ReadBoolean(data, EnableSubtitlesKey),
            MusicVolume = ReadSingle(data, VolumeMusicKey),
            VoiceVolume = hasVoiceVolume
                ? BitConverter.ToSingle(Decode(data, voiceOffset, sizeof(float)))
                : 1F,
            EffectsVolume = (ambientVolume + soundVolume) / 2F,
            HasVoiceVolume = hasVoiceVolume
        };
    }

    public static void Validate(string path) => ReadAndValidate(path);

    public void Save(string path)
    {
        byte[] data = ReadAndValidate(path);
        if (HasVoiceVolume)
            data = WriteOrInsertSingle(data, VolumeVoiceKey, Math.Clamp(VoiceVolume, 0F, 1F));
        WriteVector2(data, WindowSizeKey, Width, Height);
        WriteVector2(data, FullscreenSizeKey, Width, Height);
        WriteBoolean(data, WindowedKey, Windowed);
        WriteInt32(data, RenderQualityKey, Math.Clamp(RenderQuality, 1, 9));
        WriteInt32(data, AntiAliasingKey, Math.Clamp(AntiAliasingQuality, 0, 3));
        WriteBoolean(data, EnableSubtitlesKey, Subtitles);
        WriteSingle(data, VolumeMusicKey, Math.Clamp(MusicVolume, 0F, 1F));
        float effectsVolume = Math.Clamp(EffectsVolume, 0F, 1F);
        WriteSingle(data, VolumeSoundKey, effectsVolume);
        WriteSingle(data, VolumeAmbientKey, effectsVolume);

        string fullPath = Path.GetFullPath(path);
        string temporaryPath = fullPath + ".tmp";
        string backupPath = fullPath + ".bak";
        try
        {
            File.WriteAllBytes(temporaryPath, data);
            File.Replace(temporaryPath, fullPath, backupPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static byte[] ReadAndValidate(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("The configured prefs.prop file was not found.", path);
        byte[] data = File.ReadAllBytes(path);
        if (data.Length < 64 || !data.AsSpan(0, 4).SequenceEqual(FileSignature))
            throw new InvalidDataException("This is not a supported Back to the Future prefs.prop file.");
        foreach (ulong key in RequiredKeys)
            FindValueOffset(data, key);
        return data;
    }

    private static int ReadInt32(byte[] data, ulong key) =>
        BitConverter.ToInt32(Decode(data, FindValueOffset(data, key), sizeof(int)));

    private static float ReadSingle(byte[] data, ulong key) =>
        BitConverter.ToSingle(Decode(data, FindValueOffset(data, key), sizeof(float)));

    private static bool ReadBoolean(byte[] data, ulong key)
    {
        byte value = (byte)(data[FindValueOffset(data, key)] ^ 0xFF);
        return value switch
        {
            (byte)'0' => false,
            (byte)'1' => true,
            _ => throw new InvalidDataException("A preference contains an unsupported Boolean value.")
        };
    }

    private static (float X, float Y) ReadVector2(byte[] data, ulong key)
    {
        int offset = FindValueOffset(data, key);
        return (
            BitConverter.ToSingle(Decode(data, offset, sizeof(float))),
            BitConverter.ToSingle(Decode(data, offset + sizeof(float), sizeof(float))));
    }

    private static void WriteInt32(byte[] data, ulong key, int value) =>
        EncodeInto(data, FindValueOffset(data, key), BitConverter.GetBytes(value));

    private static void WriteSingle(byte[] data, ulong key, float value) =>
        EncodeInto(data, FindValueOffset(data, key), BitConverter.GetBytes(value));

    private static byte[] WriteOrInsertSingle(byte[] data, ulong key, float value)
    {
        if (TryFindValueOffset(data, key, out int existingOffset))
        {
            EncodeInto(data, existingOffset, BitConverter.GetBytes(value));
            return data;
        }

        int groupOffset = FindValueOffset(data, FloatTypeKey) - 12;
        int countOffset = groupOffset + 12;
        int count = BitConverter.ToInt32(Decode(data, countOffset, sizeof(int)));
        int entriesOffset = countOffset + sizeof(int);
        int insertionOffset = entriesOffset + count * 16;
        for (int index = 0; index < count; index++)
        {
            int entryOffset = entriesOffset + index * 16;
            ulong existingKey = ~BitConverter.ToUInt64(data, entryOffset);
            if (existingKey > key)
            {
                insertionOffset = entryOffset;
                break;
            }
        }

        var expanded = new byte[data.Length + 16];
        Buffer.BlockCopy(data, 0, expanded, 0, insertionOffset);
        Buffer.BlockCopy(data, insertionOffset, expanded, insertionOffset + 16,
            data.Length - insertionOffset);
        Buffer.BlockCopy(BitConverter.GetBytes(~key), 0, expanded, insertionOffset, 8);
        expanded.AsSpan(insertionOffset + 8, 4).Fill(0xFF);
        EncodeInto(expanded, insertionOffset + 12, BitConverter.GetBytes(value));
        EncodeInto(expanded, countOffset, BitConverter.GetBytes(count + 1));

        int propertyBlockSizeOffset = groupOffset - 8;
        int propertyBlockSize = BitConverter.ToInt32(
            Decode(expanded, propertyBlockSizeOffset, sizeof(int)));
        EncodeInto(expanded, propertyBlockSizeOffset, BitConverter.GetBytes(propertyBlockSize + 16));
        return expanded;
    }

    private static void WriteBoolean(byte[] data, ulong key, bool value) =>
        data[FindValueOffset(data, key)] = (byte)((value ? (byte)'1' : (byte)'0') ^ 0xFF);

    private static void WriteVector2(byte[] data, ulong key, float x, float y)
    {
        int offset = FindValueOffset(data, key);
        EncodeInto(data, offset, BitConverter.GetBytes(x));
        EncodeInto(data, offset + sizeof(float), BitConverter.GetBytes(y));
    }

    private static byte[] Decode(byte[] data, int offset, int length)
    {
        var result = new byte[length];
        for (int index = 0; index < length; index++)
            result[index] = (byte)(data[offset + index] ^ 0xFF);
        return result;
    }

    private static void EncodeInto(byte[] destination, int offset, byte[] value)
    {
        for (int index = 0; index < value.Length; index++)
            destination[offset + index] = (byte)(value[index] ^ 0xFF);
    }

    private static int FindValueOffset(byte[] data, ulong key)
    {
        if (TryFindValueOffset(data, key, out int valueOffset))
            return valueOffset;
        throw new InvalidDataException("A required game setting is missing from prefs.prop.");
    }

    private static bool TryFindValueOffset(byte[] data, ulong key, out int valueOffset)
    {
        byte[] encodedKey = BitConverter.GetBytes(~key);
        int match = -1;
        for (int offset = 0; offset <= data.Length - encodedKey.Length; offset++)
        {
            if (!data.AsSpan(offset, encodedKey.Length).SequenceEqual(encodedKey))
                continue;
            if (match >= 0)
                throw new InvalidDataException("A preference key occurs more than once in prefs.prop.");
            match = offset;
        }

        if (match < 0)
        {
            valueOffset = -1;
            return false;
        }
        if (match + 12 >= data.Length)
            throw new InvalidDataException("A game setting has an incomplete binary layout.");
        if (!data.AsSpan(match + 8, 4).SequenceEqual(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }))
            throw new InvalidDataException("A game setting has an unexpected binary layout.");
        valueOffset = match + 12;
        return true;
    }
}
