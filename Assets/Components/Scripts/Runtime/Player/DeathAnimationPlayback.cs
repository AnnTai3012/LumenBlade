using System.Collections;
using UnityEngine;

// Death presentation must finish before a corpse is removed or the player is restored.
public static class DeathAnimationPlayback
{
    public static IEnumerator WaitForCompletion(Animator animator, int stateHash, float minimumDelay)
    {
        float elapsed = 0f;
        // Animator.Play is evaluated on the next animation update.
        yield return null;
        while (true)
        {
            bool playing = false;
            if (animator != null && animator.enabled && animator.HasState(0, stateHash))
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                playing = state.fullPathHash == stateHash && state.normalizedTime < 1f;
                if (animator.IsInTransition(0))
                    playing |= animator.GetNextAnimatorStateInfo(0).fullPathHash == stateHash;
            }

            if (!playing && elapsed >= Mathf.Max(0f, minimumDelay))
                yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
