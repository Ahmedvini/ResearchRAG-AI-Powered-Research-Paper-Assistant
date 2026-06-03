/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ink: '#17201f',
        panel: '#f7f8f5',
        line: '#d9ded6',
        moss: '#3d6b58',
        rust: '#a6502f',
        gold: '#b5872f'
      }
    }
  },
  plugins: []
};

