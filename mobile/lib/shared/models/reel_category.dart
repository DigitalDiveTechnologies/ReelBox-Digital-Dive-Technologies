/// Predefined reel categories aligned with backend [MediaCategories].
class ReelCategory {
  const ReelCategory._();

  static const String general = 'General';

  /// Short label for pills + full API value.
  static const List<({String label, String? value})> filterOptions = [
    (label: 'All', value: null),
    (label: 'General', value: 'General'),
    (label: 'Food', value: 'Food & Dining'),
    (label: 'Fashion', value: 'Fashion & Clothing'),
    (label: 'Fitness', value: 'Fitness & Health'),
    (label: 'Travel', value: 'Travel'),
    (label: 'Technology', value: 'Technology'),
    (label: 'Education', value: 'Education'),
    (label: 'Business', value: 'Business'),
    (label: 'Finance', value: 'Finance'),
    (label: 'Motivation', value: 'Motivation'),
    (label: 'Entertainment', value: 'Entertainment'),
    (label: 'Sports', value: 'Sports'),
    (label: 'Gaming', value: 'Gaming'),
    (label: 'Beauty', value: 'Beauty'),
    (label: 'Pets', value: 'Pets'),
    (label: 'Automotive', value: 'Automotive'),
    (label: 'News', value: 'News'),
    (label: 'Lifestyle', value: 'Lifestyle'),
    (label: 'DIY', value: 'DIY & Crafts'),
    (label: 'Photography', value: 'Photography'),
    (label: 'Music', value: 'Music'),
    (label: 'Comedy', value: 'Comedy'),
    (label: 'Art', value: 'Art & Design'),
    (label: 'Other', value: 'Other'),
  ];
}
