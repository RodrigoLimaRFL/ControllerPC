using ControllerPC.Core.Triggers;

namespace ControllerPC.Core.Settings;

/// <summary>A Mode Trigger together with how validation messages refer to it.</summary>
/// <param name="Trigger">The Mode Trigger.</param>
/// <param name="Description">How a message names it, e.g. <c>Base Entry Trigger</c>.</param>
/// <param name="ProfileId">The Profile whose Trigger Override it is, or <c>null</c> for a Base Trigger.</param>
internal sealed record DescribedModeTrigger(ModeTrigger Trigger, string Description, Guid? ProfileId)
{
    public IReadOnlyList<Chord> Steps => Trigger.Steps;

    public bool HasSameSteps(DescribedModeTrigger other) => Steps.SequenceEqual(other.Steps);

    /// <summary>Whether this trigger's Steps are the first Steps of <paramref name="other"/>, which has more.</summary>
    public bool IsStrictPrefixOf(DescribedModeTrigger other) =>
        Steps.Count < other.Steps.Count && other.Steps.Take(Steps.Count).SequenceEqual(Steps);
}
