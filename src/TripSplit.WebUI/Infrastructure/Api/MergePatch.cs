using System.Text.Json;

namespace TripSplit.WebUI.Infrastructure.Api;

// Небольшие хелперы для JSON Merge Patch (RFC 7396): различить "поле не передано"
// (ключ отсутствует в объекте) от "поле передано как null" - то, что плоский POCO
// с System.Text.Json сделать не может (оба случая дают default/null).
public static class MergePatch
{
    public static bool TryGetField(JsonElement body, string name, out JsonElement value) =>
        body.TryGetProperty(name, out value);

    // Для полей без осмысленного "пустого" состояния - name/value/payerId/consumerIds (docs/api/review.md)
    public static void EnsureNotNull(JsonElement field, string fieldName)
    {
        if (field.ValueKind == JsonValueKind.Null)
            throw new ArgumentException($"Field '{fieldName}' cannot be null", fieldName);
    }
}
