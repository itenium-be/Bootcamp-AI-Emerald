import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useParams } from '@tanstack/react-router';
import { CheckCircle, Plus, Target, X, Zap } from 'lucide-react';
import { Button, Input, Label, Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from '@itenium-forge/ui';
import {
  fetchConsultantProfile,
  createValidation,
  createCoachGoal,
  endSession,
  type ConsultantSkill,
  type ConsultantGoal,
  type CreateValidationRequest,
  type CreateCoachGoalRequest,
} from '@/api/client';

function ValidationCard({
  skill,
  onValidate,
}: {
  skill: ConsultantSkill;
  onValidate: (skillId: number, newNiveau: number, note?: string) => void;
}) {
  const { t } = useTranslation();
  const [note, setNote] = useState('');
  const [showNote, setShowNote] = useState(false);
  const [pending, setPending] = useState<number | null>(null);
  const levels = Array.from({ length: skill.levelCount }, (_, i) => i + 1).filter((l) => l > skill.currentNiveau);

  if (levels.length === 0) return null;

  const handleValidate = (level: number) => {
    setPending(level);
    onValidate(skill.skillId, level, note || undefined);
  };

  return (
    <div className="rounded-lg border p-4 space-y-3">
      <div className="flex items-center justify-between">
        <span className="font-medium">{skill.skillName}</span>
        <span className="text-xs text-muted-foreground">
          {t('session.currentLevel', { niveau: skill.currentNiveau })}
        </span>
      </div>

      <div className="flex flex-wrap gap-2">
        {levels.map((level) => (
          <Button key={level} variant="outline" onClick={() => handleValidate(level)} disabled={pending !== null}>
            {pending === level ? <CheckCircle className="size-4 mr-1" /> : <Zap className="size-4 mr-1" />}
            {level}
          </Button>
        ))}
        <Button variant="ghost" onClick={() => setShowNote((v) => !v)}>
          {showNote ? <X className="size-4" /> : <Plus className="size-4" />}
        </Button>
      </div>

      {showNote && (
        <textarea
          className="w-full rounded-md border bg-background px-3 py-2 text-sm min-h-[80px] resize-y"
          placeholder={t('session.notePlaceholder')}
          value={note}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setNote(e.target.value)}
        />
      )}
    </div>
  );
}

function GoalForm({
  skills,
  onSubmit,
  onCancel,
}: {
  skills: { skillId: number; skillName: string }[];
  onSubmit: (title: string, description: string | null, dueDate: string | null, skillId: number | null) => void;
  onCancel: () => void;
}) {
  const { t } = useTranslation();
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [skillId, setSkillId] = useState('');

  const handleSubmit = () => {
    if (!title.trim()) return;
    onSubmit(title, description || null, dueDate || null, skillId ? Number(skillId) : null);
  };

  return (
    <div className="rounded-lg border p-4 space-y-3 bg-muted/30">
      <div className="space-y-1">
        <Label htmlFor="goal-title">{t('session.goalTitle')}</Label>
        <Input
          id="goal-title"
          placeholder={t('session.goalTitle')}
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
      </div>
      <div className="space-y-1">
        <Label htmlFor="goal-desc">{t('session.goalDescription')}</Label>
        <Input
          id="goal-desc"
          placeholder={t('session.goalDescription')}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label htmlFor="goal-due">{t('session.goalDueDate')}</Label>
          <Input id="goal-due" type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </div>
        <div className="space-y-1">
          <Label>{t('session.goalSkill')}</Label>
          <Select value={skillId} onValueChange={setSkillId}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {skills.map((s) => (
                <SelectItem key={s.skillId} value={String(s.skillId)}>
                  {s.skillName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>
      <div className="flex gap-2 justify-end">
        <Button variant="ghost" onClick={onCancel}>
          {t('session.cancel')}
        </Button>
        <Button onClick={handleSubmit} disabled={!title.trim()}>
          {t('session.createGoal')}
        </Button>
      </div>
    </div>
  );
}

export function LiveSession() {
  const { t } = useTranslation();
  const { consultantId } = useParams({ strict: false }) as { consultantId: string };
  const sessionId = sessionStorage.getItem('currentSessionId') ?? '';
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [sessionNotes, setSessionNotes] = useState('');
  const [showGoalForm, setShowGoalForm] = useState(false);

  const { data: profile, isLoading } = useQuery({
    queryKey: ['coach', 'consultant', consultantId],
    queryFn: () => fetchConsultantProfile(consultantId),
  });

  const { mutate: validate } = useMutation({
    mutationFn: (req: CreateValidationRequest) => createValidation(consultantId, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['coach', 'consultant', consultantId] });
    },
  });

  const { mutate: addGoal } = useMutation({
    mutationFn: (req: CreateCoachGoalRequest) => createCoachGoal(consultantId, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['coach', 'consultant', consultantId] });
      setShowGoalForm(false);
    },
  });

  const { mutate: end, isPending: ending } = useMutation({
    mutationFn: () => endSession(sessionId, sessionNotes),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['coach', 'consultant', consultantId, 'activity'] });
      navigate({ to: '/team/consultants/$consultantId', params: { consultantId } });
    },
  });

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  const skills = profile?.skills ?? [];
  const goals = profile?.activeGoals ?? [];

  return (
    <div className="space-y-6 max-w-2xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('session.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('session.subtitle', { name: profile?.name })}</p>
        </div>
        <Button variant="destructive" onClick={() => end()} disabled={ending}>
          {t('session.endSession')}
        </Button>
      </div>

      {/* Skill Validations */}
      <section>
        <h2 className="text-lg font-semibold mb-3">{t('session.pendingValidations')}</h2>
        <div className="space-y-3">
          {skills.filter((s) => s.currentNiveau < s.levelCount).length > 0 ? (
            skills
              .filter((s) => s.currentNiveau < s.levelCount)
              .map((skill) => (
                <ValidationCard
                  key={skill.skillId}
                  skill={skill}
                  onValidate={(skillId, newNiveau, note) => validate({ skillId, newNiveau, note })}
                />
              ))
          ) : (
            <p className="text-sm text-muted-foreground">{t('session.noSkills')}</p>
          )}
        </div>
      </section>

      {/* Active Goals */}
      {goals.length > 0 && (
        <section>
          <h2 className="text-lg font-semibold mb-3">{t('session.activeGoals')}</h2>
          <div className="space-y-2">
            {goals.map((goal: ConsultantGoal) => (
              <div key={goal.id} className="flex items-center gap-2 rounded-lg border p-3">
                <Target className="size-4 text-primary shrink-0" />
                <span className="text-sm font-medium">{goal.title}</span>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* SMART Goal Form */}
      <section>
        {showGoalForm ? (
          <GoalForm
            skills={skills}
            onSubmit={(title, description, dueDate, skillId) => addGoal({ title, description, dueDate, skillId })}
            onCancel={() => setShowGoalForm(false)}
          />
        ) : (
          <Button variant="outline" onClick={() => setShowGoalForm(true)}>
            <Plus className="size-4 mr-2" />
            {t('session.addGoal')}
          </Button>
        )}
      </section>

      {/* Session Notes */}
      <section>
        <h2 className="text-lg font-semibold mb-3">{t('session.sessionNotes')}</h2>
        <textarea
          id="session-notes"
          className="w-full rounded-md border bg-background px-3 py-2 text-sm min-h-[120px] resize-y"
          placeholder={t('session.sessionNotesPlaceholder')}
          value={sessionNotes}
          onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setSessionNotes(e.target.value)}
        />
      </section>
    </div>
  );
}
