using System.Text;

namespace AutoClicker;

internal static class ParagraphGenerator
{
    private static readonly string[] Words =
    (
        "time people way day man thing woman life child world school state family student group country problem " +
        "hand part place case week company system program question work government number night point home water " +
        "room mother area money story fact month lot right study book eye job word business issue side kind head " +
        "house service friend father power hour game line end member law car city community name team minute idea " +
        "kid body information back parent face others level office door health person art war history party result " +
        "change morning reason research girl guy moment air teacher force education quick bright simple careful " +
        "gentle clever happy small large early late final strong quiet open green"
    ).Split(' ');

    private static readonly Random Rng = new();

    public static string Create(int sentences)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < sentences; i++)
        {
            int count = Rng.Next(6, 15);
            for (int w = 0; w < count; w++)
            {
                string word = Words[Rng.Next(Words.Length)];
                if (w == 0) word = char.ToUpper(word[0]) + word[1..];
                sb.Append(word);
                if (w < count - 1) sb.Append(w > 2 && w < count - 2 && Rng.Next(8) == 0 ? ", " : " ");
            }
            sb.Append('.');
            if (i < sentences - 1) sb.Append(' ');
        }
        return sb.ToString();
    }
}
