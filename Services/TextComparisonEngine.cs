using System;
using System.Collections.Generic;

// ==========================================
// TEXT COMPARISON ENGINE
// ==========================================
// Extracted verbatim from Home.razor's @code block. No logic, algorithm,
// tokenization rule, or calculation has been changed in any way.
// NOTE: No namespace is declared so this remains globally accessible exactly
// as it was when nested inside Home.razor (no @using needed in Home.razor).
// If your project prefers a namespace, wrap this class in one and add a
// matching @using directive at the top of Home.razor.

public class TextComparisonEngine
{
    private static readonly HashSet<int> BaseSet = new();

    private static readonly HashSet<int> CombiningSet = new();

    private const int Virama = 0x094D;


    static TextComparisonEngine()
    {
        for (int cp = 0x0904; cp < 0x093A; cp++)
        {
            BaseSet.Add(cp);
        }

        for (int cp = 0x0958; cp < 0x0960; cp++)
        {
            BaseSet.Add(cp);
        }

        for (int cp = 0x093A; cp < 0x094D; cp++)
        {
            CombiningSet.Add(cp);
        }

        CombiningSet.Add(0x094D);

        for (int cp = 0x0951; cp < 0x0958; cp++)
        {
            CombiningSet.Add(cp);
        }

        int[] extraCombining =
        {
            0x0962,
            0x0963,
            0x0900,
            0x0901,
            0x0902,
            0x0903,
            0x093C
        };

        foreach (int cp in extraCombining)
        {
            CombiningSet.Add(cp);
        }
    }


    // Public helper method to expose Aksharification for UI inspector
    public List<string> ExposeSplitAksharas(string text)
    {
        return SplitAksharas(text);
    }


    // ==========================================
    // AKSHARIFICATION METHOD
    // ==========================================

    private List<string> SplitAksharas(string text)
    {
        var outList = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            return outList;
        }

        string cleanedText = NormalizeText(text);

        int i = 0;

        int n = cleanedText.Length;

        while (i < n)
        {
            int cp = cleanedText[i];

            if (!BaseSet.Contains(cp) && !CombiningSet.Contains(cp))
            {
                outList.Add(
                    cleanedText[i].ToString()
                );

                i++;

                continue;
            }

            if (!BaseSet.Contains(cp))
            {
                outList.Add(
                    cleanedText[i].ToString()
                );

                i++;

                continue;
            }

            int start = i;

            i++;

            while (
                i < n &&
                CombiningSet.Contains(cleanedText[i])
            )
            {
                bool isVirama =
                    cleanedText[i] == Virama;

                i++;

                if (
                    isVirama &&
                    i < n &&
                    BaseSet.Contains(cleanedText[i])
                )
                {
                    i++;
                }
            }

            outList.Add(
                cleanedText.Substring(
                    start,
                    i - start
                )
            );
        }

        return outList;
    }


    // ==========================================
    // COMPARE
    // ==========================================

    public ComparisonResult Compare(
        string pairName,
        string catText,
        string transcText)
    {
        var aksharas1 =
            SplitAksharas(catText);

        var aksharas2 =
            SplitAksharas(transcText);

        if (
            aksharas1.Count == 0 &&
            aksharas2.Count == 0
        )
        {
            return new ComparisonResult
            {
                PairName = pairName,

                LevenshteinDistance = 0,

                MatchPercentage = 100.0,

                IsExactMatch = true,

                StatusMessage = "Both texts are empty.",

                CatAksharaCount = 0,

                TranscAksharaCount = 0
            };
        }

        int ld =
            ComputeAksharaLevenshtein(
                aksharas1,
                aksharas2
            );

        int maxLength =
            Math.Max(
                aksharas1.Count,
                aksharas2.Count
            );

        double matchPercentage =
            maxLength == 0
                ? 100.0
                : Math.Round(
                    (
                        1.0 -
                        (double)ld / maxLength
                    ) * 100.0,
                    2
                );

        string joined1 =
            string.Join(
                "",
                aksharas1
            );

        string joined2 =
            string.Join(
                "",
                aksharas2
            );

        bool isExactMatch =
            joined1.Equals(
                joined2,
                StringComparison.Ordinal
            );

        string status =
            isExactMatch
                ? "✅ Exact Match"
                : "❌ Non-Matching / Different";

        var (catDiff, transcDiff) =
            GenerateDiff(
                NormalizeText(catText),
                NormalizeText(transcText)
            );

        return new ComparisonResult
        {
            PairName = pairName,

            LevenshteinDistance = ld,

            MatchPercentage = matchPercentage,

            IsExactMatch = isExactMatch,

            StatusMessage = status,

            CatAksharaCount = aksharas1.Count,

            TranscAksharaCount = aksharas2.Count,

            CatDiff = catDiff,

            TranscDiff = transcDiff
        };
    }


    // ==========================================
    // NORMALIZE TEXT
    // ==========================================

    private string NormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string cleaned =
            input
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ");

        while (cleaned.Contains("  "))
        {
            cleaned =
                cleaned.Replace(
                    "  ",
                    " "
                );
        }

        return cleaned.Trim();
    }


    // ==========================================
    // AKSHARA LEVENSHTEIN
    // ==========================================

    private int ComputeAksharaLevenshtein(
        List<string> s,
        List<string> t)
    {
        int n = s.Count;

        int m = t.Count;

        int[,] d =
            new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (int i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost =
                    t[j - 1] == s[i - 1]
                        ? 0
                        : 1;

                int deletion =
                    d[i - 1, j] + 1;

                int insertion =
                    d[i, j - 1] + 1;

                int substitution =
                    d[i - 1, j - 1] + cost;

                d[i, j] =
                    Math.Min(
                        Math.Min(
                            deletion,
                            insertion
                        ),
                        substitution
                    );
            }
        }

        return d[n, m];
    }


    // ==========================================
    // GENERATE DIFF
    // ==========================================

    private (
        List<(string Text, bool IsMatch)> CatDiff,
        List<(string Text, bool IsMatch)> TranscDiff
    ) GenerateDiff(
        string s,
        string t)
    {
        int n = s.Length;

        int m = t.Length;

        int[,] d =
            new int[n + 1, m + 1];

        for (
            int i = 0;
            i <= n;
            d[i, 0] = i++
        )
        {
        }

        for (
            int j = 0;
            j <= m;
            d[0, j] = j++
        )
        {
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost =
                    (t[j - 1] == s[i - 1])
                        ? 0
                        : 1;

                d[i, j] =
                    Math.Min(
                        Math.Min(
                            d[i - 1, j] + 1,
                            d[i, j - 1] + 1
                        ),
                        d[i - 1, j - 1] + cost
                    );
            }
        }

        var revCat =
            new List<(string Text, bool IsMatch)>();

        var revTransc =
            new List<(string Text, bool IsMatch)>();

        int ci = n;

        int cj = m;

        while (ci > 0 || cj > 0)
        {
            if (
                ci > 0 &&
                cj > 0 &&
                s[ci - 1] == t[cj - 1]
            )
            {
                revCat.Add(
                    (
                        s[ci - 1].ToString(),
                        true
                    )
                );

                revTransc.Add(
                    (
                        t[cj - 1].ToString(),
                        true
                    )
                );

                ci--;

                cj--;
            }
            else
            {
                bool canSub =
                    ci > 0 &&
                    cj > 0;

                bool canDel =
                    ci > 0;

                bool canIns =
                    cj > 0;

                int subCost =
                    canSub
                        ? d[ci - 1, cj - 1]
                        : int.MaxValue;

                int delCost =
                    canDel
                        ? d[ci - 1, cj]
                        : int.MaxValue;

                int insCost =
                    canIns
                        ? d[ci, cj - 1]
                        : int.MaxValue;

                if (
                    canSub &&
                    subCost <= delCost &&
                    subCost <= insCost
                )
                {
                    revCat.Add(
                        (
                            s[ci - 1].ToString(),
                            false
                        )
                    );

                    revTransc.Add(
                        (
                            t[cj - 1].ToString(),
                            false
                        )
                    );

                    ci--;

                    cj--;
                }
                else if (
                    canDel &&
                    delCost <= insCost
                )
                {
                    revCat.Add(
                        (
                            s[ci - 1].ToString(),
                            false
                        )
                    );

                    revTransc.Add(
                        (
                            "_",
                            false
                        )
                    );

                    ci--;
                }
                else
                {
                    revCat.Add(
                        (
                            "_",
                            false
                        )
                    );

                    revTransc.Add(
                        (
                            t[cj - 1].ToString(),
                            false
                        )
                    );

                    cj--;
                }
            }
        }

        revCat.Reverse();

        revTransc.Reverse();

        return (
            revCat,
            revTransc
        );
    }
}