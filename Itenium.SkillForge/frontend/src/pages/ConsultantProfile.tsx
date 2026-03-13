import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { Activity, BookOpen, Star, Target, Zap } from 'lucide-react';
import { Button, Badge } from '@itenium-forge/ui';
import {
  fetchConsultantProfile,
  fetchConsultantActivity,
  startSession,
  type ActivityFeedItem,
  type ConsultantGoal,
  type ConsultantProfile as ConsultantProfileData,
  type ConsultantSkill,
  type StartSessionResponse,
} from '@/api/client';
import { categoryColor } from '@/lib/skillCategories';

function SkillRow({ skill }: { skill: ConsultantSkill }) {
  const isCheckbox = skill.levelCount === 1;
  return (
    <div className="flex items-center justify-between py-2 border-b last:border-0">
      <div>
        <span
          className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium mr-2 ${categoryColor(skill.categoryName)}`}
        >
          {skill.categoryName}
        </span>
        <span className="font-medium">{skill.skillName}</span>
      </div>
      <div className="flex items-center gap-1.5">
        {isCheckbox ? (
          <Badge variant={skill.currentNiveau > 0 ? 'default' : 'outline'}>{skill.currentNiveau > 0 ? '✓' : '○'}</Badge>
        ) : (
          <div className="flex gap-0.5">
            {Array.from({ length: skill.levelCount }, (_, i) => (
              <span
                key={i}
                className={`h-1.5 w-3.5 rounded-full ${i < skill.currentNiveau ? 'bg-primary' : 'bg-primary/20'}`}
              />
            ))}
          </div>
        )}
        <span className="text-xs text-muted-foreground">
          {isCheckbox ? '' : `${skill.currentNiveau}/${skill.levelCount}`}
        </span>
      </div>
    </div>
  );
}

function GoalCard({ goal }: { goal: ConsultantGoal }) {
  const { t } = useTranslation();
  return (
    <div className="rounded-lg border p-3 space-y-1">
      <div className="flex items-center gap-2">
        <Target className="size-4 text-primary shrink-0" />
        <span className="font-medium text-sm">{goal.title}</span>
      </div>
      {goal.dueDate && (
        <p className="text-xs text-muted-foreground pl-6">
          {t('consultant.dueDate', { date: new Date(goal.dueDate).toLocaleDateString() })}
        </p>
      )}
    </div>
  );
}

const activityIcons: Record<string, React.ElementType> = {
  validation: Zap,
  resource: BookOpen,
  flag: Star,
  session: Activity,
  goal: Target,
};

function ActivityItem({ item }: { item: ActivityFeedItem }) {
  const Icon = activityIcons[item.type] ?? Activity;
  return (
    <div className="flex items-start gap-3 py-2 border-b last:border-0">
      <div className="mt-0.5 flex size-6 shrink-0 items-center justify-center rounded-full bg-muted">
        <Icon className="size-3.5 text-muted-foreground" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm">{item.description}</p>
        <p className="text-xs text-muted-foreground">{new Date(item.occurredAt).toLocaleDateString()}</p>
      </div>
    </div>
  );
}

export function ConsultantProfile() {
  const { t } = useTranslation();
  const { consultantId } = useParams({ strict: false }) as { consultantId: string };
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: profile, isLoading: loadingProfile } = useQuery<ConsultantProfileData>({
    queryKey: ['coach', 'consultant', consultantId],
    queryFn: () => fetchConsultantProfile(consultantId),
  });

  const { data: activity, isLoading: loadingActivity } = useQuery({
    queryKey: ['coach', 'consultant', consultantId, 'activity'],
    queryFn: () => fetchConsultantActivity(consultantId),
  });

  const { mutate: startSessionMutate, isPending } = useMutation<StartSessionResponse>({
    mutationFn: () => startSession(consultantId),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['coach', 'consultant', consultantId, 'activity'] });
      sessionStorage.setItem('currentSessionId', data.sessionId);
      navigate({ to: '/team/consultants/$consultantId/session', params: { consultantId } });
    },
  });

  if (loadingProfile || loadingActivity) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{profile?.name}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{profile?.email}</p>
        </div>
        <Button onClick={() => startSessionMutate()} disabled={isPending}>
          {t('consultant.startSession')}
        </Button>
      </div>

      {/* Skills */}
      <section>
        <h2 className="text-lg font-semibold mb-3">{t('consultant.skills')}</h2>
        {profile?.skills && profile.skills.length > 0 ? (
          <div className="rounded-lg border divide-y">
            {profile.skills.map((skill) => (
              <SkillRow key={skill.skillId} skill={skill} />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">{t('consultant.noSkills')}</p>
        )}
      </section>

      {/* Active Goals */}
      <section>
        <h2 className="text-lg font-semibold mb-3">{t('consultant.activeGoals')}</h2>
        {profile?.activeGoals && profile.activeGoals.length > 0 ? (
          <div className="space-y-2">
            {profile.activeGoals.map((goal) => (
              <GoalCard key={goal.id} goal={goal} />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">{t('consultant.noGoals')}</p>
        )}
      </section>

      {/* Activity Feed */}
      <section>
        <h2 className="text-lg font-semibold mb-3">{t('consultant.recentActivity')}</h2>
        {activity && activity.length > 0 ? (
          <div className="rounded-lg border">
            {activity.map((item) => (
              <ActivityItem key={item.id} item={item} />
            ))}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">{t('consultant.noActivity')}</p>
        )}
      </section>
    </div>
  );
}
