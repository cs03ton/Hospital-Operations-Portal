using Hop.Api.Services;
using Xunit;

namespace Hop.Api.Tests;

public class LeavePdfTests
{
    [Fact]
    public void SimplePdfWriter_CreateA4_ReturnsPdfBytes()
    {
        var bytes = SimplePdfWriter.CreateA4(
            [new PdfLine("แบบฟอร์มใบลา", 50, 790, 18)],
            logo: null);

        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
