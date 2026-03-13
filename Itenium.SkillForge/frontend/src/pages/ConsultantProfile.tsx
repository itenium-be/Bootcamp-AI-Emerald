import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from '@tanstack/react-router';
import { ArrowLeft, CheckCircle, Clock, Flag, Play } from 'lucide-react';
import { Button } from '@itenium-forge/ui';
import { toast } from 'sonner';
import { fetchConsultantGoals, fetchSessions, startSession } from '@/api/client';

export function ConsultantProfile() {
  const { t } = useTranslation();
  const { userId } = useParams({ from: '/_authenticated/coach/consultants/$userId' });
  const queryClient = useQueryClient();

  const { data: goals, isLoading: loadingGoals } = useQuery({
    queryKey: ['consultant-goals', userId],
    queryFn: () => fetchConsultantGoals(userId),
  });

  const { data: sessions, isLoading: loadingSessions } = useQuery({
    queryKey: ['sessions', userId],
    queryFn: () => fetchSessions(userId),
  });

  const startSessionMutation = useMutation({
    mutationFn: () => startSession(userId),
    onSuccess: (session) => {
      queryClient.invalidateQueries({ queryKey: ['sessions', userId] });
      toast.success(t('session.started'));
      window.location.href = `/coach/sessions/${session.id}`;
    },
    onError: () => toast.error(t('common.error')),
  });

  if (loadingGoals || loadingSessions) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  const activeGoals = goals?.filter((g) => g.status === 'Active') ?? [];
  const flaggedGoals = activeGoals.filter((g) => g.readinessFlag && !g.readinessFlag.dismissedAt);
  const overdueGoals = activeGoals.filter((g) => g.deadline && new Date(g.deadline) < new Date());
  const openSession = sessions?.find((s) => !s.closedAt);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link to="/coach/dashboard" className="text-muted-foreground hover:text-foreground">
          <ArrowLeft className="size-5" />
        </Link>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{userId}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('consultantProfile.subtitle')}</p>
        </div>
        {openSession ? (
          <Button asChild>
            <Link to="/coach/sessions/$sessionId" params={{ sessionId: String(openSession.id) }}>
              <Play className="size-4 mr-2" />
              {t('session.resumeSession')}
            </Link>
          </Button>
        ) : (
          <Button onClick={() => startSessionMutation.mutate()} disabled={startSessionMutation.isPending}>
            <Play className="size-4 mr-2" />
            {t('session.startSession')}
          </Button>
        )}
      </div>

      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        <div className="rounded-xl border bg-card p-4 text-center">
          <p className="text-2xl font-bold">{activeGoals.length}</p>
          <p className="text-xs text-muted-foreground">{t('consultantProfile.activeGoals')}</p>
        </div>
        <div className="rounded-xl border bg-card p-4 text-center">
          <p className={`text-2xl font-bold ${flaggedGoals.length > 0 ? 'text-amber-600' : ''}`}>{flaggedGoals.length}</p>
          <p className="text-xs text-muted-foreground">{t('consultantProfile.readinessFlags')}</p>
        </div>
        <div className="rounded-xl border bg-card p-4 text-center">
          <p className={`text-2xl font-bold ${overdueGoals.length > 0 ? 'text-destructive' : ''}`}>{overdueGoals.length}</p>
          <p className="text-xs text-muted-foreground">{t('consultantProfile.overdueGoals')}</p>
        </div>
      </div>

      {/* Goals */}
      <div>
        <h2 className="text-base font-semibold mb-3">{t('consultantProfile.goalsTitle')}</h2>
        <div className="space-y-3">
          {activeGoals.map((goal) => (
            <div key={goal.id} className="flex items-center gap-4 rounded-xl border bg-card p-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <p className="font-medium truncate">{goal.skill.name}</p>
                  {goal.readinessFlag && !goal.readinessFlag.dismissedAt && (
                    <Flag className="size-3.5 text-amber-500 shrink-0" />
                  )}
                </div>
                <p className="text-xs text-muted-foreground">
                  {t('goals.level')} {goal.currentNiveau} → {goal.targetNiveau}
                  {goal.deadline && ` · ${new Date(goal.deadline).toLocaleDateString()}`}
                </p>
              </div>
              {goal.deadline && new Date(goal.deadline) < new Date() && (
                <span className="text-xs text-destructive shrink-0">{t('goals.overdue')}</span>
              )}
            </div>
          ))}
          {activeGoals.length === 0 && (
            <p className="text-sm text-muted-foreground">{t('consultantProfile.noActiveGoals')}</p>
          )}
        </div>
      </div>

      {/* Session history */}
      <div>
        <h2 className="text-base font-semibold mb-3">{t('consultantProfile.sessionsTitle')}</h2>
        <div className="space-y-2">
          {sessions?.map((session) => (
            <Link
              key={session.id}
              to="/coach/sessions/$sessionId"
              params={{ sessionId: String(session.id) }}
              className="flex items-center gap-4 rounded-xl border bg-card p-3 hover:bg-accent/50 transition-colors"
            >
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium">{new Date(session.startedAt).toLocaleDateString()}</p>
                {session.notes && (
                  <p className="text-xs text-muted-foreground truncate">{session.notes}</p>
                )}
              </div>
              {session.closedAt ? (
                <CheckCircle className="size-4 text-green-500 shrink-0" />
              ) : (
                <Clock className="size-4 text-amber-500 shrink-0" />
              )}
            </Link>
          ))}
          {(!sessions || sessions.length === 0) && (
            <p className="text-sm text-muted-foreground">{t('consultantProfile.noSessions')}</p>
          )}
        </div>
      </div>
    </div>
  );
}
