using GdUnit4;
using MysticClue.Chroma.GodotClient.Audio;
using System;
using System.IO;
using static GdUnit4.Assertions;

namespace MysticClue.Chroma.GodotClient.Tests.Audio;

[TestSuite]
public class SoundFilesTest
{
    [TestCase]
    public static void TestSoundFiles()
    {
        // Check that all defined sounds map to a file path, and the file exists.
        foreach (Sounds s in Enum.GetValues(typeof(Sounds)))
        {
            AssertThat(SoundFiles.Path.Keys).Contains(s);
            AssertThat(File.Exists("Audio Files/" + SoundFiles.Path[s])).IsTrue();
        }
    }
}
