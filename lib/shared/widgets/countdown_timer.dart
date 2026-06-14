import 'dart:async';
import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';

class CountdownTimer extends StatefulWidget {
  final DateTime expiryTime;
  final TextStyle? textStyle;
  final VoidCallback? onExpired;

  const CountdownTimer({
    super.key,
    required this.expiryTime,
    this.textStyle,
    this.onExpired,
  });

  @override
  State<CountdownTimer> createState() => _CountdownTimerState();
}

class _CountdownTimerState extends State<CountdownTimer> {
  Timer? _timer;
  Duration _remaining = Duration.zero;

  @override
  void initState() {
    super.initState();
    _updateRemaining();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) => _updateRemaining());
  }

  @override
  void didUpdateWidget(CountdownTimer oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.expiryTime != widget.expiryTime) {
      _updateRemaining();
    }
  }

  void _updateRemaining() {
    final remaining = widget.expiryTime.difference(DateTime.now());
    if (remaining.isNegative) {
      _timer?.cancel();
      widget.onExpired?.call();
      if (mounted) setState(() => _remaining = Duration.zero);
    } else {
      if (mounted) setState(() => _remaining = remaining);
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  String _formatDuration(BuildContext context, Duration d) {
    if (d.isNegative) return AppLocalizations.of(context)!.expired;
    if (d.inDays > 0) {
      return '${d.inDays}d ${d.inHours.remainder(24)}h ${d.inMinutes.remainder(60)}m';
    }
    if (d.inHours > 0) {
      return '${d.inHours}h ${d.inMinutes.remainder(60)}m ${d.inSeconds.remainder(60)}s';
    }
    if (d.inMinutes > 0) {
      return '${d.inMinutes}m ${d.inSeconds.remainder(60)}s';
    }
    return '${d.inSeconds}s';
  }

  @override
  Widget build(BuildContext context) {
    final isExpired = _remaining.isNegative || _remaining == Duration.zero;
    final color = isExpired
        ? AppColors.grey500
        : _remaining.inMinutes < 30
            ? AppColors.discountRed
            : AppColors.primary;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(
          Icons.access_time_rounded,
          size: 16,
          color: color,
        ),
        const SizedBox(width: 4),
        Text(
          _formatDuration(context, _remaining),
          style: widget.textStyle ??
              AppTextStyles.countdown.copyWith(color: color),
        ),
      ],
    );
  }
}
