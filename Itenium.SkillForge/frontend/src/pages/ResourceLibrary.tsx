import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { BookOpen, ExternalLink, ThumbsDown, ThumbsUp } from 'lucide-react';
import { Button, Input } from '@itenium-forge/ui';
import { toast } from 'sonner';
import { fetchResources, markResourceComplete, rateResource, type ResourceResponse } from '@/api/client';
import { useAuthStore } from '@/stores';

const RESOURCE_TYPE_ICONS: Record<string, string> = {
  Article: '📄',
  Video: '🎥',
  Book: '📚',
  Documentation: '📖',
  Course: '🎓',
  Other: '🔗',
};

function ResourceCard({ resource }: { resource: ResourceResponse }) {
  const { t } = useTranslation();
  const { user } = useAuthStore();
  const queryClient = useQueryClient();

  const isCompleted = resource.completions.some((c) => c.userId === user?.id);
  const myRating = resource.ratings.find((r) => r.userId === user?.id);
  const positiveCount = resource.ratings.filter((r) => r.isPositive).length;
  const negativeCount = resource.ratings.filter((r) => !r.isPositive).length;

  const completeMutation = useMutation({
    mutationFn: () => markResourceComplete(resource.id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['resources'] }),
    onError: () => toast.error(t('common.error')),
  });

  const rateMutation = useMutation({
    mutationFn: (isPositive: boolean) => rateResource(resource.id, isPositive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['resources'] }),
    onError: () => toast.error(t('common.error')),
  });

  return (
    <div className={`rounded-xl border p-5 shadow-sm space-y-3 ${isCompleted ? 'border-green-300 bg-green-50/30 dark:border-green-700 dark:bg-green-900/10' : 'bg-card'}`}>
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-base">{RESOURCE_TYPE_ICONS[resource.type] || '🔗'}</span>
            <span className="text-xs font-medium text-muted-foreground uppercase">{resource.type}</span>
          </div>
          <h3 className="mt-1 font-semibold truncate">{resource.title}</h3>
          <p className="text-xs text-muted-foreground mt-0.5">
            {t('resources.niveauRange', { from: resource.fromNiveau, to: resource.toNiveau })}
          </p>
        </div>
        <a href={resource.url} target="_blank" rel="noreferrer" className="shrink-0 text-muted-foreground hover:text-primary">
          <ExternalLink className="size-4" />
        </a>
      </div>

      <div className="flex items-center justify-between">
        {/* Ratings */}
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => rateMutation.mutate(true)}
            className={`inline-flex items-center gap-1 text-xs rounded-full px-2 py-0.5 border transition-colors ${myRating?.isPositive ? 'bg-green-100 border-green-300 text-green-700 dark:bg-green-900/20 dark:border-green-700 dark:text-green-400' : 'hover:bg-muted'}`}
          >
            <ThumbsUp className="size-3" />
            {positiveCount}
          </button>
          <button
            type="button"
            onClick={() => rateMutation.mutate(false)}
            className={`inline-flex items-center gap-1 text-xs rounded-full px-2 py-0.5 border transition-colors ${myRating !== undefined && !myRating.isPositive ? 'bg-red-100 border-red-300 text-red-700 dark:bg-red-900/20 dark:border-red-700 dark:text-red-400' : 'hover:bg-muted'}`}
          >
            <ThumbsDown className="size-3" />
            {negativeCount}
          </button>
        </div>

        {/* Complete button */}
        <Button
          size="sm"
          variant={isCompleted ? 'default' : 'outline'}
          disabled={isCompleted || completeMutation.isPending}
          onClick={() => !isCompleted && completeMutation.mutate()}
          className="text-xs h-7"
        >
          {isCompleted ? t('resources.completed') : t('resources.markComplete')}
        </Button>
      </div>
    </div>
  );
}

export function ResourceLibrary() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const { data: resources, isLoading } = useQuery({
    queryKey: ['resources'],
    queryFn: () => fetchResources(),
  });

  const filtered = resources?.filter((r) =>
    r.title.toLowerCase().includes(search.toLowerCase()),
  ) ?? [];

  if (isLoading) return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold">{t('resources.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('resources.subtitle')}</p>
        </div>
      </div>

      <div className="flex gap-3">
        <Input
          placeholder={t('common.search')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-sm"
        />
      </div>

      {filtered.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((resource) => (
            <ResourceCard key={resource.id} resource={resource} />
          ))}
        </div>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-12 text-center">
          <BookOpen className="size-8 text-muted-foreground/50 mb-2" />
          <p className="text-sm text-muted-foreground">{t('resources.noResources')}</p>
        </div>
      )}
    </div>
  );
}
