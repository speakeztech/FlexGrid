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
        sans: ['Montserrat', 'system-ui', 'sans-serif'],
        heading: ['Nunito', 'system-ui', 'sans-serif'],
        mono: ['Fira Code', 'Consolas', 'Monaco', 'monospace'],
      },
      colors: {
        // SpeakEZ brand colors
        speakez: {
          teal: '#469c95',
          'teal-dark': '#007067',
          blue: '#0065b2',
          'blue-light': '#3b8ed0',
          orange: '#f58220',
          'orange-light': '#ff9a47',
          neutral: '#323232',
          'neutral-dark': '#1a1a1a',
          'neutral-light': '#eaeaea',
        },
      },
    },
  },
  plugins: [],
}
