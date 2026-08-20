namespace KurTakipApi.Models;

// Frankfurter API'sinden dönen JSON'un birebir C# karşılığı.
// "rates" alanı dinamik bir sözlük olduğu için Dictionary<string, decimal> kullanıyoruz.
public class FrankfurterYanit
{
    public string Base { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}