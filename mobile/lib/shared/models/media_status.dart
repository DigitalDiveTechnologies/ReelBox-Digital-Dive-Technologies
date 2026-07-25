/// Media processing statuses from SRS §13 (Media State Machine).
///
/// Transition rules and retry policy belong in domain logic later.
enum MediaStatus {
  preparing,
  queued,
  downloading,
  processing,
  completed,
  failed,
}
