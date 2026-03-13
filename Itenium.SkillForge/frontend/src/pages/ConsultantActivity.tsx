import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { ArrowLeft, BookOpen, CheckCircle2, ChevronRight, Target, Trophy, Zap } from 'lucide-react';
import {
  fetchConsultantActivityEvents,
  fetchConsultantSeniorityProgress,
  type ActivityEventDto,
  type ActivityEventType,
  type SeniorityProgressResult,
} from '@/api/client';

// ── Seniority card (reusable, mirrors Roadmap.tsx) ────────────────────────────

function SeniorityCard({ seniority }: { seniority: SeniorityProgressResult }) {
  const { t } = useTranslation();
  const isMaxed = seniority.nextLevel === null;
  const pct = seniority.required > 0 ? Math.round((seniority.met / seniority.required) * 100) : 100;

  return (
    <div
      className={`rounded-xl border p-5 shadow-sm ${
        isMaxed ? 'border-yellow-300 bg-yellow-50 dark:border-yellow-700 dark:bg-yellow-900/20' : 'bg-card'
      }`}
    >
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex items-center gap-3">
          {isMaxed ? (
            <Trophy className="size-6 text-yellow-500 shrink-0" />
          ) : (
            <Target className="size-6 text-primary shrink-0" />
          )}
          <div>
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              {isMaxed ? t('roadmap.seniority.achieved') : t('roadmap.seniority.targeting')}
            </p>
            <p className="text-xl font-bold">{isMaxed ? seniority.currentLevel : seniority.nextLevel}</p>
          </div>
        </div>
        <div className="text-sm font-medium tabular-nums text-muted-foreground">
          {seniority.met} / {seniority.required} {t('roadmap.seniority.criteria')}
        </div>
      </div>

      {!isMaxed && (
        <div className="mt-3">
          <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
            <div className="h-full rounded-full bg-primary transition-all duration-500" style={{ width: `${pct}%` }} />
          </div>
          <p className="mt-1 text-right text-xs text-muted-foreground">{pct}%</p>
        </div>
      )}

      {seniority.unmetCriteria.length > 0 && (
        <div className="mt-3 flex flex-wrap gap-2">
          {seniority.unmetCriteria.map((c) => (
            <span
              key={c.skillId}
              className="inline-flex items-center gap-1 rounded-full bg-destructive/10 px-2.5 py-0.5 text-xs text-destructive"
            >
              {c.skillName}
              <span className="font-semibold">
                {c.currentNiveau}/{c.minNiveau}
              </span>
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

// ── Activity event item ───────────────────────────────────────────────────────

const EVENT_CONFIG: Record<ActivityEventType, { icon: React.ElementType; color: string; bg: string }> = {
  SkillValidated: { icon: Zap, color: 'text-blue-600 dark:text-blue-400', bg: 'bg-blue-100 dark:bg-blue-900/30' },
  GoalAchieved: {
    icon: CheckCircle2,
    color: 'text-green-600 dark:text-green-400',
    bg: 'bg-green-100 dark:bg-green-900/30',
  },
  ResourceCompleted: {
    icon: BookOpen,
    color: 'text-purple-600 dark:text-purple-400',
    bg: 'bg-purple-100 dark:bg-purple-900/30',
  },
};

function ActivityItem({ event }: { event: ActivityEventDto }) {
  const { t } = useTranslation();
  const { icon: Icon, color, bg } = EVENT_CONFIG[event.eventType];

  return (
    <div className="flex items-start gap-3 py-3">
      <div className={`flex size-8 shrink-0 items-center justify-center rounded-full ${bg}`}>
        <Icon className={`size-4 ${color}`} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium leading-tight">{event.description}</p>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {t(`activity.eventType.${event.eventType}`)}
          {event.niveau != null && (
            <span className="ml-1.5 rounded-full bg-muted px-1.5 py-0.5 font-medium">
              {t('activity.niveau', { niveau: event.niveau })}
            </span>
          )}
        </p>
      </div>
      <time className="shrink-0 text-xs text-muted-foreground tabular-nums">
        {new Date(event.occurredAt).toLocaleDateString()}
      </time>
    </div>
  );
}

// ── Timeline grouped by month ─────────────────────────────────────────────────

function groupByMonth(events: ActivityEventDto[]) {
  const groups = new Map<string, ActivityEventDto[]>();
  for (const event of events) {
    const key = new Date(event.occurredAt).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'long',
    });
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key)?.push(event);
  }
  return groups;
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function ConsultantActivity({ consultantId }: { consultantId: number }) {
  const { t } = useTranslation();

  const { data: activity, isLoading: activityLoading } = useQuery({
    queryKey: ['consultant-activity', consultantId],
    queryFn: () => fetchConsultantActivityEvents(consultantId),
  });

  const { data: seniority } = useQuery({
    queryKey: ['consultant-seniority', consultantId],
    queryFn: () => fetchConsultantSeniorityProgress(consultantId),
  });

  if (activityLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  const groups = groupByMonth(activity ?? []);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <Link
          to="/team/members"
          className="mb-3 inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="size-3.5" />
          {t('activity.backToTeam')}
        </Link>
        <h1 className="text-3xl font-bold">{t('activity.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('activity.subtitle')}</p>
      </div>

      {/* Two-column layout on larger screens */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        {/* Seniority progress (sidebar on lg) */}
        <div className="lg:col-span-1 space-y-4">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
            {t('activity.seniorityProgress')}
          </h2>
          {seniority ? (
            <SeniorityCard seniority={seniority} />
          ) : (
            <div className="rounded-xl border border-dashed p-6 text-center text-sm text-muted-foreground">
              {t('roadmap.noProfile')}
            </div>
          )}
        </div>

        {/* Activity timeline */}
        <div className="lg:col-span-2 space-y-4">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">{t('activity.title')}</h2>

          {groups.size === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-16 text-center">
              <ChevronRight className="size-8 text-muted-foreground/50 mb-2" />
              <p className="text-sm font-medium">{t('activity.noActivity')}</p>
              <p className="mt-1 text-xs text-muted-foreground">{t('activity.noActivityHint')}</p>
            </div>
          ) : (
            <div className="rounded-xl border bg-card shadow-sm">
              {[...groups.entries()].map(([month, events], idx) => (
                <div key={month} className={idx > 0 ? 'border-t' : ''}>
                  <div className="bg-muted/40 px-5 py-2">
                    <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">{month}</p>
                  </div>
                  <div className="divide-y px-5">
                    {events.map((event, i) => (
                      <ActivityItem key={`${event.eventType}-${i}`} event={event} />
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
