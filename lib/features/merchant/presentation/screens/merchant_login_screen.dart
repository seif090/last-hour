import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../../shared/widgets/custom_text_field.dart';
import '../providers/merchant_providers.dart';

class MerchantLoginScreen extends ConsumerStatefulWidget {
  const MerchantLoginScreen({super.key});

  @override
  ConsumerState<MerchantLoginScreen> createState() => _MerchantLoginScreenState();
}

class _MerchantLoginScreenState extends ConsumerState<MerchantLoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(merchantAuthProvider);
    ref.listen(merchantAuthProvider, (_, next) {
      if (next.isAuthenticated) {
        context.go('/merchant/dashboard');
      }
    });

    return Scaffold(
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SizedBox(height: 48),
                Container(
                  width: 64,
                  height: 64,
                  decoration: BoxDecoration(
                    color: AppColors.secondary.withAlpha(26),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: const Icon(Icons.store_rounded, size: 32, color: AppColors.secondary),
                ),
                const SizedBox(height: 24),
                Text(AppLocalizations.of(context)!.merchantSignIn, style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                Text(AppLocalizations.of(context)!.merchantSignInSubtitle, style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500)),
                const SizedBox(height: 32),
                CustomTextField(
                  controller: _emailController,
                  label: AppLocalizations.of(context)!.email,
                  hintText: AppLocalizations.of(context)!.businessEmailHint,
                  keyboardType: TextInputType.emailAddress,
                  prefixIcon: const Icon(Icons.email_outlined),
                  validator: (v) {
                    if (v == null || v.isEmpty) return AppLocalizations.of(context)!.emailRequired;
                    if (!v.contains('@')) return AppLocalizations.of(context)!.invalidEmail;
                    return null;
                  },
                ),
                const SizedBox(height: 16),
                CustomTextField(
                  controller: _passwordController,
                  label: AppLocalizations.of(context)!.password,
                  hintText: AppLocalizations.of(context)!.passwordHint,
                  obscureText: _obscurePassword,
                  prefixIcon: const Icon(Icons.lock_outlined),
                  suffixIcon: IconButton(
                    icon: Icon(_obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                    onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                  ),
                  validator: (v) {
                    if (v == null || v.isEmpty) return AppLocalizations.of(context)!.passwordRequired;
                    if (v.length < 6) return AppLocalizations.of(context)!.passwordMinChars;
                    return null;
                  },
                ),
                const SizedBox(height: 12),
                Align(
                  alignment: Alignment.centerRight,
                  child: TextButton(
                    onPressed: () {},
                    child: Text(AppLocalizations.of(context)!.merchantForgotPassword, style: const TextStyle(color: AppColors.secondary)),
                  ),
                ),
                const SizedBox(height: 24),
                if (state.isLoading)
                  const Center(child: CircularProgressIndicator())
                else
                  CustomButton(
                    label: AppLocalizations.of(context)!.signIn,
                    onPressed: () {
                      if (_formKey.currentState?.validate() ?? false) {
                        ref.read(merchantAuthProvider.notifier).login(
                          email: _emailController.text.trim(),
                          password: _passwordController.text.trim(),
                        );
                      }
                    },
                  ),
                if (state.error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 8),
                    child: Text(
                      state.error!,
                      style: const TextStyle(color: AppColors.discountRed, fontSize: 13),
                      textAlign: TextAlign.center,
                    ),
                  ),
                const SizedBox(height: 32),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(AppLocalizations.of(context)!.noMerchantAccount, style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500)),
                    TextButton(
                      onPressed: () {},
                      child: Text(AppLocalizations.of(context)!.merchantSignUp, style: const TextStyle(color: AppColors.secondary, fontWeight: FontWeight.bold)),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
