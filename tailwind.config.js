/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./output/**/*.{js,jsx}",
    "./src/**/*.fs"
  ],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        mono: ['Consolas', 'Monaco', 'Courier New', 'monospace'],
      },
    },
  },
  plugins: [],
}
