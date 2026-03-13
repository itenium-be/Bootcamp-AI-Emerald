import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { AlertCircle, Clock, Flag, Target, TrendingUp, Users } from 'lucide-react';
import { fetchCoachDashboard, type ConsultantDashboardRow } from '@/api/client';

function ConsultantRow({ row }: { row: ConsultantDashboardRow }) {
  const { t } = useTranslation();

  return (
    <Link
      to="/coach/consultants/$userId"
      params={{ userId: row.userId }}
      className="flex items-center gap-4 p-4 rounded-xl border bg-card hover:bg-accent/50 transition-colors"
    >
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <p className="font-semibold truncate">{row.fullName}</p>
          {row.isInactive && (
            <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 dark:bg-amber-900/20 px-2 py-0.5 text-xs text-amber-700 dark:text-amber-400 shrink-0">
              <Clock className="size-3" />
              {t('coachDashboard.inactive')}
            </span>
          )}
        </div>
        {row.lastActivityAt && (
          <p className="text-xs text-muted-foreground mt-0.5">
            {t('coachDashboard.lastActivity')}: {new Date(row.lastActivityAt).toLocaleDateString()}
          </p>
        )}
      </div>

      <div className="flex items-center gap-4 shrink-0">
        {row.readinessFlagCount > 0 && (
          <div className="flex items-center gap-1 text-amber-600 dark:text-amber-400">
            <Flag className="size-4" />
            <span className="text-sm font-semibold">{row.readinessFlagCount}</span>
            {row.maxFlagAgeInDays !== null && (
              <span className="text-xs text-muted-foreground">({row.maxFlagAgeInDays}d)</span>
            )}
          </div>
        )}
        {row.overdueGoalCount > 0 && (
          <div className="flex items-center gap-1 text-destructive">
            <AlertCircle className="size-4" />
            <span className="text-sm font-semibold">{row.overdueGoalCount}</span>
          </div>
        )}
        <div className="flex items-center gap-1 text-muted-foreground">
          <Target className="size-4" />
          <span className="text-sm">{row.activeGoalCount}</span>
        </div>
      </div>
    </Link>
  );
}

export function CoachDashboard() {
  const { t } = useTranslation();
  const { data: rows, isLoading } = useQuery({
    queryKey: ['coach-dashboard'],
    queryFn: fetchCoachDashboard,
  });

  if (isLoading) return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;

  const totalFlags = rows?.reduce((sum, r) => sum + r.readinessFlagCount, 0) ?? 0;
  const totalOverdue = rows?.reduce((sum, r) => sum + r.overdueGoalCount, 0) ?? 0;
  const totalGoals = rows?.reduce((sum, r) => sum + r.activeGoalCount, 0) ?? 0;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('coachDashboard.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('coachDashboard.subtitle')}</p>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <div className="rounded-xl border bg-card p-4 space-y-1">
          <div className="flex items-center gap-2 text-muted-foreground">
            <Users className="size-4" />
            <span className="text-xs font-medium">{t('coachDashboard.consultants')}</span>
          </div>
          <p className="text-2xl font-bold">{rows?.length ?? 0}</p>
        </div>
        <div className="rounded-xl border bg-card p-4 space-y-1">
          <div className="flex items-center gap-2 text-amber-600">
            <Flag className="size-4" />
            <span className="text-xs font-medium">{t('coachDashboard.readinessFlags')}</span>
          </div>
          <p className="text-2xl font-bold">{totalFlags}</p>
        </div>
        <div className="rounded-xl border bg-card p-4 space-y-1">
          <div className="flex items-center gap-2 text-destructive">
            <AlertCircle className="size-4" />
            <span className="text-xs font-medium">{t('coachDashboard.overdueGoals')}</span>
          </div>
          <p className="text-2xl font-bold">{totalOverdue}</p>
        </div>
        <div className="rounded-xl border bg-card p-4 space-y-1">
          <div className="flex items-center gap-2 text-primary">
            <TrendingUp className="size-4" />
            <span className="text-xs font-medium">{t('coachDashboard.activeGoals')}</span>
          </div>
          <p className="text-2xl font-bold">{totalGoals}</p>
        </div>
      </div>

      {/* Consultant list — sorted: flags with oldest age first, then inactive, then others */}
      {rows && rows.length > 0 ? (
        <div className="space-y-2">
          <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide">{t('coachDashboard.teamOverview')}</h2>
          {[...rows]
            .sort((a, b) => {
              if ((b.maxFlagAgeInDays ?? -1) !== (a.maxFlagAgeInDays ?? -1))
                return (b.maxFlagAgeInDays ?? -1) - (a.maxFlagAgeInDays ?? -1);
              if (a.isInactive !== b.isInactive) return a.isInactive ? -1 : 1;
              return b.overdueGoalCount - a.overdueGoalCount;
            })
            .map((row) => (
              <ConsultantRow key={row.userId} row={row} />
            ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-12 text-center">
          <Users className="size-8 text-muted-foreground/50 mb-2" />
          <p className="text-sm text-muted-foreground">{t('coachDashboard.noConsultants')}</p>
        </div>
      )}
    </div>
  );
}
