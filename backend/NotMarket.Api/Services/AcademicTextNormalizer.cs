using System.Globalization;
using System.Text;

namespace NotMarket.Api.Services;

public static class AcademicTextNormalizer
{
    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    public static string Normalize(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        /*
         * Baştaki ve sondaki boşlukları kaldır,
         * Türkçe kurallarıyla küçük harfe çevir.
         */
        var text =
            value.Trim()
                .ToLower(TurkishCulture);

        /*
         * Aramada ı ve i ayrımını kaldır.
         */
        text =
            text.Replace('ı', 'i');

        /*
         * Unicode karakterleri temel harf ve
         * aksan işaretlerine ayır.
         *
         * Örnek:
         * ü → u + aksan işareti
         */
        var decomposed =
            text.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder(
                decomposed.Length);

        foreach (var character in decomposed)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            /*
             * Aksan işaretlerini çıkar.
             */
            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var normalized =
            builder
                .ToString()
                .Normalize(
                    NormalizationForm.FormC);

        /*
         * Birden fazla boşluğu tek boşluğa indir.
         */
        var parts =
            normalized.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

        return string.Join(
            ' ',
            parts);
    }
}