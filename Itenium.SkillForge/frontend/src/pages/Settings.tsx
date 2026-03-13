import { useTranslation } from 'react-i18next';
import { Sun, Moon, Monitor, Globe, User, Mail, Shield, Cpu } from 'lucide-react';
import { useAuthStore, useThemeStore } from '@/stores';

type Theme = 'light' | 'dark' | 'system';

const languages = [
  { code: 'en', label: 'English', flag: '🇬🇧' },
  { code: 'nl', label: 'Nederlands', flag: '🇧🇪' },
];

// ── Profile section ────────────────────────────────────────────────────────────

function ProfileSection() {
  const { t } = useTranslation();
  const { user } = useAuthStore();

  return (
    <section className="rounded-lg border bg-card p-6">
      <div className="mb-4 flex items-center gap-2">
        <User className="size-4 text-muted-foreground" />
        <h2 className="text-base font-semibold">{t('settings.profile')}</h2>
      </div>
      <div className="space-y-4">
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <p className="mb-1 text-xs font-medium text-muted-foreground">{t('settings.name')}</p>
            <div className="flex items-center gap-2 rounded-md border bg-muted/30 px-3 py-2">
              <User className="size-3.5 shrink-0 text-muted-foreground" />
              <span className="text-sm">{user?.name ?? '—'}</span>
            </div>
          </div>
          <div>
            <p className="mb-1 text-xs font-medium text-muted-foreground">{t('settings.email')}</p>
            <div className="flex items-center gap-2 rounded-md border bg-muted/30 px-3 py-2">
              <Mail className="size-3.5 shrink-0 text-muted-foreground" />
              <span className="text-sm">{user?.email ?? '—'}</span>
            </div>
          </div>
        </div>
        <div>
          <p className="mb-2 text-xs font-medium text-muted-foreground">{t('settings.roles')}</p>
          {user?.roles && user.roles.length > 0 ? (
            <div className="flex flex-wrap gap-2">
              {user.roles.map((role) => (
                <span
                  key={role}
                  className="inline-flex items-center gap-1 rounded-full border bg-primary/10 px-2.5 py-0.5 text-xs font-medium text-primary"
                >
                  <Shield className="size-3" />
                  {role}
                </span>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">{t('settings.noRoles')}</p>
          )}
        </div>
      </div>
    </section>
  );
}

// ── Appearance section ─────────────────────────────────────────────────────────

function AppearanceSection() {
  const { t } = useTranslation();
  const { theme, setTheme } = useThemeStore();

  const options: { value: Theme; label: string; icon: React.ReactNode }[] = [
    { value: 'light', label: t('settings.light'), icon: <Sun className="size-4" /> },
    { value: 'dark', label: t('settings.dark'), icon: <Moon className="size-4" /> },
    { value: 'system', label: t('settings.system'), icon: <Monitor className="size-4" /> },
  ];

  return (
    <section className="rounded-lg border bg-card p-6">
      <div className="mb-1 flex items-center gap-2">
        <Sun className="size-4 text-muted-foreground" />
        <h2 className="text-base font-semibold">{t('settings.appearance')}</h2>
      </div>
      <p className="mb-4 text-xs text-muted-foreground">{t('settings.themeDescription')}</p>
      <div className="flex gap-2">
        {options.map(({ value, label, icon }) => (
          <button
            key={value}
            type="button"
            onClick={() => setTheme(value)}
            className={`flex flex-1 flex-col items-center gap-2 rounded-lg border px-4 py-3 text-sm font-medium transition-colors ${
              theme === value
                ? 'border-primary bg-primary text-primary-foreground'
                : 'hover:bg-accent hover:text-accent-foreground'
            }`}
          >
            {icon}
            {label}
          </button>
        ))}
      </div>
    </section>
  );
}

// ── Language section ───────────────────────────────────────────────────────────

function LanguageSection() {
  const { t, i18n } = useTranslation();

  function handleLanguageChange(code: string) {
    i18n.changeLanguage(code);
    localStorage.setItem('language', code);
  }

  return (
    <section className="rounded-lg border bg-card p-6">
      <div className="mb-1 flex items-center gap-2">
        <Globe className="size-4 text-muted-foreground" />
        <h2 className="text-base font-semibold">{t('settings.language')}</h2>
      </div>
      <p className="mb-4 text-xs text-muted-foreground">{t('settings.languageDescription')}</p>
      <div className="flex gap-2">
        {languages.map(({ code, label, flag }) => (
          <button
            key={code}
            type="button"
            onClick={() => handleLanguageChange(code)}
            className={`flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
              i18n.language === code
                ? 'border-primary bg-primary text-primary-foreground'
                : 'hover:bg-accent hover:text-accent-foreground'
            }`}
          >
            <span className="text-base">{flag}</span>
            {label}
          </button>
        ))}
      </div>
    </section>
  );
}

// ── Meme section ───────────────────────────────────────────────────────────────

function MemeSection() {
  const { t } = useTranslation();
  const { user } = useAuthStore();

  return (
    <section className="rounded-lg border bg-card p-6">
      <div className="mb-4 flex items-center gap-2">
        <Cpu className="size-4 text-muted-foreground" />
        <h2 className="text-base font-semibold">{t('settings.memeTitle')}</h2>
      </div>
      <div className="relative overflow-hidden rounded-xl border-2 border-dashed border-primary/40 bg-muted/20 p-6 text-center">
        {/* Stamp watermark */}
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center opacity-5">
          <span className="rotate-[-25deg] text-7xl font-black text-primary">✓</span>
        </div>

        <p className="text-xs font-bold uppercase tracking-widest text-muted-foreground">Itenium SkillForge</p>
        <p className="mt-2 text-2xl font-black">{t('settings.memeSubtitle')}</p>
        <div className="mx-auto my-4 h-px w-24 bg-border" />
        <p className="mx-auto max-w-sm text-xs text-muted-foreground">{t('settings.memeBody')}</p>
        <div className="mx-auto my-4 h-px w-24 bg-border" />
        <p className="text-lg font-bold text-primary">{user?.name ?? 'Anonymous Developer'}</p>
        <div className="mt-4 inline-flex items-center rounded-full border border-primary bg-primary/10 px-4 py-1.5">
          <span className="text-sm font-bold text-primary">{t('settings.memeStamp')}</span>
        </div>
        <p className="mt-3 text-[10px] italic text-muted-foreground/60">{t('settings.memeDisclaimer')}</p>
      </div>
    </section>
  );
}

// ── Main page ──────────────────────────────────────────────────────────────────

export function Settings() {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('settings.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('settings.subtitle')}</p>
      </div>

      <ProfileSection />
      <AppearanceSection />
      <LanguageSection />
      <MemeSection />
    </div>
  );
}
