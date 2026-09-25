using System;

public class TextComparisonEngine
{
    private static readonly bool[] BaseSet = new bool[0x10000];

    private static readonly bool[] CombiningSet = new bool[0x10000];

    private const int Virama = 0x094D;


    static TextComparisonEngine()
    {
        for (int cp = 0x0904; cp < 0x093A; cp++)
        {
            BaseSet[cp] = true;
        }

        for (int cp = 0x0958; cp < 0x0960; cp++)
        {
            BaseSet[cp] = true;
        }

        for (int cp = 0x093A; cp < 0x094D; cp++)
        {
            CombiningSet[cp] = true;
        }

        CombiningSet[0x094D] = true;

        for (int cp = 0x0951; cp < 0x0958; cp++)
        {
            CombiningSet[cp] = true;
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
            CombiningSet[cp] = true;
        }
    }


    // Public helper method to expose Aksharification for UI inspector (returns StringCollection so .Count works in Razor)
    public System.Collections.Specialized.StringCollection ExposeSplitAksharas(string text)
    {
        return SplitAksharas(text);
    }


    // ==========================================
    // AKSHARIFICATION METHOD
    // ==========================================

    private System.Collections.Specialized.StringCollection SplitAksharas(string text)
    {
        var outList = new System.Collections.Specialized.StringCollection();

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

            if (!BaseSet[cp] && !CombiningSet[cp])
            {
                outList.Add(
                    cleanedText[i].ToString()
                );

                i++;

                continue;
            }

            if (!BaseSet[cp])
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
                CombiningSet[cleanedText[i]]
            )
            {
                bool isVirama =
                    cleanedText[i] == Virama;

                i++;

                if (
                    isVirama &&
                    i < n &&
                    BaseSet[cleanedText[i]]
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
        var col1 = SplitAksharas(catText);
        var col2 = SplitAksharas(transcText);

        string[] aksharas1 = new string[col1.Count];
        col1.CopyTo(aksharas1, 0);

        string[] aksharas2 = new string[col2.Count];
        col2.CopyTo(aksharas2, 0);

        if (
            aksharas1.Length == 0 &&
            aksharas2.Length == 0
        )
        {
            return new ComparisonResult
            {
                PairName = pairName,

                LevenshteinDistance = 0,

                MatchPercentage = 100.0,

                IsExactMatch = true,

                StatusMessage = "Both texts are empty."
            };
        }

        int ld =
            ComputeAksharaLevenshtein(
                aksharas1,
                aksharas2
            );

        int maxLength =
            Math.Max(
                aksharas1.Length,
                aksharas2.Length
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

        var diffResult =
            GenerateDiff(
                aksharas1,
                aksharas2
            );

        return new ComparisonResult
        {
            PairName = pairName,

            LevenshteinDistance = ld,

            MatchPercentage = matchPercentage,

            IsExactMatch = isExactMatch,

            StatusMessage = status,

            CatDiff = diffResult.CatDiff,

            TranscDiff = diffResult.TranscDiff
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
        string[] s,
        string[] t)
    {
        int n = s.Length;

        int m = t.Length;

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
    // GENERATE DIFF (ARRAY-BASED, NO ANGLE BRACKETS)
    // ==========================================

   private (
        (string Text, bool IsMatch)[] CatDiff,
        (string Text, bool IsMatch)[] TranscDiff
    ) GenerateDiff(
        string[] s,
        string[] t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        var tempCat = new (string Text, bool IsMatch)[n + m + 1];
        var tempTransc = new (string Text, bool IsMatch)[n + m + 1];
        int count = 0;

        int ci = n;
        int cj = m;

        while (ci > 0 || cj > 0)
        {
            if (ci > 0 && cj > 0 && s[ci - 1] == t[cj - 1])
            {
                tempCat[count] = (s[ci - 1], true);
                tempTransc[count] = (t[cj - 1], true);
                ci--;
                cj--;
                count++;
            }
            else
            {
                bool canSub = ci > 0 && cj > 0;
                bool canDel = ci > 0;
                bool canIns = cj > 0;

                int subCost = canSub ? d[ci - 1, cj - 1] : int.MaxValue;
                int delCost = canDel ? d[ci - 1, cj] : int.MaxValue;
                int insCost = canIns ? d[ci, cj - 1] : int.MaxValue;

                if (canSub && subCost <= delCost && subCost <= insCost)
                {
                    tempCat[count] = (s[ci - 1], false);
                    tempTransc[count] = (t[cj - 1], false);
                    ci--;
                    cj--;
                    count++;
                }
                else if (canDel && delCost <= insCost)
                {
                    tempCat[count] = (s[ci - 1], false);
                    tempTransc[count] = ("", false); // Replaced "_" with empty string to avoid massive underscores
                    ci--;
                    count++;
                }
                else
                {
                    tempCat[count] = ("", false); // Replaced "_" with empty string to avoid massive underscores
                    tempTransc[count] = (t[cj - 1], false);
                    cj--;
                    count++;
                }
            }
        }

        var finalCat = new (string Text, bool IsMatch)[count];
        var finalTransc = new (string Text, bool IsMatch)[count];

        for (int k = 0; k < count; k++)
        {
            finalCat[k] = tempCat[count - 1 - k];
            finalTransc[k] = tempTransc[count - 1 - k];
        }

        return (finalCat, finalTransc);
    }
}