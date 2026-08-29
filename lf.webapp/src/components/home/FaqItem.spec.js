import { describe, it, expect } from 'vitest';
import userEvent from '@testing-library/user-event';
import FaqItem from '@/components/home/FaqItem.vue';
import { renderComponent } from '@/test/renderComponent';

const props = {
  index: '01',
  question: 'Who is this for?',
  answer: 'Managers and analysts who want practice.',
};

describe('FaqItem', () => {
  it('starts collapsed and toggles open on click', async () => {
    const { getByRole, getByText } = renderComponent(FaqItem, { props });

    const trigger = getByRole('button', { name: /who is this for/i });
    expect(trigger).toHaveAttribute('aria-expanded', 'false');
    expect(getByText(props.answer)).not.toBeVisible();

    await userEvent.click(trigger);

    expect(trigger).toHaveAttribute('aria-expanded', 'true');
    expect(getByText(props.answer)).toBeVisible();
  });
});
