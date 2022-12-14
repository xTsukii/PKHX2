using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Suggestion edits that rely on a <see cref="LegalityAnalysis"/> being done.
    /// </summary>
    public static class LegalEdits
    {
        private static readonly Dictionary<Ball, Ball> LABallMapping = new()
        {
            { Ball.Poke,  Ball.LAPoke },
            { Ball.Great, Ball.LAGreat },
            { Ball.Ultra, Ball.LAUltra },
            { Ball.Heavy, Ball.LAHeavy },
        };

        public static bool ReplaceBallPrefixLA { get; set; }

        /// <summary>
        /// Set a valid Pokeball based on a legality check's suggestions.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        /// <param name="matching">Set matching ball</param>
        /// <param name="force"></param>
        /// <param name="ball"></param>
        public static void SetSuggestedBall(this PKM pk, bool matching = true, bool force = false, Ball ball = Ball.None)
        {
            if (ball != Ball.None)
            {
                var orig = pk.Ball;
                if (pk.LA && ReplaceBallPrefixLA && LABallMapping.TryGetValue(ball, out var modified))
                    ball = modified;
                pk.Ball = (int)ball;
                if (!force && !pk.ValidBall())
                    pk.Ball = orig;
            }
            else if (matching)
            {
                if (!pk.IsShiny)
                    pk.SetMatchingBall();
                else
                    Aesthetics.ApplyShinyBall(pk);
            }
            var la = new LegalityAnalysis(pk);
            var report = la.Report();
            if (!report.Contains(LegalityCheckStrings.LBallEncMismatch) || force)
                return;
            if (pk.Generation == 5 && pk.Met_Location == 75)
                pk.Ball = (int)Ball.Dream;
            else
                pk.Ball = 4;
        }

        public static bool ValidBall(this PKM pk)
        {
            var rep = new LegalityAnalysis(pk).Report(true);
            return rep.Contains(LegalityCheckStrings.LBallEnc) || rep.Contains(LegalityCheckStrings.LBallSpeciesPass);
        }

        /// <summary>
        /// Sets all ribbon flags according to a legality report.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        /// <param name="enc">Encounter matched to</param>
        /// <param name="allValid">Set all valid ribbons only</param>
        public static void SetSuggestedRibbons(this PKM pk, IBattleTemplate set, IEncounterable enc, bool allValid = true)
        {
            if (allValid)
            {
                RibbonApplicator.SetAllValidRibbons(pk);
                if (pk is PK8 pk8 && pk8.Species != (int)Species.Shedinja && pk8.GetRandomValidMark(set, enc, out var mark))
                    pk8.SetRibbonIndex(mark);
            }
            else RibbonApplicator.RemoveAllValidRibbons(pk);
        }

        /// <summary>
        /// Set ribbon values to the pkm file using reflectutil
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <param name="ribNames">string of ribbon names</param>
        /// <param name="vRib">ribbon value</param>
        /// <param name="bRib">ribbon boolean</param>
        private static void SetRibbonValues(this PKM pk, IEnumerable<string> ribNames, int vRib, bool bRib)
        {
            foreach (string rName in ribNames)
            {
                bool intRib = rName is nameof(PK6.RibbonCountMemoryBattle) or nameof(PK6.RibbonCountMemoryContest);
                ReflectUtil.SetValue(pk, rName, intRib ? vRib : bRib);
            }
        }

        /// <summary>
        /// Get ribbon names of a pkm
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <returns></returns>
        private static IEnumerable<string> GetRibbonNames(PKM pk) => ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();
    }
}
