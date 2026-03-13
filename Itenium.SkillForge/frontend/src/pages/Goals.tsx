import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, BookOpen, Calendar, CheckCircle, Flag, Target } from 'lucide-react';
import { Button } from '@itenium-forge/ui';
import { toast } from 'sonner';
import { fetchMyGoals, signalReadiness, type GoalResponse } from '@/api/client';

function FlagAge({ raisedAt }: { raisedAt: string }) {
  const { t } = useTranslation();
  const now = new Date();
  const days = Math.floor((now.getTime() - new Date(raisedAt).getTime()) / 86400000);
  return (
    <p className="text-xs text-muted-foreground text-center">
      {t('goals.flagRaisedAgo', { days })}
    </p>
  );
}

function GoalCard({ goal }: { goal: GoalResponse }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const signalMutation = useMutation({
    mutationFn: () => signalReadiness(goal.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-goals'] });
      toast.success(t('goals.readinessSignaled'));
    },
    onError: () => toast.error(t('common.error')),
  });

  const isOverdue = goal.deadline && new Date(goal.deadline) < new Date();
  const hasActiveFlag = goal.readinessFlag && !goal.readinessFlag.dismissedAt;

  return (
    <div className={`rounded-xl border p-5 shadow-sm space-y-4 ${isOverdue ? 'border-red-300 bg-red-50/30 dark:border-red-800 dark:bg-red-900/10' : 'bg-card'}`}>
      {/* Header */}
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
            {goal.skill.name}
          </p>
          <div className="flex items-center gap-2 mt-1">
            <span className="text-sm text-muted-foreground">{t('goals.level')} {goal.currentNiveau}</span>
            <span className="text-muted-foreground">→</span>
            <span className="text-sm font-semibold text-primary flex items-center gap-1">
              <Target className="size-3" />
              {t('goals.level')} {goal.targetNiveau}
            </span>
          </div>
        </div>
        {isOverdue && (
          <span className="inline-flex items-center gap-1 rounded-full bg-destructive/10 px-2.5 py-0.5 text-xs font-medium text-destructive">
            <AlertCircle className="size-3" />
            {t('goals.overdue')}
          </span>
        )}
        {hasActiveFlag && (
          <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 dark:bg-amber-900/20 px-2.5 py-0.5 text-xs font-medium text-amber-700 dark:text-amber-400">
            <Flag className="size-3" />
            {t('goals.readinessFlagged')}
          </span>
        )}
      </div>

      {/* Deadline */}
      {goal.deadline && (
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <Calendar className="size-3.5" />
          {t('goals.deadline')}: {new Date(goal.deadline).toLocaleDateString()}
        </div>
      )}

      {/* Resources */}
      {goal.goalResources.length > 0 && (
        <div>
          <p className="text-xs font-medium text-muted-foreground mb-2">{t('goals.resources')}</p>
          <ul className="space-y-1">
            {goal.goalResources.map(({ resource }) => (
              <li key={resource.id} className="flex items-center gap-2 text-sm">
                <BookOpen className="size-3.5 text-muted-foreground shrink-0" />
                <a href={resource.url} target="_blank" rel="noreferrer" className="text-primary hover:underline truncate">
                  {resource.title}
                </a>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Readiness flag button */}
      {!hasActiveFlag && (
        <Button
          size="sm"
          variant="outline"
          onClick={() => signalMutation.mutate()}
          disabled={signalMutation.isPending}
          className="w-full border-primary/30 text-primary hover:bg-primary/10"
        >
          <CheckCircle className="size-4 mr-2" />
          {t('goals.signalReadiness')}
        </Button>
      )}
      {hasActiveFlag && goal.readinessFlag && (
        <FlagAge raisedAt={goal.readinessFlag.raisedAt} />
      )}
    </div>
  );
}

export function Goals() {
  const { t } = useTranslation();
  const { data: goals, isLoading } = useQuery({
    queryKey: ['my-goals'],
    queryFn: fetchMyGoals,
  });

  if (isLoading) return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">{t('goals.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('goals.subtitle')}</p>
      </div>

      {goals && goals.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {goals.map((goal) => (
            <GoalCard key={goal.id} goal={goal} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-12 text-center">
          <Target className="size-8 text-muted-foreground/50 mb-2" />
          <p className="text-sm text-muted-foreground">{t('goals.noGoals')}</p>
        </div>
      )}
    </div>
  );
}
