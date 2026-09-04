import { describe, it, expect } from 'vitest';
import AudienceCard from '@/components/home/AudienceCard.vue';
import { renderComponent } from '@/test/renderComponent';

const props = {
  icon: 'pm',
  index: '01',
  title: 'Project managers',
  description: 'Plan, sequence and deliver initiatives.',
};

describe('home/AudienceCard', () => {
  it('renders the role title, description and index', () => {
    const { getByRole, getByText } = renderComponent(AudienceCard, { props });

    expect(getByRole('heading', { name: 'Project managers' })).toBeInTheDocument();
    expect(getByText('Plan, sequence and deliver initiatives.')).toBeInTheDocument();
    expect(getByText('01')).toBeInTheDocument();
  });

  it.each(['pm', 'product', 'team'])('renders without error for icon "%s"', (icon) => {
    const { getByRole } = renderComponent(AudienceCard, { props: { ...props, icon } });

    expect(getByRole('heading', { name: 'Project managers' })).toBeInTheDocument();
  });
});
