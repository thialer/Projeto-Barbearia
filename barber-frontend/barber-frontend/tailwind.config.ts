import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        ink: "#211C18",
        cream: "#F7F4EE",
        brass: {
          DEFAULT: "#B4793F",
          dark: "#8A5A2B",
          light: "#E4C293",
        },
        steel: {
          DEFAULT: "#4B5563",
          light: "#8994A3",
        },
        confirmed: "#2F6D4F",
        cancelled: "#B3432B",
        completed: "#3B5A8A",
        noshow: "#8A6D3B",
      },
      fontFamily: {
        display: ["Fraunces", "Georgia", "serif"],
        sans: ["Inter", "system-ui", "sans-serif"],
      },
      borderRadius: {
        sm: "6px",
        md: "10px",
        lg: "16px",
      },
    },
  },
  plugins: [],
};

export default config;
