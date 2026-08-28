import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/vue';
import userEvent from '@testing-library/user-event';
import FormField from '@/components/courses/form/FormField.vue';

describe('FormField', () => {
  it('associates the label with the control via a generated id', () => {
    const { getByLabelText } = render(FormField, { props: { label: 'Course title' } });
    const input = getByLabelText('Course title');
    expect(input.tagName).toBe('INPUT');
    expect(input).toHaveAttribute('id');
  });

  it('renders a textarea when type is "textarea"', () => {
    const { getByLabelText } = render(FormField, { props: { label: 'Summary', type: 'textarea' } });
    expect(getByLabelText('Summary').tagName).toBe('TEXTAREA');
  });

  it('emits update:modelValue when the user types', async () => {
    // The control is fully controlled (`:value="modelValue"` with no local state),
    // so assert on a single keystroke rather than an accumulated string.
    const { getByLabelText, emitted } = render(FormField, { props: { label: 'Title' } });
    await userEvent.type(getByLabelText('Title'), 'H');
    expect(emitted()['update:modelValue']).toEqual([['H']]);
  });

  it('reflects the disabled prop on the control', () => {
    const { getByLabelText } = render(FormField, { props: { label: 'Title', disabled: true } });
    expect(getByLabelText('Title')).toBeDisabled();
  });
});
