import { describe, it, expect } from 'vitest';
import CourseCard from '@/components/home/CourseCard.vue';
import { renderComponent } from '@/test/renderComponent';

const props = {
  icon: 'llm',
  index: '01',
  title: 'Introduction to LLM',
  description: 'Build and use AI agents in real workflows.',
  duration: 'New program',
  category: 'AI & Agents',
  cta: 'Learn more',
  to: { name: 'Login' },
};

describe('home/CourseCard', () => {
  it('renders program metadata and heading', () => {
    const { getByRole, getByText } = renderComponent(CourseCard, { props });

    expect(getByRole('heading', { name: 'Introduction to LLM' })).toBeInTheDocument();
    expect(getByText('Build and use AI agents in real workflows.')).toBeInTheDocument();
    expect(getByText('AI & Agents')).toBeInTheDocument();
    expect(getByText('New program')).toBeInTheDocument();
    expect(getByText('01')).toBeInTheDocument();
  });

  it('omits the CTA when no target is provided', () => {
    const { queryByText } = renderComponent(CourseCard, {
      props: { ...props, to: null },
    });

    expect(queryByText('Learn more')).not.toBeInTheDocument();
  });
});
