using System;
using System.Collections.Generic;
using System.Text;

namespace UTS.Backend.Selection
{
    public sealed record GenerationResult(
        int TestIndex,
        int FixedGraded,
        int RecommendedAdded,
        IReadOnlyList<string> Warnings
    );
}
