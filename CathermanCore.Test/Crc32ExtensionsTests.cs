using System.Text;

namespace CathermanCore.Test;

public class Crc32ExtensionsTests
{
    // ── Determinism ──────────────────────────────────────────────────────────

    [Fact]
    public void ToCrc32Int_String_SameInputSameOutput()
    {
        var first = "Hello, World!".ToCrc32Int();
        var second = "Hello, World!".ToCrc32Int();
        Assert.Equal(first, second);
    }

    [Fact]
    public void ToCrc32Int_DifferentInputs_DifferentOutputs()
    {
        Assert.NotEqual("Hello".ToCrc32Int(), "World".ToCrc32Int());
    }

    [Fact]
    public void ToCrc32Int_IsCaseSensitive()
    {
        Assert.NotEqual("hello".ToCrc32Int(), "HELLO".ToCrc32Int());
    }

    // ── Cross-overload consistency ───────────────────────────────────────────

    [Fact]
    public void ToCrc32Int_StringAndByteArray_ReturnSameValue()
    {
        var str = "For God so loved the world";
        var bytes = Encoding.UTF8.GetBytes(str);

        Assert.Equal(str.ToCrc32Int(), bytes.ToCrc32Int());
    }

    [Fact]
    public void ToCrc32Int_ByteArrayAndSpan_ReturnSameValue()
    {
        var bytes = Encoding.UTF8.GetBytes("John 3:16");
        var span = new ReadOnlySpan<byte>(bytes);

        Assert.Equal(bytes.ToCrc32Int(), span.ToCrc32Int());
    }

    // ── Empty input ───────────────────────────────────────────────────────────

    [Fact]
    public void ToCrc32Int_EmptyString_ReturnsZero()
    {
        // Empty input: CRC starts and ends at 0xFFFFFFFF, ~0xFFFFFFFF = 0
        Assert.Equal(0, "".ToCrc32Int());
    }

    [Fact]
    public void ToCrc32Int_EmptyByteArray_ReturnsZero()
    {
        Assert.Equal(0, Array.Empty<byte>().ToCrc32Int());
    }

    [Fact]
    public void ToCrc32Int_EmptySpan_ReturnsZero()
    {
        Assert.Equal(0, ReadOnlySpan<byte>.Empty.ToCrc32Int());
    }

    // ── Known test vector ─────────────────────────────────────────────────────

    [Fact]
    public void ToCrc32Int_KnownVector_ReturnsExpectedValue()
    {
        // Standard IEEE 802.3 CRC32 of "123456789" = 0xCBF43926
        // As a signed 32-bit two's complement integer: 0xCBF43926 = -873187034
        var result = "123456789".ToCrc32Int();

        Assert.Equal(-873187034, result);
        // Alternatively: Assert.Equal(unchecked((int)0xCBF43926), result);
    }
    // ── Stability (runtime-session invariant) ─────────────────────────────────

    [Fact]
    public void ToCrc32Int_StableAcrossMultipleCalls()
    {
        // Unlike string.GetHashCode(), CRC32 is stable across process restarts
        // Validate that multiple calls in the same session return the same value
        const string input = "genesis 1:1";
        var results = Enumerable.Range(0, 10).Select(_ => input.ToCrc32Int()).Distinct();
        Assert.Single(results);
    }

    // ── Unicode support ───────────────────────────────────────────────────────

    [Fact]
    public void ToCrc32Int_UnicodeString_IsDeterministic()
    {
        var greek = "λόγος"; // Greek for "word/logos"
        Assert.Equal(greek.ToCrc32Int(), greek.ToCrc32Int());
    }

    [Fact]
    public void ToCrc32Int_UnicodeAndAscii_ProduceDifferentValues()
    {
        Assert.NotEqual("logos".ToCrc32Int(), "λόγος".ToCrc32Int());
    }

    // ── Long strings (stackalloc threshold) ──────────────────────────────────

    [Fact]
    public void ToCrc32Int_StringOver256Bytes_StillConsistent()
    {
        // Forces the heap allocation path (byteCount > 256)
        var longStr = new string('a', 300);
        Assert.Equal(longStr.ToCrc32Int(), longStr.ToCrc32Int());
    }

    [Fact]
    public void ToCrc32Int_StringAt256Bytes_SameResultAsOver256()
    {
        // Boundary: 256 chars ASCII = exactly 256 bytes (stackalloc path)
        // 257 chars = heap path — both should produce consistent distinct results
        var at256 = new string('b', 256);
        var over256 = new string('b', 257);
        Assert.NotEqual(at256.ToCrc32Int(), over256.ToCrc32Int());
    }
}