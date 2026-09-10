import { describe, it, expect } from 'vitest';
import ProcessStep from '@/components/home/ProcessStep.vue';
import { renderComponent } from '@/test/renderComponent';

const props = {
  icon: 'lessons',
  index: '01',
  title: 'Work through chapters and lessons',
  description: 'Each course is split into chapters and lessons.',
};

describe('home/ProcessStep', () => {
  it('renders the step title, description and index', () => {
    const { getByRole, getByText } = renderComponent(ProcessStep, { props });

    expect(getByRole('heading', { name: 'Work through chapters and lessons' })).toBeInTheDocument();
    expect(getByText('Each course is split into chapters and lessons.')).toBeInTheDocument();
    expect(getByText('01')).toBeInTheDocument();
  });

  it.each(['lessons', 'progress', 'practice'])('renders without error for icon "%s"', (icon) => {
    const { getByRole } = renderComponent(ProcessStep, { props: { ...props, icon } });

    expect(getByRole('heading', { name: 'Work through chapters and lessons' })).toBeInTheDocument();
  });
});
