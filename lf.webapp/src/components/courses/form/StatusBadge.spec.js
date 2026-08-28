import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/vue';
import StatusBadge from '@/components/courses/form/StatusBadge.vue';

describe('StatusBadge', () => {
  it('renders the label text', () => {
    const { getByText } = render(StatusBadge, { props: { label: 'Published', variant: 'published' } });
    expect(getByText('Published')).toBeInTheDocument();
  });

  it('applies the variant modifier class', () => {
    const { getByText } = render(StatusBadge, { props: { label: 'Preview', variant: 'preview' } });
    expect(getByText('Preview')).toHaveClass('status-badge', 'status-badge--preview');
  });

  it('defaults to the draft variant', () => {
    const { getByText } = render(StatusBadge, { props: { label: 'Draft' } });
    expect(getByText('Draft')).toHaveClass('status-badge--draft');
  });
});
