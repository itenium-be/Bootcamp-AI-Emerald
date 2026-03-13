import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CheckCircle2, ExternalLink, Filter, Library, Plus, ThumbsDown, ThumbsUp, X } from 'lucide-react';
import {
  completeResource,
  createResource,
  fetchResources,
  fetchSkills,
  rateResource,
  type CreateResourceRequest,
  type ResourceDto,
  type ResourceType,
  type SkillListItem,
} from '@/api/client';

// ── Resource type badge ───────────────────────────────────────────────────────

const TYPE_COLORS: Record<ResourceType, string> = {
  Article: 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300',
  Video: 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300',
  Book: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
  Course: 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300',
  Documentation: 'bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300',
  Other: 'bg-muted text-muted-foreground',
};

const TYPE_ICONS: Record<ResourceType, string> = {
  Article: '📄',
  Video: '🎬',
  Book: '📚',
  Course: '🎓',
  Documentation: '📖',
  Other: '🔗',
};

// ── Resource card ─────────────────────────────────────────────────────────────

function ResourceCard({ resource }: { resource: ResourceDto }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const completeMutation = useMutation({
    mutationFn: () => completeResource(resource.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['resources'] }),
  });

  const rateMutation = useMutation({
    mutationFn: (isPositive: boolean) => rateResource(resource.id, isPositive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['resources'] }),
  });

  return (
    <div className="flex flex-col rounded-xl border bg-card p-5 shadow-sm transition-all hover:shadow-md">
      {/* Top row */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-start gap-2 min-w-0">
          <span className="text-xl shrink-0 mt-0.5">{TYPE_ICONS[resource.type]}</span>
          <div className="min-w-0">
            <a
              href={resource.url}
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center gap-1 font-semibold text-sm leading-tight hover:text-primary"
            >
              <span className="line-clamp-2">{resource.title}</span>
              <ExternalLink className="size-3 shrink-0 text-muted-foreground" />
            </a>
          </div>
        </div>
        <span
          className={`shrink-0 inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${TYPE_COLORS[resource.type]}`}
        >
          {resource.type}
        </span>
      </div>

      {/* Skill & niveau */}
      <div className="mt-3 flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
        <span className="font-medium text-foreground">{resource.skillName}</span>
        <span>·</span>
        <span>{t('resources.niveauRange', { from: resource.fromNiveau, to: resource.toNiveau })}</span>
      </div>

      <div className="flex-1" />

      {/* Footer: stats + actions */}
      <div className="mt-4 flex items-center justify-between gap-2 border-t pt-3">
        {/* Ratings */}
        <div className="flex items-center gap-3 text-xs text-muted-foreground">
          <button
            type="button"
            onClick={() => rateMutation.mutate(true)}
            disabled={rateMutation.isPending}
            className="flex items-center gap-1 hover:text-green-600 transition-colors disabled:opacity-50"
            aria-label={t('resources.ratePositive')}
          >
            <ThumbsUp className="size-3.5" />
            <span>{resource.positiveRatings}</span>
          </button>
          <button
            type="button"
            onClick={() => rateMutation.mutate(false)}
            disabled={rateMutation.isPending}
            className="flex items-center gap-1 hover:text-destructive transition-colors disabled:opacity-50"
            aria-label={t('resources.rateNegative')}
          >
            <ThumbsDown className="size-3.5" />
            <span>{resource.negativeRatings}</span>
          </button>
          <span>·</span>
          <span>{t('resources.completions', { count: resource.completionCount })}</span>
        </div>

        {/* Complete button */}
        <button
          type="button"
          onClick={() => completeMutation.mutate()}
          disabled={completeMutation.isPending}
          className={`flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-medium transition-colors disabled:opacity-50 ${
            completeMutation.isSuccess
              ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300'
              : 'bg-primary/10 text-primary hover:bg-primary/20'
          }`}
        >
          <CheckCircle2 className="size-3.5" />
          {completeMutation.isSuccess ? t('resources.completed') : t('resources.markComplete')}
        </button>
      </div>
    </div>
  );
}

// ── Add resource modal ────────────────────────────────────────────────────────

const RESOURCE_TYPES: ResourceType[] = ['Article', 'Video', 'Book', 'Course', 'Documentation', 'Other'];

function AddResourceModal({ skills, onClose }: { skills: SkillListItem[]; onClose: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<Partial<CreateResourceRequest>>({
    type: 'Article',
    fromNiveau: 1,
    toNiveau: 3,
  });
  const [error, setError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (req: CreateResourceRequest) => createResource(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['resources'] });
      onClose();
    },
    onError: () => setError(t('resources.createError')),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.title || !form.url || !form.skillId || !form.type) {
      setError(t('resources.requiredFields'));
      return;
    }
    createMutation.mutate(form as CreateResourceRequest);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-background/80 backdrop-blur-sm p-4">
      <div className="w-full max-w-md rounded-xl border bg-card p-6 shadow-lg">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">{t('resources.addResource')}</h2>
          <button type="button" onClick={onClose} className="rounded-full p-1 hover:bg-accent">
            <X className="size-4" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-sm font-medium">{t('resources.title')}</label>
            <input
              type="text"
              className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/40"
              value={form.title ?? ''}
              onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
              required
            />
          </div>

          <div>
            <label className="mb-1 block text-sm font-medium">{t('resources.url')}</label>
            <input
              type="url"
              className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/40"
              value={form.url ?? ''}
              onChange={(e) => setForm((f) => ({ ...f, url: e.target.value }))}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-sm font-medium">{t('resources.type')}</label>
              <select
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/40"
                value={form.type}
                onChange={(e) => setForm((f) => ({ ...f, type: e.target.value as ResourceType }))}
              >
                {RESOURCE_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {TYPE_ICONS[type]} {type}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium">{t('resources.skill')}</label>
              <select
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/40"
                value={form.skillId ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, skillId: Number(e.target.value) }))}
                required
              >
                <option value="">{t('resources.selectSkill')}</option>
                {skills.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-sm font-medium">{t('resources.fromNiveau')}</label>
              <input
                type="number"
                min={0}
                max={10}
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/40"
                value={form.fromNiveau ?? 1}
                onChange={(e) => setForm((f) => ({ ...f, fromNiveau: Number(e.target.value) }))}
              />
            </div>
            <div>
              <label className="mb-1 block text-sm font-medium">{t('resources.toNiveau')}</label>
              <input
                type="number"
                min={0}
                max={10}
                className="w-full rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-primary/40"
                value={form.toNiveau ?? 3}
                onChange={(e) => setForm((f) => ({ ...f, toNiveau: Number(e.target.value) }))}
              />
            </div>
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-accent"
            >
              {t('common.cancel')}
            </button>
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              {t('common.save')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function Resources() {
  const { t } = useTranslation();
  const [skillFilter, setSkillFilter] = useState<number | undefined>();
  const [typeFilter, setTypeFilter] = useState<ResourceType | undefined>();
  const [showModal, setShowModal] = useState(false);

  const { data: resources, isLoading } = useQuery({
    queryKey: ['resources', skillFilter, typeFilter],
    queryFn: () => fetchResources({ skillId: skillFilter, type: typeFilter }),
  });

  const { data: skills } = useQuery({
    queryKey: ['skills'],
    queryFn: () => fetchSkills(),
  });

  const hasFilters = skillFilter !== undefined || typeFilter !== undefined;

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('resources.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('resources.subtitle')}</p>
        </div>
        <button
          type="button"
          onClick={() => setShowModal(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 shrink-0 self-start"
        >
          <Plus className="size-4" />
          {t('resources.addResource')}
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <Filter className="size-4 text-muted-foreground shrink-0" />

        <select
          className="rounded-lg border bg-card px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-primary/40"
          value={skillFilter ?? ''}
          onChange={(e) => setSkillFilter(e.target.value ? Number(e.target.value) : undefined)}
        >
          <option value="">{t('resources.allSkills')}</option>
          {skills?.map((s) => (
            <option key={s.id} value={s.id}>
              {s.name}
            </option>
          ))}
        </select>

        <select
          className="rounded-lg border bg-card px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-primary/40"
          value={typeFilter ?? ''}
          onChange={(e) => setTypeFilter((e.target.value || undefined) as ResourceType | undefined)}
        >
          <option value="">{t('resources.allTypes')}</option>
          {RESOURCE_TYPES.map((type) => (
            <option key={type} value={type}>
              {TYPE_ICONS[type]} {type}
            </option>
          ))}
        </select>

        {hasFilters && (
          <button
            type="button"
            onClick={() => {
              setSkillFilter(undefined);
              setTypeFilter(undefined);
            }}
            className="flex items-center gap-1 rounded-lg border px-3 py-1.5 text-xs font-medium text-muted-foreground hover:bg-accent"
          >
            <X className="size-3" />
            {t('resources.clearFilters')}
          </button>
        )}

        <span className="ml-auto text-xs text-muted-foreground">
          {resources?.length ?? 0} {t('resources.count')}
        </span>
      </div>

      {/* Resource grid */}
      {resources && resources.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {resources.map((resource) => (
            <ResourceCard key={resource.id} resource={resource} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <Library className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('resources.noResources')}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t('resources.noResourcesHint')}</p>
        </div>
      )}

      {/* Modal */}
      {showModal && skills && <AddResourceModal skills={skills} onClose={() => setShowModal(false)} />}
    </div>
  );
}
