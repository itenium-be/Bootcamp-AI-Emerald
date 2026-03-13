const CATEGORY_COLORS: Record<string, string> = {
  'Language & Runtime': 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300',
  'Web & API': 'bg-purple-100 text-purple-800 dark:bg-purple-900/40 dark:text-purple-300',
  'Data & Persistence': 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
  Testing: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300',
  'Architecture & Design': 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300',
  'Tooling & DevOps': 'bg-teal-100 text-teal-800 dark:bg-teal-900/40 dark:text-teal-300',
};

const defaultCategoryColor = 'bg-muted text-muted-foreground';

export function categoryColor(name: string): string {
  return CATEGORY_COLORS[name] ?? defaultCategoryColor;
}
