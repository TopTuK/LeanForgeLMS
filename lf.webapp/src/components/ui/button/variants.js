import { cva } from 'class-variance-authority';

export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0',
  {
    variants: {
      variant: {
        default: 'bg-accent-coral text-white hover:bg-accent-coral-dark',
        secondary: 'bg-surface-900 text-ink border border-border-subtle hover:bg-surface-800',
        outline: 'border border-border-subtle bg-transparent text-ink hover:bg-surface-900',
        ghost: 'text-ink-muted hover:text-ink hover:bg-surface-900',
        destructive: 'bg-destructive text-white hover:opacity-90',
        link: 'text-accent-coral underline-offset-4 hover:underline',
      },
      size: {
        default: 'h-10 px-4 py-2',
        sm: 'h-8 rounded-md px-3 text-xs',
        lg: 'h-11 rounded-md px-6',
        icon: 'size-9',
        pill: 'h-10 rounded-pill px-5',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
);
