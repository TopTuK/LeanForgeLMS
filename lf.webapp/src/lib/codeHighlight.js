import { common, createLowlight } from 'lowlight';

export const CODE_LANGUAGES = [
  { value: '', label: 'Auto' },
  { value: 'plaintext', label: 'Plain text' },
  { value: 'csharp', label: 'C#' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'python', label: 'Python' },
  { value: 'java', label: 'Java' },
  { value: 'kotlin', label: 'Kotlin' },
  { value: 'cpp', label: 'C++' },
  { value: 'c', label: 'C' },
  { value: 'go', label: 'Go' },
  { value: 'rust', label: 'Rust' },
  { value: 'html', label: 'HTML / XML' },
  { value: 'css', label: 'CSS' },
  { value: 'json', label: 'JSON' },
  { value: 'sql', label: 'SQL' },
  { value: 'bash', label: 'Bash' },
  { value: 'yaml', label: 'YAML' },
  { value: 'markdown', label: 'Markdown' },
];

export const lowlight = createLowlight(common);

function hastNodeToDom(node) {
  if (node.type === 'text') {
    return document.createTextNode(node.value);
  }

  const element = document.createElement('span');
  const classNames = node.properties?.className;
  if (Array.isArray(classNames)) {
    element.className = classNames.join(' ');
  } else if (typeof classNames === 'string') {
    element.className = classNames;
  }
  element.replaceChildren(...(node.children ?? []).map(hastNodeToDom));
  return element;
}

export function highlightCodeBlocks(root) {
  for (const code of root.querySelectorAll('pre > code')) {
    const languageClass = [...code.classList].find((name) => name.startsWith('language-'));
    const language = languageClass?.slice('language-'.length) ?? '';
    const source = code.textContent ?? '';
    const tree = language && lowlight.registered(language)
      ? lowlight.highlight(language, source)
      : lowlight.highlightAuto(source);

    code.replaceChildren(...tree.children.map(hastNodeToDom));
    code.classList.add('hljs');
  }
}
