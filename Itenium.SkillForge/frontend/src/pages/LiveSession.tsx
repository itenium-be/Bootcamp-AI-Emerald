import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, useNavigate, Link } from '@tanstack/react-router';
import { ArrowLeft, CheckCircle } from 'lucide-react';
import { Button } from '@itenium-forge/ui';
import { toast } from 'sonner';
import { fetchSessions, closeSession, fetchConsultantGoals, validateSkill, dismissReadiness, type GoalResponse } from '@/api/client';

export function LiveSession() {
  const { t } = useTranslation();
  const { sessionId } = useParams({ from: '/_authenticated/coach/sessions/$sessionId' });
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [notes, setNotes] = useState('');

  const { data: sessions, isLoading: loadingSessions } = useQuery({
    queryKey: ['sessions-all'],
    queryFn: () => fetchSessions(),
  });

  const session = sessions?.find((s) => s.id === Number(sessionId));

  const { data: goals } = useQuery({
    queryKey: ['consultant-goals', session?.consultantUserId],
    queryFn: () => fetchConsultantGoals(session?.consultantUserId ?? ''),
    enabled: !!session,
  });

  const closeSessionMutation = useMutation({
    mutationFn: () => closeSession(Number(sessionId), notes || null),
    onSuccess: () => {
      toast.success(t('session.closed'));
      queryClient.invalidateQueries({ queryKey: ['sessions'] });
      if (session) {
        navigate({ to: '/coach/consultants/$userId', params: { userId: session.consultantUserId } });
      }
    },
    onError: () => toast.error(t('common.error')),
  });

  const validateMutation = useMutation({
    mutationFn: ({ skillId, niveau }: { skillId: number; niveau: number }) =>
      validateSkill({
        consultantUserId: session?.consultantUserId ?? '',
        skillId,
        niveau,
        sessionId: Number(sessionId),
      }),
    onSuccess: () => {
      toast.success(t('session.validated'));
      queryClient.invalidateQueries({ queryKey: ['consultant-goals', session?.consultantUserId] });
    },
    onError: () => toast.error(t('common.error')),
  });

  const dismissReadinessMutation = useMutation({
    mutationFn: (goalId: number) => dismissReadiness(goalId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['consultant-goals', session?.consultantUserId] });
    },
  });

  if (loadingSessions) return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  if (!session) return <div className="p-6 text-destructive">{t('session.notFound')}</div>;

  const isClosed = !!session.closedAt;
  const activeGoals = goals?.filter((g) => g.status === 'Active') ?? [];
  const flaggedGoals = activeGoals.filter((g) => g.readinessFlag && !g.readinessFlag.dismissedAt);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/coach/consultants/$userId"
          params={{ userId: session.consultantUserId }}
          className="text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="size-5" />
        </Link>
        <div className="flex-1">
          <h1 className="text-3xl font-bold">{t('session.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {session.consultantUserId} · {new Date(session.startedAt).toLocaleDateString()}
          </p>
        </div>
        {isClosed && (
          <span className="text-xs text-muted-foreground">{t('session.closed')}</span>
        )}
      </div>

      {/* Readiness flags — prioritize */}
      {flaggedGoals.length > 0 && (
        <div>
          <h2 className="text-base font-semibold mb-3">{t('session.readyForValidation')}</h2>
          <div className="space-y-3">
            {flaggedGoals.map((goal) => (
              <ValidationCard
                key={goal.id}
                goal={goal}
                onValidate={(niveau) => validateMutation.mutate({ skillId: goal.skillId, niveau })}
                onDismiss={() => dismissReadinessMutation.mutate(goal.id)}
                disabled={isClosed}
              />
            ))}
          </div>
        </div>
      )}

      {/* All active goals */}
      <div>
        <h2 className="text-base font-semibold mb-3">{t('session.activeGoals')}</h2>
        <div className="space-y-3">
          {activeGoals.map((goal) => (
            <ValidationCard
              key={goal.id}
              goal={goal}
              onValidate={(niveau) => validateMutation.mutate({ skillId: goal.skillId, niveau })}
              onDismiss={() => dismissReadinessMutation.mutate(goal.id)}
              disabled={isClosed}
            />
          ))}
          {activeGoals.length === 0 && (
            <p className="text-sm text-muted-foreground">{t('session.noActiveGoals')}</p>
          )}
        </div>
      </div>

      {/* Notes + close */}
      {!isClosed && (
        <div className="rounded-xl border bg-card p-5 space-y-4">
          <h2 className="text-base font-semibold">{t('session.notes')}</h2>
          <textarea
            className="w-full min-h-[120px] rounded-lg border bg-background p-3 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder={t('session.notesPlaceholder')}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
          <div className="flex justify-end">
            <Button onClick={() => closeSessionMutation.mutate()} disabled={closeSessionMutation.isPending}>
              <CheckCircle className="size-4 mr-2" />
              {t('session.closeSession')}
            </Button>
          </div>
        </div>
      )}

      {isClosed && session.notes && (
        <div className="rounded-xl border bg-muted/30 p-5">
          <h2 className="text-base font-semibold mb-2">{t('session.notes')}</h2>
          <p className="text-sm whitespace-pre-wrap">{session.notes}</p>
        </div>
      )}
    </div>
  );
}

function ValidationCard({
  goal,
  onValidate,
  onDismiss,
  disabled,
}: {
  goal: GoalResponse;
  onValidate: (niveau: number) => void;
  onDismiss: () => void;
  disabled: boolean;
}) {
  const { t } = useTranslation();
  const hasFlag = goal.readinessFlag && !goal.readinessFlag.dismissedAt;

  return (
    <div className={`rounded-xl border p-4 space-y-3 ${hasFlag ? 'border-amber-300 bg-amber-50/30 dark:border-amber-700 dark:bg-amber-900/10' : 'bg-card'}`}>
      <div className="flex items-center justify-between gap-2">
        <div>
          <p className="font-semibold">{goal.skill.name}</p>
          <p className="text-xs text-muted-foreground">
            {t('goals.level')} {goal.currentNiveau} → {t('goals.level')} {goal.targetNiveau}
          </p>
        </div>
        {hasFlag && (
          <button
            type="button"
            onClick={onDismiss}
            disabled={disabled}
            className="text-xs text-muted-foreground hover:text-foreground underline"
          >
            {t('session.dismissFlag')}
          </button>
        )}
      </div>

      {!disabled && (
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: goal.skill.levelCount }, (_, i) => i + 1).map((niveau) => (
            <button
              key={niveau}
              type="button"
              onClick={() => onValidate(niveau)}
              disabled={disabled}
              className={`px-3 py-1.5 text-sm rounded-lg border font-medium transition-colors ${
                niveau === goal.currentNiveau
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'hover:bg-accent'
              }`}
            >
              {t('goals.level')} {niveau}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
