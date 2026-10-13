using UnityEditor;
using Mutiny.Verification;

namespace Mutiny.Verification.Editor
{
    /// <summary>
    /// Thin menu wrappers over MutinyOnlineVerificationTest; the checks live in
    /// the runtime assembly so they can reach the internal verification routes.
    /// </summary>
    public static class MutinyOnlineVerificationMenu
    {
        [MenuItem("Mutiny/Online/Validate Online Basics")]
        public static void ValidateBasics()
        {
            MutinyOnlineVerificationTest.ValidateBasics();
        }

        [MenuItem("Mutiny/Online/Validate Match Determinism")]
        public static void ValidateDeterminism()
        {
            MutinyOnlineVerificationTest.ValidateDeterminism();
        }
    }
}
