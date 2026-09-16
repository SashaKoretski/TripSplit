using TripSplit.BusinessLogic.Models;

namespace TripSplit.DataAccess.Mapping;

internal static class ReceiptImageMapper
{
    public const string Columns =
        "id, receipt_id, storage_key, content_type, file_size_bytes, original_file_name, uploaded_at";

    public static ReceiptImage Map(DbDataReader r) => new(
        id:               r.GetGuid(r.GetOrdinal("id")),
        receiptId:        r.GetGuid(r.GetOrdinal("receipt_id")),
        storageKey:       r.GetString(r.GetOrdinal("storage_key")),
        contentType:      r.GetString(r.GetOrdinal("content_type")),
        fileSizeBytes:    r.GetInt64(r.GetOrdinal("file_size_bytes")),
        originalFileName: r.GetString(r.GetOrdinal("original_file_name")),
        uploadedAt:       r.GetFieldValue<DateTime>(r.GetOrdinal("uploaded_at"))
    );
}
