using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;

namespace UTS.WPF.Localization
{
    public static class LocalizationService
    {
        public static void ApplyCulture(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var dictUri = cultureName.StartsWith("cs", StringComparison.OrdinalIgnoreCase)
                ? new Uri("Strings.cs-CZ.xaml", UriKind.Relative)
                : new Uri("Strings.en-US.xaml", UriKind.Relative);

            var app = Application.Current;
            var merged = app.Resources.MergedDictionaries;

            // Replace the first "Strings.*" dictionary (simple convention).
            var existing = merged.FirstOrDefault(d => d.Source != null &&
                                                      d.Source.OriginalString.StartsWith("Strings.", StringComparison.OrdinalIgnoreCase));
            if (existing != null) merged.Remove(existing);

            merged.Add(new ResourceDictionary { Source = dictUri });
        }
    }
}
