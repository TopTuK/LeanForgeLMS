using System.Text;
using LF.WebApi.Common;

namespace LF.WebApiTests.Common;

public class CsvWriterTests
{
    private static string Render(byte[] bytes)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        Assert.True(bytes.AsSpan(0, preamble.Length).SequenceEqual(preamble), "expected a UTF-8 BOM");
        return Encoding.UTF8.GetString(bytes, preamble.Length, bytes.Length - preamble.Length);
    }

    [Fact]
    public void ToCsvBytes_WritesHeaderAndRows_SemicolonDelimited()
    {
        var csv = Render(CsvWriter.ToCsvBytes(["A", "B"], [["1", "2"], ["3", "4"]]));

        Assert.Equal("A;B\r\n1;2\r\n3;4\r\n", csv);
    }

    [Fact]
    public void ToCsvBytes_QuotesFieldsContainingDelimiterQuoteOrNewline()
    {
        var csv = Render(CsvWriter.ToCsvBytes(
            ["Name", "Note"],
            [["Doe; Jane", "She said \"hi\""], ["multi\nline", "plain"]]));

        Assert.Equal("Name;Note\r\n\"Doe; Jane\";\"She said \"\"hi\"\"\"\r\n\"multi\nline\";plain\r\n", csv);
    }
}
