using CommunityToolkit.Mvvm.ComponentModel;
using lawar4.Models;
using lawar4.Services;

namespace lawar4.ViewModels;

/// <summary>Review-list wrapper around a domain <see cref="Observation"/>.</summary>
public sealed class ObservationItem : ObservableObject
{
    public ObservationItem(Observation model) => Model = model;

    public Observation Model { get; }

    public string Header => $"{TitleCase(Model.Day)} · rank {Model.Rank}";
    public string Name => Model.RawName;
    public string Detail => $"{Model.Points:N0} points · {Model.MatchMethod} ({Model.MatchConfidence:P0})";
    public string Matched => Model.MatchedMemberName ?? "Unassigned";
    public bool NeedsReview => Model.MatchedMemberId is null;
    public bool HasAlternatives => Model.Alternatives.Count > 0;
    public string AssignLabel => NeedsReview ? "Assign" : "Reassign";

    public string StatusIcon => !NeedsReview
        ? "matched_status_badge.png"
        : (HasAlternatives ? "needs_review_badge.png" : "unmatched_target_skull_badge.png");

    public Color AccentColor => !NeedsReview
        ? Color.FromArgb("#00E676")
        : (HasAlternatives ? Color.FromArgb("#FF9800") : Color.FromArgb("#FF3333"));

    public void Refresh()
    {
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(Matched));
        OnPropertyChanged(nameof(NeedsReview));
        OnPropertyChanged(nameof(HasAlternatives));
        OnPropertyChanged(nameof(AssignLabel));
        OnPropertyChanged(nameof(StatusIcon));
        OnPropertyChanged(nameof(AccentColor));
    }

    private static string TitleCase(string day) =>
        day.Length == 0 ? day : char.ToUpperInvariant(day[0]) + day[1..];
}

/// <summary>Review-list ordering: unmatched first, then by day/rank. Shared so single-row moves match a full rebuild.</summary>
public sealed class ObservationSortOrder : IComparer<Observation>
{
    public static readonly ObservationSortOrder Instance = new();

    public int Compare(Observation? x, Observation? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var cmp = (y.MatchedMemberId is null).CompareTo(x.MatchedMemberId is null);
        if (cmp != 0) return cmp;
        cmp = string.Compare(x.Day, y.Day, StringComparison.Ordinal);
        if (cmp != 0) return cmp;
        return x.Rank.CompareTo(y.Rank);
    }
}
