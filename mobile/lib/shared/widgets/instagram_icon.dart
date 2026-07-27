import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';

/// Instagram brand glyph (camera outline with lens + flash dot).
class InstagramIcon extends StatelessWidget {
  const InstagramIcon({
    super.key,
    this.size = 18,
    this.color = AppColors.splashTextPrimary,
  });

  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: CustomPaint(
        painter: InstagramGlyphPainter(color: color),
      ),
    );
  }
}

/// Paints the Instagram logo glyph.
class InstagramGlyphPainter extends CustomPainter {
  const InstagramGlyphPainter({required this.color});

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final stroke = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = size.width * 0.085
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round;

    final rect = RRect.fromRectAndRadius(
      Rect.fromLTWH(
        size.width * 0.08,
        size.height * 0.08,
        size.width * 0.84,
        size.height * 0.84,
      ),
      Radius.circular(size.width * 0.22),
    );
    canvas.drawRRect(rect, stroke);

    canvas.drawCircle(
      Offset(size.width * 0.5, size.height * 0.5),
      size.width * 0.22,
      stroke,
    );

    canvas.drawCircle(
      Offset(size.width * 0.72, size.height * 0.28),
      size.width * 0.055,
      Paint()..color = color,
    );
  }

  @override
  bool shouldRepaint(covariant InstagramGlyphPainter oldDelegate) =>
      oldDelegate.color != color;
}
