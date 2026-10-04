using ReciteWords.Platform;
namespace ReciteWords.Tests;
internal static partial class Program
{
    static void WindowTests()
    {
        Check("70 percent screen and working height at 100 percent", () => {
            var size = WindowBounds.CalculateLimits(1920, 1040, 96); Equal(1344.0, size.Width); Equal(1040.0, size.Height);
        });
        Check("physical limits convert at 150 percent", () => {
            var size = WindowBounds.CalculateLimits(1920, 1040, 144); Equal(896.0, size.Width); Equal(true, Math.Abs(size.Height - 693.33333333333) < 0.001);
        });
    }
}
