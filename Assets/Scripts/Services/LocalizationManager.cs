using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    public class LocalizationManager : MonoBehaviour
    {
        private static string currentLanguage = "en";
        private static readonly Dictionary<string, Dictionary<string, string>> translations = new(StringComparer.OrdinalIgnoreCase);

        public static string CurrentLanguage
        {
            get => currentLanguage;
            set => SetLanguage(value);
        }

        static LocalizationManager()
        {
            InitializeTranslations();
        }

        private static void InitializeTranslations()
        {
            var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Search Window Categories
                { "search.category.any", "Any Category" },
                { "search.category.shopping", "Shopping" },
                { "search.category.hangout", "Hangout" },
                { "search.category.parks_nature", "Parks & Nature" },
                { "search.category.residential", "Residential" },
                { "search.category.arts_culture", "Arts & Culture" },
                { "search.category.business", "Business" },
                { "search.category.educational", "Educational" },
                { "search.category.gaming", "Gaming" },
                { "search.category.other", "Other" },

                // Search Window Actions
                { "search.action.send_im", "Send IM" },
                { "search.action.teleport", "Teleport" },
                { "search.action.join_group", "Join Group" },
                { "search.action.action", "Action" },

                // Search Window Status
                { "search.status.online", "Online" },
                { "search.status.offline", "Offline" },
                { "search.status.not_for_sale", "Not for sale" },
                { "search.status.members", "{0} members" },

                // Media Manager Titles & Buttons
                { "media.title.parcel_media", "Parcel Media" },
                { "media.title.parcel_music", "Parcel Music" },
                { "media.title.playing_music_stream", "Playing Music Stream" },
                { "media.title.music_stream_stopped", "Music Stream Stopped" },
                { "media.title.custom_media", "Custom Media" },
                { "media.button.mute", "Mute" },
                { "media.button.unmute", "Unmute" }
            };

            var es = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Search Window Categories
                { "search.category.any", "Cualquier categoría" },
                { "search.category.shopping", "Compras" },
                { "search.category.hangout", "Punto de encuentro" },
                { "search.category.parks_nature", "Parques y naturaleza" },
                { "search.category.residential", "Residencial" },
                { "search.category.arts_culture", "Arte y cultura" },
                { "search.category.business", "Negocios" },
                { "search.category.educational", "Educativo" },
                { "search.category.gaming", "Juegos" },
                { "search.category.other", "Otro" },

                // Search Window Actions
                { "search.action.send_im", "Enviar MI" },
                { "search.action.teleport", "Teletransporte" },
                { "search.action.join_group", "Unirse al grupo" },
                { "search.action.action", "Acción" },

                // Search Window Status
                { "search.status.online", "En línea" },
                { "search.status.offline", "Desconectado" },
                { "search.status.not_for_sale", "No está a la venta" },
                { "search.status.members", "{0} miembros" },

                // Media Manager Titles & Buttons
                { "media.title.parcel_media", "Medios de la parcela" },
                { "media.title.parcel_music", "Música de la parcela" },
                { "media.title.playing_music_stream", "Reproduciendo transmisión de música" },
                { "media.title.music_stream_stopped", "Transmisión de música detenida" },
                { "media.title.custom_media", "Medios personalizados" },
                { "media.button.mute", "Silenciar" },
                { "media.button.unmute", "Reactivar sonido" }
            };

            translations["en"] = en;
            translations["es"] = es;
        }

        public static void SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return;
            currentLanguage = languageCode.ToLowerInvariant();
        }

        public static string GetString(string key, string fallback = null)
        {
            if (string.IsNullOrEmpty(key)) return fallback ?? string.Empty;

            // 1. Try target currentLanguage
            if (translations.TryGetValue(currentLanguage, out var activeDict) && activeDict.TryGetValue(key, out var activeValue))
            {
                return activeValue;
            }

            // 2. Fallback to default "en"
            if (currentLanguage != "en" && translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enValue))
            {
                return enValue;
            }

            // 3. Fallback parameter if specified, otherwise return key string identifier
            return fallback ?? key;
        }
    }
}

public class LocalizationManager : Services.LocalizationManager
{
}
