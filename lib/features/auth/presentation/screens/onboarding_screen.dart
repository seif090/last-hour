import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/constants/app_constants.dart';
import '../../../../core/routes/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../core/storage/local_storage.dart';
import '../../../../shared/widgets/custom_button.dart';
import 'package:last_hour/l10n/app_localizations.dart';

class OnboardingScreen extends StatefulWidget {
  const OnboardingScreen({super.key});

  @override
  State<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends State<OnboardingScreen> {
  final _pageController = PageController();
  int _currentPage = 0;

  List<_OnboardingPage> _getPages(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return [
      _OnboardingPage(
        icon: Icons.food_bank_rounded,
        title: l10n.onboardingTitle1,
        subtitle: l10n.onboardingSubtitle1,
        color: AppColors.primary,
      ),
      _OnboardingPage(
        icon: Icons.attach_money_rounded,
        title: l10n.onboardingTitle2,
        subtitle: l10n.onboardingSubtitle2,
        color: AppColors.secondary,
      ),
      _OnboardingPage(
        icon: Icons.eco_rounded,
        title: l10n.onboardingTitle3,
        subtitle: l10n.onboardingSubtitle3,
        color: AppColors.tertiary,
      ),
    ];
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final pages = _getPages(context);
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _buildTopBar(pages),
            Expanded(
              child: PageView.builder(
                controller: _pageController,
                itemCount: pages.length,
                onPageChanged: (index) => setState(() => _currentPage = index),
                itemBuilder: (_, index) => pages[index],
              ),
            ),
            _buildBottomSection(pages),
          ],
        ),
      ),
    );
  }

  Widget _buildTopBar(List<_OnboardingPage> pages) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            AppConstants.appName,
            style: AppTextStyles.titleLarge.copyWith(
              color: AppColors.primary,
              fontWeight: FontWeight.bold,
            ),
          ),
          TextButton(
            onPressed: _completeOnboarding,
            child: Text(
              _currentPage < pages.length - 1 ? AppLocalizations.of(context)!.onboardingSkip : '',
              style: AppTextStyles.labelLarge.copyWith(color: AppColors.grey500),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBottomSection(List<_OnboardingPage> pages) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: List.generate(
              pages.length,
              (index) => _buildDot(index),
            ),
          ),
          const SizedBox(height: 32),
          CustomButton(
            label: _currentPage == pages.length - 1
                ? AppLocalizations.of(context)!.onboardingGetStarted
                : AppLocalizations.of(context)!.onboardingNext,
            onPressed: _onNextPressed,
          ),
          if (_currentPage < pages.length - 1) ...[
            const SizedBox(height: 12),
            TextButton(
              onPressed: _completeOnboarding,
              child: Text(
                AppLocalizations.of(context)!.onboardingGetStarted,
                style: AppTextStyles.bodyMedium.copyWith(
                  color: AppColors.grey500,
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildDot(int index) {
    final isActive = index == _currentPage;
    return AnimatedContainer(
      duration: const Duration(milliseconds: 300),
      margin: const EdgeInsets.symmetric(horizontal: 4),
      width: isActive ? 24 : 8,
      height: 8,
      decoration: BoxDecoration(
        color: isActive ? AppColors.primary : AppColors.grey300,
        borderRadius: BorderRadius.circular(4),
      ),
    );
  }

  void _onNextPressed() {
    if (_currentPage < _getPages(context).length - 1) {
      _pageController.nextPage(
        duration: const Duration(milliseconds: 400),
        curve: Curves.easeInOut,
      );
    } else {
      _completeOnboarding();
    }
  }

  Future<void> _completeOnboarding() async {
    final storage = await LocalStorage.getInstance();
    await storage.setOnboardingCompleted();
    if (!mounted) return;
    context.go(RouteNames.login);
  }
}

class _OnboardingPage extends StatelessWidget {
  final IconData icon;
  final String title;
  final String subtitle;
  final Color color;

  const _OnboardingPage({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 40),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 140,
            height: 140,
            decoration: BoxDecoration(
              color: color.withAlpha(26),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 64, color: color),
          ),
          const SizedBox(height: 40),
          Text(
            title,
            style: AppTextStyles.headlineMedium.copyWith(
              fontWeight: FontWeight.bold,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 16),
          Text(
            subtitle,
            style: AppTextStyles.bodyLarge.copyWith(
              color: AppColors.grey600,
              height: 1.5,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
