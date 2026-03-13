import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Activity, AlertTriangle, Flag, Search, Target, Users } from 'lucide-react';
import { fetchTeamMembers, type ConsultantSummaryDto } from '@/api/client';

// ── Consultant card ───────────────────────────────────────────────────────────

function ConsultantCard({ member }: { member: ConsultantSummaryDto }) {
  const { t } = useTranslation();
  const initials = (member.email ?? member.userId).slice(0, 2).toUpperCase();
  const hasFlags = member.activeFlagCount > 0;

  return (
    <div className="flex flex-col rounded-xl border bg-card p-5 shadow-sm transition-all hover:shadow-md">
      {/* Avatar + name */}
      <div className="flex items-start gap-3">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-primary/10 font-semibold text-primary text-sm">
          {initials}
        </div>
        <div className="min-w-0 flex-1">
          <p className="truncate font-medium text-sm leading-tight">
            {member.email ?? member.userId}
          </p>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {member.profileName ?? t('team.noProfile')} · {member.teamName}
          </p>
        </div>
        {hasFlags && (
          <AlertTriangle className="size-4 shrink-0 text-amber-500 mt-0.5" aria-label="active flags" />
        )}
      </div>

      {/* Counters */}
      <div className="mt-4 flex items-center gap-4 text-xs text-muted-foreground">
        <span className="flex items-center gap-1">
          <Target className="size-3.5" />
          {t('team.activeGoals', { count: member.activeGoalCount })}
        </span>
        {hasFlags && (
          <span className="flex items-center gap-1 text-amber-600 dark:text-amber-400 font-medium">
            <Flag className="size-3.5" />
            {t('team.activeFlags', { count: member.activeFlagCount })}
          </span>
        )}
      </div>

      {/* Action */}
      <div className="mt-4 border-t pt-3">
        <Link
          to="/members/$consultantId"
          params={{ consultantId: String(member.id) }}
          className="flex items-center justify-center gap-1.5 rounded-lg bg-primary/10 px-3 py-1.5 text-xs font-medium text-primary transition-colors hover:bg-primary/20"
        >
          <Activity className="size-3.5" />
          {t('team.viewActivity')}
        </Link>
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function TeamMembers() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const { data: members, isLoading } = useQuery({
    queryKey: ['team-members'],
    queryFn: fetchTeamMembers,
  });

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  const filtered = (members ?? []).filter(
    (m) =>
      !search ||
      (m.email ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (m.profileName ?? '').toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('team.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('team.subtitle')}</p>
        </div>
        <div className="flex items-center gap-2 rounded-lg border bg-card px-3 py-2 text-sm sm:w-64 shrink-0">
          <Search className="size-4 text-muted-foreground" />
          <input
            type="text"
            placeholder={t('team.searchPlaceholder')}
            className="flex-1 bg-transparent outline-none text-sm placeholder:text-muted-foreground"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      {/* Grid */}
      {filtered.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((member) => (
            <ConsultantCard key={member.id} member={member} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <Users className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('team.noMembers')}</p>
        </div>
      )}
    </div>
  );
}
