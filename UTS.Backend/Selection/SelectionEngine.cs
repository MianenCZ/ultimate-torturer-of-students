namespace UTS.Backend.Selection
{
    using System.Security.Cryptography;
    using UTS.Backend.Domain;

    public static class SelectionEngine
    {
        public static GenerationResult RegenerateColumn(UtsDocument doc, int t)
        {
            doc.Validate();
            if ((uint)t >= (uint)doc.T) throw new ArgumentOutOfRangeException(nameof(t));

            var warnings = new List<string>();
            int n = doc.Students.Count;

            // Global feasibility (theoretical lower bound, ignoring absences)
            if (doc.T * doc.K < n * doc.E)
                warnings.Add($"Global infeasible: T*K={doc.T * doc.K} < n*E={n * doc.E}.");

            // 1) Overwrite all D in this column
            foreach (var s in doc.Students)
                if (s[t] == CellState.Recommended) s[t] = CellState.None;

            // 2) Fixed graded in this column
            int fixedGraded = doc.Students.Count(s => s[t] == CellState.Graded);
            int slots = doc.K - fixedGraded;

            if (slots < 0)
            {
                warnings.Add($"Column {t + 1}: fixed graded Z={fixedGraded} exceeds K={doc.K}.");
                slots = 0;
            }

            // Eligible now: not absent and not already graded
            var eligible = doc.Students.Where(s => s[t] != CellState.Absent && s[t] != CellState.Graded).ToList();

            // If not enough eligible to reach K, we will select all eligible and warn.
            if (eligible.Count < slots)
                warnings.Add($"Column {t + 1}: only {eligible.Count} eligible students for {slots} remaining slots.");

            // Per-student feasibility checks and mandatory detection
            var candidates = new List<Candidate>(eligible.Count);
            foreach (var s in eligible)
            {
                int graded = s.GradedCount;
                int deficit = Math.Max(0, doc.E - graded);

                int remainingFromT = 0;
                for (int j = t; j < doc.T; j++)
                    if (s[j] != CellState.Absent && s[j] != CellState.Graded) remainingFromT++;

                int remainingTotal = 0;
                for (int j = 0; j < doc.T; j++)
                    if (s[j] != CellState.Absent && s[j] != CellState.Graded) remainingTotal++;

                if (graded + remainingTotal < doc.E)
                    warnings.Add($"Student '{s.Name}': impossible to reach E={doc.E} given current absences/grades.");

                bool mandatory = deficit > 0 && deficit == remainingFromT;

                int lastZ = s.LastGradedIndex();
                int sinceLast = lastZ < 0 ? 999 : (t - lastZ);

                candidates.Add(new Candidate(s, deficit, sinceLast, mandatory));
            }

            // 3) Apply mandatory first
            var selected = new HashSet<StudentRecord>();
            foreach (var c in candidates.Where(x => x.Mandatory))
            {
                if (selected.Count >= slots) break;
                selected.Add(c.Student);
            }
            if (candidates.Count(x => x.Mandatory) > slots)
                warnings.Add($"Column {t + 1}: mandatory count exceeds remaining slots; cannot satisfy all mandatory needs.");

            // 4) Fill remaining using A-ES keys
            int need = Math.Min(slots, eligible.Count) - selected.Count;
            if (need > 0)
            {
                var pool = candidates.Where(c => !selected.Contains(c.Student)).ToList();
                foreach (var c in SampleByKeys(pool, need))
                    selected.Add(c.Student);
            }

            // 5) Write recommendations (D) only for selected; do not change A/Z
            int added = 0;
            foreach (var s in selected)
            {
                if (s[t] == CellState.None)
                {
                    s[t] = CellState.Recommended;
                    added++;
                }
            }

            return new GenerationResult(t, fixedGraded, added, warnings);
        }

        private static IEnumerable<Candidate> SampleByKeys(List<Candidate> pool, int k)
        {
            // Efraimidis–Spirakis stable key variant: key = -ln(u)/w ; take smallest k.
            return pool
                .Select(c =>
                {
                    double w = ComputeWeight(c);
                    double u = NextDoubleOpen01();
                    double key = -Math.Log(u) / Math.Max(1e-9, w);
                    return (key, c);
                })
                .OrderBy(x => x.key)
                .Take(k)
                .Select(x => x.c);
        }

        private static double ComputeWeight(Candidate c)
        {
            // Deterministic from authoritative table state: deficit + recency.
            // Tune constants empirically.
            double w = 1.0;
            if (c.Deficit > 0) w += 5.0 * c.Deficit * c.Deficit; // strong push for below-E students
            w += 0.25 * Math.Min(20, c.SinceLastGraded);
            return w;
        }

        private static double NextDoubleOpen01()
        {
            Span<byte> b = stackalloc byte[8];
            RandomNumberGenerator.Fill(b);
            ulong x = BitConverter.ToUInt64(b);
            // Map to (0,1], avoid 0 exactly.
            return (x + 1.0) / (ulong.MaxValue + 1.0);
        }

        private sealed record Candidate(StudentRecord Student, int Deficit, int SinceLastGraded, bool Mandatory);
    }

}
