// using System.Collections.Generic;

// // ==========================================
// // COMPARISON RESULT CLASS
// // ==========================================
// // Extracted from Home.razor without any changes to fields, types, or defaults.
// // NOTE: No namespace is declared so this remains globally accessible exactly
// // as it was when nested inside Home.razor (no @using needed in Home.razor).
// // If your project prefers a namespace, wrap this class in one and add a
// // matching @using directive at the top of Home.razor.

// public class ComparisonResult
// {
//     public string PairName { get; set; } = "";

//     public int LevenshteinDistance { get; set; }

//     public double MatchPercentage { get; set; }

//     public bool IsExactMatch { get; set; }

//     public string StatusMessage { get; set; } = "";

//     public List<(string Text, bool IsMatch)> CatDiff { get; set; } = new();

//     public List<(string Text, bool IsMatch)> TranscDiff { get; set; } = new();
// }

using System;

public class ComparisonResult
{
    public string PairName { get; set; } = "";

    public int LevenshteinDistance { get; set; }

    public double MatchPercentage { get; set; }

    public bool IsExactMatch { get; set; }

    public string StatusMessage { get; set; } = "";

    public (string Text, bool IsMatch)[] CatDiff { get; set; } = Array.Empty<(string Text, bool IsMatch)>();

    public (string Text, bool IsMatch)[] TranscDiff { get; set; } = Array.Empty<(string Text, bool IsMatch)>();
}