/// <summary>
/// Tüm oyun verilerinin ortak base sınıfı.
/// Yeni bir veri türü eklerken bu sınıftan türetmen yeterlidir.
/// </summary>
public abstract class GameData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
