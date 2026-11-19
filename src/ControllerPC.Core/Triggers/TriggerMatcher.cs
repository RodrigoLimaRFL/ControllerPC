using ControllerPC.Core.Controller;

namespace ControllerPC.Core.Triggers;

/// <summary>
/// Watches a stream of held-button sets and reports when one <see cref="ModeTrigger"/> is completed.
/// </summary>
/// <remarks>
/// Matching rules (see CONTEXT.md):
/// <list type="bullet">
/// <item>A Step completes the moment exactly its Chord's buttons are held; press order within the Chord does not matter.</item>
/// <item>Holding any button outside the expected Chord breaks the match and resets the attempt.</item>
/// <item>The next Step only starts counting after every button of the previous Step is released.</item>
/// <item>If the next Step is not completed within <see cref="ModeTrigger.StepGap"/> of the previous Step's release, the attempt resets.</item>
/// <item>With Final Hold enabled, the last Chord must stay held (exactly) for the Final Hold duration.</item>
/// <item>Buttons already held when the matcher is created or <see cref="Reset"/> must be released before they can contribute.</item>
/// <item>After firing, the matcher resets itself (the buttons still held must be released before contributing again).</item>
/// <item>A trigger with no Steps never fires.</item>
/// </list>
/// </remarks>
public sealed class TriggerMatcher
{
    public TriggerMatcher(ModeTrigger trigger, ControllerButton heldAtStart = ControllerButton.None)
    {
        throw new NotImplementedException();
    }

    public ModeTrigger Trigger { get; }

    /// <summary>Feeds the currently held buttons at time <paramref name="now"/>. Returns <c>true</c> when the trigger fires.</summary>
    public bool Update(ControllerButton held, TimeSpan now) => throw new NotImplementedException();

    /// <summary>Abandons any in-progress attempt. <paramref name="held"/> must be released before contributing.</summary>
    public void Reset(ControllerButton held) => throw new NotImplementedException();
}
