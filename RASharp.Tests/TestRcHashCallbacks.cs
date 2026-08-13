// Tests for the deprecated global RcHash APIs: the message callbacks and
// the default-cdreader getter. The callbacks are global state, so every
// test resets them in a finally block; the mock filereader keeps the
// hashing paths hermetic.

using RASharp.Core;
using RASharp.Core.Models;

namespace RASharp.Tests;

/// <summary>Tests for the deprecated global RcHash APIs: the message callbacks and the default-cdreader getter. The callbacks are global state, so every test resets them in a fin</summary>
public class TestRcHashCallbacks
{
    public TestRcHashCallbacks()
    {
        MockFilereader.InitMockFilereader();
    }

    /// <summary>Tests that the deprecated error callback fires on a failed hash.</summary>
    [Fact]
    public void ErrorCallbackFiresOnFailure()
    {
        var messages = new List<string>();
        RcHash.InitErrorMessageCallback(message => messages.Add(message));
        try
        {
            /* not mocked -> the mock filereader cannot open it */
            Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RcConsolePc8800, "missing.bin"));
        }
        finally
        {
            RcHash.InitErrorMessageCallback(null);
        }

        Assert.NotEmpty(messages);
        Assert.Contains(messages, message => !string.IsNullOrEmpty(message));
    }

    /// <summary>Tests that the deprecated verbose callback fires on a successful hash.</summary>
    [Fact]
    public void VerboseCallbackFiresOnSuccess()
    {
        var image = TestDataGen.GenerateGenericFile(131072);
        MockFilereader.MockFile(0, "test.bin", image, image.Length);

        var messages = new List<string>();
        RcHash.InitVerboseMessageCallback(message => messages.Add(message));
        try
        {
            Assert.True(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsolePc8800, "test.bin"));
            Assert.Equal(32, hash.Length);
        }
        finally
        {
            RcHash.InitVerboseMessageCallback(null);
        }

        Assert.Contains(messages, message => message.Contains("Generated hash", StringComparison.Ordinal));
        Assert.Contains(messages, message => message.Contains("test.bin", StringComparison.Ordinal));
    }

    /// <summary>Tests that the callbacks are independent channels.</summary>
    [Fact]
    public void ErrorCallbackDoesNotReceiveVerboseMessages()
    {
        var image = TestDataGen.GenerateGenericFile(131072);
        MockFilereader.MockFile(0, "test.bin", image, image.Length);

        var errors = new List<string>();
        RcHash.InitErrorMessageCallback(message => errors.Add(message));
        RcHash.InitVerboseMessageCallback(_ => { });
        try
        {
            Assert.True(RcHash.GenerateFromFile(out _, ConsoleIds.RcConsolePc8800, "test.bin"));
        }
        finally
        {
            RcHash.InitErrorMessageCallback(null);
            RcHash.InitVerboseMessageCallback(null);
        }

        Assert.Empty(errors);
    }

    /// <summary>Tests that GetDefaultCdreader fills the supported handler slots.</summary>
    [Fact]
    public void GetDefaultCdreaderFillsSupportedHandlers()
    {
        var cdreader = new RcHashCdreader();

        RcHash.GetDefaultCdreader(cdreader);

        /* the legacy OpenTrack API is deliberately unsupported (null) */
        Assert.Null(cdreader.OpenTrack);
        Assert.NotNull(cdreader.ReadSector);
        Assert.NotNull(cdreader.CloseTrack);
        Assert.NotNull(cdreader.FirstTrackSector);
        Assert.NotNull(cdreader.OpenTrackIterator);
    }
}
