import { createFileRoute } from '@tanstack/react-router';
import { ResourceLibrary } from '@/pages/ResourceLibrary';

export const Route = createFileRoute('/_authenticated/resources')({ component: ResourceLibrary });
