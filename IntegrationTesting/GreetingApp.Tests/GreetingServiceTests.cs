using GreetingApp;

namespace GreetingApp.Tests;

public sealed class GreetingServiceTests
{
    [Fact]
    public void CreateGreeting_ReturnsExpectedMessage()
    {
        var greeting = GreetingService.CreateGreeting();

        Assert.Equal(
            """
            Any-anytime
            I would do-do-do-do-do-do-do-do-do-do-do
            Time away
            Yesterday-day-day-day-day
            Any-anytime
            I would do-do-do-do-do-do-do-do-do-do-do
            Time away
            Yesterday-day-day-day-day
            """,
            greeting);
    }
}
