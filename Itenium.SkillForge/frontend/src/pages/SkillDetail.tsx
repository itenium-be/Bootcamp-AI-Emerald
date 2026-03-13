import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from '@tanstack/react-router';
import { ArrowLeft, CheckCircle2, AlertTriangle, ChevronRight } from 'lucide-react';
import { fetchSkillDetail } from '@/api/client';
import { categoryColor } from '@/lib/skillCategories';

export function SkillDetail() {
  const { t } = useTranslation();
  const { skillId } = useParams({ from: '/_authenticated/skills/$skillId' });
  const id = Number(skillId);

  const { data: skill, isLoading } = useQuery({
    queryKey: ['skills', id],
    queryFn: () => fetchSkillDetail(id),
  });

  const backLink = (
    <Link
      to="/skills"
      className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
    >
      <ArrowLeft className="size-4" />
      {t('skills.backToSkills')}
    </Link>
  );

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  if (!skill) {
    return (
      <div className="space-y-6">
        {backLink}

        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <AlertTriangle className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('skills.notFound')}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t('skills.notFoundHint')}</p>
        </div>
      </div>
    );
  }

  const isCheckboxSkill = skill.levelCount === 1;

  return (
    <div className="space-y-6">
      {backLink}

      {/* Header card */}
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="space-y-2">
            {/* Category badge */}
            <span
              className={`inline-flex w-fit items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${categoryColor(skill.categoryName)}`}
            >
              {skill.categoryName}
            </span>

            {/* Skill name */}
            <h1 className="text-3xl font-bold">{skill.name}</h1>

            {/* Description */}
            {skill.description && <p className="text-muted-foreground leading-relaxed">{skill.description}</p>}
          </div>

          {/* Checkbox skill badge */}
          {isCheckboxSkill && (
            <div className="flex shrink-0 flex-col items-start sm:items-end gap-1">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-green-100 px-3 py-1 text-sm font-medium text-green-800 dark:bg-green-900/40 dark:text-green-300">
                <CheckCircle2 className="size-4" />
                {t('skills.checkboxSkill')}
              </span>
              <p className="text-xs text-muted-foreground">{t('skills.checkboxSkillHint')}</p>
            </div>
          )}

          {/* Level count badge */}
          {!isCheckboxSkill && (
            <div className="shrink-0 text-right">
              <span className="text-2xl font-bold">{skill.levelCount}</span>
              <p className="text-xs text-muted-foreground">{t('skills.levels')}</p>
            </div>
          )}
        </div>
      </div>

      {/* Levels */}
      {!isCheckboxSkill && skill.levels.length > 0 && (
        <div className="space-y-3">
          <h2 className="text-lg font-semibold">{t('skills.levels')}</h2>
          <div className="space-y-2">
            {skill.levels.map((level) => (
              <div key={level.niveau} className="flex items-start gap-4 rounded-lg border bg-card p-4 shadow-sm">
                {/* Level number */}
                <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-bold text-primary">
                  {level.niveau}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-xs font-medium text-muted-foreground mb-0.5">
                    {t('skills.levelN', { niveau: level.niveau })}
                  </p>
                  <p className="text-sm leading-relaxed">{level.descriptor}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Prerequisites */}
      {skill.prerequisites.length > 0 && (
        <div className="space-y-3">
          <div>
            <h2 className="text-lg font-semibold">{t('skills.prerequisites')}</h2>
            <p className="text-sm text-muted-foreground">{t('skills.prerequisiteHint')}</p>
          </div>
          <div className="space-y-2">
            {skill.prerequisites.map((prereq) => (
              <Link
                key={prereq.requiredSkillId}
                to="/skills/$skillId"
                params={{ skillId: String(prereq.requiredSkillId) }}
                className="group flex items-center justify-between rounded-lg border bg-card p-4 shadow-sm transition-all hover:shadow-md hover:border-primary/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              >
                <div>
                  <p className="font-medium group-hover:text-primary transition-colors">{prereq.requiredSkillName}</p>
                  <p className="text-xs text-muted-foreground">
                    {t('skills.prerequisiteMinLevel', { niveau: prereq.requiredMinNiveau })}
                  </p>
                </div>
                <ChevronRight className="size-4 text-muted-foreground/50 group-hover:text-primary transition-colors" />
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
