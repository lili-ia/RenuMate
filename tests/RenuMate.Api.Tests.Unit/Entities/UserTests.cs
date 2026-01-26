using FluentAssertions;
using RenuMate.Api.Entities;
using RenuMate.Api.Exceptions;

namespace RenuMate.Api.Tests.Unit.Entities;

public class UserTests
{
    [Fact]
    public void Create_ValidParameters_ReturnsUserWithCorrectProperties()
    {
        // Arrange
        var auth0Id = "auth0|67890";
        var email = "test@renumate.com";
        var name = "John Doe";
        
        // Act
        var user = User.Create(auth0Id, email, name);
        
        // Assert
        user.Id.Should().NotBeEmpty();
        user.Auth0Id.Should().Be(auth0Id);
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.EmailConfirmed.Should().Be(false);
        user.IsActive.Should().Be(true);
        user.IsMetadataSynced.Should().Be(false);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_Auth0IdIsEmpty_ThrowsDomainValidationException(string auth0Id)
    {
        // Arrange
        var email = "test@renumate.com";
        var name = "John Doe";
        
        // Act
        var action = () => User.Create(auth0Id, email, name);
        
        // Assert
        action.Should().Throw<DomainValidationException>()
            .WithMessage("Auth0Id is required.");
    }
    
    [Theory]
    [InlineData(null, "Email is required.")]
    [InlineData("", "Email is required.")]
    [InlineData(" ", "Email is required.")]
    [InlineData("plainemailaddress", "Invalid email format.")]
    public void Create_EmailIsEmptyOrInvalid_ThrowsDomainValidationException(string email, string expectedMessage)
    {
        // Arrange
        var auth0Id = "auth0|123";
        var name = "John Doe";

        // Act
        var action = () => User.Create(auth0Id, email, name);

        // Assert
        action.Should().Throw<DomainValidationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void UpdateProfile_DataChanged_ReturnsTrueAndSetsIsMetadataSyncedToFalse()
    {
        // Arrange
        var user = User.Create("auth0|1", "old@test.com", "Old Name");

        // Act
        var result = user.UpdateProfile("new@test.com", "New Name", false);

        // Assert
        result.Should().BeTrue();
        user.Email.Should().Be("new@test.com");
        user.IsMetadataSynced.Should().BeFalse();
    }

    [Fact]
    public void UpdateProfile_DataIdentical_ReturnsFalse()
    {
        // Arrange
        var user = User.Create("auth0|1", "test@test.com", "Name");
        user.MarkMetadataAsSynced();

        // Act
        var result = user.UpdateProfile("test@test.com", "Name", false);

        // Assert
        result.Should().BeFalse();
        user.IsMetadataSynced.Should().BeTrue(); 
    }
}