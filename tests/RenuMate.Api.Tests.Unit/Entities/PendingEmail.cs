using FluentAssertions;
using RenuMate.Api.Entities;

namespace RenuMate.Api.Tests.Unit.Entities
{
    public class PendingEmailTests
    {
        [Fact]
        public void Create_ShouldInitializePropertiesCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            
            // Act
            var email = PendingEmail.Create(
                to: "test@example.com",
                subject: "Hello",
                body: "Body",
                now: now,
                lastError: null
            );

            // Assert
            email.To.Should().Be("test@example.com");
            email.Subject.Should().Be("Hello");
            email.Body.Should().Be("Body");
            email.IsSent.Should().BeFalse();
            email.RetryCount.Should().Be(0);
            email.MaxRetries.Should().Be(5);
            email.LastAttemptAt.Should().BeNull();
            email.CreatedAt.Should().Be(now);
            email.LastError.Should().BeNull();
        }

        [Fact]
        public void MarkSent_ShouldSetIsSentAndLastAttemptAt()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var email = PendingEmail.Create("a@b.com", "subj", "body", now, null);

            var markTime = DateTime.UtcNow.AddMinutes(1);
            
            // Act
            email.MarkSent(markTime);

            // Assert
            email.IsSent.Should().BeTrue();
            email.LastAttemptAt.Should().Be(markTime);
            email.LastError.Should().BeNull();
        }

        [Fact]
        public void RegisterFailure_ShouldIncrementRetryAndSetErrorAndLastAttemptAt()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var email = PendingEmail.Create("a@b.com", "subj", "body", now, null);
            
            var failTime = DateTime.UtcNow.AddMinutes(2);
            
            // Act
            email.RegisterFailure("SMTP failed", failTime);

            // Assert
            email.RetryCount.Should().Be(1);
            email.LastAttemptAt.Should().Be(failTime);
            email.LastError.Should().Be("SMTP failed");
        }

        [Fact]
        public void MultipleFailures_ShouldIncrementRetryCountCorrectly()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var email = PendingEmail.Create("a@b.com", "subj", "body", now, null);

            // Act
            email.RegisterFailure("First failure", DateTime.UtcNow);
            email.RegisterFailure("Second failure", DateTime.UtcNow.AddMinutes(1));

            // Assert
            email.RetryCount.Should().Be(2);
            email.LastError.Should().Be("Second failure");
        }

        [Fact]
        public void CannotExceedMaxRetries()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var email = PendingEmail.Create("a@b.com", "subj", "body", now, null);
        
            // Act
            for (int i = 0; i < 10; i++)
            {
                email.RegisterFailure($"Fail {i}", DateTime.UtcNow.AddMinutes(i));
            }
        
            // Assert
            email.RetryCount.Should().Be(5); 
            email.MaxRetries.Should().Be(5);  
            email.CanRetry().Should().BeFalse();
        }
        
        [Fact]
        public void CanRetry_ShouldReturnCorrectValue()
        {
            // Arrange
            var email = PendingEmail.Create("a@b.com", "subj", "body", DateTime.UtcNow, null);
        
            email.CanRetry().Should().BeTrue();
        
            // Act
            for (int i = 0; i < 5; i++)
            {
                email.RegisterFailure($"Fail {i}", DateTime.UtcNow);
            }
        
            // Assert
            email.CanRetry().Should().BeFalse();
            email.MarkSent(DateTime.UtcNow);
            email.CanRetry().Should().BeFalse();
        }
    }
}
