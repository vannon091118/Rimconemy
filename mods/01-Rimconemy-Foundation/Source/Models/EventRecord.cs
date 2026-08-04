namespace Rimconemy.Foundation.Models
{
    /// <summary>
    /// Owner: Foundation
    /// Immutable event log record. Events are append-only.
    /// </summary>
    public class EventRecord
    {
        public int SequenceId { get; }
        public int Tick { get; }
        public string Category { get; }       // e.g. "Package", "Profile", "Save", "Diagnostic"
        public string EventType { get; }      // e.g. "Registered", "MissingDlc", "Migration"
        public string SourcePackageId { get; }
        public string Message { get; }
        public string Detail { get; }         // optional structured detail

        public EventRecord(
            int sequenceId,
            int tick,
            string category,
            string eventType,
            string sourcePackageId,
            string message,
            string detail)
        {
            SequenceId = sequenceId;
            Tick = tick;
            Category = category;
            EventType = eventType;
            SourcePackageId = sourcePackageId;
            Message = message;
            Detail = detail;
        }
    }
}
