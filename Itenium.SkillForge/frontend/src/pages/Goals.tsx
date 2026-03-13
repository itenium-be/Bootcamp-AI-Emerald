import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle,
  Bell,
  BellOff,
  BookOpen,
  CalendarDays,
  CheckCircle2,
  ExternalLink,
  Flag,
  Target,
} from 'lucide-react';
import {
  dismissReadinessFlag,
  fetchMyGoals,
  raiseReadinessFlag,
  type GoalDto,
  type GoalStatus,
  type LinkedResourceDto,
} from '@/api/client';
import { categoryColor } from '@/lib/skillCategories';

// ── Resource list ─────────────────────────────────────────────────────────────

function ResourceItem({ resource }: { resource: LinkedResourceDto }) {
  const { t } = useTranslation();

  const typeIcon: Record<string, string> = {
    Article: '📄',
    Video: '🎬',
    Book: '📚',
    Course: '🎓',
    Documentation: '📖',
    Other: '🔗',
  };

  return (
    <div className="flex items-center gap-2 rounded-lg border bg-muted/30 px-3 py-2 text-sm">
      <span className="shrink-0 text-base">{typeIcon[resource.type] ?? '🔗'}</span>
      <div className="flex min-w-0 flex-1 flex-col">
        <a
          href={resource.url}
          target="_blank"
          rel="noopener noreferrer"
          className="flex items-center gap-1 truncate font-medium hover:text-primary"
        >
          {resource.title}
          <ExternalLink className="size-3 shrink-0 text-muted-foreground" />
        </a>
        <span className="text-xs text-muted-foreground">{resource.type}</span>
      </div>
      {resource.isCompleted && (
        <CheckCircle2 className="size-4 shrink-0 text-green-500" aria-label={t('goals.resourceCompleted')} />
      )}
    </div>
  );
}

// ── Goal card ─────────────────────────────────────────────────────────────────

function GoalCard({ goal }: { goal: GoalDto }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [flagError, setFlagError] = useState<string | null>(null);

  const isAchieved = goal.status === 'Achieved';
  const isCancelled = goal.status === 'Cancelled';
  const hasActiveFlag = goal.activeReadinessFlag !== null;
  const progress = goal.targetNiveau > 0 ? Math.round((goal.currentNiveau / goal.targetNiveau) * 100) : 0;

  const deadline = goal.deadline ? new Date(goal.deadline) : null;
  const isOverdue = deadline && !isAchieved && !isCancelled && deadline < new Date();

  const raiseMutation = useMutation({
    mutationFn: () => raiseReadinessFlag(goal.id),
    onSuccess: () => {
      setFlagError(null);
      queryClient.invalidateQueries({ queryKey: ['my-goals'] });
    },
    onError: () => setFlagError(t('goals.flagError')),
  });

  const dismissMutation = useMutation({
    mutationFn: () => dismissReadinessFlag(goal.id),
    onSuccess: () => {
      setFlagError(null);
      queryClient.invalidateQueries({ queryKey: ['my-goals'] });
    },
    onError: () => setFlagError(t('goals.flagError')),
  });

  const statusColors: Record<GoalStatus, string> = {
    Active: 'border-primary/30 bg-card',
    Achieved: 'border-green-300 bg-green-50/50 dark:border-green-700 dark:bg-green-900/10',
    Cancelled: 'border-muted bg-muted/20 opacity-60',
  };

  return (
    <div className={`flex flex-col rounded-xl border p-5 shadow-sm transition-all ${statusColors[goal.status]}`}>
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <span
            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${categoryColor('Language & Runtime')}`}
          >
            {t(`goals.status.${goal.status.toLowerCase()}`)}
          </span>
          <h3 className="mt-2 text-base font-semibold leading-tight">{goal.skillName}</h3>
        </div>

        {isAchieved && <CheckCircle2 className="size-5 text-green-500 shrink-0 mt-1" />}
      </div>

      {/* Progress */}
      <div className="mt-4">
        <div className="flex items-center justify-between text-xs text-muted-foreground mb-1">
          <span>
            {t('goals.level')} {goal.currentNiveau} → {goal.targetNiveau}
          </span>
          <span className="font-medium tabular-nums">{progress}%</span>
        </div>
        <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
          <div
            className={`h-full rounded-full transition-all duration-500 ${isAchieved ? 'bg-green-500' : 'bg-primary'}`}
            style={{ width: `${Math.min(progress, 100)}%` }}
          />
        </div>
      </div>

      {/* Deadline */}
      {deadline && (
        <div
          className={`mt-3 flex items-center gap-1.5 text-xs ${isOverdue ? 'text-destructive' : 'text-muted-foreground'}`}
        >
          <CalendarDays className="size-3.5 shrink-0" />
          {isOverdue && <AlertTriangle className="size-3 shrink-0" />}
          {deadline.toLocaleDateString()}
        </div>
      )}

      {/* Resources */}
      {goal.resources.length > 0 && (
        <div className="mt-4 flex flex-col gap-1.5">
          <p className="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
            <BookOpen className="size-3.5" />
            {t('goals.resources')} ({goal.resources.filter((r) => r.isCompleted).length}/{goal.resources.length})
          </p>
          {goal.resources.map((r) => (
            <ResourceItem key={r.resourceId} resource={r} />
          ))}
        </div>
      )}

      <div className="flex-1" />

      {/* Readiness flag */}
      {!isAchieved && !isCancelled && (
        <div className="mt-4 border-t pt-4">
          {hasActiveFlag ? (
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-2 rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700 dark:bg-amber-900/20 dark:text-amber-300">
                <Bell className="size-3.5 shrink-0" />
                <span>{t('goals.flagActive', { days: goal.activeReadinessFlag?.ageDays ?? 0 })}</span>
              </div>
              <button
                type="button"
                onClick={() => dismissMutation.mutate()}
                disabled={dismissMutation.isPending}
                className="flex items-center justify-center gap-2 rounded-lg border px-3 py-1.5 text-xs font-medium transition-colors hover:bg-accent disabled:opacity-50"
              >
                <BellOff className="size-3.5" />
                {t('goals.dismissFlag')}
              </button>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => raiseMutation.mutate()}
              disabled={raiseMutation.isPending}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-primary/10 px-3 py-2 text-xs font-medium text-primary transition-colors hover:bg-primary/20 disabled:opacity-50"
            >
              <Flag className="size-3.5" />
              {t('goals.raiseFlag')}
            </button>
          )}
          {flagError && <p className="mt-1 text-xs text-destructive">{flagError}</p>}
        </div>
      )}
    </div>
  );
}

// ── Status filter tabs ────────────────────────────────────────────────────────

function StatusTabs({
  selected,
  onChange,
  counts,
}: {
  selected: GoalStatus | 'All';
  onChange: (s: GoalStatus | 'All') => void;
  counts: Record<GoalStatus | 'All', number>;
}) {
  const { t } = useTranslation();
  const tabs: (GoalStatus | 'All')[] = ['All', 'Active', 'Achieved', 'Cancelled'];

  return (
    <div className="flex flex-wrap gap-2">
      {tabs.map((tab) => (
        <button
          key={tab}
          type="button"
          onClick={() => onChange(tab)}
          className={`inline-flex items-center gap-1.5 rounded-full border px-4 py-1.5 text-sm font-medium transition-colors ${
            selected === tab
              ? 'border-primary bg-primary text-primary-foreground'
              : 'bg-card hover:bg-accent hover:text-accent-foreground'
          }`}
        >
          {tab === 'All' ? t('goals.allGoals') : t(`goals.status.${tab.toLowerCase()}`)}
          <span
            className={`rounded-full px-1.5 py-0.5 text-xs font-bold ${
              selected === tab ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-muted text-muted-foreground'
            }`}
          >
            {counts[tab]}
          </span>
        </button>
      ))}
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function Goals() {
  const { t } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<GoalStatus | 'All'>('All');

  const { data: goals, isLoading } = useQuery({
    queryKey: ['my-goals'],
    queryFn: fetchMyGoals,
    retry: false,
  });

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  if (!goals || goals.length === 0) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold">{t('goals.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('goals.subtitle')}</p>
        </div>
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <Target className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('goals.noGoals')}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t('goals.noGoalsHint')}</p>
        </div>
      </div>
    );
  }

  const counts: Record<GoalStatus | 'All', number> = {
    All: goals.length,
    Active: goals.filter((g) => g.status === 'Active').length,
    Achieved: goals.filter((g) => g.status === 'Achieved').length,
    Cancelled: goals.filter((g) => g.status === 'Cancelled').length,
  };

  const filtered = statusFilter === 'All' ? goals : goals.filter((g) => g.status === statusFilter);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">{t('goals.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('goals.subtitle')}</p>
      </div>

      {/* Status filter */}
      <StatusTabs selected={statusFilter} onChange={setStatusFilter} counts={counts} />

      {/* Goal cards */}
      {filtered.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((goal) => (
            <GoalCard key={goal.id} goal={goal} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-12 text-center">
          <Target className="size-8 text-muted-foreground/50 mb-2" />
          <p className="text-sm text-muted-foreground">{t('goals.noGoalsInFilter')}</p>
        </div>
      )}
    </div>
  );
}
