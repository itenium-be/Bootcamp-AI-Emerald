import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, CheckCircle2, ChevronDown, ChevronUp, Lock, Map, Target, Trophy } from 'lucide-react';
import {
  fetchRoadmap,
  fetchSeniorityProgress,
  type RoadmapSkillNode,
  type SeniorityProgressResult,
} from '@/api/client';
import { categoryColor } from '@/lib/skillCategories';

// ── Seniority progress card ───────────────────────────────────────────────────

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

      {/* Progress bar */}
      {!isMaxed && (
        <div className="mt-3">
          <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
            <div className="h-full rounded-full bg-primary transition-all duration-500" style={{ width: `${pct}%` }} />
          </div>
          <p className="mt-1 text-right text-xs text-muted-foreground">{pct}%</p>
        </div>
      )}

      {/* Unmet criteria chips */}
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

// ── Skill node card ───────────────────────────────────────────────────────────

function SkillNodeCard({ node }: { node: RoadmapSkillNode }) {
  const { t } = useTranslation();
  const isValidated = node.currentNiveau > 0;
  const isFullyValidated = node.currentNiveau >= node.levelCount;
  const isLocked = !node.prerequisitesMet;
  const isCheckbox = node.levelCount === 1;

  return (
    <div
      className={`flex flex-col rounded-xl border p-5 shadow-sm transition-all ${
        isFullyValidated
          ? 'border-green-300 bg-green-50/50 dark:border-green-700 dark:bg-green-900/10'
          : isValidated
            ? 'border-primary/30 bg-card'
            : isLocked
              ? 'border-dashed bg-muted/30 opacity-75'
              : 'bg-card'
      }`}
    >
      {/* Top row: category + status icons */}
      <div className="flex items-center justify-between gap-2">
        <span
          className={`inline-flex w-fit items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${categoryColor(node.categoryName)}`}
        >
          {node.categoryName}
        </span>

        <div className="flex items-center gap-1.5">
          {isFullyValidated && <CheckCircle2 className="size-4 text-green-500" />}
          {isLocked && <Lock className="size-3.5 text-muted-foreground/60" />}
          {!isLocked && !isFullyValidated && node.unmetPrerequisites.length > 0 && (
            <AlertTriangle className="size-4 text-amber-500" />
          )}
        </div>
      </div>

      {/* Skill name */}
      <h2 className="mt-3 text-base font-semibold leading-tight">{node.skillName}</h2>

      {/* Unmet prerequisite hints */}
      {node.unmetPrerequisites.length > 0 && (
        <div className="mt-2 space-y-1">
          {node.unmetPrerequisites.map((p) => (
            <p key={p.requiredSkillId} className="text-xs text-amber-600 dark:text-amber-400">
              {t('roadmap.prereqWarning', {
                skill: p.requiredSkillName,
                required: p.requiredMinNiveau,
                current: p.currentNiveau,
              })}
            </p>
          ))}
        </div>
      )}

      {/* Spacer */}
      <div className="flex-1" />

      {/* Bottom row: niveau dots + target badge */}
      <div className="mt-4 flex items-end justify-between gap-2">
        {isCheckbox ? (
          <span className="text-xs text-muted-foreground">
            {isFullyValidated ? t('roadmap.validated') : t('skills.checkboxSkill')}
          </span>
        ) : (
          <div className="flex flex-col gap-1">
            <div className="flex gap-0.5 flex-wrap">
              {Array.from({ length: node.levelCount }, (_, i) => (
                <span
                  key={i}
                  className={`h-1.5 w-3.5 rounded-full transition-colors ${
                    i < node.currentNiveau ? (isFullyValidated ? 'bg-green-500' : 'bg-primary') : 'bg-primary/20'
                  }`}
                />
              ))}
            </div>
            <span className="text-xs text-muted-foreground">
              {node.currentNiveau} / {node.levelCount}
            </span>
          </div>
        )}

        {node.targetNiveau !== null && (
          <span className="inline-flex items-center gap-1 rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
            <Target className="size-3" />
            {t('roadmap.targetLevel', { niveau: node.targetNiveau })}
          </span>
        )}
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function Roadmap() {
  const { t } = useTranslation();
  const [showAll, setShowAll] = useState(false);

  const { data: nodes, isLoading: loadingRoadmap } = useQuery({
    queryKey: ['roadmap', showAll],
    queryFn: () => fetchRoadmap(showAll),
    retry: false,
  });

  const { data: seniority, isLoading: loadingSeniority } = useQuery({
    queryKey: ['seniority-progress'],
    queryFn: fetchSeniorityProgress,
    retry: false,
  });

  if (loadingRoadmap || loadingSeniority) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  if (nodes === null) {
    return (
      <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
        <Map className="size-10 text-muted-foreground/50 mb-3" />
        <p className="text-lg font-medium">{t('roadmap.noProfile')}</p>
        <p className="mt-1 text-sm text-muted-foreground">{t('roadmap.noProfileHint')}</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold">{t('roadmap.title')}</h1>
        <p className="mt-1 text-sm text-muted-foreground">{t('roadmap.subtitle')}</p>
      </div>

      {/* Seniority progress */}
      {seniority && <SeniorityCard seniority={seniority} />}

      {/* Skill nodes */}
      {nodes && nodes.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {nodes.map((node) => (
            <SkillNodeCard key={node.skillId} node={node} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-12 text-center">
          <Map className="size-8 text-muted-foreground/50 mb-2" />
          <p className="text-sm text-muted-foreground">{t('roadmap.noSkills')}</p>
        </div>
      )}

      {/* Show all / show less toggle */}
      <div className="flex justify-center">
        <button
          type="button"
          onClick={() => setShowAll((v) => !v)}
          className="inline-flex items-center gap-2 rounded-full border bg-card px-5 py-2 text-sm font-medium shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          {showAll ? (
            <>
              <ChevronUp className="size-4" />
              {t('roadmap.showLess')}
            </>
          ) : (
            <>
              <ChevronDown className="size-4" />
              {t('roadmap.showAll')}
            </>
          )}
        </button>
      </div>
    </div>
  );
}
