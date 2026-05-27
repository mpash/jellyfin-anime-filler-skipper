using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using Jellyfin.Plugin.AnimeFillerSkipper.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Tests.Services;

public class UserSegmentPreferenceServiceTests
{
    [Fact]
    public void SetUnknownSegmentAction_PreservesExistingCustomPreferences()
    {
        var user = CreateUser("alice");
        var prefs = new Dictionary<string, string?> { ["theme"] = "dark" };
        var userManager = CreateUserManager(user);
        var displayPreferencesManager = new Mock<IDisplayPreferencesManager>();
        Dictionary<string, string?>? savedPrefs = null;

        displayPreferencesManager
            .Setup(m => m.ListCustomItemDisplayPreferences(
                user.Id,
                It.IsAny<Guid>(),
                UserSegmentPreferenceService.ClientId))
            .Returns(prefs);

        displayPreferencesManager
            .Setup(m => m.SetCustomItemDisplayPreferences(
                user.Id,
                It.IsAny<Guid>(),
                UserSegmentPreferenceService.ClientId,
                It.IsAny<Dictionary<string, string?>>()))
            .Callback<Guid, Guid, string, Dictionary<string, string?>>((_, _, _, values) => savedPrefs = values);

        var service = new UserSegmentPreferenceService(userManager.Object, displayPreferencesManager.Object);

        var result = service.SetUnknownSegmentAction(null, UserSegmentPreferenceActions.AskToSkip);

        Assert.Equal(1, result.UpdatedCount);
        Assert.NotNull(savedPrefs);
        Assert.Equal("dark", savedPrefs!["theme"]);
        Assert.Equal(UserSegmentPreferenceActions.AskToSkip, savedPrefs[UserSegmentPreferenceService.UnknownSegmentActionKey]);
    }

    [Fact]
    public void SetUnknownSegmentAction_UpdatesOnlySelectedUsers()
    {
        var alice = CreateUser("alice");
        var bob = CreateUser("bob");
        var userManager = CreateUserManager(alice, bob);
        var displayPreferencesManager = new Mock<IDisplayPreferencesManager>();

        displayPreferencesManager
            .Setup(m => m.ListCustomItemDisplayPreferences(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(new Dictionary<string, string?>());

        var service = new UserSegmentPreferenceService(userManager.Object, displayPreferencesManager.Object);

        var result = service.SetUnknownSegmentAction(
            new[] { bob.Id },
            UserSegmentPreferenceActions.Skip);

        Assert.Equal(1, result.UpdatedCount);
        displayPreferencesManager.Verify(
            m => m.SetCustomItemDisplayPreferences(
                bob.Id,
                It.IsAny<Guid>(),
                UserSegmentPreferenceService.ClientId,
                It.IsAny<Dictionary<string, string?>>()),
            Times.Once);
        displayPreferencesManager.Verify(
            m => m.SetCustomItemDisplayPreferences(
                alice.Id,
                It.IsAny<Guid>(),
                UserSegmentPreferenceService.ClientId,
                It.IsAny<Dictionary<string, string?>>()),
            Times.Never);
    }

    [Fact]
    public void SetUnknownSegmentAction_InvalidAction_Throws()
    {
        var userManager = CreateUserManager(CreateUser("alice"));
        var displayPreferencesManager = new Mock<IDisplayPreferencesManager>();
        var service = new UserSegmentPreferenceService(userManager.Object, displayPreferencesManager.Object);

        Assert.Throws<ArgumentException>(() => service.SetUnknownSegmentAction(null, "BadAction"));
    }

    [Fact]
    public void GetUnknownSegmentActionStatus_ReturnsNoneWhenPreferenceMissing()
    {
        var user = CreateUser("alice");
        var userManager = CreateUserManager(user);
        var displayPreferencesManager = new Mock<IDisplayPreferencesManager>();

        displayPreferencesManager
            .Setup(m => m.ListCustomItemDisplayPreferences(
                user.Id,
                It.IsAny<Guid>(),
                UserSegmentPreferenceService.ClientId))
            .Returns(new Dictionary<string, string?>());

        var service = new UserSegmentPreferenceService(userManager.Object, displayPreferencesManager.Object);

        var result = service.GetUnknownSegmentActionStatus();

        Assert.Single(result.Users);
        Assert.Equal("alice", result.Users[0].Name);
        Assert.Equal(UserSegmentPreferenceActions.None, result.Users[0].Action);
    }

    [Fact]
    public void GetUnknownSegmentActionStatus_FallsBackToUserIds()
    {
        var user = CreateUser("alice");
        var userManager = CreateUserManager();
        userManager.Setup(m => m.UsersIds).Returns(new[] { user.Id });
        userManager.Setup(m => m.GetUserById(user.Id)).Returns(user);

        var displayPreferencesManager = new Mock<IDisplayPreferencesManager>();
        displayPreferencesManager
            .Setup(m => m.ListCustomItemDisplayPreferences(
                user.Id,
                It.IsAny<Guid>(),
                UserSegmentPreferenceService.ClientId))
            .Returns(new Dictionary<string, string?>());

        var service = new UserSegmentPreferenceService(userManager.Object, displayPreferencesManager.Object);

        var result = service.GetUnknownSegmentActionStatus();

        Assert.Single(result.Users);
        Assert.Equal("alice", result.Users[0].Name);
    }

    private static User CreateUser(string username)
    {
        return new User(username, "auth", "reset") { Id = Guid.NewGuid() };
    }

    private static Mock<IUserManager> CreateUserManager(params User[] users)
    {
        var userManager = new Mock<IUserManager>();
        userManager.Setup(m => m.Users).Returns(users);
        userManager.Setup(m => m.UsersIds).Returns(users.Select(user => user.Id));
        return userManager;
    }
}
