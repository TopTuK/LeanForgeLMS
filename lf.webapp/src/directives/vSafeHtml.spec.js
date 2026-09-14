import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/vue';
import { defineComponent } from 'vue';
import { vSafeHtml } from '@/directives/vSafeHtml';

const Host = defineComponent({
  name: 'SafeHtmlHost',
  directives: { safeHtml: vSafeHtml },
  props: { html: { type: String, default: '' } },
  template: '<div data-testid="target" v-safe-html="html" />',
});

describe('v-safe-html directive', () => {
  it('renders sanitized markup on mount', () => {
    const { getByTestId } = render(Host, {
      props: { html: '<p>hello</p><script>alert(1)</script>' },
    });
    const el = getByTestId('target');
    expect(el.innerHTML).toBe('<p>hello</p>');
    expect(el.querySelector('script')).toBeNull();
  });

  it('re-sanitizes when the bound value changes', async () => {
    const { getByTestId, rerender } = render(Host, { props: { html: '<p>one</p>' } });
    expect(getByTestId('target').innerHTML).toBe('<p>one</p>');

    await rerender({ html: '<p>two</p><img src=x onerror=alert(1)>' });

    expect(getByTestId('target').innerHTML).toContain('<p>two</p>');
    expect(getByTestId('target').innerHTML).not.toContain('onerror');
  });

  it('highlights sanitized code blocks without interpreting code as markup', () => {
    const { getByTestId } = render(Host, {
      props: {
        html: '<pre><code class="language-javascript">const value = "&lt;script&gt;";</code></pre>',
      },
    });
    const code = getByTestId('target').querySelector('code');

    expect(code).toHaveClass('language-javascript', 'hljs');
    expect(code.querySelector('.hljs-keyword')).toHaveTextContent('const');
    expect(code.querySelector('script')).toBeNull();
    expect(code.textContent).toContain('<script>');
  });
});
