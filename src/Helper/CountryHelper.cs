using System.Collections.Generic;

/// <summary>
/// ISO 3166-1 alpha-2 country code (TR, US, DE...) ile ülke adı arasında dönüşüm yapan yardımcı sınıf.
/// Sık kullanılan ~50 ülkeyi kapsar. Türkçe ve İngilizce isim desteği vardır.
/// </summary>
public static class CountryHelper
{
    public enum Language
    {
        Turkish,
        English
    }

    private class CountryNames
    {
        public string TR;
        public string EN;

        public CountryNames(string tr, string en)
        {
            TR = tr;
            EN = en;
        }
    }

    private static readonly Dictionary<string, CountryNames> Countries = new Dictionary<string, CountryNames>
    {
        { "TR", new CountryNames("Türkiye", "Turkey") },
        { "US", new CountryNames("Amerika Birleşik Devletleri", "United States") },
        { "GB", new CountryNames("Birleşik Krallık", "United Kingdom") },
        { "DE", new CountryNames("Almanya", "Germany") },
        { "FR", new CountryNames("Fransa", "France") },
        { "IT", new CountryNames("İtalya", "Italy") },
        { "ES", new CountryNames("İspanya", "Spain") },
        { "PT", new CountryNames("Portekiz", "Portugal") },
        { "NL", new CountryNames("Hollanda", "Netherlands") },
        { "BE", new CountryNames("Belçika", "Belgium") },
        { "CH", new CountryNames("İsviçre", "Switzerland") },
        { "AT", new CountryNames("Avusturya", "Austria") },
        { "SE", new CountryNames("İsveç", "Sweden") },
        { "NO", new CountryNames("Norveç", "Norway") },
        { "DK", new CountryNames("Danimarka", "Denmark") },
        { "FI", new CountryNames("Finlandiya", "Finland") },
        { "PL", new CountryNames("Polonya", "Poland") },
        { "GR", new CountryNames("Yunanistan", "Greece") },
        { "RO", new CountryNames("Romanya", "Romania") },
        { "BG", new CountryNames("Bulgaristan", "Bulgaria") },
        { "CZ", new CountryNames("Çekya", "Czech Republic") },
        { "HU", new CountryNames("Macaristan", "Hungary") },
        { "UA", new CountryNames("Ukrayna", "Ukraine") },
        { "RU", new CountryNames("Rusya", "Russia") },
        { "IE", new CountryNames("İrlanda", "Ireland") },
        { "RS", new CountryNames("Sırbistan", "Serbia") },
        { "HR", new CountryNames("Hırvatistan", "Croatia") },
        { "AZ", new CountryNames("Azerbaycan", "Azerbaijan") },
        { "GE", new CountryNames("Gürcistan", "Georgia") },
        { "AM", new CountryNames("Ermenistan", "Armenia") },
        { "IR", new CountryNames("İran", "Iran") },
        { "IQ", new CountryNames("Irak", "Iraq") },
        { "SY", new CountryNames("Suriye", "Syria") },
        { "SA", new CountryNames("Suudi Arabistan", "Saudi Arabia") },
        { "AE", new CountryNames("Birleşik Arap Emirlikleri", "United Arab Emirates") },
        { "QA", new CountryNames("Katar", "Qatar") },
        { "IL", new CountryNames("İsrail", "Israel") },
        { "EG", new CountryNames("Mısır", "Egypt") },
        { "ZA", new CountryNames("Güney Afrika", "South Africa") },
        { "NG", new CountryNames("Nijerya", "Nigeria") },
        { "CN", new CountryNames("Çin", "China") },
        { "JP", new CountryNames("Japonya", "Japan") },
        { "KR", new CountryNames("Güney Kore", "South Korea") },
        { "IN", new CountryNames("Hindistan", "India") },
        { "PK", new CountryNames("Pakistan", "Pakistan") },
        { "ID", new CountryNames("Endonezya", "Indonesia") },
        { "TH", new CountryNames("Tayland", "Thailand") },
        { "VN", new CountryNames("Vietnam", "Vietnam") },
        { "PH", new CountryNames("Filipinler", "Philippines") },
        { "AU", new CountryNames("Avustralya", "Australia") },
        { "NZ", new CountryNames("Yeni Zelanda", "New Zealand") },
        { "CA", new CountryNames("Kanada", "Canada") },
        { "MX", new CountryNames("Meksika", "Mexico") },
        { "BR", new CountryNames("Brezilya", "Brazil") },
        { "AR", new CountryNames("Arjantin", "Argentina") },
        { "CL", new CountryNames("Şili", "Chile") },
        { "CO", new CountryNames("Kolombiya", "Colombia") },
        { "KZ", new CountryNames("Kazakistan", "Kazakhstan") },
        { "UZ", new CountryNames("Özbekistan", "Uzbekistan") },
        { "TM", new CountryNames("Türkmenistan", "Turkmenistan") },
        { "KG", new CountryNames("Kırgızistan", "Kyrgyzstan") },
        { "TJ", new CountryNames("Tacikistan", "Tajikistan") },
        { "CY", new CountryNames("Kıbrıs", "Cyprus") },
        { "AL", new CountryNames("Arnavutluk", "Albania") },
        { "MK", new CountryNames("Kuzey Makedonya", "North Macedonia") },
        { "BA", new CountryNames("Bosna Hersek", "Bosnia and Herzegovina") },
        { "MD", new CountryNames("Moldova", "Moldova") },
        { "LT", new CountryNames("Litvanya", "Lithuania") },
        { "LV", new CountryNames("Letonya", "Latvia") },
        { "EE", new CountryNames("Estonya", "Estonia") },
        { "IS", new CountryNames("İzlanda", "Iceland") },
        { "LU", new CountryNames("Lüksemburg", "Luxembourg") },
    };

    /// <summary>
    /// Verilen country code'a (TR, US, DE...) karşılık gelen ülke adını döner.
    /// Code büyük/küçük harf duyarsız çalışır.
    /// Bulunamazsa kodun kendisini döner.
    /// </summary>
    public static string GetCountryName(string countryCode, Language language = Language.Turkish)
    {
        if (string.IsNullOrEmpty(countryCode))
            return string.Empty;

        string code = countryCode.Trim().ToUpperInvariant();

        if (Countries.TryGetValue(code, out CountryNames names))
        {
            return language == Language.Turkish ? names.TR : names.EN;
        }

        // Bulunamadıysa kodu olduğu gibi geri döndür (hata fırlatmak yerine güvenli fallback)
        return countryCode;
    }

    /// <summary>
    /// Verilen country code'un listede tanımlı olup olmadığını kontrol eder.
    /// </summary>
    public static bool IsValidCountryCode(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return false;

        return Countries.ContainsKey(countryCode.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Tüm ülke kodlarının listesini döner. Örneğin bir dropdown doldurmak için kullanışlı.
    /// </summary>
    public static List<string> GetAllCountryCodes()
    {
        return new List<string>(Countries.Keys);
    }
}