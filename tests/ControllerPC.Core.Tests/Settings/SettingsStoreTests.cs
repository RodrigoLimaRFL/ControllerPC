using ControllerPC.Core.Controller;
using ControllerPC.Core.Output;
using ControllerPC.Core.Profiles;
using ControllerPC.Core.Settings;

namespace ControllerPC.Core.Tests.Settings;

public sealed class SettingsStoreTests : IDisposable
{
    private const string CorruptContent = "{ this is not json";

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "ControllerPC.Tests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaultsWithoutBackup()
    {
        var result = new SettingsStore(FilePath).Load();

        Assert.Null(result.CorruptFileBackupPath);
        Assert.Equal(DefaultJson(), WithoutIds(result.Settings));
    }

    [Fact]
    public void Load_MissingFile_DoesNotCreateFile()
    {
        new SettingsStore(FilePath).Load();

        Assert.False(File.Exists(FilePath));
    }

    [Fact]
    public void Load_SavedSettings_ReturnsThem()
    {
        var settings = DefaultSettings.Create();
        settings.ActiveProfile.Bindings[ControllerButton.X] = new MouseButtonAction(MouseButton.Middle);
        var store = new SettingsStore(FilePath);
        store.Save(settings);

        var result = store.Load();

        Assert.Null(result.CorruptFileBackupPath);
        Assert.Equal(SettingsSerializer.Serialize(settings), SettingsSerializer.Serialize(result.Settings));
    }

    [Fact]
    public void Load_CorruptFile_ReturnsDefaultsAndBackupPath()
    {
        WriteSettingsFile(CorruptContent);

        var result = new SettingsStore(FilePath).Load();

        Assert.NotNull(result.CorruptFileBackupPath);
        Assert.Equal(DefaultJson(), WithoutIds(result.Settings));
    }

    [Fact]
    public void Load_CorruptFile_MovesItToTimestampedBadBackup()
    {
        WriteSettingsFile(CorruptContent);

        var backupPath = new SettingsStore(FilePath).Load().CorruptFileBackupPath!;

        Assert.False(File.Exists(FilePath));
        Assert.Equal(_directory, Path.GetDirectoryName(backupPath));
        Assert.Matches(@"^settings\.json\.\d{8}-\d{6}\.bad$", Path.GetFileName(backupPath));
        Assert.Equal(CorruptContent, File.ReadAllText(backupPath));
    }

    [Fact]
    public void Load_CorruptFileTwice_KeepsBothBackups()
    {
        var store = new SettingsStore(FilePath);
        WriteSettingsFile(CorruptContent);
        var firstBackup = store.Load().CorruptFileBackupPath;
        WriteSettingsFile("second corrupt file");

        var secondBackup = store.Load().CorruptFileBackupPath;

        Assert.NotEqual(firstBackup, secondBackup);
        Assert.Equal(CorruptContent, File.ReadAllText(firstBackup!));
        Assert.Equal("second corrupt file", File.ReadAllText(secondBackup!));
    }

    [Fact]
    public void Load_FileWithValidationErrors_MovesItToBackup()
    {
        var settings = DefaultSettings.Create();
        settings.BaseEntryTrigger.Steps.Clear();
        WriteSettingsFile(SettingsSerializer.Serialize(settings));

        var result = new SettingsStore(FilePath).Load();

        Assert.NotNull(result.CorruptFileBackupPath);
        Assert.False(File.Exists(FilePath));
        Assert.NotEmpty(result.Settings.BaseEntryTrigger.Steps);
    }

    [Fact]
    public void Load_FileWithOnlyWarnings_IsKept()
    {
        var settings = DefaultSettings.Create();
        settings.ActiveProfile.Bindings[ControllerButton.X] = new SwitchToProfileAction(Guid.NewGuid());
        WriteSettingsFile(SettingsSerializer.Serialize(settings));

        var result = new SettingsStore(FilePath).Load();

        Assert.Null(result.CorruptFileBackupPath);
        Assert.True(File.Exists(FilePath));
        Assert.IsType<SwitchToProfileAction>(result.Settings.ActiveProfile.Bindings[ControllerButton.X]);
    }

    [Fact]
    public void Load_FileWithNewerVersion_MovesItToBackup()
    {
        var settings = DefaultSettings.Create();
        settings.Version = AppSettings.CurrentVersion + 1;
        WriteSettingsFile(SettingsSerializer.Serialize(settings));

        var result = new SettingsStore(FilePath).Load();

        Assert.NotNull(result.CorruptFileBackupPath);
        Assert.False(File.Exists(FilePath));
        Assert.Equal(AppSettings.CurrentVersion, result.Settings.Version);
    }

    [Fact]
    public void Save_MissingDirectory_CreatesIt()
    {
        var nestedPath = Path.Combine(_directory, "nested", "deeper", "settings.json");

        new SettingsStore(nestedPath).Save(DefaultSettings.Create());

        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public void Save_WritesSerializedSettings()
    {
        var settings = DefaultSettings.Create();

        new SettingsStore(FilePath).Save(settings);

        Assert.Equal(SettingsSerializer.Serialize(settings), File.ReadAllText(FilePath));
    }

    [Fact]
    public void Save_ExistingFile_ReplacesIt()
    {
        var store = new SettingsStore(FilePath);
        store.Save(DefaultSettings.Create());
        var updated = DefaultSettings.Create();
        updated.ActiveProfile.Name = "Updated";

        store.Save(updated);

        Assert.Equal("Updated", store.Load().Settings.ActiveProfile.Name);
    }

    [Fact]
    public void Save_LeavesOnlyTheSettingsFile()
    {
        var store = new SettingsStore(FilePath);
        store.Save(DefaultSettings.Create());

        store.Save(DefaultSettings.Create());

        Assert.Equal([FilePath], Directory.GetFiles(_directory));
    }

    [Fact]
    public void Save_SettingsWithValidationErrors_ThrowsAndWritesNothing()
    {
        var invalid = DefaultSettings.Create();
        invalid.Profiles.Clear();

        Assert.Throws<InvalidOperationException>(() => new SettingsStore(FilePath).Save(invalid));
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void Save_SettingsWithValidationErrors_LeavesExistingFileUntouched()
    {
        var store = new SettingsStore(FilePath);
        store.Save(DefaultSettings.Create());
        var original = File.ReadAllText(FilePath);
        var invalid = DefaultSettings.Create();
        invalid.BaseExitTrigger.Steps.Clear();

        Assert.Throws<InvalidOperationException>(() => store.Save(invalid));
        Assert.Equal(original, File.ReadAllText(FilePath));
    }

    [Fact]
    public void Save_SettingsWithOnlyWarnings_Succeeds()
    {
        var settings = DefaultSettings.Create();
        settings.ActiveProfile.Bindings[ControllerButton.X] = new SwitchToProfileAction(Guid.NewGuid());

        new SettingsStore(FilePath).Save(settings);

        Assert.True(File.Exists(FilePath));
    }

    [Fact]
    public void Save_TemporaryFileCannotBeWritten_ThrowsAndLeavesOriginalUntouched()
    {
        var store = new SettingsStore(FilePath);
        store.Save(DefaultSettings.Create());
        var original = File.ReadAllText(FilePath);
        // A directory in the temporary file's place makes both the write and the cleanup fail.
        Directory.CreateDirectory(FilePath + ".tmp");

        Assert.Throws<UnauthorizedAccessException>(() => store.Save(DefaultSettings.Create()));
        Assert.Equal(original, File.ReadAllText(FilePath));
    }

    [Fact]
    public void Save_SettingsFileLocked_ThrowsAndRemovesTemporaryFile()
    {
        var store = new SettingsStore(FilePath);
        store.Save(DefaultSettings.Create());

        using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsAny<Exception>(() => store.Save(DefaultSettings.Create()));
        }

        Assert.Equal([FilePath], Directory.GetFiles(_directory));
    }

    [Fact]
    public void FilePath_IsTheConstructorArgument()
    {
        Assert.Equal(FilePath, new SettingsStore(FilePath).FilePath);
    }

    private void WriteSettingsFile(string contents)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(FilePath, contents);
    }

    private static string DefaultJson() => WithoutIds(DefaultSettings.Create());

    // Each fresh default Profile gets a new id, so compare defaults with the ids blanked out.
    private static string WithoutIds(AppSettings settings)
    {
        var copy = SettingsSerializer.Clone(settings);
        copy.ActiveProfileId = Guid.Empty;
        copy.Profiles.ForEach(profile => profile.Id = Guid.Empty);
        return SettingsSerializer.Serialize(copy);
    }
}
