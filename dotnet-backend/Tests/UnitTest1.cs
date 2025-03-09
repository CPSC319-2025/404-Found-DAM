using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void TestAddition_ReturnsCorrectSum()
        {
            // Arrange: set up values to add
            int a = 2;
            int b = 2;
            int expected = 4;

            // Act: perform the addition
            int actual = a + b;

            // Assert: verify that the result matches the expectation
            Assert.Equal(expected, actual);
        }
    }
}
