using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.AnimeFillerSkipper.Models;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.AnimeFillerSkipper.Services;

public class UserSegmentPreferenceService : IUserSegmentPreferenceService
{
    internal const string ClientId = "emby";
    internal const string DisplayPreferencesId = "usersettings";
    internal const string UnknownSegmentActionKey = "segmentTypeAction__Unknown";

    private static readonly Guid UserSettingsItemId = DisplayPreferencesId.GetMD5();

    private readonly IUserManager _userManager;
    private readonly IDisplayPreferencesManager _displayPreferencesManager;

    public UserSegmentPreferenceService(
        IUserManager userManager,
        IDisplayPreferencesManager displayPreferencesManager)
    {
        _userManager = userManager;
        _displayPreferencesManager = displayPreferencesManager;
    }

    public SegmentPreferenceStatusDto GetUnknownSegmentActionStatus()
    {
        var users = GetUsers()
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Select(user =>
            {
                var prefs = GetCustomPreferences(user.Id);

                return new SegmentPreferenceUserDto
                {
                    Id = user.Id,
                    Name = user.Username,
                    Action = prefs.TryGetValue(UnknownSegmentActionKey, out var action)
                        && !string.IsNullOrEmpty(action)
                        ? action
                        : UserSegmentPreferenceActions.None
                };
            })
            .ToList();

        return new SegmentPreferenceStatusDto { Users = users };
    }

    public UpdateSegmentPreferenceResult SetUnknownSegmentAction(
        IReadOnlyCollection<Guid>? userIds,
        string action)
    {
        if (!IsAllowedAction(action))
        {
            throw new ArgumentException("Invalid segment action.", nameof(action));
        }

        var selectedUserIds = userIds == null || userIds.Count == 0
            ? null
            : userIds.ToHashSet();

        var users = GetUsers()
            .Where(user => selectedUserIds == null || selectedUserIds.Contains(user.Id))
            .ToList();

        foreach (var user in users)
        {
            var prefs = GetCustomPreferences(user.Id);

            prefs[UnknownSegmentActionKey] = action;

            _displayPreferencesManager.SetCustomItemDisplayPreferences(
                user.Id,
                UserSettingsItemId,
                ClientId,
                prefs);
        }

        return new UpdateSegmentPreferenceResult { UpdatedCount = users.Count };
    }

    private IReadOnlyList<User> GetUsers()
    {
        var users = (_userManager.Users ?? Enumerable.Empty<User>()).ToList();
        if (users.Count > 0)
        {
            return users;
        }

        return (_userManager.UsersIds ?? Enumerable.Empty<Guid>())
            .Select(id => _userManager.GetUserById(id))
            .Where(user => user != null)
            .Cast<User>()
            .ToList();
    }

    private Dictionary<string, string?> GetCustomPreferences(Guid userId)
    {
        var prefs = _displayPreferencesManager.ListCustomItemDisplayPreferences(
            userId,
            UserSettingsItemId,
            ClientId);

        return prefs
            .Where(pair => pair.Value != null)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static bool IsAllowedAction(string action)
    {
        return string.Equals(action, UserSegmentPreferenceActions.None, StringComparison.Ordinal)
            || string.Equals(action, UserSegmentPreferenceActions.AskToSkip, StringComparison.Ordinal)
            || string.Equals(action, UserSegmentPreferenceActions.Skip, StringComparison.Ordinal);
    }
}
