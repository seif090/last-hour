import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../auth/presentation/providers/auth_provider.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final authState = ref.watch(authProvider);

    return Scaffold(
      body: SafeArea(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            _buildProfileHeader(context, theme, authState),
            _buildMenuSection(theme, 'Account', const [
              _MenuItem(Icons.person_outline, 'Edit Profile'),
              _MenuItem(Icons.location_on_outlined, 'Saved Addresses'),
              _MenuItem(Icons.favorite_outline, 'Favorite Stores'),
              _MenuItem(Icons.payment_outlined, 'Payment Methods'),
            ]),
            _buildMenuSection(theme, 'Preferences', const [
              _MenuItem(Icons.notifications_outlined, 'Notifications'),
              _MenuItem(Icons.language_outlined, 'Language'),
              _MenuItem(Icons.dark_mode_outlined, 'Dark Mode'),
            ]),
            _buildMenuSection(theme, 'Support', const [
              _MenuItem(Icons.help_outline, 'Help Center'),
              _MenuItem(Icons.info_outline, 'About'),
            ]),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
              child: CustomButton(
                label: 'Sign Out',
                onPressed: () {
                  ref.read(authProvider.notifier).logout();
                },
                type: CustomButtonType.outline,
                foregroundColor: AppColors.error,
                borderColor: AppColors.error.withAlpha(77),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildProfileHeader(
    BuildContext context,
    ThemeData theme,
    AuthState authState,
  ) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
      child: Row(
        children: [
          Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              gradient: AppColors.primaryGradient,
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text(
                (authState.user?.name.isNotEmpty == true
                        ? authState.user!.name[0]
                        : 'U')
                    .toUpperCase(),
                style: AppTextStyles.headlineMedium.copyWith(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
          const SizedBox(width: 18),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  authState.user?.name ?? 'User',
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  authState.user?.email ?? 'user@example.com',
                  style: AppTextStyles.bodyMedium.copyWith(
                    color: AppColors.grey500,
                  ),
                ),
              ],
            ),
          ),
          IconButton(
            icon: const Icon(Icons.edit_outlined, size: 20),
            onPressed: () {},
            color: AppColors.primary,
          ),
        ],
      ),
    );
  }

  Widget _buildMenuSection(
    ThemeData theme,
    String title,
    List<_MenuItem> items,
  ) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 8),
          child: Text(
            title.toUpperCase(),
            style: AppTextStyles.caption.copyWith(
              letterSpacing: 1.2,
              fontWeight: FontWeight.w600,
              color: AppColors.grey500,
            ),
          ),
        ),
        Container(
          margin: const EdgeInsets.symmetric(horizontal: 16),
          decoration: BoxDecoration(
            color: theme.cardColor,
            borderRadius: BorderRadius.circular(16),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withAlpha(8),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Column(
            children: items.map((item) {
              final isLast = items.last == item;
              return Column(
                children: [
                  ListTile(
                    contentPadding: const EdgeInsets.symmetric(horizontal: 16),
                    leading: Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: AppColors.primary.withAlpha(26),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Icon(item.icon, size: 20, color: AppColors.primary),
                    ),
                    title: Text(
                      item.title,
                      style: AppTextStyles.labelLarge.copyWith(
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    trailing: const Icon(
                      Icons.chevron_right_rounded,
                      size: 20,
                      color: AppColors.grey400,
                    ),
                    onTap: () {},
                  ),
                  if (!isLast) const Divider(height: 1, indent: 72),
                ],
              );
            }).toList(),
          ),
        ),
      ],
    );
  }
}

class _MenuItem {
  final IconData icon;
  final String title;

  const _MenuItem(this.icon, this.title);
}
