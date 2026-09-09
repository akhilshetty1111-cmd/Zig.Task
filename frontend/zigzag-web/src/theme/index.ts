import { createTheme } from '@mui/material/styles';

/**
 * ZigZag design tokens.
 *
 * Defined once here rather than as `sx` overrides scattered across components,
 * so a brand change is a one-file edit and every surface stays consistent.
 */
export const zigzagTheme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#4338ca', light: '#6366f1', dark: '#3730a3' },
    secondary: { main: '#0891b2' },
    success: { main: '#15803d' },
    warning: { main: '#b45309' },
    error: { main: '#b91c1c' },
    background: { default: '#f7f7fb', paper: '#ffffff' },
  },

  shape: { borderRadius: 10 },

  typography: {
    fontFamily: [
      'Inter',
      '-apple-system',
      'Segoe UI',
      'Roboto',
      'Helvetica Neue',
      'Arial',
      'sans-serif',
    ].join(','),
    h1: { fontSize: '1.75rem', fontWeight: 700 },
    h2: { fontSize: '1.5rem', fontWeight: 700 },
    h3: { fontSize: '1.25rem', fontWeight: 600 },
    button: { textTransform: 'none', fontWeight: 600 },
  },

  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
    },
    MuiPaper: {
      styleOverrides: {
        // Flat, bordered surfaces read as a modern SaaS app; MUI's default
        // stacked shadows look dated on dense board/table layouts.
        root: { backgroundImage: 'none' },
      },
    },
  },
});
