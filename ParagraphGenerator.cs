using System.Text;

namespace AutoClicker;

/// <summary>Builds natural-looking paragraphs from short, grammatical sentences (about 6-12 words each).</summary>
internal static class ParagraphGenerator
{
    private static readonly string[] Subjects =
    {
        "The team", "Our manager", "My friend", "The new system", "Everyone in the office", "The customer",
        "The teacher", "Most people", "The report", "This project", "The company", "My neighbor",
        "The students", "Our client", "The update", "The weather",
    };

    private static readonly string[] Predicates =
    {
        "finished the work before lunch", "needs a little more time", "sent the files this morning",
        "looks much better than last week", "asked a few good questions", "will review the plan tomorrow",
        "made a small change to the schedule", "seems happy with the results", "is waiting for a reply",
        "started a new task today", "fixed the problem quickly", "shared some useful ideas",
        "moved the meeting to Friday", "worked late to meet the deadline", "is ready for the next step",
        "gave us clear feedback", "was busy all afternoon", "agreed with the proposal",
    };

    private static readonly string[] Endings =
    {
        "", "", "", " after the meeting", " without any issues", " as expected", " for the first time",
        " this week", " in the end", " once again",
    };

    private static readonly string[] Standalone =
    {
        "Please let me know if you have any questions.", "Thanks again for your help.",
        "We will talk about it later.", "It was a long but productive day.", "Everything is going well so far.",
        "I will send an update soon.", "That sounds like a good plan.", "Let's keep things simple for now.",
        "There is still a lot to do.", "I think we are on the right track.",
    };

    private static readonly Random Rng = new();

    /// <summary>Lines are wrapped at this many characters (standard letter line is about 60-70).</summary>
    public const int MaxLineLength = 70;

    public static string Create(int sentences)
    {
        var sb = new StringBuilder();
        var used = new HashSet<string>();
        for (int i = 0; i < sentences; i++)
        {
            string s;
            do s = Sentence(); while (!used.Add(s) && used.Count < 200);
            if (i > 0) sb.Append(' ');
            sb.Append(s);
        }
        return Wrap(sb.ToString(), MaxLineLength);
    }

    /// <summary>Breaks text into lines of at most <paramref name="width"/> characters, at spaces where possible.</summary>
    public static string Wrap(string text, int width)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line.ToString());
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
            while (line.Length > width) // a single word longer than a line
            {
                lines.Add(line.ToString(0, width));
                line.Remove(0, width);
            }
        }
        if (line.Length > 0) lines.Add(line.ToString());
        return string.Join('\n', lines);
    }

    private static string Sentence() => Rng.Next(5) == 0
        ? Pick(Standalone)
        : $"{Pick(Subjects)} {Pick(Predicates)}{Pick(Endings)}.";

    private static string Pick(string[] items) => items[Rng.Next(items.Length)];
}
