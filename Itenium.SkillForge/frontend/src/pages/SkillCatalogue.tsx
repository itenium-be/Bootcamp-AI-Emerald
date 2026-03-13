import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from '@tanstack/react-router';
import { Search, ChevronRight, CheckCircle2, BookOpen } from 'lucide-react';
import { fetchSkills } from '@/api/client';

const CATEGORY_COLORS: Record<string, string> = {
  'Language & Runtime': 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300',
  'Web & API': 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300',
  'Data & Persistence': 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
  Testing: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300',
  'Architecture & Design': 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300',
  'Tooling & DevOps': 'bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300',
};

const defaultCategoryColor = 'bg-muted text-muted-foreground';

function categoryColor(name: string) {
  return CATEGORY_COLORS[name] ?? defaultCategoryColor;
}

export function SkillCatalogue() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [activeCategory, setActiveCategory] = useState<string | null>(null);

  const { data: skills, isLoading } = useQuery({
    queryKey: ['skills'],
    queryFn: () => fetchSkills(),
  });

  const categories = useMemo(() => [...new Set((skills ?? []).map((s) => s.categoryName))].sort(), [skills]);

  const filtered = useMemo(() => {
    const q = search.toLowerCase();
    return (skills ?? []).filter(
      (s) =>
        (!q || s.name.toLowerCase().includes(q) || (s.description ?? '').toLowerCase().includes(q)) &&
        (!activeCategory || s.categoryName === activeCategory),
    );
  }, [skills, search, activeCategory]);

  if (isLoading) {
    return <div className="p-6 text-muted-foreground">{t('common.loading')}</div>;
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold">{t('skills.title')}</h1>
          <p className="mt-1 text-sm text-muted-foreground">{t('skills.subtitle')}</p>
        </div>

        {/* Search */}
        <div className="relative w-full sm:w-72">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
          <input
            type="text"
            placeholder={t('skills.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full rounded-md border bg-background py-2 pl-9 pr-3 text-sm outline-none ring-offset-background focus-visible:ring-2 focus-visible:ring-ring"
          />
        </div>
      </div>

      {/* Category tabs */}
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={() => setActiveCategory(null)}
          className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
            activeCategory === null
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground hover:bg-accent hover:text-accent-foreground'
          }`}
        >
          {t('skills.allCategories')}
        </button>
        {categories.map((cat) => (
          <button
            key={cat}
            type="button"
            onClick={() => setActiveCategory(cat)}
            className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
              activeCategory === cat
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-accent hover:text-accent-foreground'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Skills grid */}
      {filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center rounded-xl border border-dashed py-20 text-center">
          <BookOpen className="size-10 text-muted-foreground/50 mb-3" />
          <p className="text-lg font-medium">{t('skills.noSkills')}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t('skills.noSkillsHint')}</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((skill) => (
            <Link
              key={skill.id}
              to="/skills/$skillId"
              params={{ skillId: String(skill.id) }}
              className="group flex flex-col rounded-xl border bg-card p-5 shadow-sm transition-all hover:shadow-md hover:border-primary/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {/* Category badge */}
              <span
                className={`inline-flex w-fit items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${categoryColor(skill.categoryName)}`}
              >
                {skill.categoryName}
              </span>

              {/* Skill name */}
              <h2 className="mt-3 text-lg font-semibold leading-tight group-hover:text-primary transition-colors">
                {skill.name}
              </h2>

              {/* Description */}
              {skill.description && (
                <p className="mt-1.5 text-sm text-muted-foreground line-clamp-2">{skill.description}</p>
              )}

              {/* Level indicator */}
              <div className="mt-4 flex items-center justify-between">
                {skill.levelCount === 1 ? (
                  <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
                    <CheckCircle2 className="size-3.5 text-green-500" />
                    {t('skills.checkboxSkill')}
                  </span>
                ) : (
                  <div className="flex items-center gap-2">
                    <div className="flex gap-0.5">
                      {Array.from({ length: skill.levelCount }, (_, i) => (
                        <span key={i} className="h-1.5 w-4 rounded-full bg-primary/30" />
                      ))}
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {skill.levelCount} {t('skills.levels')}
                    </span>
                  </div>
                )}

                <ChevronRight className="size-4 text-muted-foreground/50 group-hover:text-primary transition-colors" />
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
